// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  public class MeshingSemanticsExampleManager: MonoBehaviour
  {
    [Header("UI")]
    [SerializeField]
    private RawImage _featuresImage = null;

    [SerializeField]
    private GameObject _toggles = null;

    [SerializeField]
    private Text _channelNameText = null;

    [Header("Context Awareness Managers")]
    [SerializeField]
    private ARDepthManager _depthManager = null;

    [SerializeField]
    private ARSemanticSegmentationManager _semanticSegmentationManager = null;

    [SerializeField]
    private Text _toggleOcclusionButtonText;

    [SerializeField]
    private Text _toggleDebugButtonText;
    
    private bool _isDisplayingContextAwareness = false;
    private bool _isSemanticsTextureDirty;
    private Texture2D _semanticsTexture;

    // Each feature channel number corresponds to a label, first is depth and the rest is from
    // semantics channel names.
    private uint _featureChannel = 0;

    private void Awake()
    {
      // Hide toggles now, because they're useless without ContextAwareness first initialized
      _toggles.SetActive(false);

      ConfigureFeaturesView(_isDisplayingContextAwareness);
    }

    private void OnEnable()
    {
      _semanticSegmentationManager.SemanticBufferInitialized += OnSemanticBufferInitialized;
      _semanticSegmentationManager.SemanticBufferUpdated += OnSemanticBufferUpdated;
    }

    private void OnDisable()
    {
      _semanticSegmentationManager.SemanticBufferInitialized -= OnSemanticBufferInitialized;
      _semanticSegmentationManager.SemanticBufferUpdated -= OnSemanticBufferUpdated;
    }

    private void Update()
    {
      // Toggle visualizations
      _depthManager.ToggleDebugVisualization(_isDisplayingContextAwareness && _featureChannel == 0);
      _featuresImage.enabled = _isDisplayingContextAwareness && _featureChannel > 0;

      if (!_isSemanticsTextureDirty || _featureChannel < 1)
        return;
      
      // Update the semantics texture (display aligned)
      _semanticSegmentationManager.SemanticBufferProcessor.CopyToAlignedTextureARGB32
      (
        (int)_featureChannel - 1,
        ref _semanticsTexture,
        Screen.orientation
      );

      _featuresImage.texture = _semanticsTexture;
      _isSemanticsTextureDirty = false;
    }

    private void OnDestroy()
    {
      // Release textures
      if (_semanticsTexture != null)
        Destroy(_semanticsTexture);
    }

    private void OnSemanticBufferInitialized(ContextAwarenessArgs<ISemanticBuffer> args)
    {
      if (_toggles != null)
        _toggles.SetActive(true);
    }

    private void OnSemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
    {
      _isSemanticsTextureDirty = _isSemanticsTextureDirty || _featureChannel > 0;
    }

    public void ToggleShowFeatures()
    {
      ConfigureFeaturesView(isEnabled: !_isDisplayingContextAwareness);
    }

    public void ToggleLiveOcclusion()
    {
      var prev = _depthManager.OcclusionTechnique;
      _depthManager.OcclusionTechnique = prev == ARDepthManager.OcclusionMode.None
        ? ARDepthManager.OcclusionMode.Auto
        : ARDepthManager.OcclusionMode.None;

      if (_depthManager.OcclusionTechnique == ARDepthManager.OcclusionMode.None)
        _toggleOcclusionButtonText.text = "Enable Depth";
      else
        _toggleOcclusionButtonText.text = "Disable Depth";
    }

    public void CycleFeatureChannel()
    {
      if (!_isDisplayingContextAwareness)
        return;

      // Get available channels
      var channelNames = _semanticSegmentationManager.SemanticBufferProcessor.Channels;
      if (channelNames == null || channelNames.Length == 0)
        return;

      // Increase channel index
      _featureChannel = (_featureChannel + 1) % ((uint)channelNames.Length + 1);

      if (_featureChannel == 0)
      {
        // In our context, zero means the depth buffer
        _channelNameText.text = "Depth";
      }
      else
      {
        // Values greater than zero will refer to semantic classes
        var text = channelNames[_featureChannel - 1];
        _channelNameText.text =
          text != null ? FormatDisplayText(channelNames[_featureChannel - 1]) : "???";
      }
    }

    // Toggle between the camera feed and the depth/semantics image.
    private void ConfigureFeaturesView(bool isEnabled)
    {
      _isDisplayingContextAwareness = isEnabled;
      _channelNameText.enabled = _isDisplayingContextAwareness;
      if (_isDisplayingContextAwareness)
        _toggleDebugButtonText.text = "Disable Semantic View";
      else
        _toggleDebugButtonText.text = "Enable Semantic View";
    }

    private static string FormatDisplayText(string text)
    {
      var parts = text.Split('_');
      var displayParts = new List<string>();
      foreach (var part in parts)
      {
        displayParts.Add(char.ToUpper(part[0]) + part.Substring(1));
      }
      return string.Join(" ", displayParts.ToArray());
    }
  }
}
