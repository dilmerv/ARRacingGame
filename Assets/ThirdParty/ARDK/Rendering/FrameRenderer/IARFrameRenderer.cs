using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.Rendering
{
  /// Arguments for rendering events.
  /// @note This will be renamed to FrameRendererArgs in a future update
  public class FrameRenderedArgs: 
    IArdkEventArgs
  {
    /// The renderer.
    public readonly IARFrameRenderer Renderer;
    
    public FrameRenderedArgs(IARFrameRenderer renderer)
    {
      Renderer = renderer;
    }
  }

  public interface IARFrameRenderer
  {
    /// Event for when the renderer had just finished rendering to its primary target. 
    event ArdkEventHandler<FrameRenderedArgs> FrameRendered;

    /// Recommended target framerate of the platform.
    int RecommendedFrameRate { get; }
    
    /// The render target. Either a camera or a GPU texture.
    RenderTarget Target { get; }
    
    /// The resolution of a rendered frame image.
    Resolution Resolution { get; }

    /// Affine transform for converting between normalized image coordinates and a
    /// coordinate space appropriate for rendering the camera image onscreen.
    Matrix4x4 DisplayTransform { get; }

    /// The projection matrix of the device's camera. This takes into account your device's
    /// focal length, size of the sensor, distortions inherent in the lenses, autofocus,
    /// temperature, and/or etc.
    Matrix4x4 ProjectionTransform { get; }

    /// Distance of the near clipping plane in world units.
    float NearPlane { get; set; }
    
    /// Distance of the far clipping plane in world units.
    float FarPlane { get; set; }

    /// Fence that should be waited on in other command buffers that utilize the
    /// texture output by this renderer.
    /// @note Only available if the renderer has been initialized.
    GraphicsFence? GPUFence { get; }

    /// Registers a new feature provider to this renderer.
    /// Call this method to insert components to the pipeline
    /// that alter or extend the background rendering. 
    void AddFeatureProvider(IRenderFeatureProvider provider);
    
    /// Removes the specified feature provider from this renderer,
    /// if it is present in its pipeline.
    void RemoveFeatureProvider(IRenderFeatureProvider provider);
  }
}
