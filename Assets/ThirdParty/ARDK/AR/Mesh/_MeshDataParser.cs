// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Extensions.Meshing;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace Niantic.ARDK.AR.Mesh
{
  internal class _MeshDataParser: IARMesh, IDisposable
  {
    /// Size of a mesh block in meters in the last parsed mesh.
    public float MeshBlockSize { get; private set; }

    /// Version of the last parsed mesh.
    public int MeshVersion { get; private set; }

    /// Number of blocks in the last parsed mesh. Each block is represented
    /// by a separate GameObject, which is an instance of _meshPrefab.
    public int MeshBlockCount { get; private set; }

    /// Number of vertices in the most recently parsed mesh.
    public int MeshVertexCount { get; private set; }

    /// Number of faces (polygons) in the last parsed mesh.
    public int MeshFaceCount { get; private set; }

    // buffers
    private NativeArray<int> _blockArray;
    private NativeArray<int> _faceArray;
    private NativeArray<int> _unshiftedFaceArray;
    private NativeArray<float> _vertexArray;

    private Dictionary<Vector3Int, MeshBlock> _blocks = new Dictionary<Vector3Int, MeshBlock>();

    public event ArdkEventHandler<MeshBlocksUpdatedArgs> MeshBlocksUpdated;
    public event ArdkEventHandler<MeshBlocksClearedArgs> MeshBlocksCleared;

    public IReadOnlyDictionary<Vector3Int, MeshBlock> Blocks { get; }

    public _MeshDataParser()
    {
      Blocks = new ReadOnlyDictionary<Vector3Int, MeshBlock>(_blocks);
    }

    /// Processes the raw data in the given mesh and updates mesh blocks if needed.
    public void ParseMesh(_IARMeshData meshData)
    {
      var version =
        meshData.GetBlockMeshInfo
        (
          out int blockBufferSize,
          out int vertexBufferSize,
          out int faceBufferSize
        );

      var canUpdate =
        blockBufferSize > 0 &&
        vertexBufferSize > 0 &&
        faceBufferSize > 0;

      if (canUpdate)
      {
        // Update mesh info
        MeshBlockSize = meshData.MeshBlockSize;
        MeshVersion = version;
        MeshBlockCount = blockBufferSize / ARMeshConstants.INTS_PER_BLOCK;
        MeshVertexCount = vertexBufferSize / ARMeshConstants.FLOATS_PER_VERTEX;
        MeshFaceCount = faceBufferSize / ARMeshConstants.INTS_PER_FACE;

        ResizeArraysIfNeeded(blockBufferSize, vertexBufferSize, faceBufferSize);
        UpdateMeshBlocks(meshData, blockBufferSize, vertexBufferSize, faceBufferSize);
      }
      else
      {
        ARLog._Warn("Invalid mesh data length.");
      }
    }

    // Recreate native buffers if the new mesh contains more blocks
    private void ResizeArraysIfNeeded(int blockBufferSize, int vertexBufferSize, int faceBufferSize)
    {
      if (!_blockArray.IsCreated)
      {
        // If the block array isn't created, that means all arrays weren't created

        _blockArray = new NativeArray<int>(blockBufferSize * 2, Allocator.Persistent);
        _vertexArray = new NativeArray<float>(vertexBufferSize * 2, Allocator.Persistent);
        _faceArray = new NativeArray<int>(faceBufferSize * 2, Allocator.Persistent);
        _unshiftedFaceArray = new NativeArray<int>(faceBufferSize * 2, Allocator.Persistent);
        return;
      }

      if (blockBufferSize > _blockArray.Length)
      {
        _blockArray.Dispose();
        _blockArray = new NativeArray<int>(blockBufferSize * 2, Allocator.Persistent);
      }

      if (vertexBufferSize > _vertexArray.Length)
      {
        _vertexArray.Dispose();
        _vertexArray = new NativeArray<float>(vertexBufferSize * 2, Allocator.Persistent);
      }

      if (faceBufferSize > _faceArray.Length)
      {
        _faceArray.Dispose();
        _faceArray = new NativeArray<int>(faceBufferSize * 2, Allocator.Persistent);

        _unshiftedFaceArray.Dispose();
        _unshiftedFaceArray = new NativeArray<int>(faceBufferSize * 2, Allocator.Persistent);
      }
    }

    // Scratch lists for both the updated and obsolete blocks used by UpdateMeshBlocks.
    private readonly List<Vector3Int> _updatedBlocksScratch = new List<Vector3Int>();
    private readonly List<Vector3Int> _obsoleteBlocksScratch = new List<Vector3Int>();

    // Obtains the mesh buffers and parses them, updating individual mesh blocks if their
    // version number is newer than the current one.
    // Raises the MeshBlocksUpdated event if mesh objects were successfully updated
    private void UpdateMeshBlocks
    (
      _IARMeshData meshData,
      int blockBufferSize,
      int vertexBufferSize,
      int faceBufferSize
    )
    {
      // In the unlikely event that a race condition occured and the buffers are not the right size,
      // just skip this update and wait for the next one.
      if (!GetBlocksAndValidateSize(meshData, blockBufferSize, vertexBufferSize, faceBufferSize))
        return;

      _updatedBlocksScratch.Clear();

      int verticesStart = 0;
      int normalsStart = vertexBufferSize / 2; // normals start halfway through the buffer
      int facesStart = 0;

      // Update all the full blocks returned by the API
      const int floatsPerVertexVector = ARMeshConstants.FLOATS_PER_VERTEX / ARMeshConstants.VECTORS_PER_VERTEX;
      for (int b = 0; b < blockBufferSize; b += ARMeshConstants.INTS_PER_BLOCK)
      {
        var currentBlock =
          GetOrCreateBlockWithInfo
          (
            b,
            out Vector3Int blockCoords,
            out int vertexCount, // number of vertices (each consisting of 3 floats)
            out int faceCount,   // number of faces (each consisting of 3 floats)
            out int blockVersion
          );

        currentBlock.MeshVersion = MeshVersion;

        // If a block was obsoleted, all blocks' views into the block/face/vertex native arrays,
        // have to be updated. It's only updating alias' into existing data, so it's not too
        // terribly inefficient.

        // Turn number of vectors into number of floats
        var numFloatsForAllVectors = vertexCount * floatsPerVertexVector;
        var numFloatsForAllFaces = faceCount * ARMeshConstants.INTS_PER_FACE;

        // Have to update geometry for every block in case obsoleted blocks
        // have affected the block offsets in the native arrays.
        UpdateBlockGeometry
        (
          currentBlock,
          verticesStart,
          numFloatsForAllVectors,
          normalsStart,
          numFloatsForAllVectors,
          facesStart,
          numFloatsForAllFaces
        );

        // Only surface block update in event if it actually updated
        if (currentBlock.Version < blockVersion)
        {
          currentBlock.Version = blockVersion;
          _updatedBlocksScratch.Add(blockCoords);
        }

        verticesStart += numFloatsForAllVectors;
        normalsStart += numFloatsForAllVectors;
        facesStart += numFloatsForAllFaces;
      }

      // Clean up obsolete blocks
      RemoveObsoleteBlocks(_obsoleteBlocksScratch);

      MeshBlocksUpdated?.Invoke
      (
        new MeshBlocksUpdatedArgs(_updatedBlocksScratch, _obsoleteBlocksScratch, this)
      );
    }

    private bool GetBlocksAndValidateSize
    (
      _IARMeshData meshData,
      int blockBufferSize,
      int vertexBufferSize,
      int faceBufferSize
    )
    {
      // Get all the blocks and validate the counts are correct
      int fullBlocksCount;
      unsafe
      {
        var blockBufferPtr = _blockArray.GetUnsafePtr();
        var vertexBufferPtr = _vertexArray.GetUnsafePtr();
        var faceBufferPtr = _unshiftedFaceArray.GetUnsafePtr();

        fullBlocksCount =
          meshData.GetBlockMesh
          (
            (IntPtr)blockBufferPtr,
            (IntPtr)vertexBufferPtr,
            (IntPtr)faceBufferPtr,
            blockBufferSize,
            vertexBufferSize,
            faceBufferSize
          );

        _faceArray.CopyFrom(_unshiftedFaceArray);
      }

      if (fullBlocksCount < 0)
      {
        ARLog._Error("Error getting mesh data, will not update the mesh.");
        return false;
      }

      if (fullBlocksCount == 0)
      {
        ARLog._Error("Mesh data provided an empty mesh, will not update.");
        return false;
      }

      var gotAllBlocks = fullBlocksCount == MeshBlockCount;
      if (!gotAllBlocks)
      {
        ARLog._ErrorFormat
        (
          "IARMesh.GetBlockMesh() returned {0} full blocks, expected {1}.",
          fullBlocksCount,
          MeshBlockCount
        );

        return false;
      }

      return true;
    }

    private MeshBlock GetOrCreateBlockWithInfo
    (
      int startIndex,
      out Vector3Int blockCoords,
      out int vertexCount,
      out int faceCount,
      out int blockVersion
    )
    {
      blockCoords =
        new Vector3Int
        (
          _blockArray[startIndex],
          _blockArray[startIndex + 1],
          _blockArray[startIndex + 2]
        );

      vertexCount = _blockArray[startIndex + 3];
      faceCount = _blockArray[startIndex + 4];
      blockVersion = _blockArray[startIndex + 5];

      if (!_blocks.TryGetValue(blockCoords, out MeshBlock block))
      {
        block = new MeshBlock();
        _blocks[blockCoords] = block;
      }

      return block;
    }

    const int FloatSize = sizeof(float);

    // End indices are exclusive
    private void UpdateBlockGeometry
    (
      MeshBlock block,
      int verticesStart,
      int verticesCount,
      int normalsStart,
      int normalsCount,
      int facesStart,
      int facesCount
    )
    {
      var blockFaces = _faceArray.GetSubArray(facesStart, facesCount);

      // Update the vertex index for the triangles to be relative to the blockVertices array rather
      // than the whole vertex array.
      var firstVertexOffset = verticesStart / 3;
      for (int i = 0; i < blockFaces.Length; i++)
      {
        blockFaces[i] -= firstVertexOffset;
      }

      block.Triangles = blockFaces;
      block.Vertices =  _vertexArray.GetSubArray(verticesStart, verticesCount).Reinterpret<Vector3>(FloatSize);
      block.Normals = _vertexArray.GetSubArray(normalsStart, normalsCount).Reinterpret<Vector3>(FloatSize);
    }

    // Both remove obsolete blocks and fill a List with all the blocks that were removed.
    private void RemoveObsoleteBlocks(List<Vector3Int> blocksThatWereObsoleted)
    {
      // Always clear out the scratch at the start so that no previous state could corrupt this
      // call. Clearing won't change the Capacity of the List, so this will prevent us needing any
      // allocations unless the List size needs to be increased.
      blocksThatWereObsoleted.Clear();
      foreach (Vector3Int blockCoords in _blocks.Keys)
      {
        var block = _blocks[blockCoords];
        if (block.MeshVersion != MeshVersion)
          blocksThatWereObsoleted.Add(blockCoords);
      }

      foreach (Vector3Int blockCoords in blocksThatWereObsoleted)
        _blocks.Remove(blockCoords);
    }

    public void Clear()
    {
      MeshVersion = 0;
      MeshBlockCount = 0;
      MeshVertexCount = 0;
      MeshFaceCount = 0;

      foreach (var kv in _blocks)
      {
        kv.Value.ClearArrays();
      }

      _blocks.Clear();

      if (_blockArray.IsCreated)
        _blockArray.Dispose();

      if (_faceArray.IsCreated)
        _faceArray.Dispose();

      if (_unshiftedFaceArray.IsCreated)
        _unshiftedFaceArray.Dispose();

      if (_vertexArray.IsCreated)
        _vertexArray.Dispose();

      MeshBlocksCleared?.Invoke(new MeshBlocksClearedArgs());
    }

    public void Dispose()
    {
      Clear();
    }

    private byte[] _sFaceArray;
    public byte[] GetSerializedBlockArray()
    {
      var validSlice = _blockArray.GetSubArray(0, MeshBlockCount * ARMeshConstants.INTS_PER_BLOCK);
      return validSlice.Reinterpret<byte>(sizeof(Int32)).ToArray();
    }

    public byte[] GetSerializedFaceArray()
    {
      var validSlice = _unshiftedFaceArray.GetSubArray(0, MeshFaceCount * ARMeshConstants.INTS_PER_FACE);
      return validSlice.Reinterpret<byte>(sizeof(Int32)).ToArray();
    }

    public byte[] GetSerializedVertexArray()
    {
      var validSlice = _vertexArray.GetSubArray(0, MeshVertexCount * ARMeshConstants.FLOATS_PER_VERTEX);
      return validSlice.Reinterpret<byte>(sizeof(float)).ToArray();
    }

    public NativeArray<int> GetNativeBlockArray()
    {
      var validSlice = _blockArray.GetSubArray(0, MeshBlockCount * ARMeshConstants.INTS_PER_BLOCK);
      return validSlice;
    }
    public NativeArray<float> GetNativeVertexArray()
    {
      var validSlice = _vertexArray.GetSubArray(0, MeshVertexCount * ARMeshConstants.FLOATS_PER_VERTEX);
      return validSlice;
    }
    public NativeArray<int> GetNativeFaceArray()
    {
      var validSlice = _unshiftedFaceArray.GetSubArray(0, MeshFaceCount * ARMeshConstants.INTS_PER_FACE);
      return validSlice;
    }
    }
}
