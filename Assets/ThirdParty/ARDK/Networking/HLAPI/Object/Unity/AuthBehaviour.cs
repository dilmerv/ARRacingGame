// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.Networking.HLAPI.Authority;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

/// @namespace Niantic.ARDK.Networking.HLAPI.Object.Unity
/// @brief Unity specific objects that will be shared over a network
namespace Niantic.ARDK.Networking.HLAPI.Object.Unity
{
  /// <summary>
  /// A NetworkedBehaviour that can be attached to a NetworkedUnityObject to automatically open
  /// channels and handle Authority over the network.  
  /// </summary>
  public sealed class AuthBehaviour: 
    NetworkedBehaviour,
    IAuthorityReplicator
  {
    private const string AUTH_IDENTIFIER = "authBehaviour";
    private IAuthorityReplicator _authorityReplicatorImplementation;
    private bool _alreadyUnregistered;

    /// <summary>
    /// Whether or not the host of the session claims the role of Authority upon connection
    /// </summary>
    [SerializeField]
    private bool _ownedByHost = true;

    /// <summary>
    /// Whether or not all peers of the session claim the role of Observer upon connection. This is
    /// superseded by _ownedByHost for the host of the session
    /// </summary>
    [SerializeField]
    private bool _observeByDefault = true;

    private Role? _startupRole;
    private Action _pass = () => {};
    private Action _fail = () => {};

    private IMultipeerNetworking _networking;

    protected override void SetupSession(out Action initializer, out int order)
    {
      initializer = () =>
      {
        Owner.Networking.Connected += 
          (args) =>
          {
            var gameObjectName = gameObject.name;
            ARLog._DebugFormat
            (
              "AuthBehaviour on {0} creating a replicator",
              false,
              gameObjectName
            );
            _authorityReplicatorImplementation =
              new GreedyAuthorityReplicator
              (
                AUTH_IDENTIFIER, 
                _alreadyUnregistered ? null : Owner.Group
              );

            if (_ownedByHost && args.IsHost)
              _startupRole = Role.Authority;

            if (_startupRole.HasValue)
            {
              ARLog._DebugFormat
              (
                "AuthBehaviour on {0} claiming role {1}",
                false,
                gameObjectName,
                _startupRole.Value
              );
              _authorityReplicatorImplementation.TryClaimRole(_startupRole.Value, _pass, _fail);
            }
            else if (_observeByDefault)
            {
              ARLog._DebugFormat
              (
                "AuthBehaviour on {0} claiming role {1}",
                false,
                gameObjectName,
                Role.Observer
              );
              _authorityReplicatorImplementation.TryClaimRole(Role.Observer, _pass, _fail);
            }
          };
      };

      order = int.MinValue;
    }

    /// <inheritdoc />
    public Role LocalRole
    {
      get
      {
        if (_authorityReplicatorImplementation == null)
          return Role.None;

        return _authorityReplicatorImplementation.LocalRole;
      }
    }

    private static readonly IReadOnlyCollection<IPeer> _emptyPeers =
      new ARDKReadOnlyCollection<IPeer>(EmptyArray<IPeer>.Instance);

    /// <inheritdoc />
    public IReadOnlyCollection<IPeer> PeersOfRole(Role role)
    {
      if (_authorityReplicatorImplementation == null)
        return _emptyPeers;

      return _authorityReplicatorImplementation.PeersOfRole(role);
    }

    /// <inheritdoc />
    public Role RoleOfPeer(IPeer peer)
    {
      if (_authorityReplicatorImplementation == null)
        return Role.None;

      return _authorityReplicatorImplementation.RoleOfPeer(peer);
    }

    /// <inheritdoc />
    public void TryClaimRole(Role role, Action onPass, Action onFail)
    {
      if (_authorityReplicatorImplementation != null)
        _authorityReplicatorImplementation.TryClaimRole(role, onPass, onFail);
      else
      {
        ARLog._DebugFormat
        (
          "Tried to claim role {0}, but no replicator has been initialized, caching request",
          false,
          role
        );
        _startupRole = role;
        _pass = onPass;
        _fail = onFail;
      }
    }

    public string Identifier
    {
      get
      {
        return AUTH_IDENTIFIER;
      }
    }

    public INetworkGroup Group
    {
      get
      {
        return Owner.Group;
      }
    }

    public void Unregister()
    {
      if (_authorityReplicatorImplementation == null)
        _alreadyUnregistered = true;
      else
        _authorityReplicatorImplementation.Unregister();
    }
  }
}
