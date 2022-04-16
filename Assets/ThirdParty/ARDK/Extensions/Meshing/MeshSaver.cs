// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.VersionUtilities;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Meshing
{
  /// This helper can be placed in a scene to save mesh data into a binary file on demand plus a
  /// MockMeshInfo json file containing meta data.
  ///
  /// HEADER:
  /// The first 16 bytes are a "magic word" equal to the ASCII string "6DBLOCKMESH" (padded with 0)
  /// The next 16 bytes are 3 Int32 values for block, vertex, and face buffer sizes
  ///  followed by 1 Float value for Mesh Block size
  ///
  /// BUFFERS:
  /// Next is the block buffer (sizeof Int32 * block buffer size, no padding)
  /// Vertex buffer (sizeof Float * vertex buffer size, no padding)
  /// Face buffer (sizeof Int32 * face buffer size, no padding)
  ///
  /// MockMeshInfo JSON:
  /// deviceModel: String
  /// dateTime: String (Format "yyyyMMdd-HHmmss")
  /// ardkVersion: String
  /// vertices: int
  /// faces: int
  ///  
  /// More details on the byte layout can be found in IARMesh.cs.
  /// Mesh files produced by this script can be loaded into the Unity Editor play mode
  /// with Niantic.ARDK.VirtualStudio.AR.Mock.MockMesh (see MeshLoader example scene)
  public class MeshSaver:
    MonoBehaviour
  {
    [SerializeField]
    private float sequentialSavingInterval = 5f;
    
    private string _meshesPath;
    private string _sessionMeshesPath;
    private IARSession _session;
    private int _meshNumber = 0;
    private MockMeshInfo _mockMeshInfo;

    private void Start()
    {
      ARSessionFactory.SessionInitialized += OnSessionInitialized;
    }

    private void OnDestroy()
    {
      ARSessionFactory.SessionInitialized -= OnSessionInitialized;
      Teardown();
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      if (_session != null)
        return;

      _session = args.Session;
      _meshesPath = Application.persistentDataPath + "/meshes";
      _sessionMeshesPath = _meshesPath + "/" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
      
      _mockMeshInfo = new MockMeshInfo(SystemInfo.deviceModel, DateTime.Now.ToString("yyyyMMdd-HHmmss"), ARDKGlobalVersion.GetARDKVersion());
      
      _session.Deinitialized += OnDeinitialized;
    }

    private void OnDeinitialized(ARSessionDeinitializedArgs args)
    {
      Teardown();
      _sessionMeshesPath = null;
    }

    private void Teardown()
    {
      if (_session != null)
      {
        _session.Deinitialized -= OnDeinitialized;
        _session = null;
      }
    }

    public void SaveMeshSequentially()
    {
      InvokeRepeating(nameof(SaveMesh), sequentialSavingInterval, sequentialSavingInterval);
    }
    
    public void StopSaveMeshSequentially()
    {
      CancelInvoke( nameof(SaveMesh));
    }

    /// Saves the current version of the mesh into a file plus meta information into a json.
    public void SaveMesh()
    {
      if (_session == null)
      {
        ARLog._ErrorFormat("Failed to save mesh because no ARSession was initialized.");
        return;
      }

      if (string.IsNullOrEmpty(_sessionMeshesPath))
      {
        ARLog._ErrorFormat("Failed to save mesh because no mesh path was specified.");
        return;
      }

      var provider = _session.Mesh as _MeshDataParser;
      if (provider == null)
      {
        ARLog._Error("Mesh data was was not found in a form compatible with MeshSaver.");
        return;
      }

      if (provider.MeshBlockCount == 0)
      {
        ARLog._ErrorFormat("Failed to save mesh because no mesh blocks were found.");
        return;
      }

      Directory.CreateDirectory(_sessionMeshesPath);

      const string MESH_FILE_FORMAT = "mesh_{0}.bin";
      string filename = string.Format(MESH_FILE_FORMAT, _meshNumber);
      var filepath = Path.Combine(_sessionMeshesPath, filename);
      byte[] magicWord = _FileARMeshData.MagicWord;
      
      // Write MockMeshInfo into Json file
      _mockMeshInfo.vertices = provider.MeshVertexCount;
      _mockMeshInfo.faces = provider.MeshFaceCount;
      
      string info = JsonUtility.ToJson(_mockMeshInfo, true);
      const string INFO_FILE_FORMAT = "meshinfo_{0}.json";
      string infoFilename = string.Format(INFO_FILE_FORMAT, _meshNumber);
      var infoFilepath = Path.Combine(_sessionMeshesPath, infoFilename);
      File.WriteAllText(infoFilepath, info);

      // Define variables of expected types here, to ensure the expected number of bytes is written.
      int blockBufferSize = provider.MeshBlockCount * ARMeshConstants.INTS_PER_BLOCK;
      int vertexBufferSize = provider.MeshVertexCount * ARMeshConstants.FLOATS_PER_VERTEX;
      int faceBufferSize = provider.MeshFaceCount * ARMeshConstants.INTS_PER_FACE;
      float blockSize = provider.MeshBlockSize;

      using (BinaryWriter writer = new BinaryWriter(File.Open(filepath, FileMode.Create)))
      {
        // 16 bytes: signature
        writer.Write(magicWord);

        // 16 bytes: array lengths
        writer.Write(blockBufferSize);
        writer.Write(vertexBufferSize);
        writer.Write(faceBufferSize);
        writer.Write(blockSize);

        // bulk of the data:
        writer.Write(provider.GetSerializedBlockArray());
        writer.Write(provider.GetSerializedVertexArray());
        writer.Write(provider.GetSerializedFaceArray());
      }

      _meshNumber++;
      ARLog._Debug("MeshSaver: successfully written to " + filename);
    }
    
    /// Delete all saved mesh files.
    public void DeleteFiles()
    {
      if (Directory.Exists(_meshesPath))
      {
        Directory.Delete(_meshesPath, true);
        ARLog._Debug("MeshSaver: successfully deleted all mesh files");
      }
      else
      {
        ARLog._Debug("MeshSaver: No files to delete!");
      }
    }
  }
}
