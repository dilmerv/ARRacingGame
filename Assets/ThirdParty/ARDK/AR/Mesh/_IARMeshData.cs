// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Mesh
{
  /// Represents a dense mesh.
  /// This type of mesh is split into "blocks", which are cubes of equal size each containing the
  ///  vertices and faces (triangles) of the total world mesh between their boundaries.
  /// The mesh is made of:
  ///  - a block size, which is the length of a side of the cube
  ///  - a version number, starting at zero
  ///  - a blocks buffer, indexing the mesh data by area.
  ///  - vertex and face (index) buffers.
  ///
  /// LAYOUT:
  /// The block buffer is a flat array of 32bit integers in a sequence
  /// Each block is defined by 6 back-to-back integers:
  ///  - (X, Y, Z) block coordinates
  ///    (0, 0, 0) is the coordinates of the block containing vertices and faces
  ///     between 0.0 and BLOCK_SIZE on every axis;
  ///    (-1, -1, -1) is the block containing vertices and faces
  ///     between -BLOCK_SIZE and 0.0 on every axis.
  ///  - a vertex count
  ///  - a face count
  ///  - a version number, which is equal to the mesh version for which the block was last updated.
  ///    The version number is to be used by applications to avoid updating unchanged mesh blocks.
  ///
  /// The vertex buffer is a flat array of single precision floats in a sequence.
  /// The first half of the buffer, starting from 0,
  ///  contains all the vertex coordinates back to back:
  ///  X Y Z X Y Z X Y Z X Y Z etc. (3 floats per vertex)
  /// The second half of the buffer, starting from vertexBufferSize/2,
  ///  contains all the vertex normal vectors back to back:
  ///  NX NY NZ NX NY NZ NX NY NZ etc. (3 floats per vertex)
  /// The vertex buffer is organized in the same order as the block buffer.
  ///
  /// The face buffer is a flat array of 32bit integers in a sequence.
  /// Each face is made of three indices, facing only one side (the one you'd see if the three
  ///  vertices are listed in counter-clockwise order).
  /// The indices are global to the whole mesh and do not reset to zero for each block, making it
  ///  easy to consolidate multiple (or all) blocks into a single mesh object.
  ///
  /// EXAMPLE:
  /// In this example, we'll have 2 blocks of 1 meter, containing 3 vertices and 1 face each.
  /// The first block will be at (0, 0, 0) and the second block will be at (1, 0, 0), both
  ///  will start at version 0.
  /// This mesh describes two triangles side by side on a flat plane: ΔΔ
  ///
  /// Block Buffer (12 int32s):
  ///  0 0 0 3 1 0                                // first block
  ///  1 0 0 3 1 0                                // second block
  /// Vertex Buffer (36 floats):
  ///  0.5 0.0 1.0   0.0 0.0 0.0   1.0 0.0 0.0    // first block vertex positions
  ///  1.5 0.0 1.0   1.0 0.0 0.0   2.0 0.0 0.0    // second block vertex positions
  ///  0.0 1.0 0.0   0.0 1.0 0.0   0.0 1.0 0.0    // first block vertex normals
  ///  0.0 1.0 0.0   0.0 1.0 0.0   0.0 1.0 0.0    // second block vertex normals
  /// Face buffer (6 int32s):
  ///  0 1 2                                      // first block face
  ///  3 4 5                                      // second block face
  ///
  /// In live data, blocks are expected to have thousands of vertices and faces each.
  internal interface _IARMeshData:
    IDisposable
  {
    /// The mesh block size, in meters.
    /// It is used to easily find (through Euclidean division) the integer coordinates of a block
    ///  containing any vertex.
    float MeshBlockSize { get; }

    /// Lightweight function to obtain mesh info without pulling all the data.
    /// The function returns a version number, which is to be used by applications to avoid updating
    ///  unchanged meshes.
    /// You have to provide 3 integers as parameters, which will be overwritten, returning the size
    ///  of buffers to be passed to GetBlockMesh().
    /// @param blockBufferSizeOut number of 32bit ints in the block buffer.
    /// @param vertexBufferSizeOut number of single precision floats in the vertex buffer.
    /// @param faceBufferSizeOut number of 32bit ints in the face (index) buffer.
    /// @returns the version number of the mesh starting from 0; -1 if no mesh is available.
    int GetBlockMeshInfo
    (
      out int blockBufferSizeOut,
      out int vertexBufferSizeOut,
      out int faceBufferSizeOut
    );

    /// "Expensive" function to obtain mesh data.
    /// Applications are expected to allocate buffers of the appropriate size (as dictated
    ///  by GetBlockMeshInfo()) and pass pointers to the buffers to this function to receive it.
    /// The native implementation of this function is a memcpy of a few megabytes,
    ///  depending on the mesh size.
    /// @param blockBuffer pointer to the block buffer.
    /// @param vertexBuffer pointer to the vertex buffer.
    /// @param faceBuffer pointer to the face buffer.
    /// @param blockBufferSize size of the block buffer in 32bit integers.
    /// @param vertexBufferSize size of the vertex buffer in single precision floats.
    /// @param faceBufferSize size of the face buffer in 32bit integers.
    /// @returns the number of blocks copied to the buffers (blockBufferSize/6) if the call is
    ///  successful; -1 if no mesh is available or if buffers are not large enough.
    int GetBlockMesh
    (
      IntPtr blockBuffer,
      IntPtr vertexBuffer,
      IntPtr faceBuffer,
      int blockBufferSize,
      int vertexBufferSize,
      int faceBufferSize
    );
  }
}
