// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.LocationService
{
  /// @brief An object that manages location updates from the device.
  public interface ILocationService
  {
    /// Informs subscribers when the session status changes.
    event ArdkEventHandler<LocationStatusUpdatedArgs> StatusUpdated;

    /// Informs subscribers when there is an update to the device's location.
    event ArdkEventHandler<LocationUpdatedArgs> LocationUpdated;
    
    /// Informs subscribers when there is an update to the device's compass.
    event ArdkEventHandler<CompassUpdatedArgs> CompassUpdated;
    
  }
}
