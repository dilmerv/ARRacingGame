// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Localization
{
  /// @cond ARDK_VPS_BETA
  /// Failure reasons for a failed localization
  /// @see [Working with the Visual Positioning System (VPS)](@ref working_with_vps)
  /// @endcond
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  public enum LocalizationFailureReason
  {
    /// No failure, no reason.
    None = 0,
    
    /// Failure, unknown reasons.
    Unknown = 1,

    /// Failure, couldn't localize within the specified timeout period, likely
    /// because the user's world tracking was unstable or the user was not 
    /// looking at a localize-able target.
    Timeout = 2,

    /// Canceled, because StopLocalization was called.
    Canceled = 3,

    // Failure, localization service is configured with an invalid URL.
    InvalidEndpoint = 4,
    
    /// Failure to connect to localization service.
    CannotConnectToServer = 5,
    
    /// Failure, localization service rejected the client request.
    BadRequest = 6,
    
    /// Failure, client rejected the localization service response.
    BadResponse = 7,

    /// Failure, Identifier is invalid
    BadIdentifier = 8,
        
    /// Failure, Location permissions were not granted by the user, or no location
    /// service was started by the app.
    LocationDataNotAvailable = 9,
    
    /// Failure, based on GPS information, system determined localization was not
    /// possible.
    NotSupportedAtLocation = 10,

    // Serious Server Failure
    InternalServerFailure = 11,

    // Failure, invalid API key
    InvalidAPIKey = 12,

    // Dev is not authenticated to use VPS
    Unauthenticated = 13
  }
}
