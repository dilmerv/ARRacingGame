// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.SLAM;
using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// A map will be discovered at this location in TimeToDiscovery seconds after an ARSession
  /// starts running.
  ///
  /// If the local peer is the host, that means the IARNetworking.PeerStateReceived event
  /// will surface a change for the local peer for PeerState.WaitingForLocalizationData to
  /// PeerState.Stable.
  ///
  /// Else, it will surface a for the local peer from PeerState.WaitingForLocalizationData to
  /// PeerState.Localizing, and then another TimeToDiscovery seconds later, another PeerStateReceived
  /// event will surface a change from PeerState.Localizing to PeerState.Stable.
  ///
  /// @note
  ///   Mock maps will only be discovered by the local player's session. To have more control over
  ///   surfacing PeerState changes, and/or to surface changes from non-local players,
  ///   use the MockPlayer.SetPeerState method or the Virtual Studio Editor Window's Mock controls.
  public sealed class MockMap:
    MockDetectableBase
  {
    private Guid _identifier = Guid.NewGuid();
    private HashSet<Guid> _discoveredInSessions = new HashSet<Guid>();

    private MockPlayer _player;

    private Coroutine _stableCoroutine;

    internal override void BeDiscovered(_IMockARSession arSession, bool isLocal)
    {
      if (!isLocal || _discoveredInSessions.Contains(arSession.StageIdentifier))
        return;

      _discoveredInSessions.Add(arSession.StageIdentifier);

      var serialMap =
        new _SerializableARMap
        (
          _identifier,
          1.0f,
          Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)
        );

      arSession.AddMap(serialMap);

      _player = _VirtualStudioManager.Instance.LocalPlayer;
      var networking = _player.Networking;
      var isHost = networking.Self.Equals(networking.Host);

      if (isHost)
      {
        _player.SetPeerState(PeerState.Stable);
      }
      else
      {
        _player.SetPeerState(PeerState.Localizing);
        Action secondChange = () => _player.SetPeerState(PeerState.Stable);
        _stableCoroutine = StartCoroutine(nameof(WaitAndChangeStateAgain), secondChange);
      }
    }

    internal override void OnSessionRanAgain(_IMockARSession arSession)
    {
      // Nothing happens on device either.
    }

    private IEnumerator WaitAndChangeStateAgain(Action secondChange)
    {
      if (_timeToDiscovery > 0)
        yield return new WaitForSeconds(_timeToDiscovery);

      secondChange();
    }
  }
}
