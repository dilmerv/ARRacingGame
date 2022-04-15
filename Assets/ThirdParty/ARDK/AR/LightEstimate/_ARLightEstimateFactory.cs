// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.LightEstimate
{
  internal static class _ARLightEstimateFactory
  {
    internal static _SerializableARLightEstimate _AsSerializable(this IARLightEstimate source)
    {
      if (source == null)
        return null;

      var possibleResult = source as _SerializableARLightEstimate;
      if (possibleResult != null)
        return possibleResult;
      
      return
        new _SerializableARLightEstimate
        (
          source.AmbientIntensity,
          source.AmbientColorTemperature,
          source.ColorCorrection
        );
    }
  }
}
