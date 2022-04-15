// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDK.AR.PointCloud
{
  [Serializable]
  internal sealed class _SerializableARPointCloud:
    IARPointCloud
  {
    internal _SerializableARPointCloud
    (
      ReadOnlyCollection<Vector3> points,
      ReadOnlyCollection<UInt64> identifiers,
      float worldScale
    )
    {
      Points = points;
      Identifiers = identifiers;
      WorldScale = worldScale;
    }

    public ReadOnlyCollection<Vector3> Points { get; private set; }
    public ReadOnlyCollection<UInt64> Identifiers { get; private set; }
    public float WorldScale { get; private set; }

    void IDisposable.Dispose()
    {
      // Do nothing as this object is fully managed.
    }
  }
}
