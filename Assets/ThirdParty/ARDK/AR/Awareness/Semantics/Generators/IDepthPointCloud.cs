// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDK.AR.Depth
{
  /// <summary>
  /// Represents a depth point cloud.
  /// </summary>
  public interface IDepthPointCloud:
    IDisposable
  {
    /// <summary>
    /// The collection of world-space depth points.
    /// </summary>
    ReadOnlyCollection<Vector3> Points { get; }

    /// The width of the depth point cloud, from the camera's perspective
    UInt32 Width { get; }
    
    /// The height of the depth point cloud, from the camera's perspective
    UInt32 Height { get; }
  }
}
