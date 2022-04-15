namespace Niantic.ARDK.Rendering
{
  /// Possible ways to determine camera feed resolution.
  public enum ResolutionMode
  {
    /// Behaviour is same as `Screen`, below.
    Default = 0,

    /// Use a custom resolution size, but automatically handle screen rotation.
    Custom,

    /// Match resolution of the CPU image surfaced by ARKit or ARCore.
    FromHardware,

    /// Match screen resolution.
    Screen,

    /// Use a custom resolution size and do not automatically handle screen rotation.
    Fixed
  }
}
