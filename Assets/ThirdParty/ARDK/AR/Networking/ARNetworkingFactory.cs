// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.VirtualStudio.AR.Networking.Mock;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.VirtualStudio.Networking.Mock;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

/// @namespace Niantic.ARDK.AR.Networking
/// @brief Handles AR Networking sessions
namespace Niantic.ARDK.AR.Networking
{
  /// A static Factory class to create ARNetworking instances, as well as to be notified when
  /// new ARNetworking instances are created.
  public static class ARNetworkingFactory
  {
    private static readonly object _arNetworkingLock = new object();
    private static IARNetworking _arNetworking;

    /// Create an ARNetworking appropriate for the current device.
    ///
    /// On a mobile device, the attempted order will be LiveDevice, Remote, and finally Mock.
    /// In the Unity Editor, the attempted order will be Remote, then Mock.
    ///
    /// This will also create an ARSession object. If an ARSession object has already been created,
    ///   use the ARNetworkingFactory.Create(IARSession session) API instead.
    ///
    /// @param stageIdentifier
    ///   The identifier used by the C++ library to connect all related components.
    ///
    /// @returns The created ARNetworking, or throws if it was not possible to create a session.
    public static IARNetworking Create()
    {
      return _Create(null);
    }

    /// Create an ARNetworking with the specified RuntimeEnvironment.
    ///
    /// This will also create an ARSession object. If an ARSession object has already been created,
    ///   use the ARNetworkingFactory.Create(IARSession session) API instead.
    ///
    /// @param env
    ///   The env used to create the ARNetworking for.
    ///
    /// @returns The created ARNetworking, or throws if it was not possible to create a session.
    public static IARNetworking Create(RuntimeEnvironment env)
    {
      return _Create(new List<RuntimeEnvironment>(){env});
    }

    /// Creates a new ARNetworking for the given session.
    ///
    /// @param session - The session to create a new ARNetworking for.
    /// @returns The newly created ARNetworking.
    public static IARNetworking Create(IARSession session)
    {
      var arNetworking = Create(session, ServerConfiguration.ARBE);
      if (arNetworking == null)
        throw new NotSupportedException("Could not create an ARNetworking with the provided ARSession");

      return arNetworking;
    }

    /// Creates a new ARNetworking for the given session.
    ///
    /// @param session - The session to create a new ARNetworking for.
    /// @param configuration - The configuration that tells how to connect to a server.
    ///
    /// @returns The newly created ARNetworking.
    public static IARNetworking Create(IARSession session, ServerConfiguration configuration)
    {
      if (session == null)
        throw new ArgumentNullException(nameof(session));

      var env = session.RuntimeEnvironment;

      var networking = MultipeerNetworkingFactory.Create(env, configuration, session.StageIdentifier);
      return Create(session, networking);
    }

    /// Creates a new ARNetworking for the given session and multipeer networking.
    ///
    /// @param session - The session to create a new ARNetworking for.
    /// @param networking - An existing multipeer networking.
    ///
    /// @returns The newly created ARNetworking.
    public static IARNetworking Create(IARSession session, IMultipeerNetworking networking)
    {
      if (session == null)
        throw new ArgumentNullException(nameof(session));

      if (networking == null)
        throw new ArgumentNullException(nameof(networking));

      var env = session.RuntimeEnvironment;
      if (env != networking.RuntimeEnvironment)
        throw new ArgumentException("session and networking must have the same RuntimeEnvironment.");

      IARNetworking result;
      switch (env)
      {
        case RuntimeEnvironment.LiveDevice:
          result = new _NativeARNetworking(session, networking);
          _InvokeNetworkingInitialized(result, isLocal: true);
          break;

        case RuntimeEnvironment.Remote:
          result =  new _RemoteEditorARNetworking(session, networking);
          _InvokeNetworkingInitialized(result, isLocal: true);
          break;

        case RuntimeEnvironment.Mock:
          result =
            _CreateVirtualStudioManagedARNetworking
            (
              session,
              networking,
              _VirtualStudioManager.Instance,
              isLocal: true
            );

          break;

        default:
          throw new InvalidEnumArgumentException("session.RuntimeEnvironment", (int)env, env.GetType());
      }

      return result;
    }

    private static ArdkEventHandler<AnyARNetworkingInitializedArgs> _arNetworkingInitialized;

    /// Event called when a new AR Networking object is initialized.
    public static event ArdkEventHandler<AnyARNetworkingInitializedArgs> ARNetworkingInitialized
    {
      add
      {
        _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _arNetworkingInitialized);

        _arNetworkingInitialized += value;

        // Safe to read with no lock.
        var arNetworking = _arNetworking;
        if (arNetworking != null)
        {
          ARLog._Debug("Calling new ARNetworkingInitialized callback to catch up");

          var args = new AnyARNetworkingInitializedArgs(arNetworking);
          value(args);
        }
      }
      remove
      {
        _arNetworkingInitialized -= value;
      }
    }

