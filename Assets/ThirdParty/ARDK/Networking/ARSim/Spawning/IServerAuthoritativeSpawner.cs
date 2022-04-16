// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking.ARSim.Spawning.GameObjectSpawning;
using Niantic.ARDK.Networking.ARSim.Spawning.ServerSpawningEventArgs;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.ARSim.Spawning
{
  /// <summary>
  /// Interface for a server authoritative spawner. Raises events when an object is spawned or
  ///   despawned, and should handle all internal logic related to spawning/despawning.
  /// @note Currently in internal development, and not useable
  /// </summary>
  public interface IServerAuthoritativeSpawner : 
    IDisposable
  {
    // Event that is called when the server spawns an object
    event ArdkEventHandler<ServerSpawnedArgs> DidSpawnObject;

    // Event that is called when the server despawns an object
    event ArdkEventHandler<ServerDespawnedArgs> WillDespawnObject;

    // Load an IAddressablePrefabManifest to this spawner, to map prefabIDs to prefabs
    void LoadPrefabManifest(IAddressablePrefabManifest manifest);

    // Spawn an object with the specified parameters
    // @note Public for now to allow for catchup spawning. Once spawn messages are guaranteed (KV),
    //   this will be internal
    void Spawn(ServerAuthoritativeSpawnerBase.ServerSpawnParams spawnParams);

    /// <summary>
    /// Set the instantiator that the spawner will use to create gameobjects. Different
    ///   IGameObjectInstantiators can be implemented to use custom spawning behaviour, such as
    ///   dependency injection or object pooling.
    /// </summary>
    /// <param name="instantiator"></param>
    void SetGameObjectInstantiator(IGameObjectInstantiator instantiator);
  }
}
