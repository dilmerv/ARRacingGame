// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Awareness;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.Extensions;
using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
  /// @brief An example script to visualize Context Awareness' depth information.
  /// @remark This example only works in portrait mode.
  public class DepthExampleManager:
    MonoBehaviour
  {
    [SerializeField]
    private ARDepthManager _arDepthManager = null;

    [Header("UI")]
    [SerializeField]
    private GameObject _toggles = null;

    [SerializeField]
    private Text _toggleViewButtonText = null;
    
    [SerializeField]
    private Text _togglePointCloudButtonText = null;

    [SerializeField]
    private Text _toggleOcclusionButtonText = null;
    
    [SerializeField]
    private Text _toggleInterpolationButtonText = null;
    
    [SerializeField]
    private Text _toggleDepthButtonText = null;

    [Header("Game Objects")]
    [SerializeField]
    private GameObject _pointer;
    
    [SerializeField]
    private GameObject _cube;
    
    private bool _isShowingDepths;
    private bool _isShowingPointCloud;

    private void Start()
    {
      if (_toggles != null)
        _toggles.SetActive(false);

      Application.targetFrameRate = 60;
      _arDepthManager.DepthBufferInitialized += OnDepthBufferInitialized;
    }

    private void OnDepthBufferInitialized(ContextAwarenessArgs<IDepthBuffer> args)
    {
      _arDepthManager.DepthBufferInitialized -= OnDepthBufferInitialized;
      if (_toggles != null)
        _toggles.SetActive(true);
    }
    
    private void Update()
    {
      var touchPosition = PlatformAgnosticInput.touchCount > 0
        ? PlatformAgnosticInput.GetTouch(0).position
        : new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);

      var worldPosition = _arDepthManager.DepthBufferProcessor.GetWorldPosition
      (
        (int)touchPosition.x,
        (int)touchPosition.y
      );

      var normal = _arDepthManager.DepthBufferProcessor.GetSurfaceNormal
      (
        (int)touchPosition.x,
        (int)touchPosition.y
      );
      
      var rotation = Quaternion.Slerp
      (
        _pointer.transform.rotation,
        Quaternion.FromToRotation(Vector3.up, normal),
        Time.deltaTime * 10.0f
      );

      _pointer.transform.position = worldPosition;
      _pointer.transform.rotation = rotation;
    }

    public void PlaceCube()
    {
      _cube.transform.position = _arDepthManager.DepthBufferProcessor.GetWorldPosition
        (Screen.width / 2, Screen.height / 2);
      
      Debug.Log("Placed cube at: " + _cube.transform.position);
    }

    public void ToggleInterpolation()
    {
      if(_arDepthManager == null)
        return;

      var provider = _arDepthManager.DepthBufferProcessor;
      var current = provider.InterpolationMode;
      switch (current)
      {
        case InterpolationMode.None:
          provider.InterpolationMode = InterpolationMode.Balanced;
          break;

        case InterpolationMode.Balanced:
          provider.InterpolationMode = InterpolationMode.Smooth;
          break;

        case InterpolationMode.Smooth:
          provider.InterpolationMode = InterpolationMode.None;
          break;

        default:
          throw new ArgumentOutOfRangeException();
      }

      _toggleInterpolationButtonText.text = "Interpolation:\n" + provider.InterpolationMode;
    }
    
    public void ToggleOcclusion()
    {
      if(_arDepthManager == null)
        return;
      
      var current = _arDepthManager.OcclusionTechnique;
      switch (current)
      {
      case ARDepthManager.OcclusionMode.None:
        _arDepthManager.OcclusionTechnique = ARDepthManager.OcclusionMode.DepthBuffer;
        break;

      case ARDepthManager.OcclusionMode.DepthBuffer:
        _arDepthManager.OcclusionTechnique = ARDepthManager.OcclusionMode.ScreenSpaceMesh;
        break;

      case ARDepthManager.OcclusionMode.ScreenSpaceMesh:
        _arDepthManager.OcclusionTechnique = ARDepthManager.OcclusionMode.None;
        break;

      case ARDepthManager.OcclusionMode.Auto:
        _arDepthManager.OcclusionTechnique = ARDepthManager.OcclusionMode.None;
        break;

      default:
        throw new ArgumentOutOfRangeException();
      }

      _toggleOcclusionButtonText.text = "Occlusion:\n" + _arDepthManager.OcclusionTechnique;
    }

    public void ToggleShowDepth()
    {
      _isShowingDepths = !_isShowingDepths;

      // Toggle UI elements
      _toggleViewButtonText.text = _isShowingDepths ? "Show Camera" : "Show Depth";
      _arDepthManager.ToggleDebugVisualization(_isShowingDepths);
    }

    public void ToggleShowPointCloud()
    {
      _isShowingPointCloud = !_isShowingPointCloud;
      
      // Toggle UI elements
      _togglePointCloudButtonText.text =
        _isShowingPointCloud ? "Hide Point Cloud" : "Show Current Point Cloud" ;
    }

    public void ToggleSessionDepthFeatures()
    {
      var depthEnabled = !_arDepthManager.enabled;

      //Toggle pointer visibility
      _pointer.SetActive(depthEnabled);
      
      // ARSession configuration through ARDepthManager
      _arDepthManager.enabled = depthEnabled;

      // Toggle UI elements
      _toggleDepthButtonText.text = depthEnabled ? "Disable Depth" : "Enable Depth";
    }
  }
}
