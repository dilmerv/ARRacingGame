// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.VirtualStudio.AR.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR.Networking.Mock;
using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.VirtualStudio.Networking.Mock;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio
{
  public sealed class MockPlayer
  {
    public IARSession ARSession
    {
      get { return _arSession; }
    }

    public IMultipeerNetworking Networking
    {
      get { return _networking; }
    }

    public IARNetworking ARNetworking
    {
      get { return _arNetworking; }
    }

    public bool IsLocal { get; private set; }

    public string Name
    {
      get { return _playerName; }
    }

    public GameObject GameObject
    {
      get { return _playerObject; }
    }

    private IARSession _arSession;
    private _MockMultipeerNetworking _networking;
    private _MockARNetworking _arNetworking;

    private MockArdkPlayerProfile _playerProfile;
    private string _playerName;

    private GameObject _playerObject;
    private Transform _playerTransform;

    private MessageHandlerBase _messageHandler;

    internal MockPlayer
    (
      _IVirtualStudioManager virtualStudioMaster,
      MockArdkPlayerProfile playerProfile = null
    )
    {
      if (playerProfile != null)
      {
        _playerProfile = playerProfile;
        _playerName = playerProfile.PlayerName;
        SetMessageHandler(new DefaultMessageHandler());
      }
      else
      {
        _playerName = _VirtualStudioManager.LOCAL_PLAYER_NAME;
        IsLocal = true;
      }

      ARLog._DebugFormat("Initialized Player {0}", objs: _playerName);
    }

    internal void SetARSession(IARSession arSession)
    {
      _arSession = arSession;

      if (!IsLocal)
      {
        _arSession.Ran +=
          args =>
          {
            if (_playerObject == null)
            {
              var playerObject = _playerProfile.SpawnPlayerObject();
              SetPlayerObject(playerObject);
            }
          };
      }
    }

    internal void SetMultipeerNetworking(_MockMultipeerNetworking networking)
    {
      if (networking == null)
      {
        _networking.PeerDataReceived -= HandlePeerDataReceived;
        _networking = null;
        return;
      }

      if (_networking != null)
      {
        ARLog._Warn
        (
          "Multiple active MultipeerNetworking instances were found, but MockPlayer " +
          "will only keep a reference to and listen for messages from the first one."
        );
        return;
      }

      networking.PeerDataReceived += HandlePeerDataReceived;
      _networking = networking;
    }

    internal void SetARNetworking(_MockARNetworking arNetworking)
    {
      _arNetworking = arNetworking;
    }

    internal void SetTransform(Transform transform)
    {
      if (_playerTransform == null)
        return;

      _playerTransform.SetPositionAndRotation(transform.position, transform.rotation);
    }

    /// <summary>
    /// Associates the input GameObject with this MockPlayer. If this MockPlayer has an
    /// ARNetworking session, the transform of the GameObject will be used as the peer's pose.
    /// </summary>
    /// <param name="playerObject"></param>
    public void SetPlayerObject(GameObject playerObject)
    {
      // Todo: Optionally destroy the old object and replace with new?
      if (_playerObject != null)
      {
        ARLog._WarnFormat("The MockPlayer {0} already has a GameObject.", objs: this);
        return;
      }

      // Could happen if no mock player prefab is set on the MockPlayConfiguration
      if (playerObject == null)
        return;

      _playerObject = playerObject;
      _playerTransform = playerObject.transform;
      _UpdateLoop.Tick += BroadcastPoseOnTick;
    }

    public void SetPeerState(PeerState state)
    {
      if (_arNetworking == null || !_arNetworking.Networking.IsConnected)
      {
        ARLog._Error("Cannot set the PeerState of a player not connected to an ARNetworking session.");
        return;
      }

      _arNetworking.BroadcastState(state);
    }

    private void BroadcastPoseOnTick()
    {
      if (_arNetworking != null &&
        _arSession.State == ARSessionState.Running &&
        _arNetworking.LocalPeerState == PeerState.Stable)
      {
        _arNetworking.BroadcastPose
        (
          Matrix4x4.TRS
          (
            _playerTransform.position,
            _playerTransform.rotation,
            _playerTransform.localScale
          ),
          Time.deltaTime
        );
      }
    }

    /// Inject a custom MessageHandlerBase implementation for this player to handle network
    /// messages through. If a custom one is not set, a default message handler that prints
    /// messages to the console will be used.
    /// @param handler
    public void SetMessageHandler(MessageHandlerBase handler)
    {
      if (IsLocal)
      {
        var msg =
          "The local player should handle messages by subscribing to the " +
          "IMultipeerNetworking.PeerDataReceived event.";

        ARLog._Error(msg);
        return;
      }

      _messageHandler = handler;
      _messageHandler.SetOwningPlayer(this);
    }

    private void HandlePeerDataReceived(PeerDataReceivedArgs args)
    {
      if (_messageHandler != null)
        _messageHandler.HandleMessage(args);
    }

    public override string ToString()
    {
      return ToString(-1);
    }

    public string ToString(int identifierLength)
    {
      var peerString = string.Empty;

      if (Networking == null || !Networking.IsConnected)
      {
        peerString = "None";
      }
      else
      {
        if (identifierLength < 0)
          peerString = Networking.Self.ToString();
        else
          peerString = Networking.Self.ToString(identifierLength);
      }

      return string.Format("{0} ({1})", _playerName, peerString);
    }
  }
}