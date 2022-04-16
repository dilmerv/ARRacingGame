using System.Collections.Generic;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Mesh
{
  /// Represents an updating dense mesh of the environment, split into "blocks." Blocks are
  /// updated as new areas are scanned and/or ARDK refines its understanding of already
  /// scanned areas.
  public interface IARMesh
  {
    /// Informs subscribers whenever mesh blocks have been added, removed, and/or their geometry has
    /// been updated.
    event ArdkEventHandler<MeshBlocksUpdatedArgs> MeshBlocksUpdated;

    /// Informs subscribers whenever the mesh has been cleared, such as when the session is
    /// re-run with the option to clear the mesh.
    event ArdkEventHandler<MeshBlocksClearedArgs> MeshBlocksCleared;

    /// The collection of blocks that make up the mesh. Each block is of equal size and has
    /// self-contained arrays of vertices and faces (triangles).
    IReadOnlyDictionary<Vector3Int, MeshBlock> Blocks { get; }

    /// Size of a mesh block in meters in the last parsed mesh.
    float MeshBlockSize { get; }

    /// Version of the last parsed mesh.
    int MeshVersion { get; }

    /// Number of blocks in the last parsed mesh. Each block is represented
    /// by a separate GameObject, which is an instance of _meshPrefab.
    int MeshBlockCount { get; }

    /// Number of vertices in the most recently parsed mesh.
    int MeshVertexCount { get; }

    /// Number of faces (polygons) in the last parsed mesh.
    int MeshFaceCount { get; }
  }
}
