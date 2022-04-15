// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Runtime.InteropServices;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  ///   Configs for encoding AR captured videos.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct AREncodingConfig
  {
    /// <summary>
    ///   The format to encode the normal video into. If none, no file will be produced.
    /// </summary>
    public RecordingVideoFormat VideoFormat;

    /// <summary>
    ///   Capturing will only capture the last N-Seconds worth of video in this time window.
    /// </summary>
    public uint WindowTime;

    /// <summary>
    ///   The target destination to the video file. This should end with .webm
    /// </summary>
    [MarshalAs(UnmanagedType.LPStr)]
    public string TargetPath;

    /// <summary>
    ///   Number of threads to use for encoding.  -1 will use the number of available cores
    /// </summary>
    public int NumThreads;

    /// <summary>
    ///   Time to spend encoding each frame, in microseconds. (0=infinite)
    /// </summary>
    public uint EncodingDeadline;

    /// <summary>
    ///   Target video bitrate
    /// </summary>
    public uint TargetBitrateKbits;

    /// <summary>
    ///   Whether or not to calculate peak signal to noise ratio for each frame
    ///   May have a performance impact on memory and CPU
    /// </summary>
    /// @note Functionally a bool (and used as such by the CalculatePSNR property), but must
    /// be a byte to match native implementation
    private byte _calculatePSNR;

    public bool CalculatePSNR
    {
      get
      {
        return _calculatePSNR == 1;
      }
    }

    public const uint DEFAULT_VIDEO_WINDOW_TIME = 10;

    /// <summary>
    ///   Creates an ARRecorderConfig, with helpers to provide defaults.
    /// </summary>
    /// <param name="videoFormat">The format to encode the video in, defaults to VP8.</param>
    /// <param name="windowTime">The window of video time, defaults to DEFAULT_VIDEO_WINDOW_TIME.</param>
    /// <param name="targetPath">
    ///   The location to put the encoded video. If not given, uses a random file in the
    ///   Application.temporaryCachePath
    /// </param>
    /// <param name="encodingDeadline"> Time to spend encoding each frame, in microseconds (0 = infinite).</param>
    /// <param name="numThreads">
    ///   Number of threads to use for encoding. Defaults to -1 (or all available
    ///   cores).
    /// </param>
    /// <param name="targetBitrateKbits">Target video bitrate.  Defaults to some huge number.</param>
    /// <param name="calculatePSNR">Whether or not to calculate peak signal to noise ratio</param>
    /// @note If this struct with all default values is desired, use the Default property instead.
    public AREncodingConfig(
      RecordingVideoFormat videoFormat = RecordingVideoFormat.VP8,
      uint windowTime = DEFAULT_VIDEO_WINDOW_TIME,
      string targetPath = null,
      int numThreads = -1,
      uint encodingDeadline = 0,
      uint targetBitrateKbits = 100000,
      bool calculatePSNR = false)
    {
      VideoFormat = videoFormat;
      WindowTime = windowTime;
      TargetPath = targetPath;
      NumThreads = numThreads;
      EncodingDeadline = encodingDeadline;
      TargetBitrateKbits = targetBitrateKbits;
      _calculatePSNR = calculatePSNR ? (byte)1 : (byte)0;
    }

    public static AREncodingConfig Default
    {
      get
      {
        return
          new AREncodingConfig
          {
            VideoFormat = RecordingVideoFormat.VP8,
            WindowTime = DEFAULT_VIDEO_WINDOW_TIME,
            TargetPath = null,
            NumThreads = -1,
            EncodingDeadline = 0,
            TargetBitrateKbits = 100000,
            _calculatePSNR = 0
          };
      }
    }
  }
}
