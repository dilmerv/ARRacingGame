// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.ReferenceImage;

namespace Niantic.ARDK.AR.Anchors
{
  /// <summary>
  /// Information about the position and orientation of an image detected in a world-tracking AR
  /// session.
  /// </summary>
  public interface IARImageAnchor:
    IARAnchor
  {
    /// <summary>
    /// The detected image referenced by the image anchor
    /// </summary>
    IARReferenceImage ReferenceImage { get; }
  }
}
