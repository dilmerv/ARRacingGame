// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR
{
  /// @brief Possible values for position tracking quality.
  public enum TrackingState:
    byte
  {
    /// Position tracking is not available.
    NotAvailable = 0,

    /// Tracking is available, but the quality is limited.
    Limited = 1,

    /// Position tracking is providing optimal results.
    Normal = 2,
  }
}
