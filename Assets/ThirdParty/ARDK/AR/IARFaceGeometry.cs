// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using UnityEngine;

namespace Niantic.ARDK.AR
{
  public interface IARFaceGeometry:
    IDisposable
  {
    /// <summary>
    /// An array of vertex positions (x, y, z) for each point in the face mesh.
    /// @remark Each position is in a coordinate system defined by the owning face's anchor's
    /// Transform matrix. E.g. "local" to that face.
    /// </summary>
    ReadOnlyCollection<Vector3> Vertices { get; }

    /// <summary>
    /// A flat buffer of texture coordinate values (u, v) for each point in the face mesh.
    /// @remark Each value at a particular index provides the u, v coordinates for the
    /// corresponding vertex in the Vertices property.
    /// </summary>
    ReadOnlyCollection<Vector2> TextureCoordinates { get; }

    /// <summary>
    /// A buffer of indices describing the triangle mesh formed by the face geometry's vertex data.
    /// @remark Each value represents an index into the Vertices and TextureCoordinates arrays.
    /// Each set of three represent the corners of a triangle in the mesh therefore, the number of
    /// triangles is `TriangleIndices.Length / 3`.
    /// </summary>
    ReadOnlyCollection<Int16> TriangleIndices { get; }

    /// <summary>
    /// An array of normal positions (x, y, z) for each vertex in the face mesh.
    /// @remark These normals are relative to the center pose of the face. 
    /// @note android-only value. Returns null on other platforms.
    /// </summary>
    ReadOnlyCollection<Vector3> Normals { get; }
  }
}
