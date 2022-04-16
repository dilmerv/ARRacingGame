// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking.HLAPI.Routing;

namespace Niantic.ARDK.Networking.HLAPI
{
  /// <summary>
  /// A group of information to be replicated over the network. Can loosely be thought of as an
  ///   "object" - it handles a number of registered data handlers, which all have a lifetime
  ///   that is tied to this group.
  /// </summary>
  public interface INetworkGroup: 
    INetworkedDataHandler
  {
    /// <summary>
    /// Register an INetworkedDataHandler to this group, which will handler sending/receiving data
    ///   as well as routing. 
    /// </summary>
    /// <param name="handler"></param>
    void RegisterHandler(INetworkedDataHandler handler);

    /// <summary>
    /// Unregisters a handler from this group, such that it will no longer send/receive data. However,
    ///   the handler itself is not destroyed by this call.
    /// </summary>
    /// <param name="handler"></param>
    void UnregisterHandler(INetworkedDataHandler handler);

    /// <summary>
    /// Creates a new nested group that is attached to this group.
    /// </summary>
    /// <param name="groupId">Unique identifier for this group, equivalent to a ulong</param>
    INetworkGroup CreateNestedGroup(NetworkId groupId);

    /// <summary>
    /// The IHlapiSession object that this group is attached to. 
    /// </summary>
    IHlapiSession Session { get; }

    /// <summary>
    /// The NetworkId representing this group
    /// </summary>
    NetworkId NetworkId { get; }

    /// <summary>
    /// Initialize the Session and receive any cached data addressed to this NetworkGroup. This will
    ///   generally be called by the HlapiSession
    /// </summary>
    /// <param name="session"></param>
    /// <param name="cache"></param>
    void ReceiveCachedDataFromSession(IHlapiSession session, UnhandledPacketCache cache);
  }
}
