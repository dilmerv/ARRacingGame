// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Niantic.ARDK.AR;

using UnityEngine;

namespace Niantic.ARDK.AR.Depth
{
  [Serializable]
  internal sealed class _DepthPointCloud : IDepthPointCloud
  {
    internal _DepthPointCloud
    (
      ReadOnlyCollection<Vector3> points,
      UInt32 width,
      UInt32 height
    )
    {
      Points = points;
      Width = width;
      Height = height;
    }

    public ReadOnlyCollection<Vector3> Points { get; }
    public UInt32 Width { get; }
    public UInt32 Height { get; }
    
    void IDisposable.Dispose()
    {
      // Do nothing as this object is fully managed.
    }
  }
}