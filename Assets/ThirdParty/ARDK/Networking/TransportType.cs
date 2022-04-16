// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Networking
{
  /// <summary>
  /// The method of getting data to other clients
  /// </summary>
  public enum TransportType:
    byte
  {
    /// <summary>
    /// Messages may drop, and won't be re-sent.
    /// They may arrive out of order, or not at all
    /// </summary>
    UnreliableUnordered = 1,

    /// <summary>
    /// Messages may be dropped, but they will be delivered in order.
    /// </summary>
    UnreliableOrdered = 2,

    /// <summary>
    /// Messages will eventually arrive at each destination they're sent to.
    /// However, they may arrive out of order in doing so.
    /// </summary>
    ReliableUnordered = 3,

    /// <summary>
    /// Messages sent with this transport type will be received by each other peer
    /// in the order they were sent. Won't be dropped or arrive out of order
    /// </summary>
    ReliableOrdered = 4,
  };
}