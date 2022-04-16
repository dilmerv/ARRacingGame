// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR.Camera;

using UnityEngine;

namespace Niantic.ARDK.AR.SLAM
{
  /// <summary>
  /// System to sync Unity-space coordinates of multiple peers using a marker (ex. QRCode, image).
  /// </summary>
  internal interface IMarkerSyncer:
    IDisposable
  {
    /// An identifier used internally to relate different systems. This is the identifier for
    /// the LocationService system.
    Guid StageIdentifier { get; }

    /// <summary>
    /// Use to publish information about the marker displayed on a host peer's device.
    /// </summary>
    /// <param name="pointLocations">Real-world space locations of the marker points (unit: meters).
    /// Points must be in the same order as in the scannedPointLocations argument of
    /// the ScanMarkerOnDevice method.</param>
    void SendMarkerInformation(Vector3[] pointLocations);

    /// <summary>
    /// Use to sync using a scan of a marker displayed on the host peer's device screen
    /// </summary>
    /// <param name="arCamera">
    /// ARCamera instance from the ARFrame that the marker was scanned on.
    /// </param>
    /// <param name="scannedPointLocations">
    /// Screen-space locations of scanned marker points (unit: pixels).
    /// Points must be in the same order as in the corners argument in the SendMarkerInformation
    /// method.
    /// </param>
    /// <param name="timestamp"></param>
    void ScanMarkerOnDevice
    (
      IARCamera arCamera,
      Vector2[] scannedPointLocations,
      double timestamp
    );

    /// <summary>
    /// Use to sync using a scan of a marker that is placed in the real world. In this use case,
    /// make sure the real world coordinate space has gravity aligned on the y-axis (ie y-axis is
    /// up/down).
    /// </summary>
    /// <param name="arCamera">
    /// ARCamera instance from the ARFrame that the marker was scanned on.
    /// </param>
    /// <param name="markerWorldTransform">
    /// Real world-space position (unit: meters) and orientation of the marker relative to the
    /// origin of the shared AR space.
    /// </param>
    /// <param name="markerPointPositions">
    /// Real world-space positions (unit: meters) of the marker points relative to the
    /// markerWorldTransform . Points must be in the same order as in the scannedPointLocations
    /// argument.
    /// </param>
    /// <param name="scannedPointPositions">
    /// Screen-space locations (unit: pixels) of the scanned marker points. Points must be in the
    /// same order as in markerPointLocations.
    /// </param>
    /// <param name="timestamp"></param>
    void ScanStationaryMarker
    (
      IARCamera arCamera,
      Matrix4x4 markerWorldTransform,
      Vector3[] markerPointPositions,
      Vector2[] scannedPointPositions,
      double timestamp
    );
  }
}
