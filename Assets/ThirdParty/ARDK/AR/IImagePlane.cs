// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Unity.Collections;

namespace Niantic.ARDK.AR
{
  /// <summary>
  /// Represents a plane of an IImageBuffer.
  /// </summary>  
  public interface IImagePlane
  {
    /// <summary>
    /// Returns a native array of bytes of the current plane.
    /// Users are not supposed to modify this Data, yet it is returned as a basic array of bytes
    /// so Buffer.BlockCopy and similar operations can be used on it.
    /// </summary>
    NativeArray<byte> Data { get; }

    int PixelWidth { get; }
    int PixelHeight { get; }

    /// <summary>
    /// Returns the bytes per row of the current plane.
    /// @remark Also referred to as row-stride.
    /// </summary>
    int BytesPerRow { get; }

    /// <summary>
    /// Returns the bytes per pixel of the current plane.
    /// @remark Also referred to as pixel-stride.
    /// </summary>
    int BytesPerPixel { get; }
  }
}
