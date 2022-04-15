// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR
{
  /// Options for transitioning an AR session's current state when you change its configuration.
  [Flags]
  public enum ARSessionRunOptions
  {
    /// No additional behaviour will occur when the configuration is changed.
    /// @note Used to set the flag programmatically, select "Nothing" in the Unity Editor
    None = 0,

    /// The session does not continue tracking from the previous configuration.
    /// @note This is an iOS-only value.
    ResetTracking = 1,

    /// Any anchor objects created previously are removed.
    RemoveExistingAnchors = 2,

    /// Meshing is reset
    RemoveExistingMesh = 4,
  }
}
