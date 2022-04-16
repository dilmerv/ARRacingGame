// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// This script can load sequences mesh files saved with Niantic.ARDK.Extensions.Meshing.MeshSaver
  /// into the Unity Editor play mode, loading all meshes inside a same directory, ordered by mesh
  /// version number, one after the other at the pace specified by _updateInterval.
  /// Use MockMesh to load single meshes.
  public sealed class MockMeshSequence:
    MockDetectableBase
  {
    /// _meshSequencePath is the path to a folder of mesh files (mesh_*.bin) in the project.
    /// Individual mesh files will be loaded at fixed intervals, ordered by version number.
    [SerializeField]
    private string _meshSequencePath = null;

    /// _updateInterval is the desired time interval in seconds between mesh updates.
    [SerializeField]
    private float _updateInterval = 1.0f;

    internal override void BeDiscovered(_IMockARSession arSession, bool isLocal)
    {
      if (!isLocal)
        return;

      ARLog._Debug("will load the meshes @ " + _meshSequencePath);

      string[] paths = GetMeshPaths();
      if (paths.Length > 0)
        StartCoroutine(UpdateMeshes(arSession, paths));
    }

    private string[] GetMeshPaths()
    {
      string[] paths = Directory.GetFiles(_meshSequencePath, "mesh_*.bin");

      // order paths by version number
      paths = paths.OrderBy
        (
          s =>
            int.Parse(Regex.Match(s, "\\d+", RegexOptions.RightToLeft).Value)
        )
        .ToArray();

      return paths;
    }

    private IEnumerator UpdateMeshes(_IMockARSession arSession, string[] paths)
    {
      foreach (string meshPath in paths)
      {
        using (_FileARMeshData loadedMeshData = new _FileARMeshData(meshPath))
          arSession.UpdateMesh(loadedMeshData);

        yield return new WaitForSeconds(_updateInterval);
      }
    }

    internal override void OnSessionRanAgain(_IMockARSession arSession)
    {
      if ((arSession.RunOptions & ARSessionRunOptions.RemoveExistingMesh) != 0)
      {
        throw new NotImplementedException("Removing meshes is not yet supported in Virtual Studio");
      }
    }
  }
}
