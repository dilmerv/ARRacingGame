// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Mesh;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions.Meshing
{
  /// This helper can be placed in a scene to easily add environment meshes.
  /// It reads meshing output from the ARSession, and instantiates mesh prefabs loaded with
  ///  components and materials for the desired behavior and rendering.
  /// Mesh visibility can be toggled on and off, using a depth mask material for occlusion effect.
  ///
  ///   OnEnable/EnableFeatures:
  ///     If an ARSession is running, meshing will be enabled and the ARSession will be re-run.
  ///     Else, the next run ARSession will have meshing enabled.
  ///   OnDisable/DisableFeatures:
  ///     If an ARSession is running, meshing will be disabled and the ARSession will be re-run.
  ///     Else, the next run ARSession will have meshing disabled.
  public class ARMeshManager: ARSessionListener
  {
    [Header("AR Configuration Properties")]
    [SerializeField]
    [Tooltip("Target number of times per second to run the mesh update routine.")]
    private uint _targetFrameRate = 20;

    [SerializeField]
    [Tooltip("Target size of a mesh block in meters")]
    private float _targetBlockSize = 1.4f;

    [SerializeField]
    [Tooltip("Radius in meters of the meshed surface around the player. 0 means 'Infinity'")]
    private float _meshingRadius = 0;

    [Header("Mesh Generation Settings")]
    [SerializeField]
    [Tooltip("When true, a Unity mesh will be instantiated and updated for each mesh block.")]
    private bool _generateUnityMeshes;

    /// This GameObject requires a MeshFilter component, and will update a MeshCollider component if
    /// able. A MeshRenderer component is optional, but required for the SetUseInvisibleMaterial method.
    [SerializeField]
    [Tooltip("The GameObject to instantiate and update for each mesh block.")]
    private GameObject _meshPrefab;

    [SerializeField]
    [Tooltip(
      "Parent of every block (piece of mesh). If empty, this is assigned to the component's " +
      "GameObject in Initialize()."
    )]
    private GameObject _meshRoot;

    /// A value of zero or lower means the MeshCollider updates every time.
    /// A throttle is sometimes needed because MeshCollider updates are a lot more expensive than
    /// MeshRenderer updates.
    [SerializeField]
    [Tooltip("The number of mesh updates to skip between two consecutive MeshCollider updates.")]
    private int _colliderUpdateThrottle = 10;

    [Header("Mesh Visibility Settings")]
    [SerializeField]
    [Tooltip(
      "When true, mesh blocks are rendered using InvisibleMaterial instead of the prefab's " +
      "default material."
    )]
    private bool _useInvisibleMaterial = false;

    [SerializeField]
    [Tooltip(
      "(Optional) Used as a substitution material when the mesh is hidden (a depth mask " +
      "material should typically be used here)."
    )]
    private Material _invisibleMaterial;

    public uint TargetFrameRate
    {
      get { return _targetFrameRate; }
      set
      {
        if (value != _targetFrameRate)
        {
          _targetFrameRate = value;
          RaiseConfigurationChanged();
        }
      }
    }

    public float TargetBlockSize
    {
      get { return _targetBlockSize; }
      set
      {
        if (!Mathf.Approximately(_targetBlockSize, value))
        {
          _targetBlockSize = value;
          RaiseConfigurationChanged();
        }
      }
    }

    public float MeshingRadius
    {
      get { return _meshingRadius; }
      set
      {
        if (value > 0 && value < 5)
        {
          ARLog._Error(MeshingRadiusError);
          return;
        }

        if (!Mathf.Approximately(_meshingRadius, value))
        {
          _meshingRadius = value;
          RaiseConfigurationChanged();
        }
      }
    }

    /// When true, mesh block GameObjects will not be updated
    /// (a running ARSession will still surface mesh updates).
    public bool GenerateUnityMeshes
    {
      get { return _generateUnityMeshes; }
      set { _generateUnityMeshes = value; }
    }

    public GameObject MeshPrefab
    {
      get { return _meshPrefab; }
      set
      {
        if (_generator != null)
        {
          ARLog._Error
          (
            "MeshPrefab value cannot be changed after an ARSession has already been initialized."
          );
        }
        else
        {
          _meshPrefab = value;
        }
      }
    }

    public GameObject MeshRoot
    {
      get { return _meshRoot; }
      set
      {
        if (_generator != null)
        {
          ARLog._Error
          (
            "MeshPrefab value cannot be changed after an ARSession has already been initialized."
          );
        }
        else
        {
          _meshRoot = value;
        }
      }
    }

    /// False if the mesh objects are visible (i.e. it renders using the prefab's default material)
    /// and true if the mesh objects are hidden (i.e. it uses the invisible material).
    public bool UseInvisibleMaterial
    {
      get { return _useInvisibleMaterial; }
      set { SetUseInvisibleMaterial(value); }
    }

    /// Called when all mesh blocks have been updated with info from the the latest mesh update.
    public event ArdkEventHandler<MeshObjectsUpdatedArgs> MeshObjectsUpdated;

    /// Called when all mesh blocks have been cleared.
    public event ArdkEventHandler<MeshObjectsClearedArgs> MeshObjectsCleared;

    private IARMesh ARMesh
    {
      get
      {
        if (ARSession != null)
          return ARSession.Mesh;

        return null;
      }
    }

    // Used to track when the Inspector-public variables are changed in OnValidate
    private uint _prevTargetFrameRate;
    private float _prevTargetBlockSize;
    private float _prevMeshingRadius;

    private bool _clearMeshOnRerun = false;

    private MeshObjectsGenerator _generator;
    private const string MeshingRadiusError =
      "The smallest meshing radius possible is 5 meters. Set the value to 0 for an infinite radius.";

    protected override void InitializeImpl()
    {
      base.InitializeImpl();

      if (!_meshRoot)
        _meshRoot = gameObject;

      if (!_meshPrefab)
      {
        ARLog._Warn("No mesh prefab set on the ARMeshManager. No mesh blocks will be generated.");
        return;
      }
    }

    protected override void DeinitializeImpl()
    {
      ClearMeshObjects();

      base.DeinitializeImpl();

      _generator?.Clear();
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      _prevTargetFrameRate = _targetFrameRate;
      _prevTargetBlockSize = _targetBlockSize;
      _prevMeshingRadius = _meshingRadius;
      RaiseConfigurationChanged();
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();
      RaiseConfigurationChanged();
    }

    protected override void ListenToSession()
    {
      // TODO (Awareness): Integrate check for if Awareness initialization failed

      _generator?.Clear();

      _generator =
        new MeshObjectsGenerator
        (
          ARSession.Mesh,
          _meshRoot,
          _meshPrefab,
          _invisibleMaterial,
          _colliderUpdateThrottle
        );

      SetUseInvisibleMaterial(_useInvisibleMaterial);

      ARSession.Mesh.MeshBlocksUpdated += OnMeshUpdated;

      _generator.MeshObjectsUpdated += OnMeshObjectsUpdated;
      _generator.MeshObjectsCleared += OnMeshObjectsCleared;
    }

    protected override void StopListeningToSession()
    {
      ARSession.Mesh.MeshBlocksUpdated -= OnMeshUpdated;
      
      _generator.MeshObjectsUpdated -= OnMeshObjectsUpdated;
      _generator.MeshObjectsCleared -= OnMeshObjectsCleared;
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration worldConfig)
      {
        worldConfig.IsMeshingEnabled = AreFeaturesEnabled;
        worldConfig.MeshingTargetFrameRate = TargetFrameRate;
        worldConfig.MeshingTargetBlockSize = TargetBlockSize;
        worldConfig.MeshingRadius = MeshingRadius;

        if (_clearMeshOnRerun)
        {
          properties.RunOptions |= ARSessionRunOptions.RemoveExistingMesh;
          _clearMeshOnRerun = false;
        }
      }
    }

    /// Convenience method to convert world coordinates in Unity to integer block coordinates.
    public bool GetBlockCoords(Vector3 worldCoords, out Vector3Int blockCoords)
    {
      // Parser dne or has not yet processed the first mesh update
      if (ARMesh == null || ARMesh.MeshVersion == 0)
      {
        blockCoords = Vector3Int.zero;
        return false;
      }

      Vector3 meshCoords = _meshRoot.transform.InverseTransformPoint(worldCoords);

      var meshBlockSize = ARMesh.MeshBlockSize;
      blockCoords = new Vector3Int
      (
        Mathf.FloorToInt(meshCoords.x / meshBlockSize),
        Mathf.FloorToInt(meshCoords.y / meshBlockSize),
        Mathf.FloorToInt(meshCoords.z / meshBlockSize)
      );

      return true;
    }

    /// Convenience method to get the mesh GameObject at the specified block coordinates.
    /// Returns null if no object exists at those coordinates.
    public GameObject GetBlockGameObject(Vector3Int blockCoords)
    {
      if (_generator == null || ARMesh == null)
        return null;

      MeshBlock block;
      if (ARMesh.Blocks.TryGetValue(blockCoords, out block))
      {
        GameObject blockObject;
        if (_generator.BlockObjects.TryGetValue(blockCoords, out blockObject))
          return blockObject;
      }

      return null;
    }

    /// Updates the MeshRenderers of all GameObjects in _blocks with either the invisible or the
    /// original prefab material. Does nothing if the prefab is null or does not contain a MeshRenderer.
    public void SetUseInvisibleMaterial(bool useInvisible)
    {
      _useInvisibleMaterial = useInvisible;

      _generator?.SetUseInvisibleMaterial(useInvisible);
    }

    /// Clear the mesh, delete all GameObjects under _meshRoot.
    /// Sends a MeshCleared event if there's a listener when it's done.
    public void ClearMeshObjects()
    {
      _generator?.Clear();
      _clearMeshOnRerun = true;
      RaiseConfigurationChanged();
    }

    // Callback on the ARSession.Mesh.MeshBlocksUpdated event.
    // Generates new Unity meshes if required.
    private void OnMeshUpdated(MeshBlocksUpdatedArgs args)
    {
      if (!GenerateUnityMeshes || (args.BlocksObsoleted.Count == 0 && args.BlocksUpdated.Count == 0))
        return;

      _generator.UpdateMeshBlocks(args);
    }

    private void OnMeshObjectsUpdated(MeshObjectsUpdatedArgs args)
    {
      // Currently just a pass-through.
      MeshObjectsUpdated?.Invoke(args);
    }

    private void OnMeshObjectsCleared(MeshObjectsClearedArgs args)
    {
      // Currently just a pass-through.
      MeshObjectsCleared?.Invoke(args);
    }

    private void OnValidate()
    {
      if (_prevTargetFrameRate != _targetFrameRate)
      {
        _prevTargetFrameRate = _targetFrameRate;
        RaiseConfigurationChanged();
      }

      if (!Mathf.Approximately(_prevTargetBlockSize, _targetBlockSize))
      {
        _prevTargetBlockSize = _targetBlockSize;
        RaiseConfigurationChanged();
      }

      if (!Mathf.Approximately(_prevMeshingRadius, _meshingRadius))
      {
        if (_meshingRadius > 0 && _meshingRadius < 5)
        {
          ARLog._Error(MeshingRadiusError);
          _meshingRadius = 5;
        }

        if (!Mathf.Approximately(_prevMeshingRadius, _meshingRadius))
        {
          _prevMeshingRadius = _meshingRadius;
          RaiseConfigurationChanged();
        }
      }
    }
  }
}
