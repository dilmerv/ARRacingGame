// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// The roles that remote device can be in.
  /// </summary>
  internal enum _RemoteDeviceRole:
    byte
  {
    /// <summary>
    /// No role.
    /// </summary>
    None,

    /// <summary>
    /// The device is running the application.
    /// </summary>
    Application,

    /// <summary>
    /// The device is running the remote sessions.
    /// </summary>
    Remote,
  }
}
