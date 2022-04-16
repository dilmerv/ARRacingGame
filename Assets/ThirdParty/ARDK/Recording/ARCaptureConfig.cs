// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Configs for AR captures.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARCaptureConfig
  {
    /// <summary>
    /// The target destination folder path for capture data.
    /// This working directory will contain individual frames as JPEG files.
    /// It will be created by the native API as soon as the capture is started.
    /// The client is responsible for deleting this folder after the capture ends.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string WorkingDirectoryPath;

    /// <summary>
    /// The target destination file path for the archived capture data.
    /// This is the path to the archive file that will be created by the native API
    /// as soon as the capture is stopped.
    /// The client is responsible for deleting the file at this path after it is
    /// uploaded.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string ArchivePath;

    [MarshalAs(UnmanagedType.Bool)]
    public bool CaptureLidarIfAvailable;

    /// <summary>
    /// Creates an ARCaptureConfig, with helpers to provide defaults.
    /// </summary>
    /// <param name="workingPath">
    /// The target destination folder path for capture data.
    /// This working directory will contain individual frames as JPEG files.
    /// It will be created by the native API as soon as the capture is started.
    /// The client is responsible for deleting this folder after the capture ends.
    /// If null, the recorder picks a working path and deletes it after archiving.
    /// </param>
    /// <param name="archivePath">
    /// The target destination file path for the archived capture data.
    /// This is the path to the archive file that will be created by the native API
    /// as soon as the capture is stopped. The file must end with ".tgz" and be in
    /// a valid path.
    /// The client is responsible for deleting the file at this path after it is
    /// uploaded.
    /// If not given, appends ".tgz" to the working path.
    /// </param>
    /// @note If this struct with all default values is desired, use the Default property instead.
    public ARCaptureConfig(string workingPath = null, string archivePath = null)
    {
      WorkingDirectoryPath = workingPath;
      ArchivePath = archivePath;
      CaptureLidarIfAvailable = false;
    }

    public static ARCaptureConfig Default
    {
      get
      {
        return new ARCaptureConfig
        {
          WorkingDirectoryPath = null,
          ArchivePath = null,
          CaptureLidarIfAvailable = false
        };
      }
    }
  }
}
