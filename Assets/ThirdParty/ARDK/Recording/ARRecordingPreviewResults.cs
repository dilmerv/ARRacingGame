// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// The preview results of an AR recording.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARRecordingPreviewResults
  {
    /// <summary>
    /// A path to the video captured during AR recording. This will usually be an .webm with vp8
    /// encoding.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string VideoPath;

    /// <summary>
    /// A comma separated string of peak signal to noise ratios for each frame.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string PSNR;

    /// <summary>
    /// Status of the preview results.  Used to detect failures.  Status::Completed indicates
    /// success.
    /// </summary>
    public ARRecordingStatus Status;
  }
}