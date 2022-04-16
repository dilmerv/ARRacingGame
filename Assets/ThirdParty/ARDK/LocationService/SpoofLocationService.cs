// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.LocationService
{
  public sealed class SpoofLocationService:
    ILocationService
  {
    public void SetStatus(LocationServiceStatus status)
    {
      var handler = StatusUpdated;
      if (handler != null)
        handler(new LocationStatusUpdatedArgs(status));
    }

    public void SetLocation
    (
      float altitude,
      float latitude,
      float longitude,
      float horizontalAccuracy,
      float verticalAccuracy,
      double timestamp
    )
    {
      var handler = LocationUpdated;
      if (handler != null)
      {
        var args =
          new LocationUpdatedArgs
          (
            altitude,
            latitude,
            longitude,
            horizontalAccuracy,
            verticalAccuracy,
            timestamp
          );

        handler(args);
      }
    }
  
    public void SetCompass
    (
      float headingAccuracy,
      float trueHeading,
      double timestamp
    )
    {
      var handler = CompassUpdated;
      if (handler != null)
      {
        var args =
          new CompassUpdatedArgs
          (
            headingAccuracy,
            trueHeading,
            timestamp
          );

        handler(args);
      }
    }
    public event ArdkEventHandler<LocationStatusUpdatedArgs> StatusUpdated;
    public event ArdkEventHandler<LocationUpdatedArgs> LocationUpdated;
    public event ArdkEventHandler<CompassUpdatedArgs> CompassUpdated;
  }
}
