// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;

using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.Internals.EditorUtilities;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Extensions
{
  [DisallowMultipleComponent]
  public sealed class ARSemanticSegmentationManager: 
    ARRenderFeatureProvider
  {
    [SerializeField]
    [_Autofill]
    private Camera _arCamera;
    
    [SerializeField]
    [Range(0, 60)]
    private uint _keyFrameFrequency = 20;

    [SerializeField]
    [HideInInspector]
    private string[] _depthSuppressionChannels;

    [SerializeField]
    [HideInInspector]
    [Tooltip("Whether the depth buffer should synchronize with the camera pose.")]
    private InterpolationMode _interpolation = InterpolationMode.Smooth;
    
    [SerializeField]
    [HideInInspector]
    [Range(0.0f, 1.0f)]
    [Tooltip("Sets whether to prefer closer or distant objects in the semantics buffer to align with color pixels more.")]
    private float _interpolationPreference = AwarenessParameters.DefaultBackProjectionDistance;

    public ISemanticBufferProcessor SemanticBufferProcessor
    {
      get => _GetOrCreateProcessor();
    }
    
    /// Event for when the first semantics buffer is received.
    public event ArdkEventHandler<ContextAwarenessArgs<ISemanticBuffer>> SemanticBufferInitialized;

    /// Event for when the contents of the semantic buffer or its affine transform was updated.
    public event
      ArdkEventHandler<ContextAwarenessStreamUpdatedArgs<ISemanticBuffer>> SemanticBufferUpdated;

    private SemanticBufferProcessor _semanticBufferProcessor;
    private SemanticBufferProcessor _GetOrCreateProcessor()
    {
      if (_semanticBufferProcessor == null)
      {
        _semanticBufferProcessor = new SemanticBufferProcessor(_arCamera)
        {
          InterpolationMode = _interpolation, 
          InterpolationPreference = _interpolationPreference
        };
      }

      return _semanticBufferProcessor;
    }
    
    /// Returns a reference to the depth suppression mask texture, if present.
    /// If the suppression feature is disabled, this returns null.
    public Texture DepthSuppressionTexture
    {
      get => _suppressionTexture;
    }
    
    internal Camera _ARCamera
    {
      get { return _arCamera; }
    }

    private Texture2D _suppressionTexture;
    private int[] _suppressionChannelIndices;

    protected override void InitializeImpl()
    {
      if (_arCamera == null)
      {
        var warning = "The Camera field is not set on the ARSemanticSegmentationManager " +
                      "before use, grabbing Unity's Camera.main";

        ARLog._Warn(warning);
        _arCamera = Camera.main;
      }

      _GetOrCreateProcessor();

      base.InitializeImpl();
    }
    
    protected override void DeinitializeImpl()
    {
      // Release the semantics processor
      _semanticBufferProcessor?.Dispose();

      if (_suppressionTexture != null)
        Destroy(_suppressionTexture);

      base.DeinitializeImpl();
    }
    
    protected override void EnableFeaturesImpl()
    {
      base.EnableFeaturesImpl();
      _semanticBufferProcessor.AwarenessStreamBegan += OnSemanticBufferInitialized;
      _semanticBufferProcessor.AwarenessStreamUpdated += OnSemanticBufferUpdated;
    }

    protected override void DisableFeaturesImpl()
    {
      base.DisableFeaturesImpl();
      
      if (_suppressionTexture != null)
        Destroy(_suppressionTexture);

      _semanticBufferProcessor.AwarenessStreamBegan -= OnSemanticBufferInitialized;
      _semanticBufferProcessor.AwarenessStreamUpdated -= OnSemanticBufferUpdated;
    }

    public override void ApplyARConfigurationChange
    (
      ARSessionChangesCollector.ARSessionRunProperties properties
    )
    {
      if (properties.ARConfiguration is IARWorldTrackingConfiguration worldConfig)
      {
        worldConfig.IsSemanticSegmentationEnabled = AreFeaturesEnabled;
        worldConfig.SemanticTargetFrameRate = _keyFrameFrequency;
      }
    }

    /// Invoked when this component is asked about the render features
    /// it is may be responsible for.
    /// @note: The implementation needs to include all features that is
    /// possible to manipulate with this component.
    protected override HashSet<string> OnAcquireFeatureSet()
    {
      return new HashSet<string>
      {
        FeatureBindings.DepthSuppression
      };
    }

    /// Invoked when it is time to calculate the actual features
    /// that this component currently manages.
    protected override RenderFeatureConfiguration OnEvaluateConfiguration()
    {
      // If the semantics manager is not active, all features are disabled
      if (!AreFeaturesEnabled)
        return new RenderFeatureConfiguration(null, featuresDisabled: Features);

      var enabledFeatures = new List<string>();

      // Is depth suppression enabled?
      if (_depthSuppressionChannels.Length > 0)
        enabledFeatures.Add(FeatureBindings.DepthSuppression);

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
      if (_depthSuppressionChannels.Length == 0)
        return;

      material.SetTexture(PropertyBindings.DepthSuppressionMask, _suppressionTexture);
      material.SetMatrix(PropertyBindings.SemanticsTransform, _semanticBufferProcessor.SamplerTransform);
    }

    private void OnSemanticBufferInitialized(ContextAwarenessArgs<ISemanticBuffer> args)
    {
      // Currently just a pass-through
      SemanticBufferInitialized?.Invoke(args);
    }

    private void OnSemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
    {
      // Avoid generating a suppression texture if suppression isn't enabled
      if (_depthSuppressionChannels.Length != 0)
      {
        // Acquire the typed buffer
        var semanticBuffer = args.Sender.AwarenessBuffer;

        // Determine whether we should update the list
        // of channels that are used to suppress depth
        var shouldUpdateSuppressionIndices = _suppressionChannelIndices == null ||
          _suppressionChannelIndices.Length != _depthSuppressionChannels.Length;

        // Update channel list
        if (shouldUpdateSuppressionIndices)
          _suppressionChannelIndices = _depthSuppressionChannels
            .Select(channel => semanticBuffer.GetChannelIndex(channel))
            .ToArray();

        // Update semantics on the GPU
        if (args.IsKeyFrame)
          semanticBuffer.CreateOrUpdateTextureARGB32
          (
            ref _suppressionTexture,
            _suppressionChannelIndices
          );
      }

      // Finally, let users know the manager has finished updating.
      SemanticBufferUpdated?.Invoke(args);
    }
  }
}
