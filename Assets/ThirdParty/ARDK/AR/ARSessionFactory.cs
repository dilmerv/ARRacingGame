// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR
{
  /// Class used to create ARSessions and also to be notified when new ARSessions are created.
  public static class ARSessionFactory
  {
    /// Create an ARSession appropriate for the current device.
    ///
    /// On a mobile device, the attempted order will be LiveDevice, Remote, and finally Mock.
    /// In the Unity Editor, the attempted order will be Remote, then Mock.
    ///
    /// @param stageIdentifier
    ///   The identifier used by the C++ library to connect all related components.
    ///
    /// @returns The created session, or throws if it was not possible to create a session.
    public static IARSession Create(Guid stageIdentifier = default(Guid))
    {
      return _Create(null, stageIdentifier);
    }

    /// Create an ARSession with the specified RuntimeEnvironment.
    ///
    /// @param env
    ///   The env used to create the session for.
    /// @param stageIdentifier
    ///   The identifier used by the C++ library to connect all related components.
    ///
    /// @returns The created session, or null if it was not possible to create a session.
    public static IARSession Create(RuntimeEnvironment env, Guid stageIdentifier = default(Guid))
    {
      if (stageIdentifier == default(Guid))
        stageIdentifier = Guid.NewGuid();

      IARSession result;
      switch (env)
      {
        case RuntimeEnvironment.Default:
          // Return early here or else _InvokeSessionInitialized will get called twice
          return Create(stageIdentifier);

        case RuntimeEnvironment.LiveDevice:
  #pragma warning disable CS0162
          if (NativeAccess.Mode != NativeAccess.ModeType.Native && NativeAccess.Mode != NativeAccess.ModeType.Testing)
            return null;
  #pragma warning restore CS0162

          if (_activeSession != null)
            throw new InvalidOperationException("There's another session still active.");

          result = new _NativeARSession(stageIdentifier);
          break;

        case RuntimeEnvironment.Remote:
          if (!_RemoteConnection.IsEnabled)
            return null;

          result = new _RemoteEditorARSession(stageIdentifier);
          break;

        case RuntimeEnvironment.Mock:
          result = new _MockARSession(stageIdentifier, _VirtualStudioManager.Instance);
          break;

        case RuntimeEnvironment.Playback:
          if (_activeSession != null)
            throw new InvalidOperationException("There's another session still active.");
          // Enable playback
          result = new _NativeARSession(stageIdentifier, true);
          break;

        default:
          throw new InvalidEnumArgumentException(nameof(env), (int)env, env.GetType());
      }

      _InvokeSessionInitialized(result, isLocal: true);
      return result;
    }

    ///Create an AR Playback Session.
    ///
    /// @param stageIdentifier
    ///   The identifier used by the C++ library to connect all related components.
    ///
    /// @returns The created session, or throws if it was not possible to create a session.
    /// @note this is an experimental feature
    public static IARSession CreatePlaybackSession(Guid stageIdentifier = default(Guid))
    {
      if (stageIdentifier == default(Guid))
        stageIdentifier = Guid.NewGuid();

#pragma warning disable CS0162
      if (NativeAccess.Mode != NativeAccess.ModeType.Native && NativeAccess.Mode != NativeAccess.ModeType.Testing)
        return null;
#pragma warning disable CS0162

      if (_activeSession != null)
        throw new InvalidOperationException("There's another session still active.");

      // Enable playback
      IARSession result = new _NativeARSession(stageIdentifier, true);

      _InvokeSessionInitialized(result, isLocal: true);
      return result;
    }

    private static ArdkEventHandler<AnyARSessionInitializedArgs> _sessionInitialized;

    /// Event invoked when a new session is created and initialized.
    public static event ArdkEventHandler<AnyARSessionInitializedArgs> SessionInitialized
    {
      add
      {
        _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _sessionInitialized);

        _sessionInitialized += value;

        IARSession activeSession;
        lock (_activeSessionLock)
          activeSession = _activeSession;

        if (activeSession != null)
        {
          var args = new AnyARSessionInitializedArgs(activeSession, isLocal: true);
          value(args);
        }
      }
      remove
      {
        _sessionInitialized -= value;
      }
    }

    internal static readonly RuntimeEnvironment[] _defaultBestMatches =
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
      new RuntimeEnvironment[] { RuntimeEnvironment.LiveDevice, RuntimeEnvironment.Remote, RuntimeEnvironment.Mock };
#else
      new RuntimeEnvironment[]
      {
        RuntimeEnvironment.Remote, RuntimeEnvironment.Mock
      };
#endif

    /// Tries to create an ARSession of any of the given envs.
    ///
    /// @param envs
    ///   A collection of runtime environments used to create the session for. As not all platforms
    ///   support all environments, the code will try to create the session for the first
    ///   environment, then for the second and so on. If envs is null or empty, then the order used
    ///   is LiveDevice, Remote and finally Mock.
    /// @param stageIdentifier
    ///   The identifier used by the C++ library to connect all related components.
    ///
    /// @returns The created session, or null if it was not possible to create a session.
    internal static IARSession _Create
    (
      IEnumerable<RuntimeEnvironment> envs = null,
      Guid stageIdentifier = default(Guid)
    )
    {
      bool triedAtLeast1 = false;

      if (envs != null)
      {
        foreach (var env in envs)
        {
          var possibleResult = Create(env, stageIdentifier);
          if (possibleResult != null)
            return possibleResult;

          triedAtLeast1 = true;
        }
      }

      if (!triedAtLeast1)
        return _Create(_defaultBestMatches, stageIdentifier);

      throw new NotSupportedException("None of the provided envs are supported by this build.");
    }

    internal static IARSession _CreateVirtualStudioManagedARSession
    (
      RuntimeEnvironment env,
      Guid stageIdentifier,
      bool isLocal,
      _IVirtualStudioManager virtualStudioManager
    )
    {
      IARSession result;

      switch (env)
      {
        case RuntimeEnvironment.Mock:
        {
          if (virtualStudioManager == null)
            virtualStudioManager = _VirtualStudioManager.Instance;

          result = new _MockARSession(stageIdentifier, virtualStudioManager);
          break;
        }

        case RuntimeEnvironment.Remote:
          result = new _RemoteEditorARSession(stageIdentifier);
          break;

        default:
          throw new InvalidEnumArgumentException(nameof(env), (int)env, typeof(RuntimeEnvironment));
      }

      ARLog._DebugFormat
      (
        "Created IARSession with env: {0} and stage identifier: {1}",
        false,
        env,
        stageIdentifier
      );

      _InvokeSessionInitialized(result, isLocal);
      return result;
    }

    private static object _activeSessionLock = new object();
    private static IARSession _activeSession;

    // As there is no ConcurrentHashSet at the moment, we use a ConcurrentDictionary and only
    // care about the key, ignoring the value.
    private static ConcurrentDictionary<IARSession, bool> _nonLocalSessions =
      new ConcurrentDictionary<IARSession, bool>(_ReferenceComparer<IARSession>.Instance);

    private static void _InvokeSessionInitialized(IARSession session, bool isLocal)
    {
      var handler = isLocal ? _sessionInitialized : _nonLocalSessionInitialized;
      if (handler != null)
      {
        var args = new AnyARSessionInitializedArgs(session, isLocal);
        handler(args);
      }

      if (isLocal)
      {
        _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _activeSession);

        lock (_activeSessionLock)
        {
          if (_activeSession != null)
            throw new InvalidOperationException("There's another session still active.");

          _activeSession = session;
        }

        session.Deinitialized +=
          (_) =>
          {
            lock (_activeSessionLock)
              if (_activeSession == session)
                _activeSession = null;
          };
      }
      else
      {
        _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _nonLocalSessions);

        if (!_nonLocalSessions.TryAdd(session, true))
          throw new InvalidOperationException("Duplicated session.");

        session.Deinitialized += (ignored) => _nonLocalSessions.TryRemove(session, out _);
      }
    }

    private static ArdkEventHandler<AnyARSessionInitializedArgs> _nonLocalSessionInitialized;

    internal static event ArdkEventHandler<AnyARSessionInitializedArgs> _NonLocalSessionInitialized
    {
      add
      {
        _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _nonLocalSessionInitialized);

        _nonLocalSessionInitialized += value;

        // Doing a foreach on a ConcurrentDictionary is safe even if values keep being added or
        // removed during the iteration. Also, it is lock free, so no chance of dead-locks.
        foreach (var session in _nonLocalSessions.Keys)
        {
          var args = new AnyARSessionInitializedArgs(session, isLocal: false);
          value(args);
        }
      }
      remove
      {
        _nonLocalSessionInitialized -= value;
      }
    }
  }
}
