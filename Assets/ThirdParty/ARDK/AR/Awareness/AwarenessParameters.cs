using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  /// Parameters that apply to awareness buffers and features
  public static class AwarenessParameters
  {
    ///   This value sets the normalized distance of the back-projection plane. Lower values result
    ///   in depths more accurate for closer pixels, but pixels further away will move faster
    ///   than they should. Use 0.5f if your subject in the scene is always closer than ~2 meters
    ///   from the device, and use 1.0f if your subject is further away most of the time.
    public const float DefaultBackProjectionDistance = 0.9f;

    /// The near clipping plane used for interpolating awareness buffers.
    public const float DefaultNear = 0.2f;

    /// The far clipping plane used for interpolating awareness buffers.
    public const float DefaultFar = 100.0f;

    /// ZBufferParams for awareness buffers used in
    /// the context of temporal warping (interpolation).
    public static Vector4 ZBufferParams = new Vector4
    (
      x: 1.0f - DefaultFar / DefaultNear,
      y: DefaultFar / DefaultNear,
      z: (1.0f - DefaultFar / DefaultNear) / DefaultFar,
      w: (DefaultFar / DefaultNear) / DefaultFar
    );
  }
}
