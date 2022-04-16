// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.ReferenceImage
{
  /// <summary>
  /// Defines the position of the alpha data in each pixel of the image
  /// </summary>
  public enum AlphaInfo: uint
  {
    /// The encoded image data does not contain any alpha data.
    None = 1,

    /// The alpha data is stored in the most significant bits.
    First = 2,

    /// The alpha data is stored in the least significant bits.
    Last = 3,
  }
}
