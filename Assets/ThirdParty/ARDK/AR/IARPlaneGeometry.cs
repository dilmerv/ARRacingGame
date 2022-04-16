// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDK.AR
{
  public interface IARPlaneGeometry:
    IDisposable
  {
    /// <summary>
    /// An array of vertex positions (x, y, z) for each point in the plane mesh.
    /// @remark Each position is in a coordinate system defined by the owning plane anchor's
    /// Transform matrix. E.g. "local" to that plane.
    /// @note iOS-only value.
    /// </summary>
    ReadOnlyCollection<Vector3> Vertices { get; }

    /// <summary>
    /// A flat buffer of texture coordinate values (u, v) for each point in the plane mesh.
    /// @remark Each value at a particular index provides the u, v coordinates for the
    /// corresponding vertex in the Vertices property.
    /// @note iOS-only value.
    /// </summary>
    ReadOnlyCollection<Vector2> TextureCoordinates { get; }

    /// <summary>
    /// A buffer of indices describing the triangle mesh formed by the plane geometry's vertex data.
    /// @remark Each value represents an index into the Vertices and TextureCoordinates arrays.
    /// Each set of three represent the corners of a triangle in the mesh therefore, the number of
    /// triangles is `TriangleIndices.Length / 3`.
    /// @note iOS-only value.
    /// </summary>
    ReadOnlyCollection<Int16> TriangleIndices { get; }

    /// <summary>
    /// An array of vertex positions (x, y, z) for each point along the plane's boundary.
    /// @remark Each position is in a coordinate system defined by the owning plane anchor's
    /// Transform matrix. E.g. "local" to that plane.
    /// @remark The vertices only represent a boundary so this property is only useful if you're
    /// interested in the general shape or outline of the plane.
    /// </summary>
    ReadOnlyCollection<Vector3> BoundaryVertices { get; }
  }
}
