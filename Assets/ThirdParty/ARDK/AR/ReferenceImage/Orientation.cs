// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.ReferenceImage
{
  /// Value describing the intended display orientation for an image.
  public enum Orientation
  {
    /// The encoded image data matches the image's intended display orientation.
    Up = 1,

    /// The encoded image data is horizontally flipped from the image's intended 
    /// display orientation.
    UpMirrored = 2,

    /// The encoded image data is rotated 180º from the image's intended display orientation.
    Down = 3,

    /// The encoded image data is vertically flipped rotated from the image's intended 
    /// display orientation.
    DownMirrored = 4,

    /// The encoded image data is horizontally flipped and rotated 90º counter-clockwise from
    ///  the image's intended display orientation.
    LeftMirrored = 5,

    /// The encoded image data is rotated 90º clockwise from the image's intended display
    ///  orientation.
    Right = 6,

    /// The encoded image data is horizontally flipped and rotated 90º clockwise from the 
    /// image's intended display orientation.
    RightMirrored = 7,

    /// The encoded image data is rotated 90º clockwise from the image's intended display 
    /// orientation.
    Left = 8,
  }
}
