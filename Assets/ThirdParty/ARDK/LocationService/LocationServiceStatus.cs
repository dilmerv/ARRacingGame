// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.LocationService
{
  /// @brief Possible values output by a LocationServiceSession's OnSessionDidChangeStatus callback.
  public enum LocationServiceStatus
  {
    None = 0,
    Initializing = 1,
    Running = 2,
    Stopped = 3,
    UserPermissionError = 4,
  }
}
