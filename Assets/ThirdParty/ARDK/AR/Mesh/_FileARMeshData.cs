// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Mesh
{
  internal sealed class _FileARMeshData: _IARMeshData
  {
    private static readonly byte[] _magicWord = new byte[]
    {
      // 6     D     B     L     O     C     K     M     E     S     H    _    _    _    _    _
      0x36, 0x44, 0x42, 0x4C, 0x4F, 0x43, 0x4B, 0x4D, 0x45, 0x53, 0x48, 0x0, 0x0, 0x0, 0x0, 0x0
    };

    public static byte[] MagicWord
    {
      get
      {
        return _magicWord;
      }
    }

    private readonly string _path;
    private int _version;
    private bool _read = false;
    private bool _valid = false;
    private int _blockBufferSize;
    private int _vertexBufferSize;
    private int _faceBufferSize;
    private float _meshBlockSize;

    public _FileARMeshData(string path)
    {
      _path = path;
      if (_path != null)
      {
        // Extract the version number from the file name
        var filename = Path.GetFileName(path);
        var versionMatch = Regex.Match(filename, @"\d+");
        if (versionMatch.Success)
        {
          _version = int.Parse(versionMatch.Value);
        }
      }
    }

    public void Dispose()
    {
    }

    public bool Valid
    {
      get
      {
        ValidateFile();
        return _valid;
      }
    }

    public float MeshBlockSize
    {
      get
      {
        return _meshBlockSize;
      }
    }

    public int GetBlockMeshInfo
    (
      out int blockBufferSizeOut,
      out int vertexBufferSizeOut,
      out int faceBufferSizeOut
    )
    {
      if (!Valid)
      {
        blockBufferSizeOut = 0;
        vertexBufferSizeOut = 0;
        faceBufferSizeOut = 0;
        return 0;
      }

      blockBufferSizeOut = _blockBufferSize;
      vertexBufferSizeOut = _vertexBufferSize;
      faceBufferSizeOut = _faceBufferSize;
      return _version;
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
      if (!Valid)
        return -1;

      // sanity check
      if (blockBufferSize < _blockBufferSize ||
        vertexBufferSize < _vertexBufferSize ||
        faceBufferSize < _faceBufferSize)
      {
        ARLog._Error("Buffer size too small to fit all mesh data.");
        return -1;
      }

      using (var file = File.Open(_path, FileMode.Open))
      {
        using (var reader = new BinaryReader(file))
        {
          // skip the file header
          reader.ReadBytes(32);
          var blockBufferLength = _blockBufferSize * sizeof(Int32);
          var vertexBufferLength = _vertexBufferSize * sizeof(float);
          var faceBufferLength = _faceBufferSize * sizeof(Int32);

          // read & copy block buffer
          var blockBytes = reader.ReadBytes(blockBufferLength);
          Marshal.Copy(blockBytes, 0, blockBuffer, blockBufferLength);

          // read & copy vertex buffer
          var vertexBytes = reader.ReadBytes(vertexBufferLength);
          Marshal.Copy(vertexBytes, 0, vertexBuffer, vertexBufferLength);

          // read & copy face buffer
          var faceBytes = reader.ReadBytes(faceBufferLength);
          Marshal.Copy(faceBytes, 0, faceBuffer, faceBufferLength);
        }
      }

      // return count of successfully read blocks
      return _blockBufferSize / ARMeshConstants.INTS_PER_BLOCK;
    }

    private void ValidateFile()
    {
      if (_read)
        return;

      _read = true;

      using (var file = File.Open(_path, FileMode.Open))
      {
        if (!file.CanRead)
        {
          _valid = false;
          return;
        }

        var reader = new BinaryReader(file);
        try
        {
          var magicWord = reader.ReadBytes(MagicWord.Length);
          if (!magicWord.SequenceEqual(MagicWord))
          {
            _valid = false;
            return;
          }

          _blockBufferSize = reader.ReadInt32();
          _vertexBufferSize = reader.ReadInt32();
          _faceBufferSize = reader.ReadInt32();
          _meshBlockSize = reader.ReadSingle();
        }
        catch
        {
          _valid = false;
          reader.Close();
          return;
        }

        var fileSize = 32 + (_blockBufferSize + _vertexBufferSize + _faceBufferSize) * sizeof(Int32);
        _valid = file.Length >= fileSize;
        reader.Close();
      }
    }
  }
}
