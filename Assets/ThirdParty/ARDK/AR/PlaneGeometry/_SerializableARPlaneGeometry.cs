// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDK.AR.PlaneGeometry
{
  [Serializable]
  internal sealed class _SerializableARPlaneGeometry:
    IARPlaneGeometry
  {
    internal _SerializableARPlaneGeometry
    (
      ReadOnlyCollection<Vector3> vertices,
      ReadOnlyCollection<Vector2> textureCoordinates,
      ReadOnlyCollection<short> triangleIndices,
      ReadOnlyCollection<Vector3> boundaryVertices
    )
    {
      Vertices = vertices;
      TextureCoordinates = textureCoordinates;
      TriangleIndices = triangleIndices;
      BoundaryVertices = boundaryVertices;
    }

    public ReadOnlyCollection<Vector3> Vertices { get; private set; }
    public ReadOnlyCollection<Vector2> TextureCoordinates { get; private set; }
    public ReadOnlyCollection<short> TriangleIndices { get; private set; }
    public ReadOnlyCollection<Vector3> BoundaryVertices { get; private set; }

    void IDisposable.Dispose()
    {
      // Do nothing as this object is fully managed.
    }
  }
}
