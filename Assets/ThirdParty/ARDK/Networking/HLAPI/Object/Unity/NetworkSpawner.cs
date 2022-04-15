// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Routing;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

using Random = System.Random;

namespace Niantic.ARDK.Networking.HLAPI.Object.Unity
{
  public struct NetworkObjectLifecycleArgs:
    IArdkEventArgs
  {
    public NetworkObjectLifecycleArgs
    (
      NetworkedUnityObject networkedUnityObject,
      IMultipeerNetworking networking,
      IPeer peer
    ):
      this()
    {
      Object = networkedUnityObject;
      Networking = networking;
      Peer = peer;
    }

    public NetworkedUnityObject Object { get; private set; }
    public IMultipeerNetworking Networking { get; private set; }
    public IPeer Peer { get; private set; }
  }

  /// <summary>
  /// A static class that handles the replication and network spawning of NetworkedUnityObjects for
  /// all peers across the network. Also handles the network destruction of spawned objects.
  /// </summary>
  public static class NetworkSpawner
  {
    private static readonly ConcurrentDictionary<NetworkId, NetworkedUnityObject> _loadedPrefabs =
      new ConcurrentDictionary<NetworkId, NetworkedUnityObject>();

    private static readonly ConcurrentDictionary<NetworkId, NetworkedUnityObject> _spawnedObjects =
      new ConcurrentDictionary<NetworkId, NetworkedUnityObject>();

    private static readonly Random _random = new Random();
    private static readonly byte[] _idGenBuffer = new byte[8];
    private static readonly int _debugPeerIdLength = 6;

    private static
      ConcurrentDictionary<Guid, IMessageStreamReplicator<SpawnMessage>> _spawnMessageStreams =
        new ConcurrentDictionary<Guid, IMessageStreamReplicator<SpawnMessage>>();

    private static
      ConcurrentDictionary<Guid, IMessageStreamReplicator<NetworkId>> _destructorMessageStreams =
        new ConcurrentDictionary<Guid, IMessageStreamReplicator<NetworkId>>();

    private static IMessageStreamReplicator<NetworkId> _destructorMessageStream;

    // Some hard coded random values so that all peers are in agreement of network spawning
    // groups/channels.
    private const ulong SpawnGroupId = 94842342L;
    private const ulong SpawnChannelId = 38901231L;
    private const ulong DestructorChannelId = 38209183L;

    public static event ArdkEventHandler<NetworkObjectLifecycleArgs> NetworkObjectSpawned;
    public static event ArdkEventHandler<NetworkObjectLifecycleArgs> NetworkObjectDestroyed;

    private static bool _initialized;
    private static void _InitializeIfNeeded()
    {
      if (_initialized)
        return;

      ARLog._Debug("Initializing NetworkSpawner");
      MultipeerNetworkingFactory.NetworkingInitialized += _MultipeerNetworkingInitialized;
      _initialized = true;
    }

    internal static void _Deinitialize()
    {
      ARLog._Debug("Deinitializing NetworkSpawner, existing sessions will still be useable");
      MultipeerNetworkingFactory.NetworkingInitialized -= _MultipeerNetworkingInitialized;
      _initialized = false;
    }

