// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK
{
  /// Options for which implementation of the core ARDK interfaces (IARSession,
  /// IMultipeerNetworking, and IARNetworking) to use at runtime.
  public enum RuntimeEnvironment
  {
    /// On a mobile device, this is equivalent to the LiveDevice value.
    /// In the Unity Editor, if Remoting is enabled in the Virtual Studio window then it is
    /// equivalent to the Remote value, else it is equivalent to the Mock value.
    Default,

    /// AR data is sourced "live" (that is, an actual camera or similar is being used), and
    /// networking connects to a live server.
    LiveDevice,

    /// AR data and networking responses are coming from a remote source.
    Remote,

    /// AR data and networking responses are completely code based and contained in the Unity Editor.
    Mock,

    /// Pre-recorded AR data and networking responses are played back
    /// @note This is an experimental feature
    Playback
  }
}
