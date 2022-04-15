// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.LocationService
{
  public struct LocationUpdatedArgs:
    IArdkEventArgs
  {
    /// Geographical device location altitude.
    public readonly float Altitude;

    /// Geographical device location latitude.
    public readonly float Latitude;

    /// Geographical device location longitude.
    public readonly float Longitude;

    /// Horizontal accuracy of the location.
    public readonly float HorizontalAccuracy;

    /// Vertical accuracy of the location.
    public readonly float VerticalAccuracy;

    /// POSIX Timestamp (in seconds since 1970) when location was last time updated.
    public readonly double Timestamp;

    public LocationUpdatedArgs
    (
      float altitude,
      float latitude,
      float longitude,
      float horizontalAccuracy,
      float verticalAccuracy,
      double timestamp
    )
    {
      Altitude = altitude;
      Latitude = latitude;
      Longitude = longitude;
      HorizontalAccuracy = horizontalAccuracy;
      VerticalAccuracy = verticalAccuracy;
      Timestamp = timestamp;
    }
  }
}
