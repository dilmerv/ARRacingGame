// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.Utilities.Marker
{
  [Serializable]
  public class StationaryMarkerMetadata:
    MarkerMetadata
  {
    public Matrix4x4 RealWorldTransform;
    public Vector3[] DetectionPointPositions;

    [SerializeField]
    private byte[] _combinedData;

    /// <summary>
    /// StationaryMarkerMetadata will be embedded in markers that are displayed in a stationary
    ///   location in the real world.
    /// </summary>
    /// <param name="sessionIdentifier">Name of networking session scanners of this marker will join.</param>
    /// <param name="data">Any user defined data that should be also embedded in the barcode.</param>
    /// <param name="realWorldTransform">Real world-space position (unit: meters) and orientation
    ///   of the marker relative to the origin of the shared AR space.</param>
    /// <param name="detectionPointPositions">Real world-space positions (unit: meters) of the
    ///   marker points relative to the markerWorldTransform .</param>
    /// <param name="serializer">Will use the EmbeddedStationaryMetadataSerializer if this arg is left null.</param>
    public StationaryMarkerMetadata
    (
      string sessionIdentifier,
      byte[] data,
      Matrix4x4 realWorldTransform,
      Vector3[] detectionPointPositions,
      IMetadataSerializer serializer = null
    )
    {
      throw new NotSupportedException("Stationary markers are not currently supported.");

      /**
      SessionIdentifier = sessionIdentifier;
      Source = MarkerSource.Stationary;
      Data = data;
      RealWorldTransform = realWorldTransform;
      DetectionPointPositions = detectionPointPositions;

      Serializer = serializer ?? new EmbeddedStationaryMetadataSerializer();
      */
    }

    public override string ToString()
    {
      return
        string.Format
        (
          "StationaryMarkerMetadata (SessionIdentifier: {0}, Source: {1}, Data Length: {2}, " +
          "World Transform P: {3} R: {4}, Points: [{5}, {6}, {7}, {8}])",

          SessionIdentifier,
          Source,
          Data == null ? 0 : Data.Length,
          RealWorldTransform.ToPosition(),
          RealWorldTransform.ToRotation().eulerAngles,
          DetectionPointPositions[0].ToString("F4"),
          DetectionPointPositions[1].ToString("F4"),
          DetectionPointPositions[2].ToString("F4"),
          DetectionPointPositions[3].ToString("F4")
        );
    }
  }
}
