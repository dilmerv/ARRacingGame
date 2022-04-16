// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.ReferenceImage
{
  /// <summary>
  /// Defines the order of data per pixel of an image.
  ///
  /// @note: Although this sounds like it handles all four bytes, the position of that alpha data
  /// is defined by the AlphaInfo, so this actually only handles the order of the RGB data.
  /// </summary>
  public enum ByteOrderInfo:
    uint
  {
    /// The bytes in an image are stored in 32-bit little-endian format. e.g. for an ARGB image,
    /// the data will look like BGRA.
    little32 = 1,

    /// The bytes in an image are stored in 32-bit big-endian format. e.g. for an ARGB image,
    /// the data will look like ARGB.
    big32 = 2
  }
}
