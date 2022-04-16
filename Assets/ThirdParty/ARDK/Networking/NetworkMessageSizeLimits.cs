// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Networking
{
  /// <summary>
  /// Information about the maximum size of various network message components. 
  /// </summary>
  public static class NetworkMessageSizeLimits
  {
    /// <summary>
    /// 8000 bytes
    /// </summary>
    public static readonly int MaxUnreliableMessageSize = 8000;
    
    /// <summary>
    /// 10 MB
    /// </summary>
    public static readonly int MaxReliableMessageSize = 10 * 1024 * 1024;
    
    /// <summary>
    /// 4 KB
    /// </summary>
    public static readonly int MaxPersistentKeyValueKeySize = 4 * 1024;
    
    /// <summary>
    /// 100 MB
    /// </summary>
    public static readonly int MaxPersistentKeyValueValueSize = 100 * 1024 * 1024;
  }
}
