namespace Niantic.ARDK.AR
{
  /// The amount of synchronization with the camera pose to apply to each awareness buffer. Currently,
  ///   awareness frames are surfaced at ~20 fps, while poses are surfaced at ~30 fps (Android) or
  ///   ~60 fps (iOS).
  public enum InterpolationMode
  {
    /// Don't sync with the camera pose.
    None = 0,

    /// The awareness buffer is synced one time with the camera when it surfaces to the app.
    Balanced = 1,

    /// The awareness buffer is synced with every pose until a new buffer replaces it.
    Smooth = 2
  }
}
