// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Config for archiving working directories related to ar recording.
  /// Archives temporary AR recording directories into a gzipped .tar
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ArchiveConfig
  {
    /// <summary>
    /// The source directory path to archive.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string SourceDirectoryPath;

    /// <summary>
    /// Target destination archive path.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string DestinationArchivePath;

    /// <summary>
    /// Creates an ArchiveConfig
    /// </summary>
    /// <param name="sourceDirectoryPath">
    /// The source directory path to archive.
    /// </param>
    /// <param name="destinationArchivePath">
    /// Target destination archive path.
    /// </param>
    public ArchiveConfig(string sourceDirectoryPath, string destinationArchivePath)
    {
      SourceDirectoryPath = sourceDirectoryPath;
      DestinationArchivePath = destinationArchivePath;
    }
  }
}