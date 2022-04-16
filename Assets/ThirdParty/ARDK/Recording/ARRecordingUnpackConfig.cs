// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Configs for unpacking the frames to a directory.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARRecordingUnpackConfig
  {
    /// <summary>
    /// The target destination file path for AR data. This should end with .gz
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string UnpackDestination;
  }
}