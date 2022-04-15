// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.Clock;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Networking.Mock
{
  internal sealed class _MockMultipeerNetworking:
    IMultipeerNetworking
  {
    private readonly _IVirtualStudioManager _virtualStudioManager;

    public Guid StageIdentifier { get; }
    public bool IsConnected { get; private set; }

    /// <inheritdoc />
    public IPeer Self { get; private set; }

    /// <inheritdoc />
    public IPeer Host { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IPeer> OtherPeers
    {
      get { return _readOnlyPeers; }
    }

    /// <inheritdoc />
    public ICoordinatedClock CoordinatedClock
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    public byte[] JoinedSessionMetadata { get; private set; }

    private readonly Dictionary<Guid, IPeer> _peers = new Dictionary<Guid, IPeer>();
    private ARDKReadOnlyCollection<IPeer> _readOnlyPeers;

    internal _MockMultipeerNetworking
    (
      Guid stageIdentifier,
      _IVirtualStudioManager virtualStudioMaster
    )
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(MultipeerNetworkingFactory));

      StageIdentifier = stageIdentifier;
      _virtualStudioManager = virtualStudioMaster;

      _readOnlyPeers = _peers.Values.AsArdkReadOnly();
    }

    private bool _isDestroyed;
    public void Dispose()
    {
      GC.SuppressFinalize(this);

      if (_isDestroyed)
        return;

      _isDestroyed = true;

      if (IsConnected)
        Leave();

      var handler = Deinitialized;
      if (handler != null)
      {
        var args = new DeinitializedArgs();
        handler(args);
      }
    }

    public void Join(byte[] metadata, byte[] token = null, long timestamp = 0)
    {
      if (metadata == null || metadata.Length == 0)
        throw new ArgumentException("Cannot be null or empty.", nameof(metadata));

      if (IsConnected)
      {
        ARLog._Warn
        (
          metadata.SequenceEqual(JoinedSessionMetadata)
            ? "ARDK: Already joined this session."
            : "ARDK: Already connected to a different session."
        );
        return;
      }

      JoinedSessionMetadata = metadata;

      Self = new _MockPeer(Guid.NewGuid(), StageIdentifier);
      Host = _virtualStudioManager.MultipeerMediator.GetHostIfSet(metadata) ?? Self;
      IsConnected = true;

      // Raise Connected event before calling AddPeer (which will raise AddedPeer),
      // because peers can only be added to connected networkings
      var handler = _connected;
      if (handler != null)
        handler(new ConnectedArgs(Self, Host));

      if (_virtualStudioManager == null || _virtualStudioManager.MultipeerMediator == null)
        return;

      var connectedNetworkings =
        _virtualStudioManager.MultipeerMediator.GetConnectedSessions(StageIdentifier);

      if (connectedNetworkings != null && connectedNetworkings.Count > 0)
      {
        // If this networking joined an already existing session,
        // add the relevant peers to each networking
        foreach (var networking in connectedNetworkings)
        {
          if (networking.StageIdentifier != StageIdentifier)
          {
            AddPeer(networking.Self);
            networking.AddPeer(Self);
          }
        }
      }
    }

    public void Leave()
    {
      if (!IsConnected)
        return;

      var connectedNetworkings =
        _virtualStudioManager.MultipeerMediator.GetConnectedSessions(StageIdentifier);

      if (connectedNetworkings != null && connectedNetworkings.Count > 0)
      {
        // Remove the peer from the remaining connected networkings
        foreach (var networking in connectedNetworkings)
        {
          if (networking.StageIdentifier != StageIdentifier)
            networking.RemovePeer(Self);
        }
      }

      JoinedSessionMetadata = null;
      Self = null;
      Host = null;
      IsConnected = false;
      _peers.Clear();

      var handler = Disconnected;
      if (handler != null)
        handler(new DisconnectedArgs());
    }

    public void SendDataToPeer
    (
      uint tag,
      byte[] data,
      IPeer peer,
      TransportType transportType,
      bool sendToSelf = false
    )
    {
      var receivers = new List<IPeer> { peer };
      SendDataToPeers(tag, data, receivers, transportType);
    }

    public void SendDataToPeers
    (
      uint tag,
      byte[] data,
      IEnumerable<IPeer> peers,
      TransportType transportType,
      bool sendToSelf = false
    )
    {
      if (!IsConnected)
      {
        ARLog._Error("Cannot send data to peers while not connected to a networking session.");
        return;
      }

      // In the native implementation, the local peer would always receive the message before
      // other peers (since it is a local event and not sent through the network) if sending
      // to self, which is why this ordering should be maintained.
      if (sendToSelf)
        ReceiveDataFromPeer(tag, Self, transportType, data);

      foreach (var peer in peers)
      {
        var mockPeer = (_MockPeer) peer;
        var receiverNetworking =
          _virtualStudioManager.MultipeerMediator.GetSession(mockPeer.StageIdentifier);

        receiverNetworking.ReceiveDataFromPeer(tag, Self, transportType, data);
      }
    }

    public void BroadcastData
    (
      uint tag,
      byte[] data,
      TransportType transportType,
      bool sendToSelf = false
    )
    {
      SendDataToPeers(tag, data, OtherPeers, transportType, sendToSelf);
    }

    private void ReceiveDataFromPeer
    (
      uint tag,
      IPeer peer,
      TransportType transportType,
      byte[] data
    )
    {
      var handler = PeerDataReceived;
      if (handler != null)
      {
        var args =
          new PeerDataReceivedArgs(peer, tag, transportType, data);

        handler(args);
      }
    }

    /// <summary>
    /// Internal event to notify listeners that an _EditorMultipeerNetworking has sent some data
    ///   to the Arm server.
    /// </summary>
    internal ArdkEventHandler<PeerDataReceivedArgs> ArmDataReceivedFromClient;

    public void SendDataToArm(uint tag, byte[] data)
    {
      var handler = ArmDataReceivedFromClient;
      if (handler != null)
      {
        var args = new PeerDataReceivedArgs(Self, tag, TransportType.ReliableOrdered, data);
        handler(args);
      }
    }

    public void StorePersistentKeyValue(string key, byte[] value)
    {
      var connectedNetworkings =
        _virtualStudioManager.MultipeerMediator.GetConnectedSessions(StageIdentifier);

      foreach (var networking in connectedNetworkings)
        networking.ReceivePersistentKeyValue(key, value);
    }

    public void ReceivePersistentKeyValue(string key, byte[] value)
    {
      var handler = PersistentKeyValueUpdated;
      if (handler != null)
        handler(new PersistentKeyValueUpdatedArgs(key, value));
    }

    public void FailConnectionWithError(uint errorCode)
    {
      var handler = ConnectionFailed;
      if (handler != null)
        handler(new ConnectionFailedArgs(errorCode));
    }

    private void AddPeer(IPeer peer)
    {
      _peers.Add(peer.Identifier, peer);

      var handler = PeerAdded;
      if (handler != null)
        handler(new PeerAddedArgs(peer));
    }

    private void RemovePeer(IPeer peer)
    {
      _peers.Remove(peer.Identifier);

      if (peer.Equals(Host))
        Host = null;

      var handler = PeerRemoved;
      if (handler != null)
        handler(new PeerRemovedArgs(peer));
    }

    /// <summary>
    /// Call to invoke a DataReceivedFromArm event on this _MockNetworkingCommandsRouter.
    /// </summary>
    /// <param name="args"></param>
    internal void _ReceiveDataFromArm(DataReceivedFromArmArgs args)
    {
      var handler = DataReceivedFromArm;

      if (handler != null)
        handler(args);
    }

    /// <summary>
    /// Call to invoke a SessionStatusReceivedFromArm event on this _MockNetworkingCommandsRouter.
    /// </summary>
    /// <param name="args"></param>
    internal void _ReceiveStatusFromArm(SessionStatusReceivedFromArmArgs args)
    {
      var handler = SessionStatusReceivedFromArm;

      if (handler != null)
        handler(args);
    }

    /// <summary>
    /// Call to invoke a SessionResultReceivedFromArm event on this _MockNetworkingCommandsRouter.
    /// </summary>
    /// <param name="args"></param>
    internal void _ReceiveResultFromArm(SessionResultReceivedFromArmArgs args)
    {
      var handler = SessionResultReceivedFromArm;

      if (handler != null)
        handler(args);
    }

    public RuntimeEnvironment RuntimeEnvironment
    {
      get { return RuntimeEnvironment.Mock; }
    }

    public string ToString(int count)
    {
      return string.Format("_MockMultipeerNetworking (ID: {0})", StageIdentifier);
    }

    public event ArdkEventHandler<ConnectionFailedArgs> ConnectionFailed;
    public event ArdkEventHandler<DisconnectedArgs> Disconnected;
    public event ArdkEventHandler<PeerDataReceivedArgs> PeerDataReceived;
    public event ArdkEventHandler<PeerAddedArgs> PeerAdded;
    public event ArdkEventHandler<PeerRemovedArgs> PeerRemoved;
    public event ArdkEventHandler<PersistentKeyValueUpdatedArgs> PersistentKeyValueUpdated;
    public event ArdkEventHandler<DeinitializedArgs> Deinitialized;

    public event ArdkEventHandler<DataReceivedFromArmArgs> DataReceivedFromArm;
    public event ArdkEventHandler<SessionStatusReceivedFromArmArgs> SessionStatusReceivedFromArm;
    public event ArdkEventHandler<SessionResultReceivedFromArmArgs> SessionResultReceivedFromArm;

    private ArdkEventHandler<ConnectedArgs> _connected;
    public event ArdkEventHandler<ConnectedArgs> Connected
    {
      add
      {
        _connected += value;
        if (IsConnected)
        {
          var args = new ConnectedArgs(Self, Host);
          value(args);
        }
      }
      remove { _connected -= value; }
    }
  }
}