// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR
{
  internal sealed class _MockARSessionMediator:
    _IEditorARSessionMediator
  {
    private readonly Dictionary<Guid, IARSession> _stageIdentifierToSession =
      new Dictionary<Guid, IARSession>();

    private _IVirtualStudioManager _virtualStudioManager;

    public _MockARSessionMediator(_IVirtualStudioManager virtualStudioMaster)
    {
      _virtualStudioManager = virtualStudioMaster;

      ARSessionFactory.SessionInitialized += HandleAnyInitialized;
      ARSessionFactory._NonLocalSessionInitialized += HandleAnyInitialized;
    }

    ~_MockARSessionMediator()
    {
      // This class has no unmanaged data, so no need to have a Dispose(false) call.
      Debug.LogError("EditorARSessionMediator should be destroyed by calling Dispose().");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      foreach (var session in _stageIdentifierToSession.Values.ToArray())
        session.Dispose();

      ARSessionFactory.SessionInitialized -= HandleAnyInitialized;
      ARSessionFactory._NonLocalSessionInitialized -= HandleAnyInitialized;
    }

    public IARSession CreateNonLocalSession(Guid stageIdentifier, RuntimeEnvironment runtimeEnvironment)
    {
      return
        ARSessionFactory._CreateVirtualStudioManagedARSession
        (
          runtimeEnvironment,
          stageIdentifier,
          isLocal: false,
          _virtualStudioManager
        );
    }

    public IARSession GetSession(Guid stageIdentifier)
    {
      IARSession session;
      if (_stageIdentifierToSession.TryGetValue(stageIdentifier, out session))
        return session;

      return null;
    }

    private void HandleAnyInitialized(AnyARSessionInitializedArgs args)
    {
      var arSession = args.Session;

      var stageIdentifier = arSession.StageIdentifier;
      if (stageIdentifier == Guid.Empty)
        throw new InvalidOperationException("A Session StageIdentifier can't be empty.");

      _stageIdentifierToSession.Add(stageIdentifier, arSession);

      arSession.Deinitialized +=
        (_) => _stageIdentifierToSession.Remove(stageIdentifier);
    }
  }
}