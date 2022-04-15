// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;
using Niantic.ARDK.VirtualStudio.Remote.Data;

using UnityEngine;
using UnityEngine.UI;

using Random = System.Random;

namespace Niantic.ARDK.VirtualStudio.Remote
{
  /// <summary>
  /// Handles the mobile display logic of Remote Connection.
  /// </summary>
  public class RemoteConnectionUI: MonoBehaviour
  {
    [Header("Pre-Connection UI")]
    [SerializeField]
    private GameObject _preSelectionUI = null;

    [SerializeField]
    private Button _usbConnectButton = null;

    [SerializeField]
    private Button _internetConnectButton = null;

    [Header("Connection Starting UI")]
    [SerializeField]
    private GameObject _postSelectionUI = null;

    [SerializeField]
    private int pinLength = 6;

    [SerializeField]
    private Text _pinDisplay = null;

    [Header("Connected UI")]
    [SerializeField]
    private Text _connectionStatusText = null;

    [SerializeField]
    private Text _arSessionStatusText = null;

    [SerializeField]
    private Text _networkingStatusText = null;

    [SerializeField]
    private Text _arNetworkingStatusText = null;

    private bool _hasSelectedMode;
    private IARSession _activeARSession;

    private Random _random = new Random();
    private const string PIN_DISPLAY_TEXT = "PIN: {0}";

    private void Awake()
    {
      SubscribeToLifecycleEvents();

      _preSelectionUI.SetActive(true);
      _postSelectionUI.SetActive(false);

      // Setup selection stage.
      Camera.main.backgroundColor = Color.black;

      _usbConnectButton.onClick.AddListener
      (
        () => { StartConnection(_RemoteConnection.ConnectionMethod.USB); }
      );

      _internetConnectButton.onClick.AddListener
      (
        () => { StartConnection(_RemoteConnection.ConnectionMethod.Internet); }
      );

      _RemoteConnection.Deinitialized += Reset;
    }

    private void StartConnection(_RemoteConnection.ConnectionMethod connectionMethod)
    {
      string pin = null;
      if (connectionMethod == _RemoteConnection.ConnectionMethod.Internet)
      {
        // Build a pin.
        var pinBuilder = new StringBuilder();

        for (var i = 0; i < pinLength; i++)
        {
          var nextChar = (char)_random.Next('A', 'Z');
          pinBuilder.Append(nextChar);
        }

        pin = pinBuilder.ToString();
        // Add "PIN:" to the display text, but not the pin used to connect
        _pinDisplay.text = string.Format(PIN_DISPLAY_TEXT, pin);
        _pinDisplay.enabled = true;
      }
      else
      {
        _pinDisplay.enabled = false;
      }

      _hasSelectedMode = true;
      _preSelectionUI.SetActive(false);
      _postSelectionUI.SetActive(true);

      // Connect using settings.
      _RemoteConnection.InitIfNone(connectionMethod);
      _RemoteConnection.Connect(pin);
    }

    private void Reset()
    {
      _hasSelectedMode = false;
      _preSelectionUI.SetActive(true);
      _postSelectionUI.SetActive(false);
      _pinDisplay.enabled = true;
      Camera.main.backgroundColor = Color.blue;
    }

    private void Update()
    {
      if (!_hasSelectedMode)
        return;

      // UI is not visible when camera feed is rendering
      if (_activeARSession != null && _activeARSession.State == ARSessionState.Running)
        return;

      // Update connection info.
      if (_RemoteConnection.IsConnected)
      {
        _connectionStatusText.text = "Connected to editor!";
        Camera.main.backgroundColor = Color.green;
        _pinDisplay.enabled = false;
      }
      else if (_RemoteConnection.IsReady)
      {
        _connectionStatusText.text = "Waiting for connection...";
        Camera.main.backgroundColor = Color.blue;
      }
      else
      {
        _connectionStatusText.text = "Waiting for service...";
        Camera.main.backgroundColor = Color.magenta;
      }
    }

    private void OnDestroy()
    {
      _RemoteConnection.Deinitialize();
    }

    private void SubscribeToLifecycleEvents()
    {
      ARSessionFactory.SessionInitialized +=
        args =>
        {
          ARLog._Debug("[Remote] ARSession Initialized: " + args.Session.StageIdentifier);
          _activeARSession = args.Session;
          _activeARSession.Deinitialized += _ => _activeARSession = null;

          UpdateStatusVisual(_arSessionStatusText, true);

          args.Session.Deinitialized +=
            deinitializedArgs =>
            {
              ARLog._Debug("[Remote] ARSession Deinitialized.");
              UpdateStatusVisual(_arSessionStatusText, false);
            };
        };

      MultipeerNetworkingFactory.NetworkingInitialized +=
        args =>
        {
          ARLog._Debug("[Remote] MultipeerNetworking Initialized: " + args.Networking.StageIdentifier);
          UpdateNetworkingsCount();
          UpdateStatusVisual(_networkingStatusText, true);

          args.Networking.Deinitialized +=
            deinitializedArgs =>
            {
              ARLog._Debug("[Remote] MultipeerNetworking Deinitialized.");

              var networkingsCount = UpdateNetworkingsCount();
              UpdateStatusVisual(_networkingStatusText, networkingsCount > 0);
            };
        };

      ARNetworkingFactory.ARNetworkingInitialized +=
        args =>
        {
          ARLog._Debug("[Remote] ARNetworking Initialized: " + args.ARNetworking.ARSession.StageIdentifier);
          UpdateStatusVisual(_arNetworkingStatusText, true);

          args.ARNetworking.Deinitialized +=
            deinitializedArgs =>
            {
              ARLog._Debug("[Remote] ARNetworking Deinitialized.");
              UpdateStatusVisual(_arNetworkingStatusText, false);
            };
        };
    }

    private readonly Color FADED_WHITE = new Color(1, 1, 1, 0.5f);
    private void UpdateStatusVisual(Text statusText, bool isConstructed)
    {
      if (statusText != null)
      {
        statusText.fontStyle = isConstructed ? FontStyle.Bold : FontStyle.Normal;
        statusText.color = isConstructed ? Color.white : FADED_WHITE;
      }
    }

    private int UpdateNetworkingsCount()
    {
      var networkingsCount = MultipeerNetworkingFactory.Networkings.Count;
      _networkingStatusText.text = "MultipeerNetworking x" + networkingsCount;
      return networkingsCount;
    }
  }
}
