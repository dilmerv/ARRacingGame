// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Configuration
{
  /// Possible states of capability for the current hardware device regarding a particular
  /// configuration.
  public enum ARHardwareCapability
  {
    /// Hardware device is not capable for the particular configuration.
    NotCapable = 0,

    /// An internal error occurred while determining capability.
    /// @note Android-only value.
    CheckFailedWithError = 1,

    /// The query to check capability timed out. This may be due to the device being offline.
    /// @note Android-only value.
    CheckFailedWithTimeout = 2,

    /// Hardware device is capable of the particular configuration.
    Capable = 4,
  }
}
