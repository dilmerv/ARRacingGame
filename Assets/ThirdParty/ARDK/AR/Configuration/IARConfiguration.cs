// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.ObjectModel;

namespace Niantic.ARDK.AR.Configuration
{
  /// The interface for AR session configurations.
  /// @note
  ///   You will not create or work with instances of this interface. Instead, you create
  ///   one of the more specialized sub-classes. In order to run an AR session,
  ///   you must pass in a configuration (one of these sub-classes) that most matches the type of
  ///   AR experience you wish to provide with your app or game.
  public interface IARConfiguration:
    IDisposable
  {
    /// A boolean specifying whether or not camera images are analyzed to estimate scene lighting.
    bool IsLightEstimationEnabled { get; set; }

    /// A value specifying how the session maps the real-world device motion into a
    /// coordinate system.
    /// @note This is an iOS-only value.
    WorldAlignment WorldAlignment { get; set; }

    /// Returns a collection of supported video formats by this configuration and device.
    /// @note iOS-only value.
    /// @note Not supported in Virtual Studio.
    ReadOnlyCollection<IARVideoFormat> SupportedVideoFormats { get; }

    /// A value specifying the options to use for the output video stream.
    /// @note This is an iOS-only value.
    /// @note **May be null** depending on system version.
    IARVideoFormat VideoFormat { get; set; }

    /// Copies the values of this configuration into the target configuration.
    void CopyTo(IARConfiguration target);
  }
}
