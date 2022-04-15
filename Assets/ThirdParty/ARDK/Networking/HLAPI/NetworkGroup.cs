// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Networking.HLAPI.Routing;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Networking.HLAPI
{
  /// <summary>
  /// A network group that is attached to an IHlapiSession. Handles routing to data handlers that
  ///   are registered to this group.
  /// </summary>
  internal sealed class NetworkGroup:
    NetworkedDataHandlerBase,
    INetworkGroup
  {
    private readonly NetworkId _id;
    private IHlapiSession _session;
    private UnhandledPacketCache _cachedPacketInfo;

    private readonly Dictionary<string, NetworkedDataHandlerBase> _handlerDict =
      new Dictionary<string, NetworkedDataHandlerBase>();

    private readonly List<INetworkGroup> _cachedNestedGroups = new List<INetworkGroup>();

    public NetworkGroup(NetworkId id)
    {
      _id = id;
      Identifier = _id.ToString();
    }

    /// <summary>
    /// Create a NetworkGroup that is attached to the IHlapiSession. Pass in any unhandled packets
    ///   so that they may be handled when Handlers are registered, or create a new one if there are
    ///   no unhandled packets (to cache packets addressed to unknown handlers).
    /// </summary>
    /// <param name="id"></param>
    /// <param name="session"></param>
    /// <param name="cache"></param>
    public NetworkGroup(NetworkId id, IHlapiSession session, UnhandledPacketCache cache):
      this(id)
    {
      _session = session;
      _cachedPacketInfo = cache ?? new UnhandledPacketCache();
    }

    public NetworkId NetworkId
    {
      get
      {
        return _id;
      }
    }

    /// <inheritdoc />
    public IHlapiSession Session
    {
      get
      {
        return _session;
      }
    }

    /// <inheritdoc />
    public void RegisterHandler(INetworkedDataHandler handler)
    {
      var handlerBase = handler as NetworkedDataHandlerBase;

      if (handlerBase == null)
        throw new Exception("Implement your INetworkedDataHandler as a NetworkedDataHandlerBase");

      if (handlerBase.Identifier == null)
        throw new Exception("Attempting to register a handler without an identifier");

      if (_handlerDict.ContainsKey(handlerBase.Identifier))
      {
        string message = "Already registered a handler with identifier: " + handlerBase.Identifier;
        throw new Exception(message);
      }

      // Tell the handler that this is now its group
      handlerBase.RegisterToGroup(this);

      // Register this handler to this group
      var identifier = handlerBase.Identifier;
      _handlerDict[identifier] = handlerBase;

      ARLog._DebugFormat
      (
        "Registered handler {0} to group {1}",
        false,
        handler.Identifier,
        _id.RawId
      );
      
      // If there are cached packets belonging to the new handler, queue a callback to handle these
      //  packets next frame. This is necessary since handling the packets now may lead to data that 
      //  will be processed before the handler's subscriptions are all set up.
      if (_cachedPacketInfo.PacketLookup.ContainsKey(identifier))
      {
        ARLog._DebugFormat
        (
          "Group {0} found cached data for handler {1}, queueing a handle request",
          false,
          _id.RawId,
          identifier
        );
        _CallbackQueue.QueueCallback
        (
          () =>
          {
            if (!_cachedPacketInfo.PacketLookup.ContainsKey(identifier))
            {
              ARLog._WarnFormat
              (
                "Handling queued data failed for identifier {0}, no data found",
                false,
                identifier
              );
              return;
            }

            _cachedPacketInfo.HandleCached(identifier, handlerBase);

            _cachedPacketInfo.PacketLookup.Remove(identifier);
          }
        );
      }
    }

    /// <inheritdoc />
    public INetworkGroup CreateNestedGroup(NetworkId groupId)
    {
      if (_handlerDict.ContainsKey(groupId.ToString()))
      {
        string message = "Already registered a handler with identifier: " + groupId.ToString();
        throw new Exception(message);
      }

      var newGroup = new NetworkGroup(groupId);

      if (_session == null)
        _cachedNestedGroups.Add(newGroup);
      else
        newGroup.ReceiveCachedDataFromSession(_session, new UnhandledPacketCache());

      RegisterHandler(newGroup);
      ARLog._DebugFormat
      (
        "Created a nested group with Id {0}, attached to group {1}",
        false,
        groupId.RawId,
        _id.RawId
      );
      return newGroup;
    }

    /// <inheritdoc />
    public void ReceiveCachedDataFromSession(IHlapiSession session, UnhandledPacketCache cache)
    {
      _session = session;
      _cachedPacketInfo = cache ?? new UnhandledPacketCache();

      if (_cachedNestedGroups.Count > 0)
        foreach (var group in _cachedNestedGroups)
          group.ReceiveCachedDataFromSession(_session, new UnhandledPacketCache());
    }

    [Serializable]
    internal struct _NetworkGroupData
    {
      internal string _key;
      internal object _data;
    }

    /// <inheritdoc />
    protected override void HandleReceivedData
    (
      object allData,
      IPeer sender,
      ReplicationMode replicationMode
    )
    {
      var removingUnknowns = new HashSet<string>();

      foreach (var unknownHandler in _cachedPacketInfo.PacketLookup)
      {
        // Try to lookup cached unknown channels
        var handler = _handlerDict.GetOrDefault(unknownHandler.Key);

        if (handler == null)
          continue;
        
        // Have the channel handle its cached messages
        _cachedPacketInfo.HandleCached(unknownHandler.Key, handler);

        removingUnknowns.Add(unknownHandler.Key);
      }

      foreach (var removingUnknown in removingUnknowns)
        _cachedPacketInfo.PacketLookup.Remove(removingUnknown);

      var array = (_NetworkGroupData[])allData;
      foreach (var groupData in array)
      {
        NetworkedDataHandlerBase handler;

        //If the channel is known, receive data, otherwise, cache it
        if (_handlerDict.TryGetValue(groupData._key, out handler))
          handler.InternalReceiveData(groupData._data, sender, replicationMode);
        else
        {
          //If the transport type is not reliable, do not cache the data
          bool shouldSkip = 
            replicationMode.Transport != TransportType.ReliableOrdered &&
            replicationMode.Transport != TransportType.ReliableUnordered;
          
          if (shouldSkip)
            continue;

          var unknownPacketQueue = _cachedPacketInfo.PacketLookup.GetOrInsertNew(groupData._key);
          unknownPacketQueue.Enqueue(new PacketInfo(groupData._data, sender, replicationMode));
        }
      }
    }

    /// <inheritdoc />
    public override void Unregister()
    {
      // If this is not a nested group, close it from the session
      if (Group == null)
      {
        _session.UnregisterGroup(this);
        return;
      }

      // Otherwise, close it from the owning group
      base.Unregister();
    }

    /// <inheritdoc />
    public void UnregisterHandler(INetworkedDataHandler handler)
    {
      if (!_handlerDict.ContainsKey(handler.Identifier))
        return;

      ARLog._DebugFormat
      (
        "Unregistering handler {0} from group {1}",
        false,
        handler.Identifier,
        _id.RawId
      );
      _handlerDict[handler.Identifier].UnregisterFromGroup();
      _handlerDict.Remove(handler.Identifier);
    }

    /// <inheritdoc />
    protected override object GetDataToSend
    (
      ICollection<IPeer> targetPeers,
      ReplicationMode mode
    )
    {
      var list = new List<_NetworkGroupData>();

      foreach (var handlerPair in _handlerDict)
      {
        var handlerResult = handlerPair.Value.InternalGetDataToSend(targetPeers, mode);

        if (handlerResult != NothingToWrite)
        {
          ARLog._DebugFormat
          (
            "Enqueueing data from handler {0}",
            true,
            handlerPair.Key
          );
          
          list.Add
          (
            new _NetworkGroupData
            {
              _key = handlerPair.Key, _data = handlerResult
            }
          );
        }
      }

      if (list.Count == 0)
        return NothingToWrite;

      return list.ToArray();
    }
  }

  /// <summary>
  /// Definition of a class that caches and handles packets belonging to unknown Groups/Handlers.
  /// Follows the Group/Handler hierarchy (one UnhandledPacketCache per group, with a dictionary
  /// of strings, which map to handlers' Identifiers).
  /// </summary>
  public sealed class UnhandledPacketCache
  {
    public readonly Dictionary<string, Queue<PacketInfo>> PacketLookup =
      new Dictionary<string, Queue<PacketInfo>>();

    /// <summary>
    /// Attempt to cache a payload in this group. Shares a static data count with all other cached
    ///   packets sent through the HLAPI.
    // TODO: Size limit for cached data
    /// </summary>
    /// <param name="data">Payload to cache</param>
    /// <param name="sender">Peer that sent the data</param>
    /// <param name="replicationMode">Information about the transport type and whether it is an
    ///   initial message</param>
    public void AttemptCachePayload(object data, IPeer sender, ReplicationMode replicationMode)
    {
      var array = (NetworkGroup._NetworkGroupData[])data;
      foreach (var groupData in array)
      {
        ARLog._DebugFormat("Caching data for identifier {0}", false, groupData._key);
        var packet = PacketLookup.GetOrInsertNew(groupData._key);
        packet.Enqueue(new PacketInfo(groupData._data, sender, replicationMode));
      }
    }

    /// <summary>
    /// Handle data that is cached with the handler's Identifier
    /// </summary>
    /// <param name="identifier">Identifier to look for in cached packets</param>
    /// <param name="handler">Handler to handle cached data with</param>
    public void HandleCached(string identifier, NetworkedDataHandlerBase handler)
    {
      ARLog._DebugFormat
      (
        "Handling cached data for identifier {0}, found {1} packets",
        false,
        identifier,
        PacketLookup[identifier].Count
      );

      foreach (var packetInfo in PacketLookup[identifier])
        handler.InternalReceiveData(packetInfo.Data, packetInfo.Sender, packetInfo.ReplicationMode);
    }
  }
}
