// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;

using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.Networking;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Helpers
{
  public class SyncStateHelpText:
    MonoBehaviour
  {
    [SerializeField]
    private Text _helpText;

    [SerializeField]
    private Image _backdrop;

    [SerializeField]
    private bool _usingQR;

    private IARNetworking _arNetworking = null;

    private const string _localizationInstructions =
                          "stand 1 to 5 meters away, and move" + 
                          " left and right keeping the object in frame.";

    private readonly Dictionary<PeerState, string> _hostHelpText =
      new Dictionary<PeerState, string>()
      {
        { PeerState.WaitingForLocalizationData, 
                          "Find 3D object with significant features. A shoe," +
                          " for example. Point camera at object, " + 
                          _localizationInstructions },

        // General fallthrough
        { PeerState.Unknown, "Waiting for connection." },
        { PeerState.Initializing, "AR stack is initializing. Please wait." },
        { PeerState.Limited, "Limited" },
        { PeerState.Failed, "Sync failed. Restart app." },

        // Fallthrough for both qr client and qr host
        { PeerState.Stabilizing, "Sync achieved with QR code. Drifting will occur and accumulate." +
                                 " Attempting to use colocalization backend to correct for drift." +
                                 " (Look at other networking examples for details on this process)" }
      };

    // Help text lookups default to the above dictionary and will be overriden by the below
    // dictionaries in certain conditions.

    private readonly Dictionary<PeerState, string> _clientTextOverride =
      new Dictionary<PeerState, string>()
      {
        { PeerState.WaitingForLocalizationData, "Wait until host localizes." },
        { PeerState.Localizing, "Point camera at the object the host scanned, " + 
                                _localizationInstructions }
      };

    private readonly Dictionary<PeerState, string> _qrHostHelpTextOverride =
      new Dictionary<PeerState, string>()
      {
        { PeerState.WaitingForLocalizationData, "Scan a plane." },
        { PeerState.Stabilizing, "Using plane to localize. Tilt phone flat to see QR code and" +
                                 " have client scan the code." }
      };

    private readonly Dictionary<PeerState, string> _qrClientHelpTextOverride =
      new Dictionary<PeerState, string>()
      {
        { PeerState.WaitingForLocalizationData, "Scan a plane." },
      };

    private void Awake()
    {
      ARNetworkingFactory.ARNetworkingInitialized += OnAnyInitialized;
    }

    private void Start()
    {
      Hide();
    }

    private void OnDestroy()
    {
      ARNetworkingFactory.ARNetworkingInitialized -= OnAnyInitialized;

      OnDeinitialized(new ARNetworkingDeinitializedArgs());
    }

    private void OnAnyInitialized(AnyARNetworkingInitializedArgs args)
    {
      // This currently only supports catching the first networking object it sees
      if (_arNetworking != null)
        return;

      _arNetworking = args.ARNetworking;
      _arNetworking.Deinitialized += OnDeinitialized;
      _arNetworking.PeerStateReceived += OnPeerStateReceived;
    }

    private void OnDeinitialized(ARNetworkingDeinitializedArgs args)
    {
      if (_arNetworking == null)
        return;

      _arNetworking.PeerStateReceived -= OnPeerStateReceived;
      _arNetworking.Deinitialized -= OnDeinitialized;
      _arNetworking = null;
    }

    private void OnPeerStateReceived(PeerStateReceivedArgs args)
    {
      if (!args.Peer.Equals(_arNetworking.Networking.Self))
        return;

      string displayText = null;
      if (args.Peer.Equals(_arNetworking.Networking.Host))
      {
        if (_usingQR && _qrHostHelpTextOverride.ContainsKey(args.State))
          displayText = _qrHostHelpTextOverride[args.State];
      }
      else
      {
        if (_usingQR && _qrClientHelpTextOverride.ContainsKey(args.State))
          displayText = _qrClientHelpTextOverride[args.State];
        else if(!_usingQR && _clientTextOverride.ContainsKey(args.State))
          displayText = _clientTextOverride[args.State];
      }

      if (string.IsNullOrEmpty(displayText) && _hostHelpText.ContainsKey(args.State))
        displayText = _hostHelpText[args.State];

      if (string.IsNullOrEmpty(displayText))
        Hide();
      else
      {
        _helpText.text = displayText;
        Show();
      }
    }

    private void Hide()
    {
      _helpText.enabled = false;
      _backdrop.enabled = false;
    }

    private void Show()
    {
      _helpText.enabled = true;
      _backdrop.enabled = true;
    }
  }
}