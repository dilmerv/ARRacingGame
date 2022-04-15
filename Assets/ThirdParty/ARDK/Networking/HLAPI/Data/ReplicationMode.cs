// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Networking.HLAPI.Data {
  /// <summary>
  /// Information about how data is being replicated.
  /// </summary>
  public struct ReplicationMode {
    /// <summary>
    /// The transport being replicated over.
    /// </summary>
    public TransportType Transport { get; set; }
    
    /// <summary>
    /// If this is the first time communicating with this particual set of peers.
    /// </summary>
    public bool IsInitial { get; set; }
  }
}