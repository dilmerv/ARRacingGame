using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDKExamples.Rendering
{
  /// Manages the ARDK's DepthBufferProcessor, which maintains the latest
  /// depth values from the ArSession and allows sampling.
  /// @note
  /// This also provides the ARFrameRenderer's shader with the depth texture,
  /// so z-buffer occlusion can be done in the rendering pipeline. The depth
  /// texture is generated on each keyframe
  public class ARDepthRenderer:
    IRenderFeatureProvider,
    ITargetableRenderFeatureProvider,
    IDisposable
  {
    private RenderTarget _renderTarget;
    private Texture2D _depthTexture;
    private Matrix4x4 _depthTransform;
    private readonly DepthBufferProcessor _depthBufferProcessor;

    /// The render target
    public RenderTarget? Target
    {
      get => _renderTarget;
      set => _renderTarget = value.GetValueOrDefault();
    }

    // The class uses this to notify the renderer that the configuration changed.
    public event ArdkEventHandler<RenderFeaturesChangedArgs> ActiveFeaturesChanged;

    // This is part of the public API of IRenderFeatureProvider.
    public ISet<string> Features { get; } = new HashSet<string>
    {
      FeatureBindings.DepthDebug, FeatureBindings.DepthZWrite
    };

    /// Create the AR Depth Renderer
    /// @param camera The camera to render depth with (or Camera.main if none is provided)
    /// @param interpolationMode The mode of interpolation to use)
    public ARDepthRenderer
    (
      Camera camera = null,
      InterpolationMode interpolationMode = InterpolationMode.Smooth
    )
    {
      if (!camera)
        camera = Camera.main;

      _renderTarget = new RenderTarget(camera);
      _depthBufferProcessor = new DepthBufferProcessor(_renderTarget)
      {
        InterpolationMode = interpolationMode
      };

      _depthBufferProcessor.AwarenessStreamUpdated += OnAwarenessStreamUpdated;
    }

    /// Dispose of the AR Depth Renderer
    public void Dispose()
    {
      ActiveFeaturesChanged?.Invoke
      (
        new RenderFeaturesChangedArgs
        (
          new RenderFeatureConfiguration(new List<string>(), Features)
        )
      );

      if (_depthTexture)
        UnityEngine.Object.Destroy(_depthTexture);

      _depthBufferProcessor.AwarenessStreamUpdated -= OnAwarenessStreamUpdated;
      _depthBufferProcessor.Dispose();
    }

    /// This notifies the ARFrameRenderer which sets the proper shader variables.
    /// @note
    /// This is a no-op until the ARFrameRenderer has been initialized, so it should
    /// only be called after that happens. 
    public void SetOcclusionEnabled(bool enabled)
    {
      var enabledFeatures = enabled ? Features : new HashSet<string>();
      var disabledFeatures = enabled ? new HashSet<string>() : Features;

      ActiveFeaturesChanged?.Invoke
      (
        new RenderFeaturesChangedArgs
        (
          new RenderFeatureConfiguration(enabledFeatures, disabledFeatures)
        )
      );
    }

    /// Handles depth buffer updates.
    /// @note
    /// This maintains a copy of the depth texture on the
    /// CPU side which is used in the rendering shader.
    private void OnAwarenessStreamUpdated(ContextAwarenessStreamUpdatedArgs<IDepthBuffer> args)
    {
      // Acquire new information
      var sender = args.Sender;
      var awarenessBuffer = sender.AwarenessBuffer;
      _depthTransform = sender.SamplerTransform;

      if (args.IsKeyFrame)
      {
        // Deliver depth in a floating point texture intact
        awarenessBuffer.CreateOrUpdateTextureRFloat(ref _depthTexture);
      }
    }

    /// This sets shader variables needed for rendering occlusion and suppression
    /// @note
    /// This gets called from the ARFrameRenderer, which itself is called by the
    /// ARFrameRenderer.UpdateState() call in this class's own LateUpdate
    public void UpdateRenderState(Material material)
    {
      // This is the time when the frame renderer asks for additional
      // information for rendering the AR background. Here we pass the
      // texture with depth information and a matrix to align it with
      // the viewport.
      material.SetTexture(PropertyBindings.DepthChannel, _depthTexture);
      material.SetMatrix(PropertyBindings.DepthTransform, _depthTransform);

      // Only dealing with the z-buffer rendering. No mesh. Do not scale
      material.SetFloat(PropertyBindings.DepthScaleMin, 0);
      material.SetFloat(PropertyBindings.DepthScaleMax, 1);
    }
  }
}
