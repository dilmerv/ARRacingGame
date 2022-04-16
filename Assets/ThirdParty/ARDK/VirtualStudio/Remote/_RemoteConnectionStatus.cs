// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// The state of the remote connection.
  /// </summary>
  internal enum _RemoteConnectionState: byte
  {
    /// <summary>
    /// No remote connection stack has been made.
    /// </summary>
    None,

    /// <summary>
    /// The remote connection stack has been made.
    /// </summary>
    Initialized,

    /// <summary>
    /// Remote connection is in the process of connecting.q
    /// </summary>
    Connecting,

    /// <summary>
    /// The remote connection is connected.
    /// </summary>
    Connected,
  }

  internal enum _RemoteConnectionError: byte
  {
    /// <summary>
    /// No error.
    /// </summary>
    None,

    /// <summary>
    /// Remote connection failed to connect.
    /// </summary>
    ConnectionError,
  }

  /// <summary>
  /// The status of the remote connection.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  internal struct _RemoteConnectionStatus
  {
    /// <summary>
    /// What state the remote connection is in.
    /// </summary>
    public _RemoteConnectionState State;

    /// <summary>
    /// The error assosiated with the state.
    /// </summary>
    public _RemoteConnectionError Error;
  }
}
