// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Extensions;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  /// An example script to set up Context Awareness' depth-based occlusion.
  /// @remark Use the slider to move the cube further away, and see it become occluded by objects.
  /// @remark This example only works in portrait mode.
  public class DepthMeshOcclusionExampleManager:
    MonoBehaviour
  {
    [SerializeField]
    private Slider _depthSlider = null;

    [SerializeField]
    private Text _depthText = null;

    [SerializeField]
    private GameObject _cube = null;

    [SerializeField]
    private float _maxDepth = 25.0f;

    [SerializeField]
    private Camera _sceneCamera = null;

    [SerializeField]
    private Text _pinButtonText = null;

    [SerializeField]
    private ARDepthManager _arDepthManager = null;

    [SerializeField]
    private Toggle _toggleUI = null;

    private const float DEGREES_PER_SECOND = 30.0f;
    private const float DEFAULT_SLIDER_VALUE = 0.15f;
    private bool _pinnedToWorldSpace;

    /// Toggle whether the object is pinned in world space or following the camera at some depth
    public void TogglePinToWorldSpace()
    {
      _pinnedToWorldSpace = !_pinnedToWorldSpace;

      if (_pinnedToWorldSpace)
      {
        _pinButtonText.text = "Move with Camera";
        _depthText.text = "Pinned To World";
      }
      else
      {
        _pinButtonText.text = "Pin To World";
        _depthSlider.value = DEFAULT_SLIDER_VALUE;
      }
    }

    public void ToggleOcclusion()
    {
      if (!_toggleUI.isOn)
        _arDepthManager.DisableFeatures();
      else
        _arDepthManager.EnableFeatures();
    }

    private void Awake()
    {
      _depthSlider.onValueChanged.AddListener(AdjustDepth);
    }

    /// Rotate the cube every frame
    private void Update()
    {
      AdjustDepth(_depthSlider.value);
      _cube.transform.Rotate(Vector3.up * Time.deltaTime * DEGREES_PER_SECOND);
    }

    /// Project the cube some depth forward from the camera
    private void AdjustDepth(float sliderPos)
    {
      if (_pinnedToWorldSpace)
        return;

      var convertedDepth = sliderPos * sliderPos * _maxDepth;
      var pos = _sceneCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, convertedDepth));

      _depthText.text = "Depth: " + convertedDepth + " meters";
      _cube.transform.position = pos;
    }

    private bool _isShowingDepth;
    public void ToggleShowDepth()
    {
      _isShowingDepth = !_isShowingDepth;
      _arDepthManager.ToggleDebugVisualization(_isShowingDepth);
    }
  }
}
