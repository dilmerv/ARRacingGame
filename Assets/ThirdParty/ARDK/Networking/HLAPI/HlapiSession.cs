// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Routing;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.BinarySerialization;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Niantic.ARDK.Networking.HLAPI
{
  /// <summary>
  /// A <see cref="IHlapiSession"/> that will hook up with <see cref="MultipeerNetworkingFactory"/>.
  /// </summary>
  public sealed class HlapiSession:
    IHlapiSession
  {
    private readonly uint _messageTag;

    private readonly Dictionary<NetworkId, INetworkGroup> _groupLookup =
      new Dictionary<NetworkId, INetworkGroup>();

    private readonly Dictionary<NetworkId, UnhandledPacketCache> _unhandledPackets =
      new Dictionary<NetworkId, UnhandledPacketCache>();

    private readonly List<HashSet<IPeer>> _broadcastGroups = new List<HashSet<IPeer>>();
    private readonly HashSet<HashSet<IPeer>> _sentGroups = new HashSet<HashSet<IPeer>>();

    private IMultipeerNetworking _networking;

    private readonly TransportType[] _transportTypeList =
    {
      TransportType.ReliableOrdered,
      TransportType.ReliableUnordered,
      TransportType.UnreliableOrdered,
      TransportType.UnreliableUnordered
    };

    private readonly MemoryStream _cachedStream = new MemoryStream(1024);

    /// <inheritdoc />
    public IMultipeerNetworking Networking
    {
      get
      {
        return _networking;
      }
    }

    /// <inheritdoc />
    public void UnregisterGroup(INetworkGroup group)
    {
      ARLog._DebugFormat
      (
        "Removing group {0} from HlapiSession with tag {1}",
        false,
        group.NetworkId.RawId,
        _messageTag
      );
      _groupLookup.Remove(group.NetworkId);
    }

    /// <inheritdoc />
    public INetworkGroup CreateAndRegisterGroup(NetworkId groupId)
    {
      if (_groupLookup.ContainsKey(groupId))
        throw new ArgumentException("Group id " + groupId + " already exists");

      var unhandledPackets = _unhandledPackets.GetOrDefault(groupId);

      _groupLookup[groupId] = new NetworkGroup(groupId, this, unhandledPackets);

      ARLog._DebugFormat
      (
        "Created and registered group {0} to HlapiSession with tag {1}",
        false,
        groupId.RawId,
        _messageTag
      );

      if (unhandledPackets != null)
      {
        _unhandledPackets.Remove(groupId);
        ARLog._DebugFormat("Removing cached packets for group: {0}", false, groupId.RawId);
      }

      return _groupLookup[groupId];
    }

    /// <inheritdoc />
    public void RegisterGroup(INetworkGroup group)
    {
      var groupId = group.NetworkId;

      if (_groupLookup.ContainsKey(groupId))
        throw new ArgumentException("Group id " + groupId + " already exists");

      var handlerBase = group as NetworkedDataHandlerBase;
      if (handlerBase == null)
        throw new Exception("Implement your INetworkedDataHandler as a NetworkedDataHandlerBase");

      var unhandledPackets = _unhandledPackets.GetOrDefault(groupId);

      group.ReceiveCachedDataFromSession(this, unhandledPackets);

      _groupLookup[groupId] = group;

      ARLog._DebugFormat
      (
        "Registered group {0} to HlapiSession with tag {1}",
        false,
        groupId.RawId,
        _messageTag
      );

      if (unhandledPackets != null)
      {
        _unhandledPackets.Remove(groupId);
        ARLog._DebugFormat("Removing cached packets for group: {0}", false, groupId.RawId);
      }
    }

    /// <summary>
    /// Create an unmanaged HlapiSession (must be manually ticked) that will
    ///   attach itself to the first IMultipeerNetworking that is created and send all data over
    ///   that network. If an IMultipeerNetworking has already been initialized, this will
    ///   automatically attach itself that that networking (will attach itself to the first
    ///   networking on MultipeerNetworking.Networkings if there are multiple already).
    /// </summary>
    /// <param name="messageTag">Message tag that all messages pertaining to this manager
    ///   will use</param>
    public HlapiSession(uint messageTag)
    {
      _messageTag = messageTag;

      MultipeerNetworkingFactory.NetworkingInitialized += OnAnyMultipeerSessionDidInitialize;
      ARLog._Debug
      (
        "Created an HlapiSession using tag {0}, waiting for the first networking initialized"
      );
    }

    /// <summary>
    /// Create an unmanaged HlapiSession (must be manually ticked) that will
    ///   attach itself to the specified IMultipeerNetworking. This is useful if you want
    ///   to manage ticking (message batching + sending) behaviour, rather than rely on
    ///   the automatic per-frame sending behaviour
    /// </summary>
    /// <param name="messageTag">Message tag that all messages pertaining to this manager
    ///   will use</param>
    /// <param name="networking">The networking session this HLAPI session will use</param>
    public HlapiSession(uint messageTag, IMultipeerNetworking networking)
    {
      _messageTag = messageTag;

      ARLog._DebugFormat("Created an HlapiSession using tag {0}", false, _messageTag);
      OnAnyMultipeerSessionDidInitialize(new AnyMultipeerNetworkingInitializedArgs(networking));
    }

    /// <summary>
    /// Call this to query all attached groups (and their attached INetworkedDataHandlers) for any
    ///   data that they have accumulated and send that data. The specific data that will be written
    ///   and sent depends on the implementation of the handler (ie. a transform packer will only
    ///   check and send the transform at the time of this call, regardless of how many changes
    ///   there have been between calls to this method, while a message stream will send all queued
    ///   messages between the previous and current calls).
    /// Data that is received will be processed when the message is received, and is independent of calling
    ///   this method
    /// </summary>
    public void SendQueuedData()
    {
      // Only tick if there is actually a network
      if (_networking == null)
      {
        ARLog._WarnFormat
        (
          "Ticking HlapiSession with tag {0}, but it is not connected to a networking",
          true,
          _messageTag
        );
        return;
      }

      // Query each group/handler with each transport type, since they may have varying protocols
      foreach (var transportType in _transportTypeList)
      {
        foreach (var broadcastGroup in _broadcastGroups)
        {
          // Build a payload that pertains to this specific peer group/transport type
          var stream = _cachedStream;
          stream.Position = 0;
          stream.SetLength(0);

          // Initial message flag for each new group
          var isInitial = !_sentGroups.Contains(broadcastGroup);
          stream.WriteByte(isInitial ? (byte)1 : (byte)0);

          var dataStartMarker = stream.Position;

          var replicationMode = new ReplicationMode();
          replicationMode.IsInitial = isInitial;
          replicationMode.Transport = transportType;

          // Query groups and attempt to write data to the buffer
          object data = GetDataToSend(broadcastGroup, replicationMode);
          if (data != NetworkedDataHandlerBase.NothingToWrite)
            using (var serializer = new BinarySerializer(stream))
              serializer.Serialize(data);

          // If any data has been written, send the data to the relevant peer(s)
          long distance = stream.Position - dataStartMarker;
          if (Math.Abs(distance) > 0)
          {
            var buffer = stream.ToArray();

            IPeer[] targets;
            if (broadcastGroup.Count == 1 && _networking.OtherPeers.Count > 1)
              targets = new[] { broadcastGroup.First() };
            else
              targets = _networking.OtherPeers.ToArray();

            _networking.SendDataToPeers(_messageTag, buffer, targets, transportType);
          }
        }
      }

      // Remember that every group has had an initial message sent
      _sentGroups.UnionWith(_broadcastGroups);
    }

    // Upon any changes to the peer list, regenerate the broadcast group list (each individual peer,
    // as well as all peers in the session)
    private void RefreshBroadcastGroups()
    {
      _broadcastGroups.Clear();

      var remotePeers = _networking.OtherPeers;

      if (remotePeers == null)
        return;

      foreach (var peer in remotePeers)
        _broadcastGroups.Add(new HashSet<IPeer>(new[] {peer}));

      if (remotePeers.Count > 1)
        _broadcastGroups.Add(new HashSet<IPeer>(remotePeers));
    }

    public void Dispose()
    {
      MultipeerNetworkingFactory.NetworkingInitialized -= OnAnyMultipeerSessionDidInitialize;

      if (_networking == null)
        return;

      _networking.PeerDataReceived -= OnDidReceiveDataFromPeer;
      _networking.PeerAdded -= OnDidAddPeer;
      _networking.PeerRemoved -= OnDidRemovePeer;
      _networking.Connected -= OnDidConnect;
    }

    // This is only subscribed to if no IMultipeerNetworking is explicitly passed in during
    // construction.
    private void OnAnyMultipeerSessionDidInitialize(AnyMultipeerNetworkingInitializedArgs args)
    {
      if (_networking != null)
        return;

      var networking = args.Networking;
      _networking = networking;

      networking.PeerDataReceived += OnDidReceiveDataFromPeer;

      networking.PeerAdded += OnDidAddPeer;
      networking.PeerRemoved += OnDidRemovePeer;

      networking.Connected += OnDidConnect;
    }

    // Handle data that is received
    private void OnDidReceiveDataFromPeer(PeerDataReceivedArgs args)
    {
      // If the data does not belong this manager, do nothing
      if (args.Tag != _messageTag)
        return;

      using (var stream = args.CreateDataReader())
      {
        // Attempt to pass the received data to the corresponding group(s)
        var isInitial = stream.ReadByte() == 1;
        
        var array = (_NetworkIdAndData[])GlobalSerializer.Deserialize(stream);
        
        var replicationMode = new ReplicationMode();
        replicationMode.Transport = args.TransportType;
        replicationMode.IsInitial = isInitial;

        ARLog._DebugFormat
        (
          "HlapiSession {0} got data containing {1} elements",
          true,
          _messageTag,
          array.Length
        );
        ReceiveData(array, args.Peer, replicationMode);
      }
    }

    private void OnDidAddPeer(PeerAddedArgs args)
    {
      RefreshBroadcastGroups();
    }

    private void OnDidRemovePeer(PeerRemovedArgs args)
    {
      RefreshBroadcastGroups();
    }

    private void OnDidConnect(ConnectedArgs args)
    {
      RefreshBroadcastGroups();
    }
    
    // TODO: Find a better place for this.
    [Serializable]
    internal struct _NetworkIdAndData
    {
      internal NetworkId _networkId;
      internal object _data;
    }

    /// <summary>
    /// Called when the session should send data.
    /// </summary>
    /// <param name="target">The target peers to send to.</param>
    /// <param name="mode">The mode to send.</param>
    private object GetDataToSend(HashSet<IPeer> target, ReplicationMode mode)
    {
      var list = new List<_NetworkIdAndData>();
      
      // Query each attached group for data to be written
      foreach (var networkGroupPair in _groupLookup)
      {
        var handler = (NetworkedDataHandlerBase)networkGroupPair.Value;
        var data = handler.InternalGetDataToSend(target, mode);

        if (data != NetworkedDataHandlerBase.NothingToWrite)
        {
          ARLog._DebugFormat
          (
            "HlapiSession {0} is queueing some data for group {1}",
            true,
            _messageTag,
            networkGroupPair.Key
          );
          list.Add
          (
            new _NetworkIdAndData
            {
              _networkId = networkGroupPair.Key, _data = data
            }
          );
        }
      }

      if (list.Count == 0)
        return NetworkedDataHandlerBase.NothingToWrite;

      return list.ToArray();
    }

    /// <summary>
    /// Called to receive data from a payload.
    /// </summary>
    /// <param name="payload">The payload to receive.</param>
    /// <param name="from">The peer it is from.</param>
    /// <param name="mode">The mode it was sent with.</param>
    private void ReceiveData(_NetworkIdAndData[] array, IPeer from, ReplicationMode mode)
    {
      foreach (var pair in array)
      {
        INetworkGroup group;

        // If there is a group that can handle this data (same NetworkId), give it to that group;
        //   else, cache reliable messages until the relevant group is opened.
        bool found = _groupLookup.TryGetValue(pair._networkId, out group);
        var handler = group as NetworkedDataHandlerBase;
        if (found && handler != null)
        {
          ARLog._DebugFormat
          (
          "HlapiSession {0} routing data to group {1}",
            true,
            _messageTag,
            group.NetworkId.RawId
          );
          handler.InternalReceiveData(pair._data, from, mode);
        }
        else
        {
          //If the transport type is not reliable, do not cache the data
          bool mustSkip = 
            mode.Transport != TransportType.ReliableOrdered &&
            mode.Transport != TransportType.ReliableUnordered;

          if (mustSkip)
            continue;

          var unknownGroup =
            _unhandledPackets.GetOrInsert(pair._networkId, () => new UnhandledPacketCache());

          ARLog._DebugFormat
          (
            "HlapiSession {0} could not find group {1}, attempting to cache data",
            true,
            _messageTag,
            pair._networkId.RawId
          );
          unknownGroup.AttemptCachePayload(pair._data, from, mode);
        }
      }
    }
  }

  /// <summary>
  /// Provides a HlapiSession that is tied to each _NativeMultipeerNetworking instance
  /// that is created. These HlapiSessions can be accessed with the Guid of the networking instance,
  /// and will be destroyed when the networking instance is destroyed.
  /// </summary>
  [DefaultExecutionOrder(Int32.MinValue)]
  public static class HlapiSessionExtension
  {
    // Arbitrary, but constant, tag so that all peers are in agreement of the tag to use for
    //   the HlapiSession
    private const uint HlapiSessionTag = 920348;

    private static readonly object _sessionCreationLock = new object();
    
    private static readonly ConcurrentDictionary<Guid, HlapiSession>
      _managedSessionLookup = new ConcurrentDictionary<Guid, HlapiSession>();

    private static readonly _ReadOnlyDictionary<Guid, HlapiSession> _roManagedSessionLookup;

    private static readonly ConcurrentDictionary<Scene, HlapiSession>
      _sceneSessionLookup = new ConcurrentDictionary<Scene, HlapiSession>();

    static HlapiSessionExtension()
    {
      _roManagedSessionLookup = new _ReadOnlyDictionary<Guid, HlapiSession>(_managedSessionLookup);
    }

    /// <summary>
    /// The currently active HlapiSessions managed by this class
    /// </summary>
    public static IReadOnlyDictionary<Guid, HlapiSession> ManagedSessionLookup
    {
      get { return _roManagedSessionLookup; }
    }

    public static HlapiSession GetOrCreateManagedSession(this Scene scene)
    {
      HlapiSession session;
      if (_sceneSessionLookup.TryGetValue(scene, out session))
        return session;
      
      // Ideally we should use GetOrAdd from the ConcurrentDictionary. Unfortunately, GetOrAdd
      // allows 2 values to be created in parallel and simply lets one of them "be garbage
      // collected". The problem is that the HlapiSession actually registers itself in static
      // events, so we should not allow 2 or more instances to be created in parallel.
      lock (_sessionCreationLock)
      {
        // Check again inside the lock. If the value is still not there, then we can safely
        // create and add it.
        if (_sceneSessionLookup.TryGetValue(scene, out session))
          return session;
        
        var sceneHashCodeAsUint = (uint)scene.name.GetHashCode();
        ARLog._DebugFormat("Creating an HlapiSession with tag {0}", false, sceneHashCodeAsUint);
        session = new HlapiSession(sceneHashCodeAsUint);
        
        if (!_sceneSessionLookup.TryAdd(scene, session))
          throw new InvalidOperationException("2 sessions were created for the same scene.");
      }

      return session;
    }

    /// <summary>
    /// Gets or creates a HlapiSession that corresponds to a specific networking
    /// instance.
    /// </summary>
    /// <param name="networking">The networking instance to get a HlapiSession for</param>
    /// <returns>The HlapiSession that corresponds to the input network</returns>
    public static HlapiSession GetOrCreateManagedSession(this IMultipeerNetworking networking)
    {
      HlapiSession session;

      var stageIdentifier = networking.StageIdentifier;
      if (_managedSessionLookup.TryGetValue(stageIdentifier, out session))
        return session;
      
      // Similar to GetOrCreateManagedSession, we need to guarantee we never create 2 sessions
      // in parallel.
      lock(_sessionCreationLock)
      {
        // During the lock acquisition, a session might have been created. So check again.
        if (_managedSessionLookup.TryGetValue(stageIdentifier, out session))
          return session;
        
        // Now we can safely create a new session.
        
        ARLog._DebugFormat
        (
          "Creating an HlapiSession with tag {0}, attached to networking {1}",
          false,
          HlapiSessionTag, 
          stageIdentifier
        );
        
        session = new HlapiSession(HlapiSessionTag, networking);
        if (!_managedSessionLookup.TryAdd(stageIdentifier, session))
          throw new InvalidOperationException("Duplicated StageIdentifier.");

        networking.Deinitialized +=
          (ignored) => _managedSessionLookup.TryRemove(stageIdentifier, out _);
      }

      return session;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Startup()
    {
      _UpdateLoop.LateTick += Update;
      SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private static void Update()
    {
      foreach (var session in _managedSessionLookup.Values)
        session.SendQueuedData();

      foreach (var session in _sceneSessionLookup.Values)
        session.SendQueuedData();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
      _sceneSessionLookup.TryRemove(scene, out _);
    }
  }
}
