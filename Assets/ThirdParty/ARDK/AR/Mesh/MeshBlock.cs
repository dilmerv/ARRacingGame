using Unity.Collections;

using UnityEngine;

namespace Niantic.ARDK.AR.Mesh
{
  /// A cube-shaped portion of the mesh of the environment.
  public class MeshBlock
  {
    /// The version number of this specific block.
    public int Version { get; internal set; } = -1;

    /// The version of the mesh that this block was last updated from.
    public int MeshVersion { get; internal set; } = -1;

    /// The last block-specific Version that this block had its collider updated at.
    /// Not set by the provider, besides to leave it at -1 initially. The consumer then uses this to
    /// track their own work.
    public int ColliderVersion { get; internal set; } = -1;

    /// The vertices associated with this mesh block. This array will only be valid until the next
    /// frame when the _MeshDataParser updates, when this value will also be updated to once again
    /// point to a valid array. If the mesh as a whole updated but this block didn't, then there
    /// won't be an associated _MeshBlockParser.MeshBlockUpdated call but this value will still
    /// update to point to a valid array with the same contents. It is strongly recommended never to
    /// store these values, and always access them through the MeshBlock. If you do need access to
    /// old values you need to create a managed copy of the array.
    /// If the _MeshDataParser is not configured to provide block data, or if it has been Disposed,
    /// this will be a default NativeArray whose IsCreated property will be false.
    public NativeArray<Vector3> Vertices;

    /// The normals associated with this mesh block, sharing indices with the Vertices array. It is
    /// valid when the Vertices array is valid, see that variable's comments for details.
    public NativeArray<Vector3> Normals;

    /// The triangle indices associated with this mesh block. Each triple of values are 3 indices
    /// into the Vertices/Normals arrays defining a triangle. It is valid when the Vertices array is
    /// valid, see that variable's comments for details.
    public NativeArray<int> Triangles;

    /// The Unity mesh created from the Vertices, Normals, and Triangles data. This value will
    /// be null until set by a consumer of the MeshBlocks provided by IARSession.Mesh,
    /// such as the ARMeshManager component.
    public UnityEngine.Mesh Mesh;

    internal void ClearArrays()
    {
      Vertices = new NativeArray<Vector3>();
      Normals = new NativeArray<Vector3>();
      Triangles = new NativeArray<int>();
    }
  }
}