    private static void _MultipeerNetworkingInitialized
    (
      AnyMultipeerNetworkingInitializedArgs networkingArgs
    )
    {
      var multipeerNetworking = networkingArgs.Networking;

      var netSpawnerGroup =
        multipeerNetworking
          .GetOrCreateManagedSession()
          .CreateAndRegisterGroup(new NetworkId(SpawnGroupId));

      var streamReplicator =
        new MessageStreamReplicator<SpawnMessage>
        (
          "_NSSpawn",
          multipeerNetworking.AnyToAnyDescriptor(TransportType.ReliableOrdered),
          netSpawnerGroup
        );

      _spawnMessageStreams[multipeerNetworking.StageIdentifier] = streamReplicator;

      streamReplicator.MessageReceived +=
        (args) => SpawnMessageStreamOnMessageReceive(args.Message, args.Sender, streamReplicator);

      var destructorMessageStream =
        new MessageStreamReplicator<NetworkId>
        (
          "_NSDestroy",
          multipeerNetworking.AnyToAnyDescriptor(TransportType.ReliableOrdered),
          netSpawnerGroup
        );

      _destructorMessageStreams[multipeerNetworking.StageIdentifier] = destructorMessageStream;

      destructorMessageStream.MessageReceived += DestroyMessageStreamOnMessageReceived;

      multipeerNetworking.PeerAdded +=
        (peerAddedArgs) =>
        {
          foreach (var repUnityObject in _spawnedObjects.Values)
          {
            bool isMine =
              repUnityObject.WasSpawnedByMe &&
              repUnityObject.Networking.StageIdentifier.Equals
                (multipeerNetworking.StageIdentifier);

            if (isMine)
            {
              var targets = new HashSet<IPeer>(new[] {peerAddedArgs.Peer });

              var message =
                new SpawnMessage()
                {
                  Location = repUnityObject.transform.position,
                  Rotation = repUnityObject.transform.rotation,
                  NewId = repUnityObject.Id,
                  PrefabId = repUnityObject.PrefabId,
                };

              ARLog._DebugFormat
              (
                "New peer {0} added, informing them of NetworkedUnityObject {1}",
                false,
                peerAddedArgs.Peer.ToString(_debugPeerIdLength),
                repUnityObject.Id.RawId
              );
              streamReplicator.SendMessage(message, targets);
            }
          }
        };

      multipeerNetworking.Deinitialized +=
        (ignoredArgs) =>
        {
          _spawnMessageStreams.TryRemove(multipeerNetworking.StageIdentifier, out _);
          _destructorMessageStreams.TryRemove(multipeerNetworking.StageIdentifier, out _);
        };

      //If a peer leaves, locally destroy all objects belonging to that peer if they can be destroyed
      multipeerNetworking.PeerRemoved +=
        (removedArgs) =>
        {
          var peer = removedArgs.Peer;

          foreach (var pair in _spawnedObjects)
          {
            var key = pair.Key;
            var spawnedObject = pair.Value;
            if (!spawnedObject.CanDestroyIfDestructorLeaves(peer))
              continue;

            ARLog._DebugFormat
            (
              "Peer {0} left, destroying their spawned object {1}",
              false,
              peer.ToString(_debugPeerIdLength),
              spawnedObject.Id.RawId
            );
            var handler = NetworkObjectDestroyed;
            if (handler != null)
            {
              var args =
                new NetworkObjectLifecycleArgs(spawnedObject, multipeerNetworking, peer);

              handler(args);
            }

            // It is safe to remove an item from a ConcurrentDictionary during a foreach
            _spawnedObjects.TryRemove(key, out _);

            spawnedObject._isDestroyed = true;
            UnityEngine.Object.Destroy(spawnedObject.gameObject);
          }
        };
    }

    // This method is invoked by tests using reflection.
    private static void DestroyMessageStreamOnMessageReceive
    (
      NetworkId networkId, IPeer sender
    )
    {
      var args = new MessageReceivedEventArgs<NetworkId>(sender, networkId);
      DestroyMessageStreamOnMessageReceived(args);
    }

    private static void DestroyMessageStreamOnMessageReceived
    (
      MessageReceivedEventArgs<NetworkId> messageReceivedArgs
    )
    {
      var objectID = messageReceivedArgs.Message;
      NetworkedUnityObject networkedObject;
      _spawnedObjects.TryGetValue(objectID, out networkedObject);

      var peer = messageReceivedArgs.Sender;
      // This may occur when two valid peers destroy the same object simultaneously, or if a message
      // gets duplicated. (Or if some peer is cheating). In those cases, do nothing
      if (networkedObject == null || !networkedObject.IsDestructionAuthorizedPeer(peer))
      {
        ARLog._WarnFormat
        ("Received an invalid NetworkDestroy call from peer {0}, for object {1}",
          false,
          peer.ToString(_debugPeerIdLength),
          objectID
        );
        return;
      }

      _spawnedObjects.TryRemove(objectID, out _);
      networkedObject._isDestroyed = true;

      ARLog._DebugFormat
      (
        "Network destroying spawned object {0}, destroyed by peer {1}",
        false,
        objectID.RawId,
        peer.ToString(_debugPeerIdLength)
      );

      var handler = NetworkObjectDestroyed;
      if (handler != null)
      {
        var networking = networkedObject.Networking;
        var args = new NetworkObjectLifecycleArgs(networkedObject, networking, peer);
        handler(args);
      }

#if UNITY_EDITOR // Added specifically so this can be run in Unity unit tests
      UnityEngine.Object.DestroyImmediate(networkedObject.gameObject);
#else
      UnityEngine.Object.Destroy(networkedObject.gameObject);
#endif
    }

