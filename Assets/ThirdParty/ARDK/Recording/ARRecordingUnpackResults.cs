// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// The unpacking results of an AR recording.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARRecordingUnpackResults
  {
    /// <summary>
    /// The unpack destination path.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string UnpackPath;

    /// <summary>
    /// Status of unpacking.
    /// </summary>
    public ARRecordingStatus Status;
  }
}