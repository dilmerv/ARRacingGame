// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.IO;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Research config for recording AR Data and video
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARRecordingResearchConfig
  {
    /// <summary>
    /// The target destination file path for AR data. This should end with .gz
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string TargetARDataPath;

    /// <summary>
    /// A working path for creating TargetARData.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string WorkingDirectory;

    /// <summary>
    /// Configuration for encoding the research video
    /// </summary>
    public AREncodingConfig EncodingConfig;

    /// <summary>
    /// Creates an ARRecordingResearchConfig
    /// </summary>
    /// <param name="encodingConfig">Configuration for encoding the preview video</param>
    /// <param name="targetARDataPath">
    /// The target destination file path for AR data. If not given, uses a random file in the
    /// Application.temporaryCachePath.
    /// </param>
    /// <param name="workingDirectory">
    /// A working directory for creating TargetARData. If not given, uses a random file in the
    /// Application.temporaryCachePath.
    /// </param>
    public ARRecordingResearchConfig(
      AREncodingConfig encodingConfig,
      string targetARDataPath = null,
      string workingDirectory = null)
    {
      EncodingConfig = encodingConfig;
      TargetARDataPath = targetARDataPath ??
        Path.Combine(Application.temporaryCachePath, "target_ar_data");

      WorkingDirectory = workingDirectory ??
        Path.Combine(Application.temporaryCachePath, "ar_working_data");
    }
  }
}