    private static void SpawnMessageStreamOnMessageReceive
    (
      SpawnMessage message,
      IPeer peer,
      IMessageStreamReplicator<SpawnMessage> stream
    )
    {
      // If the object already exists (duplicated message), do nothing
      if (_spawnedObjects.ContainsKey(message.NewId))
      {
        ARLog._DebugFormat("Object with id {0} already exists.",false, message.NewId);
        return;
      }

      NetworkedUnityObject prefab;

      if (!_loadedPrefabs.TryGetValue(message.PrefabId, out prefab))
      {
        ARLog._WarnFormat("No prefab with id: {0}.", false, message.PrefabId);
        return;
      }

      var newInstance = UnityEngine.Object.Instantiate(prefab, message.Location, message.Rotation);
      newInstance.Id = message.NewId;
      newInstance.Networking = stream.Group.Session.Networking;
      newInstance.SpawningPeer = peer;
      _spawnedObjects[message.NewId] = newInstance;
      ARLog._DebugFormat
      (
        "NetworkSpawned object with PrefabId {0}, RawId {1}, by peer {2}",
        false,
        message.PrefabId,
        message.NewId.RawId,
        peer.ToString(_debugPeerIdLength)
      );

      newInstance.Initialize();

      var handler = NetworkObjectSpawned;
      if (handler != null)
      {
        var args = new NetworkObjectLifecycleArgs(newInstance, newInstance.Networking, peer);
        handler(args);
      }
    }

    /// <summary>
    /// Loads all prefabs in the PrefabManifest to prepare for network spawning. This is
    /// automatically called by the NetworkSceneSpawnManifest MonoBehaviour.
    /// </summary>
    /// <param name="manifest">Manifest containing prefabs to load into memory</param>
    public static void LoadManifest(PrefabManifest manifest)
    {
      _InitializeIfNeeded();

      ARLog._Debug("NetworkSpawner loading prefab manifest");
      foreach (var prefab in manifest.Prefabs)
        _loadedPrefabs.TryAdd(prefab.PrefabId, prefab);
    }

    /// <summary>
    /// Unloads prefabs loaded from the PrefabManifest from memory. This is called by the
    /// NetworkSceneSpawnManifest MonoBehaviour
    /// </summary>
    /// <param name="manifest">Manifest containing prefabs to unload from memory</param>
    public static void UnloadManifest(PrefabManifest manifest)
    {
      ARLog._Debug("NetworkSpawner unloading prefab manifest");
      foreach (var prefab in manifest.Prefabs)
        _loadedPrefabs.TryRemove(prefab.PrefabId, out _);
    }

    /// <summary>
    /// Locally instantiates a prefab with given starting parameters, then sends a message across
    /// the network for all listening peers to also instantiate the same prefab.
    /// </summary>
    /// <param name="spawnObject">The prefab to spawn</param>
    /// <param name="networking">The networking stack on which to spawn the object</param>
    /// <param name="position">The initial position of the object</param>
    /// <param name="rotation">The initial rotation of the object</param>
    /// <param name="startingLocalRole">The starting role of the local peer</param>
    /// <param name="newNetId">The network ID of the new object, will be automatically assigned if empty</param>
    /// <param name="targetPeers">The peers for which to spawn the object</param>
    /// <returns>The spawned NetworkedUnityObject</returns>
    public static NetworkedUnityObject NetworkSpawn
    (
      this NetworkedUnityObject spawnObject,
      IMultipeerNetworking networking,
      Vector3? position = null,
      Quaternion? rotation = null,
      Role? startingLocalRole = null,
      NetworkId? newNetId = null,
      List<IPeer> targetPeers = null
    )
    {
      _InitializeIfNeeded();

      var newInstance =
        NetworkSpawnHelper
        (
          spawnObject,
          networking,
          position,
          rotation,
          startingLocalRole,
          newNetId,
          targetPeers
        );

      return newInstance;
    }

