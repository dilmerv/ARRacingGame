// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Niantic.ARDK.Networking.Clock
{
  /// <summary>
  /// Reports the quality of the timestamp returned by the Coordinated Clock
  /// </summary>
  public enum CoordinatedClockTimestampQuality:
    byte
  {
    /// There has not been an update from the server
    Invalid = 0,

    /// The server has sent updates, but there is still variance in syncing
    Syncing,

    /// The timestamp is now synchronized with the server
    Stable
  };

  /// <summary>
  /// A coordinated clock that synchronizes with a server sided clock so that all peers within
  ///   the same networking session the same timestamp.
  /// </summary>
  public interface ICoordinatedClock
  {
    /// <summary>
    /// The current server synchronized time
    ///
    /// @note CurrentCorrectedTime itself has no guarantees of epoch or standard - it is a timestamp
    ///   in milliseconds. It is better used as a stopwatch or timer, rather than used to represent a
    ///   real world time (though this can be done by locally comparing it to another known clock).
    /// </summary>
    long CurrentCorrectedTime { get; }

    /// <summary>
    /// The quality of the timestamp returned by this CoordinatedClock
    /// </summary>
    CoordinatedClockTimestampQuality SyncStatus { get; }
  }
}
