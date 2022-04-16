// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif
#if (UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_DESKTOP) && !UNITY_EDITOR
#define AR_NATIVE_SUPPORT
#endif

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.VirtualStudio;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.AR.Mock;
#if ARDK_HAS_URP
using Niantic.ARDK.Rendering.SRP;
#endif

using UnityEngine;
using UnityEngine.Rendering;

using Debug = UnityEngine.Debug;

namespace Niantic.ARDK.Extensions
{
  /// A helper class illustrating the most basic setup for rendering images coming from an
  /// ARCameraFeed. The ARSceneCamera prefab contains this component.
  ///
  /// @note
  ///   Compatible with all RuntimeEnvironments, where image data is provided in different ways.
  public class ARCameraRenderingHelper: MonoBehaviour
  {
    private class SavedSettings
    {
      /// The previous Game target frame rate before this script initialized -- used to restore
      /// the previous frame rate when the script is destroyed.
      private int _targetFrameRate;

      /// The previous Game sleep timeout before this script initialized -- used to restore
      /// the previous sleep timeout when the script is destroyed.
      private int _sleepTimeout;

      private Matrix4x4 _cameraProjection;
      private Camera _camera;
      private int _previousCullingMask;

      public SavedSettings(int targetFrameRate, int sleepTimeout, int cullingMask, Camera camera)
      {
        _targetFrameRate = targetFrameRate;
        _sleepTimeout = sleepTimeout;

        _camera = camera;
        _cameraProjection = camera.projectionMatrix;
        _previousCullingMask = cullingMask;
      }

      public void Apply()
      {
        Application.targetFrameRate = _targetFrameRate;
        Screen.sleepTimeout = _sleepTimeout;
        _camera.projectionMatrix = _cameraProjection;
        _camera.cullingMask = _previousCullingMask;
      }
    }

    /// The Unity Camera in the scene upon which the images will be rendered.
    public Camera Camera;

    public CommandBuffer BackgroundCommandBuffer
    {
      get
      {
        return _backgroundDrawCb;
      }
    }

    /// Prevents us from writing back the values _we_ set in case we accidentally set the
    /// previous rates twice,
    /// i.e. we set the application's previous frame rate and sleep timeout in
    /// `SetupRendering`, next time it gets called (users can call Run multiple times) we then
    /// set the frame rate and sleep _again_ except this time it's not truly the applications's,
    /// it's the ones we set last time.
    private SavedSettings _savedSettings;

    /// The command buffer we will encode to do our blit-ing, as well as a native plugin callback.
    private CommandBuffer _backgroundDrawCb;

    // Cached camera feed so that we can unsubscribe from it on deinitialize
    private ARCameraFeed _cameraFeed;

    private IARSession _arSession;

    /// Sets up state and callbacks necessary for this script.
    private void Awake()
    {
      if (Camera == null)
      {
        Debug.LogWarning("No camera was specified for the ARCameraRenderingHelper.");
        return;
      }

      // Add a warning if the cullingMask is not set to Everything.
      if (Camera.cullingMask != -1)
      {
        var warning = "The ARSceneCamera's culling mask is not set to everything. Some " +
                      "objects may not be rendered.";

        ARLog._Warn(warning);
      }

      ARSessionFactory.SessionInitialized += OnSessionInitialized;
    }

    private void OnDestroy()
    {
      TeardownRendering();
      ARSessionFactory.SessionInitialized -= OnSessionInitialized;
    }

    private void OnSessionInitialized(AnyARSessionInitializedArgs args)
    {
      _arSession = args.Session;

      _arSession.Ran += OnSessionRan;
      _arSession.Paused += OnSessionPaused;
      _arSession.Deinitialized += OnSessionDeinitialized;
    }

    private void OnSessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      TeardownRendering();
      _arSession = null;
    }

    private void OnSessionRan(ARSessionRanArgs args)
    {
      // If the session is being run and we haven't yet setup rendering we need to set it up.
      if (_backgroundDrawCb == null)
        SetupRendering(Vector2.one, Vector2.zero);
    }

    // Initializes the command buffer and sets textures according to the runtime environment
    private void SetupRendering(Vector2 scale, Vector2 offset)
    {
      if (_backgroundDrawCb != null)
      {
        // In this case, where the ARSession was re-run without first being paused,
        // rendering was already set up and nothing more needs to be done.
        return;
      }

      if (Camera.clearFlags != CameraClearFlags.Color)
      {
        Camera.clearFlags = CameraClearFlags.Color;

        var msg = "The `clearFlags` property of {0} was changed to CameraClearFlags.Color" +
          " to make it usable with the ARCameraRenderingHelper.";

        ARLog._DebugFormat(msg, objs: Camera.name);
      }

      _savedSettings =
        new SavedSettings
        (
          Application.targetFrameRate,
          Screen.sleepTimeout,
          Camera.cullingMask,
          Camera
        );

      // If it is a mock session, attempt to disable the layer all mock objects are on so "real"
      // objects aren't doubly rendered by mock device camera and the scene camera.
      if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Mock)
      {
        _MockFrameBufferProvider.RemoveLayerFromCamera(Camera, _MockFrameBufferProvider.MOCK_LAYER_NAME);
      }

      SetupCameraFeed();

      Application.targetFrameRate = (int)_cameraFeed.FrameRate;
      Screen.sleepTimeout = SleepTimeout.NeverSleep;


      var clearDepth =
        _arSession.Configuration is IARWorldTrackingConfiguration worldConfig &&
        worldConfig.IsDepthEnabled;

      // Construct and setup the steps in our command buffer
      _backgroundDrawCb =
        ARSessionBuffersHelper.ConstructBackgroundBuffer
        (
          _cameraFeed,
          scale,
          offset,
          clearDepth
        );

      // Add the command buffer to the camera so it gets run
      ARSessionBuffersHelper.AddBackgroundBuffer(Camera, _backgroundDrawCb);
    }

    private void SetupCameraFeed()
    {
      var cameraResolution =
        new ARCameraFeed.CameraFeedResolution()
        {
          Mode = ARCameraFeed.CameraFeedResolutionMode.Custom,
          Height = Camera.pixelHeight,
          Width = Camera.pixelWidth,
        };

      var textureType = ARCameraFeed.TextureType.Platform;

 #if UNITY_EDITOR
      if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Mock)
        textureType = ARCameraFeed.TextureType.BGRA;
 #endif

      _cameraFeed = _arSession.GetCameraFeed(cameraResolution, textureType);
      _cameraFeed.SetupCameraProjectionMatrixUpdates(Camera);
    }

    private void OnSessionPaused(ARSessionPausedArgs args)
    {
      TeardownRendering();
    }

    private void TeardownRendering()
    {
      if (_savedSettings != null)
      {
        _savedSettings.Apply();
        _savedSettings = null;
      }

      DeinitializeCommandBuffer();
    }

    // Deinitialize the command buffer and set textures to null
    private void DeinitializeCommandBuffer()
    {
      if (Camera != null && _backgroundDrawCb != null)
        ARSessionBuffersHelper.RemoveBackgroundBuffer(Camera, _backgroundDrawCb);

      _backgroundDrawCb = null;

      _cameraFeed = null;
    }
  }
}
