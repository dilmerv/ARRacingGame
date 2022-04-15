// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Configuration
{
  [Flags]
  public enum DepthFeatures
  {
    None = 0,

    /// Basic depth estimation. Will result in a DisparityBuffer provided alongside ARFrame objects
    Depth = 1,

    /// Transforms DisparityBuffer frames into a set of world space points, stored in ARFrame
    PointCloud = 2,
  }
}
