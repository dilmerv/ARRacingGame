// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities.Marker;

using UnityEngine;

using ZXing;

namespace Niantic.ARDK.Utilities.QR
{
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public class ZXingBarcodeParser:
    IMarkerParser
  {
    private readonly BarcodeReader _reader;

    public ZXingBarcodeParser():
      this(new MarkerScannerSettings())
    {
    }

    public ZXingBarcodeParser(MarkerScannerSettings settings)
    {
      _reader = new BarcodeReader();
      _reader.AutoRotate = settings.ParserAutoRotate;
      _reader.TryInverted = settings.ParserTryInverted;
      _reader.Options.TryHarder = settings.ParserTryHarder;
    }

    public bool Decode
    (
      Color32[] pixels,
      int width,
      int height,
      out IParserResult parserResult
    )
    {
      parserResult = new BarcodeParserResult();

      if (pixels == null || pixels.Length == 0 || width == 0 || height == 0)
        return false;

      var success = false;
      try
      {
        var result = _reader.Decode(pixels, width, height);

        if (result != null)
        {
          var vectorPoints = new Vector2[4];

          for (var i = 0; i < vectorPoints.Length; ++i)
          {
            var resultPoint = result.ResultPoints[i];
            vectorPoints[i] = new Vector2(resultPoint.X, resultPoint.Y);
          }

          parserResult.Data = Convert.FromBase64String(result.Text);
          parserResult.DetectedPoints = vectorPoints;

          success = true;
        }
      }
      catch (Exception e)
      {
        Debug.Log(e);
      }

      return success;
    }
  }
}
