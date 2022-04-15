// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.LocationService
{
  public struct CompassUpdatedArgs:
    IArdkEventArgs
  { 
    /// The heading in degrees relative to the geographic North Pole.
    public readonly float TrueHeading;
    
    /// Accuracy of heading reading in degrees.
    /// Negative value mean unreliable reading.
    /// If accuracy is not supported or not available, 0 is returned.
    /// Not all platforms support this pricise accuracy,
    /// so the value may vary between few constant values.
    public readonly float HeadingAccuracy;

    /// POSIX Timestamp (in seconds since 1970) when the heading was last time updated.
    public readonly double Timestamp;
    
    public CompassUpdatedArgs
    (
      float trueHeading,
      float headingAccuracy,
      double timestamp
    )
    {
      TrueHeading = trueHeading;
      HeadingAccuracy = headingAccuracy;
      Timestamp = timestamp;
    }

  }
}
