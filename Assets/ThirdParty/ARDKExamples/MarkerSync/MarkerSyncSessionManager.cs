// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.Extensions.MarkerSync;
using Niantic.ARDK.Networking;

using UnityEngine;
using UnityEngine.UI;

/// @namespace Niantic.ARDKExamples.MarkerSync
/// @brief
/// @note This is part of an experimental feature that is not advised to be used in release builds.
namespace Niantic.ARDKExamples.MarkerSync
{
  /// An example using MarkerSync to speed up joining an AR networking session
  /// BEST PRACTICES:
  ///   For the best quality marker sync, the host/mapping device should be broadcasting its pose
  ///   at least 60 times per second while other devices are trying to scan to sync (ie when the
  ///   marker is being displayed). Pose broadcasting can be enabled/disable, and the target
  ///   broadcast rate can be configured using the IARNetworking API.
  /// @note
  ///   This example has not been polished to the standard of others in ARDK-Examples,
  ///   but is provided as a demonstration of how the MarkerSync APIs can be used.
  public class MarkerSyncSessionManager:
    MonoBehaviour
  {
    [Header("QR Components")]
    /** Mapper Components */
    [SerializeField]
    private BarcodeDisplay _barcodeDisplay = null;

    /** UI */
    [Header("UI Components")] [SerializeField]
    private GameObject _startupUI = null;

    [SerializeField] private Button _createButton = null;
    [SerializeField] private Button _scanButton = null;
    [SerializeField] private Button _resetButton = null;

    [SerializeField]
    private Text _scanPlaneReminder = null;

    private MarkerMappingManager _mapper;
    private MarkerScanningManager _scanner;

    public IARSession ARSession { get; private set; }
    public IARNetworking ARNetworking { get; private set; }

    public BarcodeDisplay BarcodeDisplay
    {
      get { return _barcodeDisplay; }
    }

    public Action UpdateTick;

    private void Awake()
    {
      _scanPlaneReminder.gameObject.SetActive(false);
      _mapper = new MarkerMappingManager(this);
      _scanner = new MarkerScanningManager(this);

      _createButton.onClick.AddListener(_mapper.CreateSession);
      _scanButton.onClick.AddListener(_scanner.ScanToJoinSession);
      _resetButton.onClick.AddListener(BackToInitialization);
    }

    private void OnDestroy()
    {
      _mapper.Reset();
      _scanner.Reset();

      ResetSessions();
    }

    private void BackToInitialization()
    {
      ResetSessions();
      _startupUI.SetActive(true);

      _mapper.Reset();
      _scanner.Reset();
    }

    private void ResetSessions()
    {
      if (ARSession != null)
      {
        ARSession.Dispose();
        ARSession = null;
      }

      if (ARNetworking != null)
      {
        if (ARNetworking.Networking != null)
        {
          if (ARNetworking.Networking.IsConnected)
            ARNetworking.Networking.Leave();

          ARNetworking.Networking.Dispose();
        }

        ARNetworking.Dispose();
        ARNetworking = null;
      }
    }

    public void RunARSession()
    {
      if (ARSession == null)
      {
        Debug.LogWarning("Need to create an ARSession before running it.");
        return;
      }

      var configuration = ARWorldTrackingConfigurationFactory.Create();
      configuration.WorldAlignment = WorldAlignment.Gravity;
      configuration.IsLightEstimationEnabled = true;
      configuration.IsAutoFocusEnabled = true;
      configuration.PlaneDetection = PlaneDetection.Horizontal;
      configuration.IsSharedExperienceEnabled = true;

      ARSession.Run(configuration);

      ARSession.AnchorsAdded += AnchorsAdded;

      _scanPlaneReminder.gameObject.SetActive(true);
    }

    private void AnchorsAdded(AnchorsArgs args)
    {
        ARSession.AnchorsAdded -= AnchorsAdded;
        _scanPlaneReminder.gameObject.SetActive(false);
    }

    public void InitializeARSession()
    {
      _startupUI.SetActive(false);

      ARSession = ARSessionFactory.Create();
    }

    public void InitializeARNetworking()
    {
      ARNetworking = ARNetworkingFactory.Create(ARSession);
    }

    private void Update()
    {
      UpdateTick?.Invoke();
    }
  }
}