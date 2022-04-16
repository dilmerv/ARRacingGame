// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Helpers
{
  public class PeerPoseVisualizer:
    MonoBehaviour
  {
    /// The object we will place to track peers
    [SerializeField]
    private GameObject _peerIndicatorPrefab = null;

    /// Text on button to toggle pose broadcasting
    [SerializeField]
    private Text _togglePoseText = null;

    /// A reference AR networking instance
    private IARNetworking _arNetworking;

    // Disable pose broadcasting
    private bool _isPoseBroadcastingEnabled = true;

    /// Hash-maps of game objects (cubes are drawn per peer)
    private Dictionary<IPeer, GameObject> _peerGameObjects =
      new Dictionary<IPeer, GameObject>();

    /// <summary>
    /// Toggles pose broadcasting for self. For use with a button.
    /// </summary>
    public void TogglePoseBroadcast()
    {
      if (_arNetworking == null)
        return;

      _isPoseBroadcastingEnabled = !_isPoseBroadcastingEnabled;

      if (_isPoseBroadcastingEnabled)
      {
        _arNetworking.EnablePoseBroadcasting();
        _togglePoseText.text = "Disable Pose Broadcasting";
      }
      else
      {
        _arNetworking.DisablePoseBroadcasting();
        _togglePoseText.text = "Enable Pose Broadcasting";
      }
    }

    /// <summary>
    /// Sets the outgoing pose latency in milliseconds. For use with a text box.
    /// </summary>
    /// <param name="latencyStr"></param>
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

    /*  Handle catching the ARNetworking object  */
    private void Awake()
    {
      ARNetworkingFactory.ARNetworkingInitialized += OnAnyInitialized;
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnAnyInitialized;
    }

    /*  Handle subscriptions to the ARNetworking object  */
    private void OnAnyInitialized(AnyARNetworkingInitializedArgs args)
    {
      // This script only functions for the first network it encounters.
      if (_arNetworking != null)
        return;

      _arNetworking = args.ARNetworking;
      _arNetworking.Deinitialized += OnDeinitialized;
      _arNetworking.PeerPoseReceived += OnPeerPoseReceived;
      _arNetworking.Networking.PeerAdded += OnDidAddPeer;
      _arNetworking.Networking.PeerRemoved += OnDidRemovePeer;
    }

    private void OnDeinitialized(ARNetworkingDeinitializedArgs args)
    {
      if (_arNetworking == null)
        return;

      _arNetworking.Deinitialized -= OnDeinitialized;
      _arNetworking.PeerPoseReceived -= OnPeerPoseReceived;
      _arNetworking.Networking.PeerAdded -= OnDidAddPeer;
      _arNetworking.Networking.PeerRemoved -= OnDidRemovePeer;

      _arNetworking = null;
    }

    /*  Handlers for tracking peer poses  */
    private void OnDidAddPeer(PeerAddedArgs args)
    {
      // Instantiating peer object
      _peerGameObjects[args.Peer] =
        Instantiate
        (
          _peerIndicatorPrefab,
          new Vector3(99999, 99999, 99999),
          Quaternion.identity
        );
    }

    private void OnDidRemovePeer(PeerRemovedArgs args)
    {
      var peer = args.Peer;

      // Destroy peer object
      Destroy(_peerGameObjects[peer]);
      _peerGameObjects.Remove(peer);
    }

    private void OnPeerPoseReceived(PeerPoseReceivedArgs args)
    {
      var peerGameObject = _peerGameObjects[args.Peer];

      var pose = args.Pose;
      peerGameObject.transform.position = pose.ToPosition();
      peerGameObject.transform.rotation = pose.ToRotation();
    }
  }
}
