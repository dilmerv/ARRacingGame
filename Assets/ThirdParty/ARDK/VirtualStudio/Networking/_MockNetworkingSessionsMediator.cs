// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Niantic.ARDK.AR;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Networking.Mock;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Networking
{
  internal sealed class _MockNetworkingSessionsMediator:
    _IEditorMultipeerSessionMediator
  {
    private readonly Dictionary<Guid, _MockMultipeerNetworking> _stageIdentifierToSession =
      new Dictionary<Guid, _MockMultipeerNetworking>();

    // StageIdentifier --> SessionMetadata
    // Since the _EditorMultipeerNetworking.JoinedSessionMetadata will be null when the disconnected
    // event is raised, but this mediator needs to know which session was left
    private readonly Dictionary<Guid, byte[]> _stageIdentifierToMetadata =
      new Dictionary<Guid, byte[]>();

    // StageIdentifier --> PeerIdentifier
    // Need to keep track of mapping because IMultipeerNetworking.Self can be null
    // when the disconnected event is raised, but this mediator needs to know which peer
    // it was that left
    private readonly Dictionary<Guid, Guid> _stageToPeerIdentifier =  new Dictionary<Guid, Guid>();

    // SessionMetadata --> Collection of all sessions connected with that metadata
    private readonly Dictionary<byte[], HashSet<_MockMultipeerNetworking>> _metadataToSession =
      new Dictionary<byte[], HashSet<_MockMultipeerNetworking>>(_ByteArrayComparer.Instance);

    private sealed class _MultipeerHelper
    {
      private readonly _MockNetworkingSessionsMediator _owner;
      private readonly _MockMultipeerNetworking _networking;

      internal _MultipeerHelper
      (
        _MockNetworkingSessionsMediator owner,
        _MockMultipeerNetworking networking
      )
      {
        _owner = owner;
        _networking = networking;

        networking.Connected += _Connected;
        networking.Disconnected += _Disconnected;
        networking.Deinitialized += Deinitialized;
      }

      internal void _Dispose()
      {
        _networking.Connected -= _Connected;
        _networking.Disconnected -= _Disconnected;
        _networking.Deinitialized -= Deinitialized;
      }

      private void _Connected(ConnectedArgs args)
      {
        _owner._HandleAnyConnected(_networking);
      }

      private void _Disconnected(DisconnectedArgs args)
      {
        _owner._HandleAnyDisconnected(_networking);
      }

      private void Deinitialized(DeinitializedArgs args)
      {
        _owner._stageIdentifierToSession.Remove(_networking.StageIdentifier);
        _owner._multipeerHelpers.Remove(_networking);
      }
    }

    private static int _activeCount;

    internal static void _CheckActiveCountIsZero()
    {
      int activeCount = _activeCount;
      if (activeCount != 0)
      {
        throw new InvalidOperationException
        (
          "_MockNetworkingSessionsMediator active count is " + activeCount + "."
        );
      }
    }

    private readonly Dictionary<IMultipeerNetworking, _MultipeerHelper> _multipeerHelpers =
      new Dictionary<IMultipeerNetworking, _MultipeerHelper>
      (
        _ReferenceComparer<IMultipeerNetworking>.Instance
      );

    private _IVirtualStudioManager _virtualStudioManager;

    public _MockNetworkingSessionsMediator(_IVirtualStudioManager virtualStudioMaster)
    {
      _virtualStudioManager = virtualStudioMaster;

      MultipeerNetworkingFactory.NetworkingInitialized += HandleAnyInitialized;
      MultipeerNetworkingFactory._NonLocalNetworkingInitialized += HandleAnyInitialized;

      Interlocked.Increment(ref _activeCount);
    }

    private int _isDisposed;
    public void Dispose()
    {
      // Dispose only once, in a thread safe manner.
      if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) != 0)
        return;

      MultipeerNetworkingFactory.NetworkingInitialized -= HandleAnyInitialized;
      MultipeerNetworkingFactory._NonLocalNetworkingInitialized -= HandleAnyInitialized;

      foreach (var networking in _stageIdentifierToSession.Values.ToArray())
        if (networking != null)
          networking.Dispose();

      foreach (var helper in _multipeerHelpers.Values)
        helper._Dispose();

      Interlocked.Decrement(ref _activeCount);
    }

    public _MockMultipeerNetworking CreateNonLocalSession(Guid stageIdentifier, RuntimeEnvironment source)
    {
      var networking =
        MultipeerNetworkingFactory._CreateVirtualStudioManagedNetworking
        (
          source,
          ServerConfiguration.ARBE,
          stageIdentifier,
          _virtualStudioManager,
          isLocal: false
        );

      return (_MockMultipeerNetworking) networking;
    }

    // Returned collection also includes the networking with the given stageIdentifier.
    public IReadOnlyCollection<_MockMultipeerNetworking> GetConnectedSessions
    (
      Guid stageIdentifier
    )
    {
      if (_stageIdentifierToMetadata.ContainsKey(stageIdentifier))
      {
        var sessionMetadata = _stageIdentifierToMetadata[stageIdentifier];
        if (_metadataToSession.TryGetValue(sessionMetadata, out HashSet<_MockMultipeerNetworking> networkings))
          return networkings.AsArdkReadOnly();
      }

      return EmptyArdkReadOnlyCollection< _MockMultipeerNetworking>.Instance;
    }

    private HashSet<_MockMultipeerNetworking> GetSessionsConnectedWithMetadata
    (
      byte[] sessionMetadata
    )
    {
      HashSet<_MockMultipeerNetworking> networkings;
      _metadataToSession.TryGetValue(sessionMetadata, out networkings);
      return networkings;
    }

    public _MockMultipeerNetworking GetSession(Guid stageIdentifier)
    {
      _MockMultipeerNetworking networking;
      _stageIdentifierToSession.TryGetValue(stageIdentifier, out networking);
      return networking;
    }

    public IPeer GetHostIfSet(byte[] sessionMetadata)
    {
      HashSet<_MockMultipeerNetworking> networkings;
      if (!_metadataToSession.TryGetValue(sessionMetadata, out networkings))
      {
        // If this method was called before any networking implementations have raised AnyDidConnect
        // using this sessionMetadata, the host will not have been set
        return null;
      }

      var connectedNetworking = networkings.First();
      return connectedNetworking.Host;
    }

    private void HandleAnyInitialized(AnyMultipeerNetworkingInitializedArgs args)
    {
      var mockNetworking = args.Networking as _MockMultipeerNetworking;
      if (mockNetworking == null)
      {
        ARLog._Error
        (
          "While VirtualStudio is running mock networks, only other mock networking instances " +
          "can be initialized."
        );
        return;
      }

      var helper = new _MultipeerHelper(this, mockNetworking);
      _multipeerHelpers.Add(mockNetworking, helper);

      _stageIdentifierToSession.Add(mockNetworking.StageIdentifier, mockNetworking);
    }

    private void _HandleAnyConnected(_MockMultipeerNetworking networking)
    {
      var joinedSessionMetadata = networking.JoinedSessionMetadata;
      var connectedNetworkings = GetSessionsConnectedWithMetadata(joinedSessionMetadata);

      // This mapping has to be established first, to support the use case where
      // messages are sent when handling a networking's DidAddPeer event
      _stageToPeerIdentifier.Add(networking.StageIdentifier, networking.Self.Identifier);
      _stageIdentifierToMetadata.Add(networking.StageIdentifier, joinedSessionMetadata);

      if (connectedNetworkings != null && connectedNetworkings.Count > 0)
        connectedNetworkings.Add(networking);
      else
      {
        // If this networking is the first to join, add a new entry to the dictionary
        var hashset = new HashSet<_MockMultipeerNetworking> { networking };
        _metadataToSession.Add(joinedSessionMetadata, hashset);
      }
    }

    private void _HandleAnyDisconnected(_MockMultipeerNetworking networking)
    {
      byte[] sessionMetadata;
      if (!_stageIdentifierToMetadata.TryGetValue(networking.StageIdentifier, out sessionMetadata))
      {
        ARLog._Warn("Tried to disconnect a networking session that was not properly connected.");
        return;
      }

      var connectedNetworkings = GetSessionsConnectedWithMetadata(sessionMetadata);
      if (connectedNetworkings == null || !connectedNetworkings.Contains(networking))
      {
        ARLog._Warn("Tried to disconnect a networking session that was not properly connected.");
        return;
      }

      // Remove this networking from collections keeping track of connections
      _stageIdentifierToMetadata.Remove(networking.StageIdentifier);
      connectedNetworkings.Remove(networking);

      // Remove the networking's peer from collections keeping track of peers
      _stageToPeerIdentifier.Remove(networking.StageIdentifier);

      if (connectedNetworkings.Count == 0)
      {
        // Clear metadata entry if no more networkings are connected with this metadata
        _metadataToSession.Remove(sessionMetadata);
      }
    }
  }
}