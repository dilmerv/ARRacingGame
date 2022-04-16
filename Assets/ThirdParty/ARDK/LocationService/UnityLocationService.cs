// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityLocationServiceStatus = UnityEngine.LocationServiceStatus;

namespace Niantic.ARDK.LocationService
{
  /// @brief An object that manages location updates from the device.
  /// @note In order to use LocationServices on iOS 10+, the "Location Usage Description" box in
  ///   PlayerPrefs must be filled out.
  /// @note This is currently not supported with Remote Connection.
  public sealed class UnityLocationService:
    ILocationService
  {
    private const float _BroadcastInterval = 0.1f;
    private const float _DefaultAccuracyMeters = 10f;
    private const float _DefaultDistanceMeters = 10f;

    /// Timestamp of the latest location update
    private double _lastKnownLocationTimestamp = -1;

    private double _lastKnownCompassTimestamp = -1;
    
    private double _timestep;

    /// Last known status
    private LocationServiceStatus _prevStatus = LocationServiceStatus.None;

    public void StartSession
    (
      float desiredAccuracyInMeters = _DefaultAccuracyMeters,
      float updateDistanceInMeters = _DefaultDistanceMeters,
      float updateInterval = _BroadcastInterval
    )
    {
      _timestep = updateInterval;

      // Setup location service
      var locationServiceStarted =
        StartUnityLocationService(desiredAccuracyInMeters, updateDistanceInMeters);

      if (locationServiceStarted)
        _UpdateLoop.Tick += OnUpdate;
    }

    public void StopSession()
    {
      Input.location.Stop();

      // Stop update loop
      _UpdateLoop.Tick -= OnUpdate;
    }

    // Setup the Unity location service
    private bool StartUnityLocationService
    (
      float desiredAccuracyInMeters,
      float updateDistanceInMeters
    )
    {
      // First, check if user has location service enabled
      if (!Input.location.isEnabledByUser)
        return false;

      // Start service
      _lastKnownLocationTimestamp = -1;
      Input.location.Start(desiredAccuracyInMeters, updateDistanceInMeters);
      Input.compass.enabled = true;
      
      return true;
    }

    // Check for location updates every frame
    private void OnUpdate()
    {
      var currentStatus = ConvertToCompatibleStatus(Input.location.status);
      CheckAndPublishStatusChange(currentStatus);

      switch (currentStatus)
      {
        case LocationServiceStatus.Initializing:
        case LocationServiceStatus.Stopped:
          // Do nothing
          return;

        case LocationServiceStatus.UserPermissionError:
          StopSession();
          return;

        case LocationServiceStatus.Running:
          CheckAndPublishLocationChange(Input.location.lastData);
          CheckAndPublishCompassChange(Input.compass);
          return;
      }
    }

    // Convert between Unity location status and native location status
    private LocationServiceStatus ConvertToCompatibleStatus(UnityLocationServiceStatus unityStatus)
    {
      switch (unityStatus)
      {
        case UnityLocationServiceStatus.Initializing:
          return LocationServiceStatus.Initializing;

        case UnityLocationServiceStatus.Stopped:
          return LocationServiceStatus.Stopped;

        case UnityLocationServiceStatus.Failed:
          return LocationServiceStatus.UserPermissionError;

        case UnityLocationServiceStatus.Running:
          return LocationServiceStatus.Running;

        default:
          var message =
            "No ARDK.LocationService.LocationServiceStatus compatible with " +
            "UnityEngine.LocationServiceStatus {0} could be found.";

          Debug.LogWarningFormat(message, unityStatus);
          return LocationServiceStatus.None;
      }
    }

    // Publish change in status of location service if needed
    private void CheckAndPublishStatusChange(LocationServiceStatus newStatus)
    {
      if (_prevStatus == newStatus)
        return;

      _prevStatus = newStatus;

      var handler = StatusUpdated;
      if (handler != null)
        handler(new LocationStatusUpdatedArgs(newStatus));
    }

    // Publish update in location if needed
    private void CheckAndPublishLocationChange(LocationInfo info)
    {
      if (Math.Abs(_lastKnownLocationTimestamp - info.timestamp) < _timestep)
        return;

      _lastKnownLocationTimestamp = info.timestamp;

      var handler = LocationUpdated;
      if (handler != null)
      {
        var args =
          new LocationUpdatedArgs
          (
            info.altitude,
            info.latitude,
            info.longitude,
            info.horizontalAccuracy,
            info.verticalAccuracy,
            info.timestamp
          );

        handler(args);
      }
    }

    private void CheckAndPublishCompassChange(Compass compass)
    {
      if (Math.Abs(_lastKnownCompassTimestamp - compass.timestamp) < _timestep)
        return;
      
      _lastKnownCompassTimestamp = compass.timestamp;
      
      var handler = CompassUpdated;
      if (handler != null)
      {
        var args = new CompassUpdatedArgs
        (
          compass.trueHeading,
          compass.headingAccuracy,
          compass.timestamp
        );
        
        handler(args);
      }
    } 
    
    /// <inheritdoc />
    public event ArdkEventHandler<LocationStatusUpdatedArgs> StatusUpdated;

    /// <inheritdoc />
    public event ArdkEventHandler<LocationUpdatedArgs> LocationUpdated;

    /// <inheritdoc />
    public event ArdkEventHandler<CompassUpdatedArgs> CompassUpdated;
  }
}
