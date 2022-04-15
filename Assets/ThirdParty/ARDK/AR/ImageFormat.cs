// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR
{
  /// Possible formats for an ImageBuffer.
  public enum ImageFormat
  {
    /// Single plane, grayscale image.
    Grayscale = 0,

    /// Single plane, BGRA image.
    RGB = 1,

    /// Single plane, BGRA image.
    RGBA = 2,

    /// Single plane, BGRA image.
    BGRA = 3,

    /// Dual plane, YCbCr (4:2:0) image.
    /// The YCbCr NV12 format, with Cb(U) coming before Cr(V) in each pair on the UV plane
    YCbCr12 = 4,

    /// Dual plane, YCbCr (4:2:0) image.
    /// The YCbCr NV21 format, with Cr(V) coming before Cb(U) in each pair on the UV plane
    YCbCr21 = 5,

    Incomplete = 6,
  }
}
