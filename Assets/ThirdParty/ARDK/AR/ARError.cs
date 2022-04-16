// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR
{
  /// Possible error values output by an AR session's didFailWithError callback.
  public enum ARError
  {
    /// The ERConfiguration object passed to the Run() method is not supported by the current device.
    UnsupportedConfiguration = 100,

    /// A sensor required to run the session is not available.
    SensorUnavailable = 101,

    /// A sensor failed to provide the required input.
    SensorFailed = 102,

    /// The user has denied your app permission to use the device camera.
    CameraUnauthorized = 103,

    /// World tracking has encountered a fatal error.
    WorldTrackingFailed = 200,

    /// An invalid reference image was passed in the configuration.
    InvalidReferenceImage = 300,

    /// AR features are not available currently, check for availability using the
    /// ARCore Availability API.
    /// @note This is an Android-only value.
    AvailabilityFailure = 400,
  }
}
