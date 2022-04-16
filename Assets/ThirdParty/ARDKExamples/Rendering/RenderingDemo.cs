using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Configuration;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Rendering
{
  /// The goal of this example is to demonstrate rendering with C# components, rather than our
  /// ARRenderingManager and ARDepthManager.
  /// @note
  /// For most developers, those components will provide
  /// enough functionality out of the box.  This sample is for developers that want very fine
  /// control over the rendering pipeline, or to integrate their own rendering on top of the
  /// ARDK pipeline.
  public class RenderingDemo: MonoBehaviour
  {
    private IARSession _arSession;
    private ARRenderer _arRenderer;
    private ARDepthRenderer _arDepthRenderer;
    private bool _renderDepth = false;

    private void Start()
    {
      StartARSession();
    }

    private void LateUpdate()
    {
      //This is called to render the actual frame.  It needs to happen in late update to prevent
      //rendering issues.
      _arRenderer?.RenderFrame();
    }

    private void OnDestroy()
    {
      StopRendering();
      _arSession.Dispose();
    }

    /// Start rendering the scene
    public void StartRendering()
    {
      if (_arDepthRenderer == null)
        _arDepthRenderer = new ARDepthRenderer();

      if (_arRenderer == null)
        _arRenderer = new ARRenderer(Camera.main, _arDepthRenderer);

      //Must update render features after creating ARRenderer
      _arDepthRenderer.SetOcclusionEnabled(_renderDepth);
    }

    /// Stop rendering the scene
    public void StopRendering()
    {
      _arDepthRenderer?.Dispose();
      _arDepthRenderer = null;

      _arRenderer?.Dispose();
      _arRenderer = null;
    }

    /// Toggles whether or not depth will be rendered
    /// @param buttonText Text of depth rendering button to update
    public void ToggleDepthRendering(Text buttonText)
    {
      _renderDepth = !_renderDepth;
      buttonText.text = _renderDepth ? "Hide Depth" : "Show Depth";
      if (_arRenderer != null)
        StartRendering();
    }

    private void StartARSession()
    {
      _arSession = ARSessionFactory.Create();
      var configuration = CreateARWorldTrackingConfiguration();
      _arSession.Run(configuration);
    }

    private IARWorldTrackingConfiguration CreateARWorldTrackingConfiguration()
    {
      var configuration = ARWorldTrackingConfigurationFactory.Create();
      configuration.IsDepthEnabled = true;
      configuration.DepthTargetFrameRate = 30;
      configuration.WorldAlignment = WorldAlignment.Gravity;
      return configuration;
    }
  }
}
