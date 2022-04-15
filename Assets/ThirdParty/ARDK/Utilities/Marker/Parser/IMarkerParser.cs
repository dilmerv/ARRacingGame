// Copyright 2021 Niantic, Inc. All Rights Reserved.

using UnityEngine;

namespace Niantic.ARDK.Utilities.Marker
{
  public interface IMarkerParser
  {
    /// <summary>
    /// Process and try to obtain marker from the given image.
    /// </summary>
    /// <param name="pixels">Array of pixels of the image to process.</param>
    /// <param name="width">Width of the image to process.</param>
    /// <param name="height">Height of the image to process.</param>
    /// <param name="parserResult">Information about the marker, if one was parsed from the image.</param>
    /// <returns>True if a barcode was found.</returns>
    bool Decode
    (
      Color32[] pixels,
      int width,
      int height,
      out IParserResult parserResult
    );
  }
}
