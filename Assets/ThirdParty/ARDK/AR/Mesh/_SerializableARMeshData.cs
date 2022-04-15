// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

using UnityEngine;

namespace Niantic.ARDK.AR.Mesh
{
  [Serializable]
  internal sealed class _SerializableARMeshData:_IARMeshData
  {
    internal _SerializableARMeshData
    (
      float meshBlockSize
    )
    {
      MeshBlockSize = meshBlockSize;
    }

    public float MeshBlockSize { get; private set; }

    void IDisposable.Dispose()
    {
      // Do nothing as this object is fully managed.
    }

    private static readonly _SerializableARMeshData _EmptyMesh = new _SerializableARMeshData(0);
    public static _SerializableARMeshData EmptyMesh()
    {
      return _EmptyMesh;
    }

    public int GetBlockMeshInfo
    (
      out int blockBufferSizeOut,
      out int vertexBufferSizeOut,
      out int faceBufferSizeOut
    )
    {
      blockBufferSizeOut = 0;
      vertexBufferSizeOut = 0;
      faceBufferSizeOut = 0;
      return 0;
    }

    public int GetBlockMesh
    (
      IntPtr blockBuffer,
      IntPtr vertexBuffer,
      IntPtr faceBuffer,
      int blockBufferSize,
      int vertexBufferSize,
      int faceBufferSize
    )
    {
      return 0;
    }
  }
}