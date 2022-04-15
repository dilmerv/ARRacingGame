// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Networking.HLAPI.Data;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Networking.HLAPI.Authority
{
  /// <summary>
  /// <see cref="IAuthorityReplicator"/> That will greedily take roles. If it can be proven locally that the role is
  /// able to be taken by the local peer, then the local peer will assume it can take the role, otherwise it will fail.
  /// This can result in race conditions if two peers try to claim a role at the same time so it should only be used in
  /// scenarios where authority can be statically determined (such as authority is always host).
  /// </summary>
  public sealed class GreedyAuthorityReplicator: 
    NetworkedDataHandlerBase,
    IAuthorityReplicator
  {
    private Dictionary<IPeer, Role> _peerToRoleLookup = new Dictionary<IPeer, Role>();

    private Dictionary<Role, HashSet<IPeer>> _roleToPeerLookup =
      new Dictionary<Role, HashSet<IPeer>>();

    private bool _isDirty;

    /// <param name="identifier">The identifier of this</param>
    /// <param name="group">The group to use</param>
    public GreedyAuthorityReplicator(string identifier, INetworkGroup group)
    {
      Identifier = identifier;

      if (group == null)
        return;

      group.RegisterHandler(this);
    }

    public Role LocalRole
    {
      get
      {
        var self = GetSelfOrNull();

        return self == null ? Role.None : RoleOfPeer(Group.Session.Networking.Self);
      }
    }

    public IReadOnlyCollection<IPeer> PeersOfRole(Role role)
    {
      return _roleToPeerLookup.GetOrInsertNew(role).AsArdkReadOnly();
    }

    public Role RoleOfPeer(IPeer peer)
    {
      return _peerToRoleLookup.GetOrInsert(peer, Role.None);
    }

    public void TryClaimRole(Role role, Action onPass, Action onFail)
    {
      var self = GetSelfOrNull();

      if (self == null) 
      {
        onFail();
        return;
      }

      var allowMultiple = role != Role.Authority;

      var peersOfSameRole = PeersOfRole(role);

      if (peersOfSameRole.Count > 1 && !allowMultiple)
      {
        onFail();
        return;
      }

      if (peersOfSameRole.Count == 1 && !allowMultiple && !peersOfSameRole.Contains(self))
      {
        onFail();
        return;
      }

      ChangeRoleOfPeer(self, role);
      _isDirty = true;

      onPass();
    }

    private void ChangeRoleOfPeer(IPeer peer, Role newRole)
    {
      var prevRole = RoleOfPeer(peer);
      _roleToPeerLookup.GetOrInsertNew(prevRole).Remove(peer);
      _roleToPeerLookup.GetOrInsertNew(newRole).Add(peer);
      _peerToRoleLookup[peer] = newRole;

      ARLog._DebugFormat
      (
        "Authority Replicator on group {0} updating role of peer {1} from {2} to {3}",
        false,
        Group.NetworkId.RawId,
        peer,
        prevRole,
        newRole
      );
    }

    protected override object GetDataToSend
    (
      ICollection<IPeer> targetPeers,
      ReplicationMode replicationMode
    )
    {
      if (replicationMode.Transport != TransportType.ReliableOrdered)
        return NothingToWrite;

      // Only send if there is a pending role transfer, or it is an initial com.
      if (!_isDirty && !replicationMode.IsInitial)
        return NothingToWrite;

      var targetPeersHash = new HashSet<IPeer>(targetPeers);

      // Only send if broadcasting or it's an initial com.
      if (!targetPeersHash.SetEquals(Group.Session.Networking.OtherPeers) && !replicationMode.IsInitial)
        return NothingToWrite;

      // Clear the dirty bit when sending as a broadcast.
      if (targetPeersHash.SetEquals(Group.Session.Networking.OtherPeers))
        _isDirty = false;

      return LocalRole;
    }

    protected override void HandleReceivedData
    (
      object data,
      IPeer sender,
      ReplicationMode replicationMode
    )
    {
      ChangeRoleOfPeer(sender, (Role)data);
    }
  }
}
