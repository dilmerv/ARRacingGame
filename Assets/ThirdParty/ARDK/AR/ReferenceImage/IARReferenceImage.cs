// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;

namespace Niantic.ARDK.AR.ReferenceImage
{
  /// @brief An image to be recognized in the real-world environment during a world-tracking AR
  /// session.
  /// @note The ARReferenceImage will contain the full image buffer until it is destroyed.
  ///   Unless reuse of the constructed ARReferenceImage is required in the near future, it is
  ///   recommended to destroy images after adding them to a configuration.
  public interface IARReferenceImage:
    IDisposable
  {
    /// <summary>
    /// A name for the image.
    /// @note Limited to 25 characters on native
    /// </summary>
    string Name { get; set; }

    /// The real-world dimensions [width, height], in meters, of the image.
    /// @note On Android, this may initially be 0,0 when multiple images are being detected, as
    ///   ARCore attempts to estimate the physical size of the image. This will happen even if a
    ///   physical size is used to construct the ARReferenceImage.
    Vector2 PhysicalSize { get; }
  }
}
