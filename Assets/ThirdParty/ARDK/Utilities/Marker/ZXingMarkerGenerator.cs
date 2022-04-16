// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

using ZXing;

namespace Niantic.ARDK.Utilities.Marker
{
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public static class ZXingMarkerGenerator
  {
    public class MarkerGenerationResult
    {
      public Texture2D Texture { get; private set; }
      public Color32[] RawPixels { get; private set; }

      public string EncodedText { get; private set; }

      public MarkerGenerationResult(Texture2D texture, Color32[] pixels, string encodedText)
      {
        Texture = texture;
        RawPixels = pixels;
        EncodedText = encodedText;
      }
    }

    private const float INCH_TO_METER = 0.0254f;

    public static MarkerGenerationResult GenerateBarcode
    (
      MarkerMetadata metadata,
      BarcodeFormat format,
      int width,
      int height
    )
    {
      var data = metadata.Serialize();

      // Pad data buffer
      var remainder = data.Length % 4;
      var paddedData = new byte[data.Length + remainder];
      data.CopyTo(paddedData, 0);

      for (var i = 0; i < remainder; ++i)
        paddedData[data.Length + i] = (byte)'=';

      // Generate the BitMatrix
      var encodedText = Convert.ToBase64String(paddedData);
      var bitMatrix = new MultiFormatWriter().encode(encodedText, format, width, height);

      // Generate the pixel array
      var texPixels = new Color[width * height];
      var parserPixels = new Color32[texPixels.Length];
      var pos = 0;

      for (var x = 0; x < width; x++)
        for (var y = 0; y < height; y++)
          texPixels[pos++] = bitMatrix[x, y] ? Color.black : Color.white;

      // Rotate 180
      var orientedPixels =
        ImageDataUtils.Transform
        (
          texPixels,
          ImageDataUtils.TransformType.RotateCounterclockwise,
          width,
          height
        );

      for (var index = 0; index < orientedPixels.Length; index++)
        parserPixels[index] = orientedPixels[index];

      // Setup the Texture
      var tex = new Texture2D(width, height);
      tex.SetPixels(orientedPixels);
      tex.Apply();

      var generatorResult = new MarkerGenerationResult(tex, parserPixels, encodedText);

      var resultLog = "Generated QR code using the ZXing library.";

      if (metadata.Source == MarkerMetadata.MarkerSource.Stationary)
      {
        resultLog +=
          string.Format
          (
            " To generate a QR code for stationary display, use this site: {0} with this string: {1}",
            "https://zxing.appspot.com/generator",
            generatorResult.EncodedText
          );
      }

      Debug.Log(resultLog);

      return generatorResult;
    }

    public static Vector3[] GetRealWorldPointPositions(Vector2 center, Vector2[] points)
    {
      var locatorDistToAxis = Math.Abs(points[0].x - center.x);
      var alignmentDistToAxis = Math.Abs(points[3].x - center.x);

      var screenWidthInMeters = Screen.width / Screen.dpi * INCH_TO_METER;
      var pixelSize = screenWidthInMeters / Screen.width;

      var botLeft =
        new Vector3
        (
          -locatorDistToAxis * pixelSize,
          locatorDistToAxis * pixelSize,
          0
        );

      var topLeft =
        new Vector3
        (
          -locatorDistToAxis * pixelSize,
          -locatorDistToAxis * pixelSize,
          0
        );

      var topRight =
        new Vector3
        (
          locatorDistToAxis * pixelSize,
          -locatorDistToAxis * pixelSize,
          0
        );

      var alignment =
        new Vector3
        (
          alignmentDistToAxis * pixelSize,
          alignmentDistToAxis * pixelSize,
          0
        );

      return new[] {botLeft, topLeft, topRight, alignment};
    }
  }
}
