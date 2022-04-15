// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// Info about the local device.
  /// </summary>
  /// <remarks>
  /// This structure matches its native counter part.
  /// </remarks>
  [StructLayout(LayoutKind.Explicit)]
  internal struct _LocalDeviceInfo
  {
    /// <summary>
    /// The handle of the local device.
    /// </summary>
    [FieldOffset(0)]
    public _DeviceHandle localHandle;
  }
}
