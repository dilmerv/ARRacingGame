// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Configs for recording AR.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct ARRecorderConfig
  {
    /// <summary>
    /// The framerate to capture video at.
    /// </summary>
    public uint Framerate;

    /// <summary>
    /// The target destination folder path for intermediate recording data
    /// This is a temporary path for unprocessed data and should be will be deleted
    /// after all processing is complete.
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string WorkingPath;

    public const uint DEFAULT_FRAMERATE = 6;

    /// <summary>
    /// Creates an ARRecorderConfig, with helpers to provide defaults.
    /// </summary>
    /// <param name="framerate">The framerate to record at, defaults to DEFAULT_FRAMERATE.</param>
    /// <param name="workingPath">
    /// The target destination folder path for intermediate recording data.
    /// This is a temporary path for unprocessed data and should be will be deleted
    /// after all processing is complete.
    /// If not given, uses a random file in the Application.temporaryCachePath.
    /// </param>
    /// @note If this struct with all default values is desired, use the Default property instead.
    public ARRecorderConfig(uint framerate = DEFAULT_FRAMERATE, string workingPath = null)
    {
      Framerate = framerate;
      WorkingPath = workingPath;
    }

    public static ARRecorderConfig Default
    {
      get
      {
        return new ARRecorderConfig
        {
          Framerate = DEFAULT_FRAMERATE,
          WorkingPath = null
        };
      }
    }
  }
}
