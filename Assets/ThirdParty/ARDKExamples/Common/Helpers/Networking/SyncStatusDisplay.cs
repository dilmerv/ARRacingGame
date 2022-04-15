// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDK.Extensions
{
  public class SyncStatusDisplay:
    MonoBehaviour
  {
    [Header("Toggle UI")]
    [SerializeField]
    private Button _showButton = null;

    [SerializeField]
    private Button _hideButton = null;

    [SerializeField]
    private GameObject _display = null;

    [Header("Display UI")]
    // Local peer's location
    [SerializeField]
    private Text _localText = null;

    // Host's location
    [SerializeField]
    private Text _hostText = null;

    // Are we synced?
    [SerializeField]
    private Text _syncStateText = null;

    // Time to sync
    [SerializeField]
    private Text _timeToSyncText = null;

    private DateTime _initialTime;

    private IARNetworking _arNetworking;

    private void Awake()
    {
      _hideButton.onClick.AddListener(HideDisplay);
      _showButton.onClick.AddListener(ShowDisplay);

      HideDisplay();

      ARNetworkingFactory.ARNetworkingInitialized += OnAnyInitialized;
    }

    private void OnAnyInitialized(AnyARNetworkingInitializedArgs args)
    {
      _arNetworking = args.ARNetworking;

      _arNetworking.Networking.Connected += OnDidConnect;

      _arNetworking.ARSession.FrameUpdated += OnFrameUpdated;

      _arNetworking.PeerStateReceived += OnPeerStateReceived;
      _arNetworking.PeerPoseReceived += OnPeerPoseReceived;
    }

    private void OnDidConnect(ConnectedArgs args)
    {
      _initialTime = DateTime.Now;

      if (args.IsHost)
        _hostText.text = "I am the host";
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      _localText.text =
        string.Format
        (
          "Local Position: {0}",
          MatrixUtils.PositionFromMatrix(args.Frame.Camera.Transform).ToString()
        );
    }

    private void OnPeerStateReceived(PeerStateReceivedArgs args)
    {
      if (!args.Peer.Equals(_arNetworking.Networking.Self))
        return;

      var state = args.State;
      _syncStateText.text = state.ToString();

      if (state != PeerState.Stable)
        return;

      var syncTime = (DateTime.Now - _initialTime).TotalMilliseconds.ToString();
      _timeToSyncText.text = "Synced in: " + syncTime + " ms";
    }

    private void OnPeerPoseReceived(PeerPoseReceivedArgs args)
    {
      if (args.Peer.Equals(_arNetworking.Networking.Host))
      {
        _hostText.text =
          string.Format
          (
            "Host Position: {0}",
            MatrixUtils.PositionFromMatrix(args.Pose).ToString()
          );
      }
    }

    private void HideDisplay()
    {
      _display.SetActive(false);
      _showButton.gameObject.SetActive(true);
    }

    private void ShowDisplay()
    {
      _display.SetActive(true);
      _showButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnAnyInitialized;

      if (_arNetworking != null)
      {
        var networking = _arNetworking.Networking;
        if (networking != null)
          networking.Connected -= OnDidConnect;

        _arNetworking.ARSession.FrameUpdated -= OnFrameUpdated;
        _arNetworking.PeerStateReceived -= OnPeerStateReceived;
        _arNetworking.PeerPoseReceived -= OnPeerPoseReceived;
        _arNetworking = null;
      }
    }
  }
}
