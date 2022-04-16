using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Niantic.ARDK.Helpers
{
  /// Event args for when the AR video feed gets updated.
  public sealed class VideoFeedUpdatedArgs: IArdkEventArgs
  {
    /// The renderer.
    public readonly IARFrameRenderer Renderer;

    /// The AR background image.
    public readonly RenderTexture Texture;
    
    public VideoFeedUpdatedArgs(IARFrameRenderer renderer, RenderTexture texture)
    {
      Renderer = renderer;
      Texture = texture;
    }
  }
  
  /// AR background video feed. It is designed to set up
  /// continuous rendering to an offscreen texture.
  public sealed class ARVideoFeed: IDisposable
  {
    private ARFrameRenderer _renderer;
    private readonly IARSession _session;

    /// The renderer component used by this feed
    public IARFrameRenderer Renderer
    {
      get => _renderer;
    }
    
    /// The rendered AR background texture.
    public RenderTexture GPUTexture { get; }
    
    /// Prevents us from writing back the values _we_ set in case we accidentally set the
    /// previous rates twice,
    /// i.e. we set the application's previous frame rate and sleep timeout in
    /// `SetupRendering`, next time it gets called (users can call Run multiple times) we then
    /// set the frame rate and sleep _again_ except this time it's not truly the applications's,
    /// it's the ones we set last time.
    private SavedRenderingSettings _savedRenderingSettings;

    /// Event for when the target texture had been updated.
    public event ArdkEventHandler<VideoFeedUpdatedArgs> FeedUpdated;

    public ARVideoFeed
    (
      IARSession session,
      Resolution resolution,
      float near = 0.1f,
      float far = 100.0f,
      bool canRotate = true,
      bool autoDispose = true
    )
    {
      // Allocate the GPU texture
      GPUTexture = new RenderTexture
      (
        resolution.width,
        resolution.height,
        24,
        RenderTextureFormat.ARGB32
      );

      // Set up the renderer
      _renderer = CreateRenderer
      (
        GPUTexture,
        session.RuntimeEnvironment,
        near,
        far,
        !canRotate
      );

      if (_renderer == null)
      {
        ARLog._Error("Failed to create a renderer for the running platform.");
        return;
      }

      _renderer.Initialize();

      // Rendering is tied to whether the session is running
      _session = session;
      _session.Ran += Session_Ran;
      _session.Paused += Session_Paused;

      if (autoDispose)
        _session.Deinitialized += args => Dispose();
    }

    private void UpdateLoop_OnLateTick()
    {
      _renderer.UpdateState(_session.CurrentFrame);
    }

    private void Session_Ran(ARSessionRanArgs args)
    {
      // Push application settings
      _savedRenderingSettings = new SavedRenderingSettings
      (
        Application.targetFrameRate,
        Screen.sleepTimeout,
        QualitySettings.vSyncCount
      );
      
      // Configure target frame rate
      QualitySettings.vSyncCount = 0;
      Application.targetFrameRate = _renderer.RecommendedFrameRate;
      
      _renderer.Enable();
      _renderer.FrameRendered += OnFrameRendered;
      _UpdateLoop.LateTick += UpdateLoop_OnLateTick;
    }

    private void Session_Paused(ARSessionPausedArgs args)
    {
      // Revert application settings
      _savedRenderingSettings?.Apply();
      _savedRenderingSettings = null;
      
      _renderer.Disable();
      _renderer.FrameRendered -= OnFrameRendered;
      _UpdateLoop.LateTick -= UpdateLoop_OnLateTick;
    }
    
    private void OnFrameRendered(FrameRenderedArgs args)
    {
      var arguments = new VideoFeedUpdatedArgs(args.Renderer, GPUTexture);
      FeedUpdated?.Invoke(arguments);
    }

    private static ARFrameRenderer CreateRenderer
    (
      RenderTarget target,
      RuntimeEnvironment env,
      float near,
      float far,
      bool lockOrientation
    )
    {
      ARFrameRenderer result = ARFrameRendererFactory.Create(target, env, near, far);
      result.IsOrientationLocked = lockOrientation;
      return result;
    }

    public void Dispose()
    {
      // Remove renderer
      if (_renderer.IsEnabled)
        _UpdateLoop.LateTick -= UpdateLoop_OnLateTick;

      _renderer.Dispose();
      _renderer = null;

      // Remove session
      if (_session != null)
      {
        _session.Ran -= Session_Ran;
        _session.Paused -= Session_Paused;
      }

      // Release target texture
      Object.Destroy(GPUTexture);
    }
  }
}
