// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Utilities.Marker;
using Niantic.ARDKExamples.Helpers;
using UnityEngine;

namespace Niantic.ARDKExamples.MarkerSync
{
  /// @note
  ///   This example has not been polished to the standard of others in ARDK-Examples,
  ///   but is provided as a demonstration of how the MarkerSync APIs can be used.
  public class MarkerScanningManager
  {
    private readonly MarkerSyncSessionManager _manager;

    public MarkerScanningManager(MarkerSyncSessionManager manager)
    {
      _manager = manager;
    }

    public void Reset()
    {
      if (_manager.ARSession != null)
        _manager.ARSession.AnchorsAdded -= StartScanForMarkers;
    }

    public void ScanToJoinSession()
    {
      if (_manager.ARSession != null) { return; }

      Debug.Log("Running scanner session.");

      _manager.InitializeARSession();
      _manager.ARSession.AnchorsAdded += StartScanForMarkers;

      _manager.RunARSession();
    }

    private void StartScanForMarkers(AnchorsArgs args)
    {
      foreach (var anchor in args.Anchors)
      {
        // It's recommended to call ScanForMarker after the device has had some chance to look
        // around the space (which is why this is starting after the DidAddAnchors event is raised),
        // but it's not required.
        if (anchor.AnchorType != AnchorType.Plane)
        {
          Debug.Log("Found an anchor but not a plane");
          continue;
        }

        _manager.ARSession.AnchorsAdded -= StartScanForMarkers;
        _manager.InitializeARNetworking();

        Debug.Log("Starting scan");
        _manager.ARNetworking.ScanForMarker
        (
          MarkerScanOption.ScanToJoin | MarkerScanOption.ScanToSync,
          GotResult
        );

        return;
      }
    }

    private void GotResult(MarkerMetadata metadata)
    {
      Debug.Log("Got metadata: " + metadata);
      Debug.Log("Got message: " + Encoding.UTF8.GetString(metadata.Data));
    }
  }
}