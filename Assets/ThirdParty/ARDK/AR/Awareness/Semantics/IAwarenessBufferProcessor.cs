using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  public interface IAwarenessBufferProcessor
  {
    /// A composited matrix to fit the awareness buffer to the screen.
    /// This affine transform converts normalized screen coordinates to
    /// the buffer's coordinate frame while accounting for interpolation.
    Matrix4x4 SamplerTransform { get; }

    /// The current interpolation setting.
    InterpolationMode InterpolationMode { get; set; }

    /// The current setting whether to align with close (0.0f) or distant pixels (1.0f)
    /// during interpolation.
    float InterpolationPreference { get; set; }
  }
}