    /// Tries to create an ARNetworking of any of the given envs.
    ///
    /// @param envs
    ///   A collection of envs used to create the networking for. As not all platforms support
    ///   all envs, the code will try to create the networking for the first env, then for the
    ///   second and so on. If envs is null or empty, then the order used is LiveDevice,
    ///   Remote and finally Mock.
    ///
    /// @returns The created networking, or null if it was not possible to create the object.
    internal static IARNetworking _Create(IEnumerable<RuntimeEnvironment> envs = null)
    {
      IARSession session;
      try
      {
        session = ARSessionFactory._Create(envs);
      }
      catch (InvalidOperationException)
      {
        var errorMessage =
          "The ARNetworkingFactory is trying to create an ARSession when one already exists, " +
          "pass the existing ARSession into ARNetworkingFactory.Create(IARSession session)";

        throw new InvalidOperationException(errorMessage);
      }

      if (session == null)
        throw new NotSupportedException("None of the provided runtime environments are supported by this build.");

      return Create(session);
    }

    internal static IARNetworking _CreateVirtualStudioManagedARNetworking
    (
      IARSession arSession,
      IMultipeerNetworking networking,
      _IVirtualStudioManager virtualStudioManager,
      bool isLocal
    )
    {
      var env = arSession.RuntimeEnvironment;
      if (env != networking.RuntimeEnvironment)
        throw new ArgumentException("arSession and networking must have the same RuntimeEnvironment.");

      IARNetworking result;

      ARLog._DebugFormat
      (
        "Making {0} ARNetworking",
        objs: env
      );

      switch (env)
      {
        case RuntimeEnvironment.Mock:
          result = new _MockARNetworking(arSession, networking, virtualStudioManager);
          break;

        case RuntimeEnvironment.Remote:
          result = new _RemoteEditorARNetworking(arSession, networking);
          break;

        default:
          throw new InvalidOperationException("Invalid or unknown RuntimeEnvironment: " + env);
      }

      _InvokeNetworkingInitialized(result, isLocal);
      return result;
    }

    // As there is no ConcurrentHashSet, we use a ConcurrentDictionary and ignore the value.
    private static readonly ConcurrentDictionary<IARNetworking, bool> _nonLocalARNetworkings =
      new ConcurrentDictionary<IARNetworking, bool>(_ReferenceComparer<IARNetworking>.Instance);

    private static ArdkEventHandler<AnyARNetworkingInitializedArgs>
      _nonLocalARNetworkingInitialized;

    internal static event
      ArdkEventHandler<AnyARNetworkingInitializedArgs> NonLocalARNetworkingInitialized
    {
      add
      {
        _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _nonLocalARNetworkingInitialized);

        _nonLocalARNetworkingInitialized += value;

        // It is safe to iterate a ConcurrentDictionary even if items are added or removed during
        // the iteration.
        foreach (var arNetworking in _nonLocalARNetworkings.Keys)
        {
          ARLog._Debug("Calling new NonLocalARNetworkingInitialized callback to catch up");

          var args = new AnyARNetworkingInitializedArgs(arNetworking);
          value(args);
        }
      }
      remove
      {
        _nonLocalARNetworkingInitialized -= value;
      }
    }

    private static void _InvokeNetworkingInitialized(IARNetworking arNetworking, bool isLocal)
    {
      ArdkEventHandler<AnyARNetworkingInitializedArgs> handler;
      if (isLocal)
      {
        ARLog._Debug("Initializing a local ARNetworking");

        _StaticMemberValidator._FieldIsNullWhenScopeEnds(() => _arNetworking);

        lock (_arNetworkingLock)
        {
          if (_arNetworking != null)
            throw new InvalidOperationException("There's another ARNetworking still active.");

          _arNetworking = arNetworking;
        }

        handler = _arNetworkingInitialized;

        arNetworking.Deinitialized +=
          (_) =>
          {
            lock (_arNetworkingLock)
              if (_arNetworking == arNetworking)
                _arNetworking = null;
          };
      }
      else
      {
        ARLog._Debug("Initializing a non-local ARNetworking");

        _StaticMemberValidator._CollectionIsEmptyWhenScopeEnds(() => _nonLocalARNetworkings);

        if (!_nonLocalARNetworkings.TryAdd(arNetworking, true))
          throw new InvalidOperationException("Duplicated ARNetworking.");

        handler = _nonLocalARNetworkingInitialized;

        arNetworking.Deinitialized +=
          (ignored) => _nonLocalARNetworkings.TryRemove(arNetworking, out _);

      }

      if (handler != null)
      {
        var args = new AnyARNetworkingInitializedArgs(arNetworking);
        handler(args);
      }
    }
  }
}
