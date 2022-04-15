// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.Clock;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine.Networking.PlayerConnection;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// <see cref="_IEditorMultipeerNetworking"/> that will run on the editor to handle
  /// logic for remote multipeer networking.
  ///
  /// All session command invoked through this object will be proxied to the remote device and all
  /// events on the device will be proxied back to this object.
  /// </summary>
  internal sealed class _RemoteEditorMultipeerNetworking:
    IMultipeerNetworking
  {
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
        // Todo: Implement a mock ICoordinatedClock
        throw new NotSupportedException();
      }
    }

    private readonly Dictionary<Guid, IPeer> _peers = new Dictionary<Guid, IPeer>();
    private ARDKReadOnlyCollection<IPeer> _readOnlyPeers;

    private byte[] _joinedSessionMetadata;

    public _RemoteEditorMultipeerNetworking(ServerConfiguration configuration, Guid stageIdentifier)
    {
      _FriendTypeAsserter.AssertCallerIs(typeof(MultipeerNetworkingFactory));

      _readOnlyPeers = _peers.Values.AsArdkReadOnly();
      StageIdentifier = stageIdentifier;

      if (configuration.ClientMetadata == null)
        configuration.GenerateRandomClientId();

      _EasyConnection.Send
      (
        new NetworkingInitMessage
        {
          Configuration = configuration,
          StageIdentifier = stageIdentifier
        },
        TransportType.ReliableOrdered
      );

      _RemoteConnection.Register
      (
        NetworkingConnectedMessage.ID.Combine(stageIdentifier),
        HandleDidConnectMessage
      );

      _RemoteConnection.Register
      (
        NetworkingConnectionFailedWithErrorMessage
          .ID.Combine(stageIdentifier),
        HandleConnectionDidFailWithErrorMessage
      );

      _RemoteConnection.Register
      (
        NetworkingDisconenctedMessage.ID.Combine(stageIdentifier),
        HandleWillDisconnectMessage
      );

      _RemoteConnection.Register
      (
        NetworkingPeerDataReceivedMessage.ID.Combine(stageIdentifier),
        HandleDidReceiveDataFromPeer
      );

      _RemoteConnection.Register
      (
        NetworkingPeerAddedMessage.ID.Combine(stageIdentifier),
        HandleAddedPeerMessage
      );

      _RemoteConnection.Register
      (
        NetworkingPeerRemovedMessage.ID.Combine(stageIdentifier),
        HandleRemovedPeerMessage
      );

      _RemoteConnection.Register
      (
        NetworkingDeinitializedMessage.ID.Combine(stageIdentifier),
        HandleDeinitializingMessage
      );

      _RemoteConnection.Register
      (
        NetworkingPersistentKeyValueUpdatedMessage.ID.Combine(stageIdentifier),
        HandleUpdatedPersistentKeyValueMessage
      );

      _RemoteConnection.Register
      (
        NetworkingDataReceivedFromArmMessage.ID.Combine(stageIdentifier),
        HandleDataReceivedFromArmMessage
      );

      _RemoteConnection.Register
      (
        NetworkingStatusReceivedFromArmMessage.ID.Combine(stageIdentifier),
        HandleStatusReceivedFromArmMessage
      );

      _RemoteConnection.Register
      (
        NetworkingResultReceivedFromArmMessage.ID.Combine(stageIdentifier),
        HandleResultReceivedFromArmMessage
      );
    }

    ~_RemoteEditorMultipeerNetworking()
    {
      ARLog._Error("_RemoteEditorMultipeerNetworking should be destroyed by an explicit call to Dispose().");
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
        handler(new DeinitializedArgs());

      _RemoteConnection.Send
      (
        NetworkingDestroyMessage.ID.Combine(StageIdentifier),
        new NetworkingDestroyMessage().SerializeToArray(),
        TransportType.ReliableOrdered
      );
    }

    public void Join(byte[] metadata, byte[] token = null, long timestamp = 0)
    {
      if (metadata == null)
        throw new ArgumentNullException(nameof(metadata));

      if (IsConnected)
      {
        ARLog._Warn
        (
          metadata.SequenceEqual(_joinedSessionMetadata)
            ? "ARDK: Already joined this session."
            : "ARDK: Already connected to a different session."
        );
        return;
      }

      _RemoteConnection.Send
      (
        NetworkingJoinMessage.ID.Combine(StageIdentifier),
        new NetworkingJoinMessage() { Metadata = metadata }.SerializeToArray(),
        TransportType.ReliableOrdered
      );
    }

    public void Leave()
    {
      if (!IsConnected)
        return;

      _RemoteConnection.Send
      (
        NetworkingLeaveMessage.ID.Combine(StageIdentifier),
        new NetworkingLeaveMessage().SerializeToArray(),
        TransportType.ReliableOrdered
      );
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

      // Same here as in _NativeMutlipeerNetworking
      if (sendToSelf)
      {
        var handler = PeerDataReceived;
        if (handler != null)
        {
          var args = new PeerDataReceivedArgs(Self, tag, transportType, data);
          handler(args);
        }
      }

      var message =
        new NetworkingSendDataToPeersMessage
        {
          TransportType = (byte) transportType,
          Peers = peers.Select(peer => peer.Identifier).ToArray(),
          Tag = tag,
          Data = data
        };

      _RemoteConnection.Send
      (
        NetworkingSendDataToPeersMessage.ID.Combine(StageIdentifier),
        message.SerializeToArray(),
        TransportType.ReliableOrdered
      );
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

    public void SendDataToArm(uint tag, byte[] data)
    {
      var message =
        new NetworkingSendDataToArmMessage
        {
          Tag = tag,
          Data = data
        };

      _RemoteConnection.Send
      (
        NetworkingSendDataToArmMessage.ID.Combine(StageIdentifier),
        message.SerializeToArray(),
        TransportType.ReliableOrdered
      );
    }

    public void StorePersistentKeyValue(string key, byte[] value)
    {
      var message =
        new NetworkingStorePersistentKeyValueMessage
        {
          Key = System.Text.Encoding.UTF8.GetBytes(key),
          Value = value
        };

      _RemoteConnection.Send
      (
        NetworkingStorePersistentKeyValueMessage.ID.Combine(StageIdentifier),
        message.SerializeToArray(),
        TransportType.ReliableOrdered
      );
    }

    private void HandleDidConnectMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingConnectedMessage>();

      IsConnected = true;
      Self = _Peer.FromIdentifier(message.SelfIdentifier);
      Host = _Peer.FromIdentifier(message.HostIdentifier);

      var handler = _connected;
      if (handler != null)
        handler(new ConnectedArgs(Self, Host));
    }

    private void HandleConnectionDidFailWithErrorMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingConnectionFailedWithErrorMessage>();

      var handler = ConnectionFailed;
      if (handler != null)
        handler(new ConnectionFailedArgs(message.ErrorCode));
    }

    private void HandleWillDisconnectMessage(MessageEventArgs e)
    {
      _joinedSessionMetadata = null;
      IsConnected = false;
      Self = null;
      Host = null;
      _peers.Clear();

      var handler = Disconnected;
      if (handler != null)
        handler(new DisconnectedArgs());
    }

    private void HandleDidReceiveDataFromPeer(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingPeerDataReceivedMessage>();
      var peer = _Peer.FromIdentifier(message.PeerIdentifier);

      var handler = PeerDataReceived;
      if (handler != null)
      {
        var args =
          new PeerDataReceivedArgs
          (
            peer,
            message.Tag,
            (TransportType) message.TransportType,
            message.Data
          );

        handler(args);
      }
    }

    private void HandleAddedPeerMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingPeerAddedMessage>();
      var peer = _Peer.FromIdentifier(message.PeerIdentifier);

      _peers.Add(peer.Identifier, peer);

      var handler = PeerAdded;
      if (handler != null)
        handler(new PeerAddedArgs(_Peer.FromIdentifier(message.PeerIdentifier)));
    }

    private void HandleRemovedPeerMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingPeerRemovedMessage>();
      var peer = _Peer.FromIdentifier(message.PeerIdentifier);

      _peers.Remove(peer.Identifier);

      if (peer.Equals(Host))
        Host = null;

      var handler = PeerRemoved;
      if (handler != null)
        handler(new PeerRemovedArgs(_Peer.FromIdentifier(message.PeerIdentifier)));
    }

    private void HandleUpdatedPersistentKeyValueMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingPersistentKeyValueUpdatedMessage>();
      var key = System.Text.Encoding.UTF8.GetString(message.Key);
      var value = message.Value;

      var handler = PersistentKeyValueUpdated;
      if (handler != null)
        handler(new PersistentKeyValueUpdatedArgs(key, value));
    }

    private void HandleResultReceivedFromArmMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingResultReceivedFromArmMessage>();

      var handler = SessionResultReceivedFromArm;
      if (handler != null)
        handler(new SessionResultReceivedFromArmArgs(message.Outcome, message.Details));
    }

    private void HandleStatusReceivedFromArmMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingStatusReceivedFromArmMessage>();

      var handler = SessionStatusReceivedFromArm;
      if (handler != null)
        handler(new SessionStatusReceivedFromArmArgs(message.Status));
    }

    private void HandleDataReceivedFromArmMessage(MessageEventArgs e)
    {
      var message = e.data.DeserializeFromArray<NetworkingDataReceivedFromArmMessage>();

      var handler = DataReceivedFromArm;
      if (handler != null)
      {
        var args = new DataReceivedFromArmArgs(message.Tag, message.Data);
        handler(args);
      }
    }

    private void HandleDeinitializingMessage(MessageEventArgs e)
    {
      var handler = Deinitialized;
      if (handler != null)
        handler(new DeinitializedArgs());

      _RemoteConnection.Unregister
      (
        NetworkingConnectedMessage.ID.Combine(StageIdentifier),
        HandleDidConnectMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingConnectionFailedWithErrorMessage.ID.Combine(StageIdentifier),
        HandleConnectionDidFailWithErrorMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingDisconenctedMessage.ID.Combine(StageIdentifier),
        HandleWillDisconnectMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingPeerDataReceivedMessage.ID.Combine(StageIdentifier),
        HandleDidReceiveDataFromPeer
      );

      _RemoteConnection.Unregister
      (
        NetworkingPeerAddedMessage.ID.Combine(StageIdentifier),
        HandleAddedPeerMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingPeerRemovedMessage.ID.Combine(StageIdentifier),
        HandleRemovedPeerMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingDeinitializedMessage.ID.Combine(StageIdentifier),
        HandleDeinitializingMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingPersistentKeyValueUpdatedMessage.ID.Combine(StageIdentifier),
        HandleUpdatedPersistentKeyValueMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingDataReceivedFromArmMessage.ID.Combine(StageIdentifier),
        HandleDataReceivedFromArmMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingStatusReceivedFromArmMessage.ID.Combine(StageIdentifier),
        HandleStatusReceivedFromArmMessage
      );

      _RemoteConnection.Unregister
      (
        NetworkingResultReceivedFromArmMessage.ID.Combine(StageIdentifier),
        HandleResultReceivedFromArmMessage
      );
    }

    public RuntimeEnvironment RuntimeEnvironment
    {
      get { return RuntimeEnvironment.Remote; }
    }

    public string ToString(int count)
    {
      return string.Format("_RemoteMultipeerNetworking (ID: {0})", StageIdentifier);
    }

    public event ArdkEventHandler<ConnectionFailedArgs> ConnectionFailed;
    public event ArdkEventHandler<DisconnectedArgs> Disconnected;
    public event ArdkEventHandler<PeerDataReceivedArgs> PeerDataReceived;
    public event ArdkEventHandler<PeerAddedArgs> PeerAdded;
    public event ArdkEventHandler<PeerRemovedArgs> PeerRemoved;
    public event ArdkEventHandler<PersistentKeyValueUpdatedArgs> PersistentKeyValueUpdated;
    public event ArdkEventHandler<DeinitializedArgs> Deinitialized;

#pragma warning disable CS0067
    public event ArdkEventHandler<DataReceivedFromArmArgs> DataReceivedFromArm;
    public event ArdkEventHandler<SessionStatusReceivedFromArmArgs> SessionStatusReceivedFromArm;
    public event ArdkEventHandler<SessionResultReceivedFromArmArgs> SessionResultReceivedFromArm;
#pragma warning restore CS0067

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