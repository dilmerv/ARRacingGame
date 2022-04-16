using System;
using System.IO;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARDK.Assets.ARDK.Extensions.Meshing
{
    public class MeshObjSaver: MonoBehaviour
    {
        private string _meshDir;
        private string _meshPath;
        private IARSession _session;

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
            _meshDir = Application.persistentDataPath +"/meshobj/";
            _meshPath = _meshDir + DateTime.Now.ToString("yyyyMMdd-HHmmss");

            _session.Deinitialized += OnDeinitialized;
        }

        private void OnDeinitialized(ARSessionDeinitializedArgs args)
        {
            Teardown();
            _meshPath = null;
        }

        private void Teardown()
        {
            if (_session != null)
            {
                _session.Deinitialized -= OnDeinitialized;
                _session = null;
            }
        }

        public void SaveMeshObj()
        {
            if (_session == null)
            {
                ARLog._ErrorFormat("Failed to save mesh because no ARSession was initialized.");
                return;
            }

            if (string.IsNullOrEmpty(_meshPath))
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
            Directory.CreateDirectory(_meshPath);

            string filename = "mesh_" + provider.MeshVersion + ".obj";
            var filepath = _meshPath + "/" + filename;

            var vertexArray = provider.GetNativeVertexArray();
            var faceArray = provider.GetNativeFaceArray();

            int numOfVertex = provider.MeshVertexCount;
            int numOfFace = provider.MeshFaceCount;

            StreamWriter writer = new StreamWriter(filepath);

            for (int i=0; i< numOfVertex; i++)
            {
                writer.WriteLine("v {0} {1} {2}", vertexArray[i*3+0], vertexArray[i*3+1], vertexArray[i*3+2]);
            }

            Debug.LogFormat("vertex length: " + numOfVertex + " " + vertexArray.Length);

            for (int i=0; i< numOfFace; i++)
            {
                writer.WriteLine("f {0} {1} {2}", faceArray[i*3+0] + 1, faceArray[i*3+1] + 1, faceArray[i*3+2] + 1);
            }

            writer.Close();

            ARLog._Debug("MeshObjSaver: successfully written to " + filename);
        }

        /// Delete all saved mesh files.
        public void DeleteFiles()
        {
            if (Directory.Exists(_meshDir))
            {
                Directory.Delete(_meshDir, true);
                ARLog._Debug("MeshObjSaver: successfully deleted all mesh files");
            }
            else
            {
                ARLog._Debug("MeshObjSaver: No files to delete!");
            }
        }
    }
}
