// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.Networking.ARSim.Spawning.GameObjectSpawning;
using Niantic.ARDK.Networking.ARSim.Spawning.ServerSpawningEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.Networking.ARSim.Spawning
{
  /// <summary>
  /// Abstract base for a server authoritative spawner. Contains base implementation for maintaining
  ///   and verifying spawning objects, despawning objects, and updating the positions of spawned
  ///   objects.
  /// </summary>
  public abstract class ServerAuthoritativeSpawnerBase :
    IServerAuthoritativeSpawner
  {
    // Structs that contains spawning/despawning/updating parameters
    
    // @note This is public for catch-up spawning. Will be made internal after spawning is guaranteed.
    public struct ServerSpawnParams
    {
      public string ObjectId { get; set; }
      public string PrefabId { get; set; }
      public Vector3 Location { get; set; }
      public Vector3 Rotation { get; set; }
    }

    protected struct UpdatePositionParams
    {
      public string ObjectId { get; set; }
      public Vector3 Location { get; set; }
      public Vector3 Rotation { get; set; }
    }

    protected struct ServerDespawnParams
    {
      public string ObjectId { get; set; }
    }

    private bool _haveSpawnedObject;
    
    private IAddressablePrefabManifest _manifest;
    private readonly Dictionary<string, GameObject> _spawnedObjects = 
      new Dictionary<string, GameObject>();
    private readonly Dictionary<string, UpdatePositionParams> _cachedUpdateParams = 
      new Dictionary<string, UpdatePositionParams>();
    
    protected IGameObjectInstantiator Instantiator;

    /// <inheritdoc />
    public event ArdkEventHandler<ServerSpawnedArgs> DidSpawnObject;
    /// <inheritdoc />
    public event ArdkEventHandler<ServerDespawnedArgs> WillDespawnObject;
    
    /// <inheritdoc />
    public abstract void Dispose();
    
    /// <inheritdoc />
    public void LoadPrefabManifest(IAddressablePrefabManifest manifest)
    {
      _manifest = manifest;
    }

    /// <summary>
    /// Spawn an object with the specified parameters
    /// @note This is public for catch-up spawning. Will be made internal after
    ///   spawning is guaranteed.
    /// </summary>
    /// <param name="spawnParams">Details of the object to spawn</param>
    public void Spawn(ServerSpawnParams spawnParams)
    {
      if (_manifest == null)
      {
        Debug.LogError("Got a spawn message, but have not loaded a manifest");
        return;
      }

      if (!_manifest.PrefabManifest.ContainsKey(spawnParams.PrefabId))
      {
        Debug.Log("Manifest does not contain prefab with ID: " + spawnParams.PrefabId);
        return;
      }

      var id = spawnParams.ObjectId;
      if (_spawnedObjects.ContainsKey(id))
      {
        Debug.LogFormat("Object of id {0} has already been spawned", id);
        return;
      }

      Vector3 location, rotation;

      if (_cachedUpdateParams.ContainsKey(id))
      {
        var param = _cachedUpdateParams[id];
        location = param.Location;
        rotation = param.Rotation;
        _cachedUpdateParams.Remove(id);
      }
      else
      {
        location = spawnParams.Location;
        rotation = spawnParams.Rotation;
      }

      var quat = Quaternion.Euler(rotation);
      if(Instantiator == null)
        Instantiator = new UnityGameObjectInstantiator();
      
      var obj = Instantiator.Instantiate(_manifest.PrefabManifest[spawnParams.PrefabId], location, quat);
      _haveSpawnedObject = true;
      _spawnedObjects[id] = obj;

      var handler = DidSpawnObject;
      if (handler != null)
      {
        var args = new ServerSpawnedArgs(id, obj);
        handler(args);
      }
    }

    /// <summary>
    /// Set the instantiator that the spawner will use to create gameobjects. Different
    ///   IGameObjectInstantiators can be implemented to use custom spawning behaviour, such as
    ///   dependency injection or object pooling.
    /// @note If no instantiator is provided by the first spawn call, a UnityGameObjectInstantiator
    ///   will be used.
    /// </summary>
    public void SetGameObjectInstantiator(IGameObjectInstantiator instantiator)
    {
      Instantiator = instantiator;
      if (_haveSpawnedObject)
      {
        Debug.LogWarning
        (
          "Set a new instantiator after an object has been spawned. Object management" +
          "may not work as intended."
        );
      }
    }

    /// <summary>
    /// Update the position of a specified object. If the object has not been spawned yet, cache the
    ///   parameters until it is spawned, then update its position on spawn.
    /// </summary>
    /// <param name="updateParams">Details of the object to update</param>
    protected void UpdatePosition(UpdatePositionParams updateParams)
    {
      string id = updateParams.ObjectId;
      if (!_spawnedObjects.ContainsKey(id))
      {
        _cachedUpdateParams[id] = updateParams;
        return;
      }
      _spawnedObjects[id].transform.position = updateParams.Location;
      _spawnedObjects[id].transform.rotation = Quaternion.Euler(updateParams.Rotation);
    }

    /// <summary>
    /// Despawn an object that has been spawned
    /// </summary>
    /// <param name="despawnParams">Details of the object to despawn</param>
    protected void Despawn(ServerDespawnParams despawnParams)
    {
      var id = despawnParams.ObjectId;
      if (!_spawnedObjects.ContainsKey(id))
      {
        Debug.LogFormat("Object of id {0} has not been spawned, or has been despawned already", id);
        return;
      }

      var obj = _spawnedObjects[id];
      _spawnedObjects.Remove(id);

      var handler = WillDespawnObject;
      if (handler != null)
      {
        var args = new ServerDespawnedArgs(id, obj);
        handler(args);
      }

      if(Instantiator == null)
        Instantiator = new UnityGameObjectInstantiator();
      
      Instantiator.Destroy(obj);
    }
  }
}
