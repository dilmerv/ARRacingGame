// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Meshing
{
  public class MeshObjectsGenerator
  {
    private IARMesh _arMesh;

    private GameObject _root;
    private GameObject _prefab;

    private Material _invisibleMaterial;
    private bool _usingInvisibleMaterial;

    private int _colliderUpdateThrottle;

    private Dictionary<Vector3Int, GameObject> _blockObjects = new Dictionary<Vector3Int, GameObject>();

    public IReadOnlyDictionary<Vector3Int, GameObject> BlockObjects { get; }

    /// Called when all mesh blocks have been updated with info from the the latest mesh update.
    public event ArdkEventHandler<MeshObjectsUpdatedArgs> MeshObjectsUpdated;

    /// Called when all mesh blocks have been cleared.
    public event ArdkEventHandler<MeshObjectsClearedArgs> MeshObjectsCleared;

    public MeshObjectsGenerator
    (
      IARMesh arMesh,
      GameObject root,
      GameObject prefab,
      Material invisibleMaterial,
      int colliderUpdateThrottle
    )
    {
      _arMesh = arMesh;

      _root = root;
      _prefab = prefab;
      _invisibleMaterial = invisibleMaterial;
      _colliderUpdateThrottle = Math.Max(colliderUpdateThrottle, 0);

      BlockObjects = new ReadOnlyDictionary<Vector3Int, GameObject>(_blockObjects);
    }

    public void Clear()
    {
      if (_blockObjects.Count == 0)
        return;

      foreach (var go in _blockObjects.Values)
        GameObject.Destroy(go);

      _blockObjects.Clear();
      MeshObjectsCleared?.Invoke(new MeshObjectsClearedArgs());
    }

    public bool TryGetBlockObject(Vector3Int blockCoords, out GameObject blockObject)
    {
      return _blockObjects.TryGetValue(blockCoords, out blockObject);
    }

    private List<GameObject> _updatedBlocks;
    private List<GameObject> _updatedColliders;
    public void UpdateMeshBlocks(MeshBlocksUpdatedArgs args)
    {
      _updatedBlocks = new List<GameObject>();
      _updatedColliders = new List<GameObject>();

      foreach (var updatedBlock in args.BlocksUpdated)
        OnMeshBlockUpdated(updatedBlock);

      foreach (var obsoletedBlock in args.BlocksObsoleted)
        OnMeshBlockObsoleted(obsoletedBlock);

      MeshObjectsUpdated?.Invoke(new MeshObjectsUpdatedArgs(_updatedBlocks, _updatedColliders));
    }

    private void OnMeshBlockUpdated(Vector3Int blockCoords)
    {
      if (!_blockObjects.ContainsKey(blockCoords))
        AddMeshBlock(blockCoords);

      UpdateMeshObject(blockCoords);
      _updatedBlocks.Add(_blockObjects[blockCoords]);
    }

    private void OnMeshBlockObsoleted(Vector3Int blockCoords)
    {
      // User triggered Clears could have already removed the game object.
      if (TryGetBlockObject(blockCoords, out var blockObject))
      {
        GameObject.Destroy(blockObject.gameObject);
        _blockObjects.Remove(blockCoords);
      }
    }

    public void SetUseInvisibleMaterial(bool useInvisible)
    {
      _usingInvisibleMaterial = useInvisible;

      Material newSharedMaterial = null;
      if (!useInvisible)
      {
        if (_prefab == null)
        {
          ARLog._Error("Failed to change the mesh material because no mesh prefab was set.");
          return;
        }

        MeshRenderer meshRenderer = _prefab.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
          ARLog._Error("Failed to change the mesh material because the mesh prefab lacks a MeshRenderer.");
          return;
        }

        newSharedMaterial = meshRenderer.sharedMaterial;
        if (newSharedMaterial == null)
        {
          ARLog._Error
          (
            "Failed to change the mesh material because the mesh prefab's MeshRenderer component " +
            "lacks a shared material."
          );

          return;
        }
      }
      else
      {
        newSharedMaterial = _invisibleMaterial;
        if (newSharedMaterial == null)
        {
          ARLog._Error("Failed to change the mesh material because no invisible material was set.");
          return;
        }
      }

      foreach (var blockObject in _blockObjects.Values)
      {
        var blockRenderer = blockObject.GetComponent<MeshRenderer>();
        if (blockRenderer)
          blockRenderer.material = newSharedMaterial;
      }
    }

    private void AddMeshBlock(Vector3Int blockCoords)
    {
      if (!_arMesh.Blocks.TryGetValue(blockCoords, out MeshBlock meshBlock))
      {
        ARLog._Error("No MeshBlock found at block coordinates: " + blockCoords);
        return;
      }

      var go = GameObject.Instantiate(_prefab, _root.transform, true);
      go.transform.localScale = Vector3.one;
      go.name = _prefab.name + blockCoords;

      if (_usingInvisibleMaterial && _invisibleMaterial != null)
      {
        var meshRenderer =
          go.GetComponent<MeshRenderer>();

        if (meshRenderer != null)
          meshRenderer.material = _invisibleMaterial;
      }

      _blockObjects[blockCoords] = go;
    }

    private void UpdateMeshObject(Vector3Int blockCoords)
    {
      if (!_arMesh.Blocks.TryGetValue(blockCoords, out MeshBlock block))
      {
        ARLog._Error("No MeshBlock found at block coordinates: " + blockCoords);
        return;
      }

      if (!_blockObjects.TryGetValue(blockCoords, out GameObject blockObject))
      {
        ARLog._Error("No mesh GameObject found at block coordinates: " + blockCoords);
        return;
      }

      var meshFilter = blockObject.GetComponent<MeshFilter>();
      var meshCollider = blockObject.GetComponent<MeshCollider>();

      if (block.Mesh == null)
      {
        block.Mesh = new Mesh();
        block.Mesh.MarkDynamic();
      }

      var mesh = block.Mesh;
      mesh.Clear();
      mesh.SetVertices(block.Vertices);
      mesh.SetNormals(block.Normals);
      mesh.SetIndices(block.Triangles, MeshTopology.Triangles, 0);

      if (meshFilter != null)
        meshFilter.sharedMesh = mesh;

      if (meshCollider != null)
      {
        // update the collider less often for optimal performance
        int minColliderUpdateVersion =
          block.ColliderVersion +
          _colliderUpdateThrottle +
          1;

        var colliderNeedsUpdate =
          block.ColliderVersion < 0 ||
          block.Version >= minColliderUpdateVersion;

        if (colliderNeedsUpdate)
        {
          meshCollider.sharedMesh = mesh;
          block.ColliderVersion = block.Version;

          _updatedColliders.Add(blockObject);
        }
      }
    }
  }
}
