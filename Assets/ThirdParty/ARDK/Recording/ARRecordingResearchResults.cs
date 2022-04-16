// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// The research results of an AR recording.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARRecordingResearchResults
  {
    /// <summary>
    /// A path to the video captured during AR recording. This will usually be an .webm with vp8
    /// encoding.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string VideoPath;

    /// <summary>
    /// A path to the AR data rocksbd database.  This is a directory, and should be archived before
    /// uploading.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string ARDataPath;

    /// <summary>
    /// A comma separated string of peak signal to noise ratios for each frame
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