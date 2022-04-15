using System.Collections.Generic;
using System.Text.RegularExpressions;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Depth.Effects;
using Niantic.ARDK.Internals.EditorUtilities;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  [DisallowMultipleComponent]
  public class ARDepthManager: 
    ARRenderFeatureProvider
  {
    /// Event for when the first depth buffer is received.
    public event ArdkEventHandler<ContextAwarenessArgs<IDepthBuffer>> DepthBufferInitialized;

    /// Event for when the contents of the depth buffer or its affine transform was updated.
    public event
      ArdkEventHandler<ContextAwarenessStreamUpdatedArgs<IDepthBuffer>> DepthBufferUpdated;

    public enum OcclusionMode
    {
      /// No occlusions.
      None = 0,

      /// The depth of an ARFrame is written to the target's z-buffer.
      DepthBuffer = 1,

      /// Screen space mesh with its vertices pushed out to corresponding depth values.
      ScreenSpaceMesh = 2,

      /// Uses the depth buffer on capable hardware, falls back to a screen space mesh occluder otherwise.
      Auto = 3
    }

    [SerializeField]
    [_Autofill]
    private Camera _arCamera;

    [SerializeField]
    [Range(0, 60)]
    private uint _keyFrameFrequency = 20;

    [SerializeField]
    [HideInInspector]
    [Tooltip("Whether the depth buffer should synchronize with the camera pose.")]
    private InterpolationMode _interpolation = InterpolationMode.Smooth;

    [SerializeField]
    [HideInInspector]
    [Range(0.0f, 1.0f)]
    [Tooltip
    (
      "Sets whether to prefer closer or distant objects in the depth buffer to align "
        + "with color pixels more."
    )]
    private float _interpolationPreference = AwarenessParameters.DefaultBackProjectionDistance;

    [SerializeField]
    private OcclusionMode _occlusionMode = OcclusionMode.Auto;

    [SerializeField]
    [HideInInspector]
    private FilterMode _textureFilterMode = FilterMode.Point;

    public OcclusionMode OcclusionTechnique
    {
      get => _occlusionMode;
      set
      {
        if (_occlusionMode == value)
          return;

        _occlusionMode = value;

        VerifyOcclusionTechnique();

        // Reset the mesh occluder if it's present
        if (_meshOccluder != null)
          _meshOccluder.Enabled = _occlusionMode == OcclusionMode.ScreenSpaceMesh;

        // Notify the renderer that the active features have changed
        InvalidateActiveFeatures();
      }
    }

    /// Returns the underlying context awareness processor.
    public IDepthBufferProcessor DepthBufferProcessor
    {
      get => _GetOrCreateProcessor();
    }

    /// Returns the latest depth buffer on CPU memory.
    /// This buffer is not displayed aligned, and
    /// needs to be sampled with the DepthTransform
    /// property.
    public IDepthBuffer CPUDepth
    {
      get => _cpuDepth;
    }
    private IDepthBuffer _cpuDepth;

    /// Returns the latest depth buffer on GPU memory.
    /// The resulting texture is not display aligned,
    /// and needs to be used with the DepthTransform
    /// property.
    /// @note May be null, if occlusions are disabled.
    public Texture GPUDepth
    {
      get => _depthTexture;
    }

    /// Returns a transformation that fits the depth buffer
    /// to the target viewport.
    public Matrix4x4 DepthTransform
    {
      get => _depthTransform;
    }
    private Matrix4x4 _depthTransform;

    // The context awareness processor for depth buffers
    private DepthBufferProcessor _depthBufferProcessor;

    private DepthBufferProcessor _GetOrCreateProcessor()
    {
      if (_depthBufferProcessor == null)
      {
        _depthBufferProcessor = new DepthBufferProcessor(_arCamera)
        {
          InterpolationMode = _interpolation,
          InterpolationPreference = _interpolationPreference
        };
      }

      return _depthBufferProcessor;
    }

    // GPU depth for occlusions
    // TODO: Please make efforts to allow using a render texture for this in future updates
    // Note: If we used a render texture, we could avoid a CPU copy that we don't need
    private Texture2D _depthTexture;

    // Get the MeshOccluder created by this object, if created
    // @note May be null
    public DepthMeshOccluder MeshOccluder
    {
      get
      {
        return _meshOccluder;
      }
    }

    // Controls the occluder mesh, in case the
    // screen space mesh technique is selected
    private DepthMeshOccluder _meshOccluder;

    // The semantics manager is responsible for maintaining
    // a suppression mask if that feature is enabled.
    private ARSemanticSegmentationManager _semanticSegmentationManager;

    // Helpers
    private bool _supportsFloatingPointTextures;
    private bool _supportsZWrite;
    private bool _debugVisualizationEnabled;

    private bool IsDepthNormalized
    {
      get => _occlusionMode != OcclusionMode.None && !_supportsFloatingPointTextures;
    }

    internal Camera _ARCamera
    {
      get { return _arCamera; }
    }

    protected override void InitializeImpl()
    {
      if (_arCamera == null)
      {
        var warning = "The Camera field is not set on the ARDepthManager before use, " +
          "grabbing Unity's Camera.main";

        ARLog._Warn(warning);
        _arCamera = Camera.main;
      }

      _GetOrCreateProcessor();

      // Check hardware capabilities
      _supportsFloatingPointTextures = SystemInfo.SupportsTextureFormat(TextureFormat.RFloat);
      _supportsZWrite = DoesDeviceSupportWritingToZBuffer();

      // Verify the selected occlusion technique
      VerifyOcclusionTechnique();

      base.InitializeImpl();
    }

    private void VerifyOcclusionTechnique()
    {
      // Explicit technique
      if (_occlusionMode != OcclusionMode.Auto)
      {
        // If depth buffering is selected without hardware support
        if (_occlusionMode == OcclusionMode.DepthBuffer && !_supportsZWrite)
          ARLog._Error
          (
            "Using depth buffer occlusion on a device that does not support Z Buffering"
          );

        return;
      }

      // Determine which occlusion technique to use based on the running hardware
      _occlusionMode = _supportsZWrite
        ? OcclusionMode.DepthBuffer
        : OcclusionMode.ScreenSpaceMesh;
    }

    protected override void DeinitializeImpl()
    {
      // Release the depth processor
      _depthBufferProcessor?.Dispose();

      // Release the mesh occluder, if any
      _meshOccluder?.Dispose();

      // Release depth texture
      if (_depthTexture != null)
        Destroy(_depthTexture);

      base.DeinitializeImpl();
    }

    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();

      if (_meshOccluder != null)
        _meshOccluder.Enabled = _occlusionMode == OcclusionMode.ScreenSpaceMesh;

      // Attempt to acquire the semantics manager
      if (_semanticSegmentationManager == null)
        _semanticSegmentationManager = GetComponent<ARSemanticSegmentationManager>();

      // If the semantics manager is present, listen to its updates
      if (_semanticSegmentationManager != null)
        _semanticSegmentationManager.SemanticBufferUpdated += OnSemanticBufferUpdated;

      // Listen to updates from the depth stream
      _depthBufferProcessor.AwarenessStreamBegan += OnDepthBufferInitialized;
      _depthBufferProcessor.AwarenessStreamUpdated += OnDepthBufferUpdated;
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();

      if (_meshOccluder != null)
        _meshOccluder.Enabled = false;

      if (_semanticSegmentationManager != null)
        _semanticSegmentationManager.SemanticBufferUpdated -= OnSemanticBufferUpdated;

      _depthBufferProcessor.AwarenessStreamBegan -= OnDepthBufferInitialized;
      _depthBufferProcessor.AwarenessStreamUpdated -= OnDepthBufferUpdated;
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration worldConfig)
      {
        worldConfig.IsDepthEnabled = AreFeaturesEnabled;
        worldConfig.DepthTargetFrameRate = _keyFrameFrequency;
      }
    }

    private void OnDepthBufferInitialized(ContextAwarenessArgs<IDepthBuffer> args)
    {
      // Currently just a pass-through
      DepthBufferInitialized?.Invoke(args);
    }

    private void OnDepthBufferUpdated(ContextAwarenessStreamUpdatedArgs<IDepthBuffer> args)
    {
      // Acquire new information
      var processor = args.Sender;
      _cpuDepth = processor.AwarenessBuffer;
      _depthTransform = processor.SamplerTransform;

      // Only update the depth texture when occlusions are enabled
      var shouldUpdateTexture = _occlusionMode != OcclusionMode.None && args.IsKeyFrame;
      if (shouldUpdateTexture)
      {
        if (_supportsFloatingPointTextures)
          // Deliver depth in a floating point texture intact
          _cpuDepth.CreateOrUpdateTextureRFloat(ref _depthTexture, filterMode: _textureFilterMode);
        else
        {
          // Deliver depth in an ARGB32 (8888) texture normalized
          float max = _cpuDepth.FarDistance;
          float min = _cpuDepth.NearDistance;
          _cpuDepth.CreateOrUpdateTextureARGB32
          (
            ref _depthTexture,
            filterMode: _textureFilterMode,
            valueConverter: depth => (depth - min) / (max - min)
          );
        }
      }

      // Update the screen space mesh occluder
      if (_occlusionMode == OcclusionMode.ScreenSpaceMesh)
      {
        // Do we need to allocate the occluder?
        if (_meshOccluder == null)
          TryCreateMeshOccluder();

        if (_meshOccluder != null)
        {
          // Update the sampler transform for the occluder
          _meshOccluder.DepthTransform = _depthTransform;

          // Update the screen orientation (the rendered mesh might change)
          _meshOccluder.Orientation = Screen.orientation;
        }
      }
      
      // Finally, let users know the manager has finished updating.
      DepthBufferUpdated?.Invoke(args);
    }

    private void OnSemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
    {
      // Sync interpolation preference with the semantics processor
      args.Sender.InterpolationPreference = _GetOrCreateProcessor().InterpolationPreference;

      // Update the depth suppression mask, if present
      if (_occlusionMode == OcclusionMode.ScreenSpaceMesh && _meshOccluder != null)
      {
        _meshOccluder.SuppressionTexture = _semanticSegmentationManager.DepthSuppressionTexture;
        _meshOccluder.SemanticsTransform = args.Sender.SamplerTransform;
      }
    }

    /// Attempts to allocate and initialize a new mesh occluder.
    /// @note This method does nothing if the depth texture is
    ///   not available or the mesh occluder is already present.
    private void TryCreateMeshOccluder()
    {
      // The mesh occluder instance already exists
      if (_meshOccluder != null)
        return;
      
      // Depth texture is not ready
      if (_depthTexture == null)
        return;

      // Allocate the mesh occluder component
      _meshOccluder = new DepthMeshOccluder
      (
        targetCamera: _arCamera,
        depthTexture: _depthTexture,
        meshResolution: _cpuDepth.CalculateDisplayFrame
        (
          _arCamera.pixelWidth,
          _arCamera.pixelHeight
        )
      )
      {
        DebugColorMask = _debugVisualizationEnabled
          ? DepthMeshOccluder.ColorMask.Depth
          : DepthMeshOccluder.ColorMask.None
      };

      // If we push normalized values to the GPU,
      // we need to input the respective min and max values
      // to scale them back during rendering. Otherwise,
      // min 0 and max 1 will result in no scaling applied.
      if (IsDepthNormalized)
        // Scale using the interval [near, far]
        _meshOccluder.SetScaling(_cpuDepth.NearDistance, _cpuDepth.FarDistance);
      else
        // Do not scale
        _meshOccluder.SetScaling(0, 1);
    }

    /// Invoked when this component is asked about the render features
    /// it may be responsible for.
    /// @note: The implementation needs to include all features that are
    /// possibly managed by this component.
    protected override HashSet<string> OnAcquireFeatureSet()
    {
      return new HashSet<string>
      {
        FeatureBindings.DepthZWrite,
        FeatureBindings.DepthDebug
      };
    }

    /// Invoked when it is time to calculate the actual features that
    /// this component currently manages.
    protected override RenderFeatureConfiguration OnEvaluateConfiguration()
    {
      // If the depth manager is not active, all features are disabled
      if (!AreFeaturesEnabled)
        return new RenderFeatureConfiguration(null, featuresDisabled: Features);

      var enabledFeatures = new List<string>();

      // Depth z-write?
      if (_occlusionMode == OcclusionMode.DepthBuffer && _supportsZWrite)
        enabledFeatures.Add(FeatureBindings.DepthZWrite);

      // Visualize depth?
      if (_debugVisualizationEnabled)
        enabledFeatures.Add(FeatureBindings.DepthDebug);

      // All other features are considered disabled
      var disabledFeatures = new HashSet<string>(Features);
      disabledFeatures.ExceptWith(enabledFeatures);
      return new RenderFeatureConfiguration(enabledFeatures, disabledFeatures);
    }

    protected override void OnRenderTargetChanged(RenderTarget? target)
    {
      _GetOrCreateProcessor().AssignViewport(target ?? _arCamera);
    }

    /// Called when it is time to copy the current render state to the main rendering material.
    /// @param material Material used to render the frame.
    public override void UpdateRenderState(Material material)
    {
      // This material is only used for the depth buffering technique...
      if (_occlusionMode != OcclusionMode.DepthBuffer)
        return;

      // This is the time when the frame renderer asks for additional
      // information for rendering the AR background. Here we pass the
      // texture with depth information and a matrix to align it with
      // the viewport.
      material.SetTexture(PropertyBindings.DepthChannel, _depthTexture);
      material.SetMatrix(PropertyBindings.DepthTransform, _depthTransform);

      // When the running hardware does not support floating point textures,
      // we normalize depth to fit in 8 bits and deliver an ARGB32 (8888) texture.
      // In this case, we need to scale depth on the GPU before use.
      if (IsDepthNormalized)
      {
        // Scale depth using the interval [MinDepth, MaxDepth]
        material.SetFloat(PropertyBindings.DepthScaleMin, _depthBufferProcessor.MinDepth);
        material.SetFloat(PropertyBindings.DepthScaleMax, _depthBufferProcessor.MaxDepth);
      }
      else
      {
        // Do not scale
        material.SetFloat(PropertyBindings.DepthScaleMin, 0);
        material.SetFloat(PropertyBindings.DepthScaleMax, 1);
      }
    }

    public void ToggleDebugVisualization(bool isEnabled)
    {
      if (_debugVisualizationEnabled == isEnabled)
        return;

      _debugVisualizationEnabled = isEnabled;

      if (_meshOccluder != null)
      {
        _meshOccluder.DebugColorMask =
          isEnabled
            ? DepthMeshOccluder.ColorMask.Depth
            : DepthMeshOccluder.ColorMask.None;
      }

      InvalidateActiveFeatures();
    }

    private static bool DoesDeviceSupportWritingToZBuffer()
    {
      var device = SystemInfo.graphicsDeviceVersion.ToLower();
      var openglExp = new Regex("opengl es [3-9].");
      return device.Contains("metal") || openglExp.IsMatch(device);
    }
  }
}