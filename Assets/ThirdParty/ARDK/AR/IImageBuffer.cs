// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR
{
  /// <summary>
  /// Interface for implementations of an Image Buffer
  /// </summary>
  public interface IImageBuffer:
    IDisposable
  {
    /// <summary>
    /// The format of the image. See ARDK.AR.ImageFormat.
    /// </summary>
    ImageFormat Format { get; }

    /// <summary>
    /// Access the collection of planes that this IImageBuffer manages.
    /// </summary>
    IImagePlanes Planes { get; }
  }
}
