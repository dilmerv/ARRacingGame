// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.SLAM
{
  /// Values for specifying what role this device should have in building AR maps and/or how those
  /// maps should be shared.
  /// @note This is part of an experimental feature that is currently disabled in release builds.
  public enum MappingRole
  {
    MapperIfHost,
    Mapper,
    Localizer
  }
}