    /// <summary>
    /// Locally instantiates a prefab with given starting parameters, then sends a message across the
    /// network for all listening peers to also instantiate the same prefab.
    /// </summary>
    /// <param name="spawnObject">The prefab to spawn</param>
    /// <param name="position">The initial position of the object</param>
    /// <param name="rotation">The initial rotation of the object</param>
    /// <param name="startingLocalRole">The starting role of the local peer</param>
    /// <param name="newNetId">The network ID of the new object, will be automatically assigned if empty</param>
    /// <param name="targetPeers">The peers for which to spawn the object</param>
    /// <returns>The spawned NetworkedUnityObject</returns>
    public static NetworkedUnityObject NetworkSpawn
    (
      this NetworkedUnityObject spawnObject,
      Vector3? position = null,
      Quaternion? rotation = null,
      Role? startingLocalRole = null,
      NetworkId? newNetId = null,
      List<IPeer> targetPeers = null
    )
    {
      _InitializeIfNeeded();

      var newInstance =
        NetworkSpawnHelper
        (
          spawnObject,
          MultipeerNetworkingFactory.Networkings.First(),
          position,
          rotation,
          startingLocalRole,
          newNetId,
          targetPeers
        );

      return newInstance;
    }

    // Helper function for both versions of NetworkSpawn()
    private static NetworkedUnityObject NetworkSpawnHelper
    (
      this NetworkedUnityObject spawnObject,
      IMultipeerNetworking networking,
      Vector3? position = null,
      Quaternion? rotation = null,
      Role? startingLocalRole = null,
      NetworkId? newNetId = null,
      List<IPeer> targetPeers = null
    )
    {
      _InitializeIfNeeded();

      if (networking == null || !networking.IsConnected)
      {
        ARLog._Error("Trying to network spawn before the networking is connected");
        return null;
      }

      if (newNetId.HasValue && _spawnedObjects.ContainsKey(newNetId.Value))
      {
        ARLog._WarnFormat
        (
          "Object with id {0} already exists. Cannot spawn another one.",
          false,
          newNetId.Value
        );

        return null;
      }

      var newInstance = UnityEngine.Object.Instantiate(spawnObject);
      newInstance.Networking = networking;
      newInstance.SpawningPeer = networking.Self;

      if (position.HasValue)
        newInstance.transform.position = position.Value;

      if (rotation.HasValue)
        newInstance.transform.rotation = rotation.Value;

      if (newNetId.HasValue)
        newInstance.Id = newNetId.Value;
      else
      {
        _random.NextBytes(_idGenBuffer);
        var nextId = BitConverter.ToUInt64(_idGenBuffer, 0);
        newInstance.Id = (NetworkId)nextId;
      }

      ARLog._DebugFormat
      (
        "Local peer NetworkSpawned object with PrefabId {0}, RawId {1}",
        false,
        newInstance,
        newInstance.Id.RawId
      );

      if (startingLocalRole.HasValue)
        newInstance.Auth.TryClaimRole(startingLocalRole.Value, () => {}, () => {});

      if (targetPeers == null)
        targetPeers = networking.OtherPeers.ToList();

      var message =
        new SpawnMessage()
        {
          PrefabId = newInstance.PrefabId,
          NewId = newInstance.Id,
          Location = newInstance.transform.position,
          Rotation = newInstance.transform.rotation,
        };

      IMessageStreamReplicator<SpawnMessage> streamReplicator;
      if (_spawnMessageStreams.TryGetValue(networking.StageIdentifier, out streamReplicator))
        streamReplicator.SendMessage(message, targetPeers);

      _spawnedObjects[newInstance.Id] = newInstance;

      newInstance.Initialize();

      var handler = NetworkObjectSpawned;
      if (handler != null)
      {
        var args =
          new NetworkObjectLifecycleArgs
          (
            newInstance,
            newInstance.Networking,
            newInstance.Networking.Self
          );

        handler(args);
      }

      return newInstance;
    }

