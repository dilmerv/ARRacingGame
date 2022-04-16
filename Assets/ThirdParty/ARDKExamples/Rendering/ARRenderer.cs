using System;

using Niantic.ARDK;
using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Rendering;

using UnityEngine;

namespace Niantic.ARDKExamples.Rendering
{
  /// Class in charge of overall rendering
  public sealed class ARRenderer: IDisposable
  {
    private readonly Camera _camera = null;
    private readonly IRenderFeatureProvider[] _renderFeatureProviders;
    private IARSession _session;
    private ARFrameRenderer _renderer;

    /// Creates the AR Renderer
    /// @param camera Camera used to render, or Camera.main if none is provided
    /// @param renderFeatureProviders The render feature provided used to change the rendering
    public ARRenderer
    (
      Camera camera = null,
      params IRenderFeatureProvider[] renderFeatureProviders
    )
    {
      if (!camera)
        camera = Camera.main;

      _camera = camera;
      _renderFeatureProviders = renderFeatureProviders;

      //This is called upon registration, even though the AR Session has already been initialized
      ARSessionFactory.SessionInitialized += ARSessionFactory_SessionInitialized;
    }

    /// Disposes the AR Renderer
    public void Dispose()
    {
      ARSessionFactory.SessionInitialized -= ARSessionFactory_SessionInitialized;

      // Release the renderer
      _renderer?.Dispose();
      _renderer = null;
    }

    /// Renders the current frame.  This should be called from LateUpdate.
    public void RenderFrame()
    {
      if (_renderer == null)
        return;

      var frame = _session?.CurrentFrame;
      if (frame == null)
        return;

      _renderer.UpdateState(frame);
    }

    private void ARSessionFactory_SessionInitialized(AnyARSessionInitializedArgs args)
    {
      _session = args.Session;
      _session.Ran += OnSessionRan;
      _session.Deinitialized += OnSessionDeinitialized;
    }

    private void OnSessionRan(ARSessionRanArgs args)
    {
      // Create the renderer
      _renderer = CreateRenderer(_session.RuntimeEnvironment);
      if (_renderer == null)
      {
        Debug.LogError("Failed to create a renderer for the running platform.");
        return;
      }

      // Initialize the renderer
      _renderer.Initialize();

      // Attach any render state providers
      foreach (var renderFeatureProvider in _renderFeatureProviders)
        _renderer.AddFeatureProvider(renderFeatureProvider);

      // Configure target frame rate
      QualitySettings.vSyncCount = 0;
      Application.targetFrameRate = _renderer.RecommendedFrameRate;

      // Enable renderer
      _renderer.Enable();
    }

    private ARFrameRenderer CreateRenderer(RuntimeEnvironment environment)
    {
      var renderTarget = new RenderTarget(_camera);

      var result = ARFrameRendererFactory.Create
      (
        renderTarget,
        environment
      );

      if (result != null)
        result.IsOrientationLocked = false;

      return result;
    }

    private void OnSessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      if (_session != null)
      {
        _session.Ran -= OnSessionRan;
        _session.Deinitialized -= OnSessionDeinitialized;
        _session = null;
      }

      _renderer?.Dispose();
      _renderer = null;
    }
  }
}
