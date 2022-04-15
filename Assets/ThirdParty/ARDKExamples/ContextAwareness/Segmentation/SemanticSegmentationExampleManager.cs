// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.Extensions;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  /// @brief An example script to demonstrate Context Awareness' semantic segmentation.
  /// @remark Use the Change Feature Channel button to swap between which active semantic will be
  /// painted white.
  /// @remark This example only works in portrait mode.
  public class SemanticSegmentationExampleManager:
    MonoBehaviour
  {
    [SerializeField]
    private ARSessionManager _arSessionManager = null;

    [SerializeField]
    private ARSemanticSegmentationManager _semanticSegmentationManager;

    [Header("Rendering")]
    // The UI image that the camera overlay is rendered in.
    [SerializeField]
    private RawImage _segmentationOverlayImage = null;

    [Header("UI")]
    [SerializeField]
    private GameObject _togglesParent = null;

    [SerializeField]
    private Text _toggleFeaturesButtonText = null;

    [SerializeField]
    private Text _toggleInterpolationText = null;

    [SerializeField]
    private Text _channelNameText = null;

    private Texture2D _semanticTexture;

    // The current active channel that is painted white. -1 means that no semantic is used.
    private int _featureChannel = -1;
    private bool _isTextureDirty;

    private void Start()
    {
      if (_togglesParent != null)
        _togglesParent.SetActive(false);

      // TODO: this should be set from renderer
      Application.targetFrameRate = 60;

      _arSessionManager.EnableFeatures();
      _semanticSegmentationManager.SemanticBufferInitialized += OnSemanticBufferInitialized;
      _semanticSegmentationManager.SemanticBufferUpdated += OnSemanticBufferUpdated;
    }

    private void Update()
    {
      // Should update the semantics representation?
      if (_isTextureDirty)
      {
        // Update
        _semanticSegmentationManager.SemanticBufferProcessor.CopyToAlignedTextureARGB32
        (
          texture: ref _semanticTexture,
          channel: _featureChannel,
          orientation: Screen.orientation
        );

        // Assign
        _segmentationOverlayImage.texture = _semanticTexture;
        _isTextureDirty = false;
      }

      if (_featureChannel == -1)
      {
        var detectedChannels = string.Empty;
        if (Input.touchCount > 0)
        {
          // Display the names of the channels the user is touching on the screen
          var touchPosition = Input.touches[0].position;
          var channelsForPixel =
            _semanticSegmentationManager.SemanticBufferProcessor.GetChannelNamesAt
            (
              (int)touchPosition.x,
              (int)touchPosition.y
            );

          detectedChannels = channelsForPixel.Aggregate
          (
            detectedChannels,
            (
              current,
              channelName
            ) => string.IsNullOrEmpty(current)
              ? (current + channelName)
              : (current + ", " + channelName)
          );
        }

        _channelNameText.text = detectedChannels.Length > 0 ? detectedChannels : "None";
      }
    }

    private void OnDestroy()
    {
      _semanticSegmentationManager.SemanticBufferUpdated -= OnSemanticBufferUpdated;

      // Release semantic overlay texture
      if (_semanticTexture != null)
        Destroy(_semanticTexture);
    }

    private void OnSemanticBufferInitialized(ContextAwarenessArgs<ISemanticBuffer> args)
    {
      _semanticSegmentationManager.SemanticBufferInitialized -= OnSemanticBufferInitialized;
      if (_togglesParent != null)
        _togglesParent.SetActive(true);
    }

    private void OnSemanticBufferUpdated(ContextAwarenessStreamUpdatedArgs<ISemanticBuffer> args)
    {
      _isTextureDirty = _isTextureDirty || _featureChannel != -1;
    }

    public void ChangeFeatureChannel()
    {
      var channelNames = _semanticSegmentationManager.SemanticBufferProcessor.Channels;

      // If the channels aren't yet known, we can't change off the initial default channel.
      if (channelNames == null)
        return;

      // Increment the channel count with wraparound.
      _featureChannel += 1;
      if (_featureChannel == channelNames.Length)
        _featureChannel = -1;

      // Update the displayed name of the channel, and enable or disable the overlay.
      if (_featureChannel == -1)
      {
        _channelNameText.text = "None";
        _segmentationOverlayImage.enabled = false;
      }
      else
      {
        _channelNameText.text = FormatChannelName(channelNames[_featureChannel]);
        if (_semanticSegmentationManager.AreFeaturesEnabled)
        {
          _segmentationOverlayImage.enabled = true;
        }

        _isTextureDirty = true;
      }
    }

    public void ToggleSessionSemanticFeatures()
    {
      var newEnabledState = !_semanticSegmentationManager.enabled;

      _toggleFeaturesButtonText.text = newEnabledState ? "Disable Features" : "Enable Features";

      _semanticSegmentationManager.enabled = newEnabledState;
      _segmentationOverlayImage.enabled = newEnabledState;

      if (!newEnabledState)
      {
        Destroy(_semanticTexture);
        _semanticTexture = null;
      }
    }

    public void ToggleInterpolation()
    {
      var provider = _semanticSegmentationManager.SemanticBufferProcessor;
      var current = provider.InterpolationMode;
      provider.InterpolationMode = current == InterpolationMode.None
        ? InterpolationMode.Smooth
        : InterpolationMode.None;

      _toggleInterpolationText.text =
        provider.InterpolationMode != InterpolationMode.None
          ? "Disable Interpolation"
          : "Enable Interpolation";
    }

    private string FormatChannelName(string text)
    {
      var parts = text.Split('_');
      List<string> displayParts = new List<string>();
      foreach (var part in parts)
      {
        displayParts.Add(char.ToUpper(part[0]) + part.Substring(1));
      }

      return String.Join(" ", displayParts.ToArray());
    }
  }
}
