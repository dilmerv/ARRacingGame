// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDK.AR
{
  /// <summary>
  /// Represents a point cloud.
  /// </summary>
  public interface IARPointCloud:
    IDisposable
  {
    /// <summary>
    /// The collection of detected points.
    /// </summary>
    ReadOnlyCollection<Vector3> Points { get; }

    /// <summary>
    /// The collection of unique identifiers corresponding to detected feature points.
    /// </summary>
    ReadOnlyCollection<UInt64> Identifiers { get; }

    /// <summary>
    /// The scaling factor applied to the point cloud's points.
    /// </summary>
    float WorldScale { get; }
  }
}
