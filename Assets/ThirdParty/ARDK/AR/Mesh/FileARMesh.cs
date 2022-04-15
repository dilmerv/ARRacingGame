using System.Collections.Generic;

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Mesh
{
  public class FileARMesh:
    IARMesh
  {
    public float MeshBlockSize
    {
      get { return _dataParser.MeshBlockSize; }
    }
    public int MeshVersion
    {
      get { return _dataParser.MeshVersion; }
    }

    public int MeshBlockCount
    {
      get { return _dataParser.MeshBlockCount; }
    }

    public int MeshVertexCount
    {
      get { return _dataParser.MeshVertexCount; }
    }

    public int MeshFaceCount
    {
      get { return _dataParser.MeshFaceCount; }
    }

    public event ArdkEventHandler<MeshBlocksUpdatedArgs> MeshBlocksUpdated
    {
      add { _dataParser.MeshBlocksUpdated += value; }
      remove { _dataParser.MeshBlocksUpdated -= value; }
    }

    public event ArdkEventHandler<MeshBlocksClearedArgs> MeshBlocksCleared
    {
      add { _dataParser.MeshBlocksCleared += value; }
      remove { _dataParser.MeshBlocksCleared -= value; }
    }

    public IReadOnlyDictionary<Vector3Int, MeshBlock> Blocks
    {
      get { return _dataParser.Blocks; }
    }

    private _MeshDataParser _dataParser;

    public FileARMesh(string path)
    {
      _dataParser = new _MeshDataParser();
      var data = new _FileARMeshData(path);

      if (data.Valid)
      {
        _dataParser.ParseMesh(data);
      }
    }
  }
}
