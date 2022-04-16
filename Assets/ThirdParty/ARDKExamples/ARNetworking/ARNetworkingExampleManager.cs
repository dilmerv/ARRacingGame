// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  /// @brief A helper script to demonstrate AR together with networking.
  /// @remark All interaction is event/button-based to better show how the usage might be incorporated
  /// into a larger project.
  public class ARNetworkingExampleManager:
    MonoBehaviour
  {
    // The object we will place to track peers
    [SerializeField]
    private GameObject _peerIndicatorPrefab = null;

    [Header("UI")]
    // Text on button to toggle pose broadcasting
    [SerializeField]
    private Text _togglePoseText = null;

    // A reference to the ARNetworking instance
    private IARNetworking _arNetworking;

    // Keep track of is pose broadcasting is enabled in order to toggle it.
    // Pose broadcasting is enabled by default when a peer joins an ARNetworking session.
    private bool _isPoseBroadcastingEnabled = true;

    /// Hash-maps of game objects (cubes are drawn per peer)
    private Dictionary<IPeer, GameObject> _peerGameObjects = new Dictionary<IPeer, GameObject>();

    // Only print of first X digits of peer id onto screen
    const int PeerStringLimit = 6;

    private void Awake()
    {
      ARNetworkingFactory.ARNetworkingInitialized += OnARNetworkingInitialized;
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnARNetworkingInitialized;
      _arNetworking = null;
    }

    private void OnARNetworkingInitialized(AnyARNetworkingInitializedArgs args)
    {
      _arNetworking = args.ARNetworking;

      // Subscribe to any callbacks
      _arNetworking.PeerPoseReceived += OnPeerPoseReceived;
      _arNetworking.PeerStateReceived += OnPeerStateReceived;
      _arNetworking.Networking.PeerDataReceived += OnDidReceiveDataFromPeer;
      _arNetworking.Networking.PeerAdded += peerAddedArgs => OnDidAddPeer(peerAddedArgs.Peer);
      _arNetworking.Networking.PeerRemoved += OnDidRemovePeer;

      _arNetworking.ARSession.MapsAdded +=
        mapsArgs =>
        {
          foreach (var map in  mapsArgs.Maps)
            Debug.Log("Added map " + map.Identifier);
        };

      _arNetworking.ARSession.MapsUpdated +=
        mapsArgs =>
        {
          foreach (var map in  mapsArgs.Maps)
            Debug.Log("Updated map " + map.Identifier);
        };

      // In case ARNetworking was initialized after Networking was already connected,
      // iterate through all already-added peers
      if (_arNetworking.Networking.IsConnected)
      {
        foreach (var peer in _arNetworking.Networking.OtherPeers)
          OnDidAddPeer(peer);
      }
    }

    /** Button Methods */
    public void DebugSendMessage()
    {
      if (_arNetworking == null || !_arNetworking.Networking.IsConnected)
        return;

      var data = new byte[11];
      _arNetworking.Networking.BroadcastData(4, data, TransportType.UnreliableUnordered, true);
    }

    public void TogglePoseBroadcast()
    {
      if (_arNetworking == null)
        return;

      if (_isPoseBroadcastingEnabled)
      {
        _isPoseBroadcastingEnabled = false;
        _arNetworking.DisablePoseBroadcasting();
        _togglePoseText.text = "Enable Pose Broadcasting";
      }
      else
      {
        _isPoseBroadcastingEnabled = true;
        _arNetworking.EnablePoseBroadcasting();
        _togglePoseText.text = "Disable Pose Broadcasting";
      }
    }

    public void SetPoseLatency(string latencyStr)
    {
      if (_arNetworking == null)
        return;

      long latency;
      if (long.TryParse(latencyStr, out latency))
      {
        _arNetworking.SetTargetPoseLatency(latency);
        Debug.Log("Set target pose latency to " + latency);
      }
    }

    /** ARNetworking Callbacks */
    private void OnPeerStateReceived(PeerStateReceivedArgs args)
    {
      var peer = args.Peer;
      var syncPeerState = args.State;

      var peerString = peer.ToString(PeerStringLimit);
      Debug.LogFormat("Received state: '{0}' from '{1}'", syncPeerState.ToString(), peerString);

      if (_arNetworking.Networking.Self.Identifier != peer.Identifier)
        return;

      if (syncPeerState == PeerState.Failed)
        _arNetworking.Networking.Leave();
    }

    private void OnPeerPoseReceived(PeerPoseReceivedArgs args)
    {
      //Debug.Log("Received pose from peer: " + args.Peer.Identifier);

      var peer = args.Peer;
      var peerGameObject = _peerGameObjects[peer];

      var pose = args.Pose;
      peerGameObject.transform.position = pose.ToPosition();
      peerGameObject.transform.rotation = pose.ToRotation();
    }

    private void OnDidAddPeer(IPeer peer)
    {
      // Instantiating peer object
      _peerGameObjects[peer] =
        Instantiate
        (
          _peerIndicatorPrefab,
          new Vector3(99999, 99999, 99999),
          Quaternion.identity
        );

      _arNetworking.Networking.SendDataToPeer
      (
        3,
        new byte[7],
        peer,
        TransportType.ReliableUnordered
      );
    }

    private void OnDidRemovePeer(PeerRemovedArgs args)
    {
      // Destroy peer object
      Destroy(_peerGameObjects[args.Peer]);
      _peerGameObjects.Remove(args.Peer);
    }

    private void OnDidReceiveDataFromPeer(PeerDataReceivedArgs args)
    {
      var peerString = args.Peer.ToString(PeerStringLimit);
      Debug.LogFormat("Received data of length: '{0}', from '{1}'", args.DataLength, peerString);
    }
  }
}