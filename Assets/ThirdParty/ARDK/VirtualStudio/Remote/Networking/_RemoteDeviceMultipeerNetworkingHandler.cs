// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine.Networking.PlayerConnection;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  internal sealed class _RemoteDeviceMultipeerNetworkingHandler
  {
    private readonly IMultipeerNetworking _networking;

    internal IMultipeerNetworking InnerNetworking
    {
      get { return _networking; }
    }

    internal _RemoteDeviceMultipeerNetworkingHandler(ServerConfiguration configuration, Guid stageIdentifier)
    {
      _networking =
        MultipeerNetworkingFactory.Create(configuration, stageIdentifier);

      _networking.Connected += OnInternalConnected;
      _networking.ConnectionFailed += OnInternalConnectionFailed;
      _networking.Disconnected += OnInternalDisconnected;
      _networking.PeerDataReceived += OnInternalPeerDataReceived;
      _networking.PeerAdded += OnInternalPeerAdded;
      _networking.PeerRemoved += OnInternalPeerRemoved;
      _networking.Deinitialized += OnInternalDeinitializing;
      _networking.PersistentKeyValueUpdated += OnInternalPersistentKeyValueUpdated;
      _networking.DataReceivedFromArm += OnInternalDidReceiveDataFromArm;
      _networking.SessionStatusReceivedFromArm += OnInternalDidReceiveStatusFromArm;
      _networking.SessionResultReceivedFromArm += OnInternalDidReceiveResultFromArm;

      _RemoteConnection.Register
      (
        NetworkingJoinMessage.ID.Combine(stageIdentifier),
        HandleJoinMessage
      );

      _RemoteConnection.Register
      (
        NetworkingLeaveMessage.ID.Combine(stageIdentifier),
        HandleLeaveMessage
      );

      _RemoteConnection.Register
      (
        NetworkingDestroyMessage.ID.Combine(stageIdentifier),
        HandleDestroyMessage
      );

      _RemoteConnection.Register
      (
        NetworkingSendDataToPeersMessage.ID.Combine(stageIdentifier),
        HandleSendDataToPeersMessage
      );

      _RemoteConnection.Register
      (
        NetworkingStorePersistentKeyValueMessage.ID.Combine(stageIdentifier),
        HandleStorePersistentKeyValueMessage
      );

      _RemoteConnection.Register
      (
        NetworkingSendDataToArmMessage.ID.Combine(stageIdentifier),
        HandleSendDataToArmMessage
      );
    }

    ~_RemoteDeviceMultipeerNetworkingHandler()
    {
      ARLog._Error("_RemoteDeviceMultipeerNetworkingHandler should be destroyed by an explicit call to Dispose().");
    }

    private bool _isDestroyed;

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      if (_isDestroyed)
        return;

      _isDestroyed = true;

      _networking.Connected -= OnInternalConnected;
      _networking.ConnectionFailed -= OnInternalConnectionFailed;
      _networking.Disconnected -= OnInternalDisconnected;
      _networking.PeerDataReceived -= OnInternalPeerDataReceived;
      _networking.PeerAdded -= OnInternalPeerAdded;
      _networking.PeerRemoved -= OnInternalPeerRemoved;
      _networking.Deinitialized -= OnInternalDeinitializing;
      _networking.PersistentKeyValueUpdated -= OnInternalPersistentKeyValueUpdated;
      _networking.DataReceivedFromArm -= OnInternalDidReceiveDataFromArm;
      _networking.SessionStatusReceivedFromArm -= OnInternalDidReceiveStatusFromArm;
      _networking.SessionResultReceivedFromArm -= OnInternalDidReceiveResultFromArm;

      var stageId = InnerNetworking.StageIdentifier;
      _RemoteConnection.Unregister
      (
        NetworkingJoinMessage.ID.Combine(stageId),
        HandleJoinMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingLeaveMessage.ID.Combine(stageId),
        HandleLeaveMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingSendDataToPeersMessage.ID.Combine(stageId),
        HandleSendDataToPeersMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingDestroyMessage.ID.Combine(stageId),
        HandleDestroyMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingStorePersistentKeyValueMessage.ID.Combine(stageId),
        HandleStorePersistentKeyValueMessage
      );

      _networking.Dispose();
    }

    private void OnInternalConnected(ConnectedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingConnectedMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingConnectedMessage
        {
          SelfIdentifier = args.Self.Identifier, HostIdentifier = args.Host.Identifier,
        }.SerializeToArray()
      );
    }

    private void OnInternalConnectionFailed(ConnectionFailedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingConnectionFailedWithErrorMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingConnectionFailedWithErrorMessage
        {
          ErrorCode = args.ErrorCode,
        }.SerializeToArray()
      );
    }

    private void OnInternalDisconnected(DisconnectedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingDisconenctedMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingDisconenctedMessage().SerializeToArray()
      );
    }

    private void OnInternalPeerDataReceived(PeerDataReceivedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingPeerDataReceivedMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingPeerDataReceivedMessage
        {
          Tag = args.Tag,
          Data = args.CopyData(),
          PeerIdentifier = args.Peer.Identifier,
          TransportType = (byte)args.TransportType
        }.SerializeToArray()
      );
    }

    private void OnInternalPeerAdded(PeerAddedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingPeerAddedMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingPeerAddedMessage
          {
            PeerIdentifier = args.Peer.Identifier
          }
          .SerializeToArray()
      );
    }

    private void OnInternalPeerRemoved(PeerRemovedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingPeerRemovedMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingPeerRemovedMessage
          {
            PeerIdentifier = args.Peer.Identifier
          }
          .SerializeToArray()
      );
    }

    private void OnInternalDeinitializing(DeinitializedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingDeinitializedMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingDeinitializedMessage().SerializeToArray()
      );
    }

    private void OnInternalPersistentKeyValueUpdated(PersistentKeyValueUpdatedArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingPersistentKeyValueUpdatedMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingPersistentKeyValueUpdatedMessage()
        {
          Key = System.Text.Encoding.UTF8.GetBytes(args.Key), Value = args.CopyValue()
        }.SerializeToArray()
      );
    }

    private void OnInternalDidReceiveDataFromArm(DataReceivedFromArmArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingDataReceivedFromArmMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingDataReceivedFromArmMessage
        {
          Tag = args.Tag,
          Data = args.CreateDataReader().ToArray(),
        }.SerializeToArray()
      );
    }

    private void OnInternalDidReceiveStatusFromArm(SessionStatusReceivedFromArmArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingStatusReceivedFromArmMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingStatusReceivedFromArmMessage
        {
          Status = args.Status,
        }.SerializeToArray()
      );
    }

    private void OnInternalDidReceiveResultFromArm(SessionResultReceivedFromArmArgs args)
    {
      _RemoteConnection.Send
      (
        NetworkingResultReceivedFromArmMessage.ID.Combine(InnerNetworking.StageIdentifier),
        new NetworkingResultReceivedFromArmMessage
        {
          Outcome = args.Outcome,
          Details = args.CreateDetailsReader().ToArray(),
        }.SerializeToArray()
      );
    }

    private void HandleJoinMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingJoinMessage>();

      _networking.Join(message.Metadata);
    }

    private void HandleLeaveMessage(MessageEventArgs e)
    {
      _networking.Leave();
    }

    private void HandleSendDataToPeersMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingSendDataToPeersMessage>();
      var peers = new IPeer[message.Peers.Length];

      for (var i = 0; i < message.Peers.Length; i++)
        peers[i] = _Peer.FromIdentifier(message.Peers[i]);

      _networking.SendDataToPeers
      (
        message.Tag,
        message.Data,
        peers,
        (TransportType)message.TransportType,
        message.SendToSelf
      );
    }

    private void HandleStorePersistentKeyValueMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingStorePersistentKeyValueMessage>();

      var key = System.Text.Encoding.UTF8.GetString(message.Key);
      var value = message.Value;

      _networking.StorePersistentKeyValue(key, value);
    }

    private void HandleSendDataToArmMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingSendDataToArmMessage>();

      _networking.SendDataToArm(message.Tag, message.Data);
    }

    private void HandleDestroyMessage(MessageEventArgs e)
    {
      Dispose();
    }
  }
}
