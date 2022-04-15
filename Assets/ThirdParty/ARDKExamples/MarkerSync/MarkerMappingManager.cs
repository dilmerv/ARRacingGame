// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Text;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Marker;

using Niantic.ARDKExamples.Common.Helpers;
using Niantic.ARDKExamples.Helpers;

using UnityEngine;

namespace Niantic.ARDKExamples.MarkerSync
{
  /// @note
  ///   This example has not been polished to the standard of others in ARDK-Examples,
  ///   but is provided as a demonstration of how the MarkerSync APIs can be used.
  public class MarkerMappingManager
  {
    private readonly MarkerSyncSessionManager _manager;

    private string _sessionIdentifier;

    private bool _foundPlane;
    private bool _generatedBarcode;

    public MarkerMappingManager(MarkerSyncSessionManager manager)
    {
      _manager = manager;

      if (SystemInfo.supportsGyroscope)
      {
        Input.gyro.enabled = true;
        Input.gyro.updateInterval = 0.0167f;
      }
    }

    public void Reset()
    {
      _manager.BarcodeDisplay.Hide();

      if (_manager.ARSession != null)
        _manager.ARSession.AnchorsAdded -= OnAnchorsShowBarcodeIfAble;

      _manager.UpdateTick -= ToggleBarcodeDisplay;
    }

    public void CreateSession()
    {
      if (_manager.ARSession != null)
        return;

      _manager.InitializeARSession();
      _manager.InitializeARNetworking();

      // It's recommended to call InitializeForMarkerScanning after the device has had some chance
      // to look around the space so that tracking is relatively stable (which is why this is
      // starting after the DidAddAnchors event is raised).
      _manager.ARSession.AnchorsAdded += OnAnchorsShowBarcodeIfAble;
      _manager.RunARSession();

      // Must be connected to networking session before showing barcode, because the peer displaying
      // the marker used to sync must be the network host.
      _manager.ARNetworking.Networking.Connected += OnConnectShowBarcodeIfAble;

      _sessionIdentifier = Guid.NewGuid().ToString();
      Debug.Log("SessionIdentifier: " + _sessionIdentifier);
#if UNITY_EDITOR
      // The example code to use the stationary marker sync flow has been left here to demonstrate
      // how it could be done, but this flow is not supported in ARDK 2.3, and will throw a
      // NotSupportedException if used.
      SetupBarcode();
#else
      _manager.ARNetworking.Networking.Join(Encoding.UTF8.GetBytes(_sessionIdentifier));
#endif
    }

    private void OnConnectShowBarcodeIfAble(ConnectedArgs args)
    {
      _manager.ARNetworking.Networking.Connected -= OnConnectShowBarcodeIfAble;

      if (_foundPlane)
        SetupBarcode();
    }

    private void OnAnchorsShowBarcodeIfAble(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        if (anchor.AnchorType == AnchorType.Plane)
        {
          _foundPlane = true;
          _manager.ARSession.AnchorsAdded -= OnAnchorsShowBarcodeIfAble;

          if (_manager.ARNetworking.Networking.IsConnected)
            SetupBarcode();
        }
      }
    }

    private void SetupBarcode()
    {
      var userData = Encoding.UTF8.GetBytes("Hello world.");

#if UNITY_EDITOR
      // This extra embedded data is only required for the stationary marker use case
      var exampleObjectPoints =
        new[]
        {
          new Vector3(-0.0395f, 0.0395f, 0),
          new Vector3(-0.0395f, -0.0395f, 0),
          new Vector3(0.0395f, -0.0395f, 0),
          new Vector3(0.0285f, 0.0285f, 0)
        };

      var embeddedInfo =
        new StationaryMarkerMetadata
        (
          _sessionIdentifier,
          userData,
          Matrix4x4.TRS(Vector3.one, Quaternion.Euler(new Vector3(0, 90, 0)), Vector3.one),
          exampleObjectPoints
        );
#else
      var embeddedInfo =
        new MarkerMetadata
        (
          _sessionIdentifier,
          MarkerMetadata.MarkerSource.Device,
          userData
        );
#endif

      var generationResult = _manager.BarcodeDisplay.GenerateBarcode(embeddedInfo, true);
      _generatedBarcode = true;

#if UNITY_EDITOR
      Debug.Log
      (
        "Generated barcode with embedded data: " +
        EmbeddedStationaryMetadataSerializer.StaticDeserialize
          (Convert.FromBase64String(generationResult.EncodedText))
      );
#else
      Debug.Log("Generated barcode with embedded data: " +
                BasicMetadataSerializer.StaticDeserialize(Convert.FromBase64String(generationResult.EncodedText)));
#endif

      // InitializeForMarkerScanning only needs to be called when using Marker Sync for both
      // joining a networking session and to sync. If only being used for the former, you don't
      // need to call this method, and just need to display the QR code.
      var realWorldPoints =
        ZXingMarkerGenerator.GetRealWorldPointPositions
        (
          _manager.BarcodeDisplay.Center,
          _manager.BarcodeDisplay.Points
        );

      _manager.ARNetworking.InitializeForMarkerScanning(realWorldPoints);

      if (SystemInfo.supportsGyroscope)
        _manager.UpdateTick += ToggleBarcodeDisplay;
    }

    private void ToggleBarcodeDisplay()
    {
      if (!_generatedBarcode)
        return;

      var pitch = ReadGyroscopeRotation().eulerAngles.x;

      if (pitch >= 75f && pitch <= 90f)
        _manager.BarcodeDisplay.Show();
      else
        _manager.BarcodeDisplay.Hide();
    }

    private static readonly Quaternion _first = new Quaternion(0.5f, 0.5f, -0.5f, 0.5f);
    private static readonly Quaternion _mask = new Quaternion(0, 0, 1, 0);

    private static Quaternion ReadGyroscopeRotation()
    {
      return _first * Input.gyro.attitude * _mask;
    }
  }
}
