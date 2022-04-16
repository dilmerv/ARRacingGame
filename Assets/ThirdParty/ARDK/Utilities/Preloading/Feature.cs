// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Preloading
{
  public enum Feature
  {
    /// Used by computer vision algorithms to generate semantic segmentation, depth, and mesh data.
    ContextAwareness = 0,

    /// Used by computer vision algorithms to synchronize multiple devices to the
    /// same coordinate space.
    Dbow = 1
  }
}