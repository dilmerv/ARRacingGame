// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.Marker
{
  /// @note This is part of an experimental feature that is not advised to be used in release builds.
  public interface IMetadataSerializer
  {
    byte[] Serialize(MarkerMetadata stationaryMetadata);

    MarkerMetadata Deserialize(byte[] data);
  }
}
