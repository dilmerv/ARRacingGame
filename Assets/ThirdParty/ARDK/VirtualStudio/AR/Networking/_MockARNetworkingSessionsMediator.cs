// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR.Networking.Mock;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Networking
{
  internal sealed class _MockARNetworkingSessionsMediator:
    _IEditorARNetworkingSessionMediator
  {
    private readonly Dictionary<Guid, _MockARNetworking> _stageIdentifierToSession =
      new Dictionary<Guid, _MockARNetworking>();

    private _IVirtualStudioManager _virtualStudioManager;

    public _MockARNetworkingSessionsMediator(_IVirtualStudioManager virtualStudioMaster)
    {
      _virtualStudioManager = virtualStudioMaster;

      ARNetworkingFactory.ARNetworkingInitialized += HandleAnyInitialized;
      ARNetworkingFactory.NonLocalARNetworkingInitialized += HandleAnyInitialized;
    }

    ~_MockARNetworkingSessionsMediator()
    {
      ARLog._Error("_MockARNetworkingSessionsMediator was not correctly disposed.");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      foreach (var session in _stageIdentifierToSession.Values.ToArray())
        session.Dispose();

      ARNetworkingFactory.ARNetworkingInitialized -= HandleAnyInitialized;
      ARNetworkingFactory.NonLocalARNetworkingInitialized -= HandleAnyInitialized;
    }

    public _MockARNetworking CreateNonLocalSession(Guid stageIdentifier)
    {
      var networking = _virtualStudioManager.MultipeerMediator.GetSession(stageIdentifier);
      var arSession = _virtualStudioManager.ArSessionMediator.GetSession(stageIdentifier);

      var arNetworking =
        ARNetworkingFactory._CreateVirtualStudioManagedARNetworking
        (
          arSession,
          networking,
          _virtualStudioManager,
          isLocal: false
        );

      return (_MockARNetworking) arNetworking;
    }

    public _MockARNetworking GetSession(Guid stageIdentifier)
    {
      return HasSession(stageIdentifier) ? _stageIdentifierToSession[stageIdentifier] : null;
    }

    public IReadOnlyCollection<_MockARNetworking> GetConnectedSessions(Guid stageIdentifier)
    {
      var connectedARNetworkings = new HashSet<_MockARNetworking>();
      var readonlyARNetworkings = connectedARNetworkings.AsArdkReadOnly();

      if (GetSession(stageIdentifier).Networking.IsConnected)
      {
        var connectedNetworkings =
          _virtualStudioManager.MultipeerMediator.GetConnectedSessions(stageIdentifier);

        if (connectedNetworkings != null)
          foreach (var connectedNetworking in connectedNetworkings)
            connectedARNetworkings.Add(GetSession(connectedNetworking.StageIdentifier));
      }

      return readonlyARNetworkings;
    }

    public bool HasSession(Guid stageIdentifier)
    {
      return _stageIdentifierToSession.ContainsKey(stageIdentifier);
    }

    private void HandleAnyInitialized(AnyARNetworkingInitializedArgs args)
    {
      var mockARNetworking = args.ARNetworking as _MockARNetworking;
      if (mockARNetworking == null)
      {
        ARLog._Error
        (
          "While VirtualStudio is running mock AR networks, only other mock ARNetworking " +
          "instances can be initialized."
        );
        return;
      }

      var stageIdentifier = mockARNetworking.ARSession.StageIdentifier;
      mockARNetworking.Deinitialized += (_) => _stageIdentifierToSession.Remove(stageIdentifier);
      _stageIdentifierToSession.Add(stageIdentifier, mockARNetworking);
    }
  }
}
