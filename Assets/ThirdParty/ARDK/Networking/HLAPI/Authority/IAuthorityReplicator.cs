// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.Networking.HLAPI.Authority
{
  /// <summary>
  /// A replicator that will replicate authority info about a network entity.
  /// </summary>
  public interface IAuthorityReplicator: INetworkedDataHandler
  {
    /// <summary>
    /// The local role of this network entity.
    /// </summary>
    Role LocalRole { get; }

    /// <summary>
    /// Gets all peers that are of a given role.
    /// This method should never return null, if needed an empty collection is returned.
    /// </summary>
    IReadOnlyCollection<IPeer> PeersOfRole(Role role);

    /// <summary>
    /// Gets the role of a peer.
    /// </summary>
    Role RoleOfPeer(IPeer peer);

    /// <summary>
    /// Trys to claim a role for the local peer.
    /// </summary>
    /// <param name="role">The role to claim.</param>
    /// <param name="onPass">Called if the role could be claimed.</param>
    /// <param name="onFail">Called if the role could not be claimed.</param>
    void TryClaimRole(Role role, Action onPass, Action onFail);
  }


  public static class AuthorityReplicatorExtension
  {
    public static IPeer PeerOfRole(this IAuthorityReplicator replicator, Role role)
    {
      return replicator.PeersOfRole(role).FirstOrDefault();
    }
  }
}
