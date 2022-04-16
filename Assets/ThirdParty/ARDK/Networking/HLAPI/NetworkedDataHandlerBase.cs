// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Networking.HLAPI.Data;

namespace Niantic.ARDK.Networking.HLAPI
{
  /// <summary>
  /// Abstract class to extend to implement custom data handlers for the HLAPI. The protected
  ///   methods will define the data handler's behaviour with sending/receiving data, but cannot
  ///   be accessed through the INetworkedDataHandler interface (users should never call these methods)
  /// </summary>
  public abstract class NetworkedDataHandlerBase: 
    INetworkedDataHandler
  {
    // Return this value during WriteToDataBuilder if this handler result should be ignored
    // altogether.
    public static readonly object NothingToWrite = new object();
    
    /// <inheritdoc />
    public string Identifier { get; internal set; }

    /// <inheritdoc />
    public INetworkGroup Group { get; private set; }

    /// <summary>
    /// Inform this handler that the group has registered this handler. A handler cannot be registered
    ///   to more than one group at a time.
    /// </summary>
    /// <param name="group">The group that has registered this handler</param>
    internal void RegisterToGroup(INetworkGroup group)
    {
      if (Group != null)
      {
        throw new Exception(
          String.Format(
            "This handler: {0} has already been registered to group: {1}",
            Identifier,
            Group.Identifier));
      }

      Group = group;
    }

    internal void UnregisterFromGroup()
    {
      if (Group == null)
      {
        throw new Exception(
          String.Format(
            "Attempting to unregister handler: {0} that was never registered to a group",
            Identifier));
      }

      Group = null;
    }

    /// <summary>
    /// Override this method to define how the custom data handler will write data (ie, transport type,
    /// what data to send, when to send it, etc).
    /// </summary>
    protected abstract object GetDataToSend(
      ICollection<IPeer> targetPeers,
      ReplicationMode replicationMode);

    /// <summary>
    /// Override this method to define how the custom data handler will receive data. It is guaranteed
    ///   that the received payload will follow the same format as the data written by this handler
    ///   (the data that is written in GetDataToSend)
    /// </summary>
    protected abstract void HandleReceivedData(
      object data,
      IPeer sender,
      ReplicationMode replicationMode);

    /// <summary>
    /// Only called by INetworkGroup, each time the group's manager calls SendQueuedData()
    /// </summary>
    internal object InternalGetDataToSend(
      ICollection<IPeer> targetPeers,
      ReplicationMode replicationMode)
    {
      return GetDataToSend(targetPeers, replicationMode);
    }

    /// <summary>
    /// Only called by INetworkGroup, each time the group's manager receives data addressed to this handler
    /// </summary>
    internal void InternalReceiveData(
      object data,
      IPeer sender,
      ReplicationMode replicationMode)
    {
      HandleReceivedData(data, sender, replicationMode);
    }

    /// <summary>
    /// Returns the self peer if possible, otherwise returns null. For example, if the group's manager
    ///   has not yet been attached to an IMultipeerNetworking, there is no concept of Self, so
    ///   return null.
    /// </summary>
    /// <returns></returns>
    public IPeer GetSelfOrNull()
    {
      if (Group == null || Group.Session == null || Group.Session.Networking == null)
        return null;

      return Group.Session.Networking.Self;
    }

    /// <summary>
    /// Remove this data handler from its current group. No more data will be sent/received by this
    ///   handler until it is registered to a new group
    /// </summary>
    public virtual void Unregister()
    {
      if (Group == null)
        return;

      Group.UnregisterHandler(this);
      Group = null;
    }
  }
}
