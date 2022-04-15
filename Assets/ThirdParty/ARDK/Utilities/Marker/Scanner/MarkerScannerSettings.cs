// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Marker
{
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public class MarkerScannerSettings
  {
    // Scanner Options
    public bool ScannerBackgroundThread { get; set; }
    public int ScannerDelayFrameMin { get; set; }

    // This is in seconds
    public float ScannerDecodeInterval { get; set; }

    // Parser Options
    public bool ParserAutoRotate { get; set; }
    public bool ParserTryInverted { get; set; }
    public bool ParserTryHarder { get; set; }

    public MarkerScannerSettings()
    {
      ScannerBackgroundThread = true;
      ScannerDelayFrameMin = 3;
      ScannerDecodeInterval = 0.1f;

      ParserAutoRotate = true;
      ParserTryInverted = true;
      ParserTryHarder = false;

      // Device dependent settings

      // Disable background thread for webgl : Thread not supported
#if UNITY_WEBGL
			ScannerDecodeInterval = 0.5f;
			ScannerBackgroundThread = false;
#endif

      // Enable only for desktop usage : heavy CPU consumption
#if UNITY_STANDALONE || UNITY_EDITOR
      ParserTryHarder = true;
#endif
    }
  }
}
