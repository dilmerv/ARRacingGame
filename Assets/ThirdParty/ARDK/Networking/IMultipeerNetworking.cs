// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;

using Niantic.ARDK.AR;
using Niantic.ARDK.Networking.Clock;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Networking
{
  public interface IMultipeerNetworking:
    IDisposable
  {
    /// The runtime environment this MultipeerNetworking is compatible with.
    RuntimeEnvironment RuntimeEnvironment { get; }

    /// A unique identifier for this MultipeerNetworking instance
    Guid StageIdentifier { get; }

    /// A boolean indicating whether or not the instance has connected to a game or session via the
    /// `Join` method.
    bool IsConnected { get; }

    /// The local peer.
    IPeer Self { get; }

    /// The host peer.
    IPeer Host { get; }

    /// The set of all peers other than self for a particular connection.
    IReadOnlyCollection<IPeer> OtherPeers { get; }

    /// This networking session's internal coordinated clock
    ICoordinatedClock CoordinatedClock { get; }

    /// Sends `tag` and `data` to another peer using a specific transport type.
    /// @param tag A unsigned integer that can be used to know how to resolve the format of the
    ///            bytes in `data` on the receiving side.
    /// @param data An array of bytes to be sent to the receiving peer.
    /// @param peer The peer to which to send the `tag` and `data`.
    /// @param transportType Options that control the way the `tag` and `data` are sent to the peer.
    /// @param sendToSelf Whether or not the local peer sending this data should also receive the
    ///                   data locally in the `DidReceiveDataFromPeer` callback.
    /// @note Please be aware of that the size of `data` may have adverse affects on overall
    ///       performance.
    void SendDataToPeer(
      uint tag,
      byte[] data,
      IPeer peer,
      TransportType transportType,
      bool sendToSelf = false);

    /// Sends `tag` and `data` to a number of peers using a specific transport type.
    /// @param tag A unsigned integer the can be used to know how to resolve the format of the
    ///            bytes in `data` on the receiving side.
    /// @param data An array of bytes to be sent to the receiving peers.
    /// @param peers An array of peers to which to send the `tag` and `data`. If empty, will send to
    ///              all connected peers.
    /// @param transportType Options that control the way the `tag` and `data` are sent to the
    ///                      peers.
    /// @param sendToSelf Whether or not the local peer sending this data should also receive the
    ///                   data locally in the `DidReceiveDataFromPeer` callback.
    /// @note Please be aware of that the size of `data` may have adverse affects on overall
    ///       performance.
    /// @note Sending an empty list in the peers field will send data to all peers
    void SendDataToPeers(
      uint tag,
      byte[] data,
      IEnumerable<IPeer> peers,
      TransportType transportType,
      bool sendToSelf = false);

    /// Sends `tag` and `data` to all connected peers using a specific transport type.
    /// @param tag A unsigned integer the can be used to know how to resolve the format of the
    ///            bytes in `data` on the receiving side.
    /// @param data An array of bytes to be sent to all peers.
    /// @param transportType Options that control the way the `tag` and `data` are sent to the
    ///                      peers.
    /// @param sendToSelf Whether or not the local peer sending this data should also receive the
    ///                   data locally in the `DidReceiveDataFromPeer` callback.
    /// @note Please be aware of that the size of `data` may have adverse affects on overall
    ///       performance.
    void BroadcastData(uint tag, byte[] data, TransportType transportType, bool sendToSelf = false);

    /// Sends the `key` and `value` pair to a KeyValue store on the server. The KeyValue store
    /// is persistent through the entire session. The server ensures that every client sees the
    /// latest available value for every key. So this API can be used as a persistent and sync'd key
    /// value store across all clients. Although every client will eventually see the latest value
    /// for every key, clients may miss intermediate values. If multiple clients are writing to the
    /// same key, the last update to make it over the internet to the server wins.
    /// @param key The key for the value being stored
    /// @param value The value to store
    void StorePersistentKeyValue(string key, byte[] value);

    /// <summary>
    /// Sends the specified data to the ARM server (if connected to an ARM session) with the tag
    /// @note This is currently undergoing internal development and testing, and will not do anything.
    /// </summary>
    /// <param name="tag">Tag that will be sent to the ARM server</param>
    /// <param name="data">Data that will be sent to the ARM server</param>
    void SendDataToArm(uint tag, byte[] data);

    /// Joins a specific game or session. Games or sessions are linked by `metadata`. Meaning that
    /// in order for two peers to join the same game or session, they must use the same `metadata`
    /// value.
    void Join(byte[] metadata, byte[] token = null, Int64 timestamp = 0);

    /// Leaves a specific game or session.
    /// @note After leaving a session, the IMultipeerNetworking object cannot be reused to join a
    ///   new session (or rejoin the old one). Dispose the existing object and create a new
    ///   IMultipeerNetworking object to join a new session.
    void Leave();

    /// <summary>
    /// Returns a string representation of the MultipeerNetworking.
    /// </summary>
    /// <returns>A string representation of the MultipeerNetworking</returns>
    string ToString();

    /// <summary>
    /// Returns a truncated string representation of the MultipeerNetworking, for easy printing.
    /// </summary>
    /// <param name="count">The character limit of the returned string</param>
    /// <returns>A truncated string representation of the MultipeerNetworking</returns>
    string ToString(int count);

    /// Event fired upon connection success.
    event ArdkEventHandler<ConnectedArgs> Connected;

    /// Event fired when a join command failed.
    event ArdkEventHandler<ConnectionFailedArgs> ConnectionFailed;

    /// Event fired when this class is about to disconnect.
    event ArdkEventHandler<DisconnectedArgs> Disconnected;

    /// Event fired whenever a message is received from another peer.
    event ArdkEventHandler<PeerDataReceivedArgs> PeerDataReceived;

    /// Event fired when a peer is added.
    event ArdkEventHandler<PeerAddedArgs> PeerAdded;

    /// Event fired when a peer is removed, either from intentional action, timeout, or error.
    event ArdkEventHandler<PeerRemovedArgs> PeerRemoved;

    /// Event fired when someone has added a new key-value pair to the server KeyValue store,
    /// or updated an existing one.
    event ArdkEventHandler<PersistentKeyValueUpdatedArgs> PersistentKeyValueUpdated;

    /// Event fired when this object is about to deinitialize.
    event ArdkEventHandler<DeinitializedArgs> Deinitialized;

    /// <summary>
    /// Event fired when receiving data from an ARM server
    /// @note This is currently undergoing internal development and testing, and will not be fired.
    /// </summary>
    event ArdkEventHandler<DataReceivedFromArmArgs> DataReceivedFromArm;

    /// <summary>
    /// @note This is currently undergoing internal development and testing, and will not be fired.
    /// </summary>
    event ArdkEventHandler<SessionStatusReceivedFromArmArgs> SessionStatusReceivedFromArm;

    /// <summary>
    /// @note This is currently undergoing internal development and testing, and will not be fired.
    /// </summary>
    event ArdkEventHandler<SessionResultReceivedFromArmArgs> SessionResultReceivedFromArm;
  }
}
