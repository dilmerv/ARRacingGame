// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

namespace Niantic.ARDK.AR
{
  public interface IARLightEstimate:
    IDisposable
  {
    /// <summary>
    /// The detected ambient light intensity.
    /// @remark For iOS, this value is in lumens, 1000 representing "neutral" lighting. 
    /// @remark For Android, this value represents average pixel intensity of the range 0.0 - 1.0.
    /// </summary>
    float AmbientIntensity { get; }

    /// <summary>
    /// The estimated color temperature, in degrees Kelvin, of ambient light throughout the scene.
    /// @note This is an iOS-only value.
    /// </summary>
    float AmbientColorTemperature { get; }

    /// <summary>
    /// A 4-element vector. Components 0-2 represent the scaling factor to be applied to the 
    /// r, g, and b values, respectively. The last component is the pixel intensity 
    /// (Identical to ARLightEstimate.AmbientIntensity).
    /// @note This is an Android-only value.
    /// @note The green channel [1] is always 1.0, to be used as the reference baseline.
    /// </summary>
    ReadOnlyCollection<float> ColorCorrection { get; }
  }
}
