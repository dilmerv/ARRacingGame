// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking.HLAPI.Routing;

namespace Niantic.ARDK.Networking.HLAPI
{
  /// Responsible for replicating registered groups and their data across the network, by
  /// addressing and routing messages so that a registered data handler will always receive
  /// data from its corresponding data handler (on the other end of the networking
  /// object). Implementations of this interface use an IMultipeerNetworking object to
  /// send and receive messages
  public interface IHlapiSession:
    IDisposable
  {
    /// <summary>
    /// Creates a new network group that is attached to this session.
    /// </summary>
    /// <param name="groupId">Unique identifier for this group, equivalent to a ulong</param>
    INetworkGroup CreateAndRegisterGroup(NetworkId groupId);

    /// <summary>
    /// Register an existing group to this session.
    /// </summary>
    /// <param name="group"></param>
    void RegisterGroup(INetworkGroup group);

    /// <summary>
    /// Close the specified group
    /// </summary>
    /// <param name="group">Group to remove from this session</param>
    void UnregisterGroup(INetworkGroup group);

    /// <summary>
    /// The networking object that this session is attached to. May be null if the session is created
    ///   with no networking object. In that case, this will become the first initialized
    ///   IMultipeerNetworking object, and all data will be passed along that network.
    /// </summary>
    IMultipeerNetworking Networking { get; }

    /// <summary>
    /// Query all network groups attached to this session to write any relevant data that their
    ///   data handlers have accumulated since the last call of this method to buffers, then send
    ///   the buffers to each relevant peer.
    /// </summary>
    void SendQueuedData();
  }
}