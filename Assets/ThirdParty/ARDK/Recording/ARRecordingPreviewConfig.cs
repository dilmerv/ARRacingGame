// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Preview config for recordings
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARRecordingPreviewConfig
  {
    /// <summary>
    /// Configuration for encoding the preview video
    /// </summary>
    public AREncodingConfig EncodingConfig;

    /// <summary>
    /// Creates an ARRecordingPreviewConfig
    /// </summary>
    /// <param name="encodingConfig">Configuration for encoding the preview video</param>
    public ARRecordingPreviewConfig(AREncodingConfig encodingConfig)
    {
      EncodingConfig = encodingConfig;
    }
  }
}