// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

using Niantic.ARDK.Utilities.Collections;

namespace Niantic.ARDK.AR.LightEstimate
{
  [Serializable]
  internal sealed class _SerializableARLightEstimate:
    IARLightEstimate
  {
    internal _SerializableARLightEstimate
    (
      float ambientIntensity,
      float ambientColorTemperature,
      ReadOnlyCollection<float> colorCorrection
    )
    {
      AmbientIntensity = ambientIntensity;
      AmbientColorTemperature = ambientColorTemperature;

      if (colorCorrection != null && colorCorrection.Count > 0)
        ColorCorrection = colorCorrection;
      else
        ColorCorrection = EmptyReadOnlyCollection<float>.Instance;
    }
    
    public float AmbientIntensity { get; private set; }
    public float AmbientColorTemperature { get; private set; }
    public ReadOnlyCollection<float> ColorCorrection { get; private set; }
    
    void IDisposable.Dispose()
    {
      // Do nothing as this implementation is fully managed.
    }
  }
}
