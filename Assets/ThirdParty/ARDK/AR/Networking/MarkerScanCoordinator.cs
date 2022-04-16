// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Text;

using Niantic.ARDK.AR.Networking.ARNetworkingEventArgs;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.Clock;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.Utilities.Marker;
using UnityEngine;

namespace Niantic.ARDK.AR.Networking
{
  internal sealed class MarkerScanCoordinator:
    IDisposable
  {
    public event Action Finished;

    private readonly IARNetworking _arNetworking;

    private IMarkerSyncer _markerSyncer;
    private IMarkerScanner _markerScanner;

    private IMetadataSerializer _metadataSerializer;
    private MarkerMetadata _cachedMetadata;

    private MarkerScanOption _activeScanOptions;
    private Action<MarkerMetadata> _finalResultCallback;

    public MarkerScanCoordinator(IARNetworking arNetworking, IMarkerSyncer syncer = null)
    {
      _arNetworking = arNetworking;
      _markerSyncer = syncer ?? new NativeMarkerSyncer(arNetworking.Networking.StageIdentifier);
    }

    ~MarkerScanCoordinator()
    {
      ARLog._Error("MarkerScanCoordinator should be destroyed by an explicit call to Dispose().");
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var markerSyncer = _markerSyncer;
      if (markerSyncer != null)
      {
        _markerSyncer = null;
        markerSyncer.Dispose();
      }

      var markerScanner = _markerScanner;
      if (markerScanner != null)
      {
        _markerScanner = null;
        markerScanner.Dispose();
      }
    }

    public void InitializeForMarkerScanning(Vector3[] markerPointLocations)
    {
      _markerSyncer.SendMarkerInformation(markerPointLocations);
    }

    public void ScanForMarker
    (
      MarkerScanOption options,
      Action<MarkerMetadata> gotResult = null,
      IMarkerScanner scanner = null,
      IMetadataSerializer metadataSerializer = null
    )
    {
      _markerScanner = scanner ?? new ARFrameMarkerScanner(_arNetworking);
      _metadataSerializer = metadataSerializer;
      _activeScanOptions = options;
      _finalResultCallback = gotResult;

      if ((options & MarkerScanOption.ScanToJoin) != 0)
      {
        ARLog._Debug("Scanning for a marker to join");
        _markerScanner.GotResult += GotJoiningScanResult;
      }
      else if ((options & MarkerScanOption.ScanToSync) != 0)
      {
        if (!_arNetworking.Networking.IsConnected)
        {
          ARLog._Error
          (
            "Cannot scan for syncable markers without first being connected to a " +
             "networking session."
          );
          return;
        }

        ARLog._Debug("Scanning for a marker to sync");
        _markerScanner.GotResult += GotSyncingScanResult;
      }
      else
      {
        ARLog._Error("No valid options were given for marker scanning.");
        return;
      }

      _markerScanner.Scan();
    }

    private void GotJoiningScanResult(ARFrameMarkerScannerGotResultArgs args)
    {
      _markerScanner.Stop();
      _markerScanner.GotResult -= GotJoiningScanResult;

      ValidateCachedMetadata(args.ParserResult.Data);
      _arNetworking.Networking.Join(Encoding.UTF8.GetBytes(_cachedMetadata.SessionIdentifier));

      if ((_activeScanOptions & MarkerScanOption.ScanToSync) != 0)
      {
        ARLog._Debug("Marker scanned, starting sync");
        _arNetworking.Networking.Connected += ConnectedToNetworking;
      }
      else
      {
        ARLog._Debug("Marker scanned and finalizing join");
        if (_finalResultCallback != null)
          _finalResultCallback(_cachedMetadata);

        if (Finished != null)
          Finished();
      }
    }

    private void ConnectedToNetworking(ConnectedArgs args)
    {
      _arNetworking.Networking.Connected -= ConnectedToNetworking;

      // Don't need to wait for coordinated clocks to sync anymore, but this was noted as
      // something that might be revisited in the future, so leaving the line here commented out.
      // _UpdateLoop.Tick += CheckSynchronizedClockStatus;

      // For now, start scanning for sync immediately instead of waiting for clocks to sync
      StartScanningForSync();
    }

    private void StartScanningForSync()
    {
      ARLog._Debug("Marker Sync starting scanning for pose sync");
      _arNetworking.PeerStateReceived += OnPeerStateReceived;
      _markerScanner.GotResult += GotSyncingScanResult;
      _markerScanner.Scan();
    }

    private void ValidateCachedMetadata(byte[] data)
    {
      if (_cachedMetadata != null) { return; }

      _cachedMetadata =
        _metadataSerializer == null
          ? BasicMetadataSerializer.StaticDeserialize(data)
          : _metadataSerializer.Deserialize(data);
    }

    private void GotSyncingScanResult(ARFrameMarkerScannerGotResultArgs args)
    {
      var parserResult = args.ParserResult;
      
      ValidateCachedMetadata(parserResult.Data);

      ARLog._DebugFormat
      (
        "Marker sync got scan results from {0}",
        false,
        _cachedMetadata.Source
      );

      switch (_cachedMetadata.Source)
      {
        case MarkerMetadata.MarkerSource.Device:
        {
          _markerSyncer.ScanMarkerOnDevice
          (
            parserResult.ARCamera,
            parserResult.DetectedPoints,
            parserResult.Timestamp
          );
          break;
        }
        case MarkerMetadata.MarkerSource.Stationary:
        {
          _cachedMetadata =
            _metadataSerializer == null
              ? EmbeddedStationaryMetadataSerializer.StaticDeserialize(parserResult.Data)
              : _metadataSerializer.Deserialize(parserResult.Data);

          var stationaryMetadata = (StationaryMarkerMetadata) _cachedMetadata;

          _markerSyncer.ScanStationaryMarker
          (
            parserResult.ARCamera,
            stationaryMetadata.RealWorldTransform,
            stationaryMetadata.DetectionPointPositions,
            parserResult.DetectedPoints,
            parserResult.Timestamp
          );
          break;
        }
      }

      if (_finalResultCallback != null)
        _finalResultCallback(_cachedMetadata);
    }

    private void OnPeerStateReceived(PeerStateReceivedArgs args)
    {
      var state = args.State;
      var validFinishedState = state == PeerState.Stabilizing || state == PeerState.Stable;
      
      if (validFinishedState && args.Peer.Equals(_arNetworking.Networking.Self))
      {
        ARLog._Debug("Marker Sync finished syncing position");
        
        _markerScanner.Dispose();

        if (Finished != null)
          Finished();
      }
    }
  }
}