    /// <summary>
    /// Check if we are allowed to propagate the destruction message, then send it if allowed.
    /// If no one had previously destroyed the object, destroy it after sending the message. Only
    /// objects that were network spawned can be network destroyed
    /// </summary>
    /// <param name="networkedObject">The object to be destroyed</param>
    public static void NetworkDestroy(this NetworkedUnityObject networkedObject)
    {
      ARLog._DebugFormat
      (
        "Attempting to NetworkDestroy object {0}",
        false,
        networkedObject.Id.RawId
      );

      _InitializeIfNeeded();

      if (!networkedObject.IsDestructionAuthorizedPeer(networkedObject.Networking.Self))
      {
        if (networkedObject.SpawningPeer != null)
        {
          ARLog._WarnFormat
          (
            "Local peer is not a valid destructor for object: {0}",
            false,
            networkedObject.Id
          );
        }

        return;
      }

      _spawnedObjects.TryRemove(networkedObject.Id, out _);

      var networking = networkedObject.Networking;
      if (networking != null)
      {
        IMessageStreamReplicator<NetworkId> stream;
        if (_destructorMessageStreams.TryGetValue(networking.StageIdentifier, out stream))
          stream.SendMessage(networkedObject.Id, networking.OtherPeers);
      }

      // This is for the case that a valid peer destroys an object with the Object.Destroy method, and
      // will attempt to send a message to network destroy the object.
      // Todo (awang): make SendMessageImmediate that actually somewhat works on scene close
      if (networkedObject._isDestroyed)
        return;

      networkedObject._isDestroyed = true;

      var handler = NetworkObjectDestroyed;
      if (handler != null)
      {
        IPeer self = null;
        if (networking != null)
          self = networking.Self;

        var args = new NetworkObjectLifecycleArgs(networkedObject, networking, self);
        handler(args);
      }

#if UNITY_EDITOR // Added specifically so NetworkDestroy() can be run in Unity unit tests
      UnityEngine.Object.DestroyImmediate(networkedObject.gameObject);
#else
      UnityEngine.Object.Destroy(networkedObject.gameObject);
#endif
    }

    [Serializable]
    internal struct SpawnMessage
    {
      public NetworkId PrefabId { get; set; }
      public NetworkId NewId { get; set; }
      public Vector3 Location { get; set; }
      public Quaternion Rotation { get; set; }
    }

    // TODO: Create an ItemSerializer for SpawnMessage for performance. It is [Serializable] already
    /*
    private class SpawnMessageSerializationProvider: ISerializationProvider<SpawnMessage>
    {
      public static readonly ISerializationProvider<SpawnMessage> Shared =
        new SpawnMessageSerializationProvider
        (
          NetworkId.SerializationProvider.Shared,
          NetVector3.SerializationProvider.DefaultShared,
          NetQuaternion.SerializationProvider.DefaultShared
        );

      private readonly Serializer<SpawnMessage> _serializer;
      private readonly Deserializer<SpawnMessage> _deserializer;

      public SpawnMessageSerializationProvider
      (
        ISerializationProvider<NetworkId> networkIdSerializationProvider,
        ISerializationProvider<NetVector3> vectorSerializationProvider,
        ISerializationProvider<NetQuaternion> quaternionSerializationProvider
      )
      {
        _serializer =
          (data, writer) =>
          {
            networkIdSerializationProvider.Serializer(data.PrefabId, writer);
            networkIdSerializationProvider.Serializer(data.NewId, writer);
            vectorSerializationProvider.Serializer(data.Location, writer);
            quaternionSerializationProvider.Serializer(data.Rotation, writer);
          };

        _deserializer =
          reader => new SpawnMessage()
          {
            PrefabId = networkIdSerializationProvider.Deserializer(reader),
            NewId = networkIdSerializationProvider.Deserializer(reader),
            Location = vectorSerializationProvider.Deserializer(reader),
            Rotation = quaternionSerializationProvider.Deserializer(reader),
          };
      }

      public Serializer<SpawnMessage> Serializer
      {
        get
        {
          return _serializer;
        }
      }

      public Deserializer<SpawnMessage> Deserializer
      {
        get
        {
          return _deserializer;
        }
      }
    }*/
  }
}
