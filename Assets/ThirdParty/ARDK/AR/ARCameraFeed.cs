// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
// This should be defined if, after calling the native render method, there's no guarantee the frame
// will be ready immediately.
#define DOUBLE_BUFFER_VIDEO_FEED
#endif

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.Utilities.Extensions;
using Niantic.ARDK.Rendering;
using Niantic.ARDK.Utilities;

#if ARDK_HAS_URP
using Niantic.ARDK.Rendering.SRP;
#endif

using JetBrains.Annotations;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace Niantic.ARDK.AR
{
  /// Receives camera feed data coming from the device and outputs Unity compatible textures.
  /// that can be rendered to the screen. On all platforms, an RGB format Texture will be
  /// available through the `VideoFull` property. Additional Texture2Ds will be available based on
  /// the current platform.
  /// @note
  ///   The textures will be be updated once per render loop.
  public sealed class ARCameraFeed:
    IDisposable
  {
    /// Possible types of the ARCameraFeed's output textures.
    public enum TextureType
    {
      /// Texture format is determined by platform (YCbCr for iOS, BGRA for Android or in-editor).
      Platform,

      /// YCbCr format. This value is only valid on iOS devices.
      YCbCr,

      /// BGRA format. This value is valid on Android devices and in-editor.
      BGRA
    }

    /// Possible ways to determine camera feed resolution.
    public enum CameraFeedResolutionMode
    {
      /// Behaviour is same as `Screen`, below.
      Default = 0,

      /// Use a custom resolution size, but automatically handle screen rotation.
      Custom,

      /// Match resolution of the CPU image surfaced by ARKit or ARCore.
      FromHardware,

      /// Match screen resolution.
      Screen,

      /// Use a custom resolution size and do not automatically handle screen rotation.
      Fixed
    }

    /// Container for properties that determine a camera feed's resolution.
    public struct CameraFeedResolution
    {
      public int Width;
      public int Height;

      public CameraFeedResolutionMode Mode;
    }

#if UNITY_STANDALONE_DESKTOP
    private const bool isStandAloneDesktop = true;
#else
    private const bool isStandAloneDesktop = false;
#endif


#if UNITY_EDITOR
    private const bool isUnityEditor = true;
#else
    private const bool isUnityEditor = false;
#endif

    // Optimal values obtained from PGO team
    private const float NEAR_CLIP_PLANE = 0.25f;
    private const float FAR_CLIP_PLANE = 100.0f;

    private static readonly Shader _bgraShader;
    private static readonly Shader _yuvShader;
    private static readonly Shader _bgraNonOESShader;

    // Cached shader property ID for accessing the "_texture" property of the BGRAMaterial.
    private static readonly int Texture = Shader.PropertyToID("_texture");

    // Cached shader property ID for accessing the "_textureY" property of the YCbCrMaterial.
    private static readonly int TextureY = Shader.PropertyToID("_textureY");

    // Cached shader property ID for accessing the "_textureCbCr" property of the YCbCrMaterial.
    private static readonly int TextureCbCr = Shader.PropertyToID("_textureCbCr");

    // Cached shader property ID for accessing the "_textureTransform" property of both the
    // BGRAMaterial and the YCbCrMaterial.
    private static readonly int TextureTransform = Shader.PropertyToID("_textureTransform");

    static ARCameraFeed()
    {
      _bgraShader = Resources.Load<Shader>("BGRAShader");
      _yuvShader = Resources.Load<Shader>("YCbCrShader");
      _bgraNonOESShader = Resources.Load<Shader>("BGRANonOESShader");

      Assert.IsNotNull(_yuvShader, "_yuvShader != null");
      Assert.IsNotNull(_bgraShader, "_bgraShader != null");
      Assert.IsNotNull(_bgraNonOESShader, "_bgraNonOESShader != null");
    }

    /// The type of texture being output.
    /// @note
    ///   This value will always be either `TextureMode.YCbCr` or `TextureMode.BGRA`.
    public TextureType TextureMode { get; private set; }

    /// When in YCbCr texture mode, this value is the Y component of the texture.
    public Texture2D VideoTextureY { get; private set; }

    /// When in YCbCr texture mode, this value is the CbCr component of the texture.
    public Texture2D VideoTextureCbCr { get; private set; }

    /// When in BGRA texture mode, this value is the full BGRA texture.
    public Texture2D VideoTextureBGRA { get; private set; }

    /// The current frame's camera feed texture in RGB format.
    public Texture VideoFull
    {
      get
      {
#if DOUBLE_BUFFER_VIDEO_FEED
        return _videoFullAlias;
#else
        return _videoFull;
#endif
      }
    }

    /// Affine transform for converting between normalized image coordinates and a
    /// coordinate space appropriate for rendering the camera image onscreen.
    public Matrix4x4 DisplayTransform { get; private set; }

    /// The projection matrix of the device's camera. This takes into account your device's
    /// focal length, size of the sensor, distortions inherent in the lenses, autofocus,
    /// temperature, and/or etc.
    public Matrix4x4 ProjectionTransform { get; private set; }

    /// Fence that should be waited on in other command buffers that utilize the
    /// texture output by ARCameraFeed.
#if UNITY_2019_1_OR_NEWER
    public GraphicsFence VideoFeedFence { get; private set; }
#else
    public GPUFence VideoFeedFence { get; private set; }
#endif

    /// Recommended target framerate when using the ARCameraFeed.
    public float FrameRate
    {
      get
      {
        if (Application.platform == RuntimePlatform.Android)
          return 30;

        return 60;
      }
    }

    /// Alerts subscribers when the feed has been updated with new camera images.
    public Action<ARCameraFeed> FeedUpdated = feed => {};

    private readonly IARSession _arSession;
    private readonly _VirtualCamera _virtualCamera;
    private readonly Material _bgraMaterial;
    private readonly Material _yuvMaterial;

    private bool _hasGottenFirstFrame;
    private bool _hasSetupRenderTexture;

    private CameraFeedResolution _cameraFeedResolution;
    private ScreenOrientation _originalOrientation;

    private UnityEngine.Camera _camera;
    private float _nearClipPlane = NEAR_CLIP_PLANE;
    private float _farClipPlane = FAR_CLIP_PLANE;

#if DOUBLE_BUFFER_VIDEO_FEED
    // For ARCore, ArSession_update updates both camera/anchor positions and the native camera
    // texture, and is called during the native render function. This means that for Unity frame N+1
    // virtual objects are updated and rendered using positions gotten from frame N, but the camera
    // image used is from frame N+1.
    // Due to a lack of control over how OpenGL schedules work on the GPU, it's impractical to
    // try to only call ArSesssion_update after the native camera texture from frame N is copied to
    // a Unity texture. So the best way to ensure that the camera image from frame N is accessible
    // during frame N+1 is to double buffer the Unity camera textures (_videoFullDoubleBuffer) so
    // that during frame N+1 one texture will still have frame N's image and can be used for
    // rendering while the other is filled with frame N+1's image from the native texture.
    // To ensure that the publicly visible interface doesn't change, _videoFullAlias is a texture
    // whose underlying texture handle is changed every frame to be the one that should be used for
    // rendering.

    // A pair of textures that will be alternately filled each frame with the camera feed.
    private readonly RenderTexture[] _videoFullDoubleBuffer;
    // Command buffers that will fill the corresponding texture from _videoFullDoubleBuffer.
    private readonly CommandBuffer[] _commandBuffers;
    // A texture that will have its native handle updated to point to whichever entry in
    // _videoFullDoubleBuffer should be used for rendering during this frame.
    private readonly Texture2D _videoFullAlias;
    // A texture created to ensure that _videoFullAlias can be created before the
    // _videoFullDoubleBuffer textures are valid, but still point to a valid texture handle.
    private Texture2D _videoFullAliasPlaceholder;
    // Fill _videoFullDoubleBuffer from ARCore with this index, and have _videoFullAlias reference
    // the other one.
    private int _currentDoubleBufferIndex = 0;
#else
    private readonly CommandBuffer _commandBuffer;
    private readonly RenderTexture _videoFull;
#endif

    /// Creates a new camera feed.
    /// @param arSession The ARSession to create the feed from.
    /// @param textureType The type of texture to output.
    /// @param cameraFeedResolution How to determine camera feed resolution.
    /// @param autoDisposeOnDeinitialize
    ///   If true, the camera feed will clean itself up when the session is destroyed.
    public ARCameraFeed
    (
      IARSession arSession,
      TextureType textureType,
      CameraFeedResolution cameraFeedResolution = new CameraFeedResolution(),
      bool autoDisposeOnDeinitialize = false
    ) : this(arSession, textureType, cameraFeedResolution, null, autoDisposeOnDeinitialize)
    {
    }

    internal ARCameraFeed
    (
      IARSession arSession,
      TextureType textureType,
      CameraFeedResolution cameraFeedResolution = new CameraFeedResolution(),
      [CanBeNull] Func<CommandBuffer, int, _VirtualCamera> virtualCameraFactory = null,
      bool autoDisposeOnDeinitialize = false
    )
    {
      _cameraFeedResolution = cameraFeedResolution;

      bool customOrFixed =
        _cameraFeedResolution.Mode == CameraFeedResolutionMode.Custom ||
        _cameraFeedResolution.Mode == CameraFeedResolutionMode.Fixed;

      if (customOrFixed)
        _originalOrientation = Screen.orientation;

      if (virtualCameraFactory == null)
        virtualCameraFactory = _VirtualCameraFactory.CreateContinousVirtualCamera;

      _arSession = arSession;

      if (textureType == TextureType.Platform)
      {
#if UNITY_ANDROID && !UNITY_EDITOR
        textureType = TextureType.BGRA;
#else
        textureType = TextureType.YCbCr;
#endif
      }

      TextureMode = textureType;

#if DOUBLE_BUFFER_VIDEO_FEED
      _commandBuffers = new CommandBuffer[]
      {
        new CommandBuffer(), new CommandBuffer(),
      };
      for (int i = 0; i < 2; i++)
      {
         _commandBuffers[i].IssuePluginEventAndData(_arSession);
      }
#else
      _commandBuffer = new CommandBuffer();
      _commandBuffer.IssuePluginEventAndData(_arSession);
#endif

      _arSession.FrameUpdated += OnFrameUpdated;

      if (autoDisposeOnDeinitialize)
        _arSession.Deinitialized += ArSessionDeinitialized;

      Material renderMaterial = null;

      if (textureType == TextureType.YCbCr)
      {
        _yuvMaterial = new Material(_yuvShader);
        renderMaterial = _yuvMaterial;
      }
      else if (textureType == TextureType.BGRA)
      {
#if NREAL_HEADSET
        _bgraMaterial = new Material(_bgraNonOESShader);
#else
        _bgraMaterial = new Material(_bgraShader);
#endif
        renderMaterial = _bgraMaterial;
      }

#if DOUBLE_BUFFER_VIDEO_FEED
      _videoFullDoubleBuffer = new RenderTexture[]
      {
        new RenderTexture(1, 1, 0),
        new RenderTexture(1, 1, 0),
      };

      _videoFullAliasPlaceholder = new Texture2D
      (
        1,
        1,
        TextureFormat.RGBA32,
        false
      );

      _videoFullAlias = Texture2D.CreateExternalTexture
      (
        1,
        1,
        TextureFormat.RGBA32,
        false,
        true,
        _videoFullAliasPlaceholder.GetNativeTexturePtr()
      );

      for (int i = 0; i < 2; i++)
      {
        _commandBuffers[i].Blit(null, _videoFullDoubleBuffer[i], renderMaterial);
#if UNITY_2019_1_OR_NEWER
        VideoFeedFence = _commandBuffers[i].CreateAsyncGraphicsFence();
#else
        VideoFeedFence = _commandBuffers[i].CreateGPUFence();
#endif
      }

      _virtualCamera = virtualCameraFactory(_commandBuffers[0], Int32.MinValue);

      _UpdateLoop.Tick += SwapVideoBuffer;
#else
      _videoFull = new RenderTexture(1, 1, 0);
      _commandBuffer.Blit(null, VideoFull, renderMaterial);

#if UNITY_2019_1_OR_NEWER
      VideoFeedFence = _commandBuffer.CreateAsyncGraphicsFence();
#else
      VideoFeedFence = _commandBuffer.CreateGPUFence();
#endif

      _virtualCamera = virtualCameraFactory(_commandBuffer, Int32.MinValue);
#endif

      _virtualCamera.OnPostRender +=
        (camera) =>
        {
          bool isUpdated =
#if DOUBLE_BUFFER_VIDEO_FEED
            _videoFullDoubleBuffer[_currentDoubleBufferIndex].IsCreated() &&
            _videoFullDoubleBuffer[_currentDoubleBufferIndex].width > 0 &&
#else
            _videoFull.IsCreated() &&
            _videoFull.width > 0 &&
#endif
            !ProjectionTransform.isIdentity &&
            _hasGottenFirstFrame;

          if (isUpdated)
          {
            if (_camera != null)
              _camera.projectionMatrix = ProjectionTransform;

            FeedUpdated(this);
          }
        };
    }

    /// On every render update, the given camera's projectionMatrix property will be set to
    /// the ProjectionTransform value. This allows the camera to render virtual objects in
    /// such a way as to mimic how the device’s real camera would render those objects.
    /// @param camera Camera used to render virtual objects
    public void SetupCameraProjectionMatrixUpdates(UnityEngine.Camera camera)
    {
      _camera = camera;
      _nearClipPlane = _camera.nearClipPlane;
      _farClipPlane = _camera.farClipPlane;
    }

#if DOUBLE_BUFFER_VIDEO_FEED
    private void SwapVideoBuffer()
    {
      var camera = _virtualCamera.GetCamera();

      var currBuffer = _commandBuffers[_currentDoubleBufferIndex];
      ARSessionBuffersHelper.RemoveAfterRenderingBuffer(camera, currBuffer);

      _currentDoubleBufferIndex = 1 - _currentDoubleBufferIndex;
      var nativeTex = _videoFullDoubleBuffer[1 - _currentDoubleBufferIndex].GetNativeTexturePtr();
      _videoFullAlias.UpdateExternalTexture(nativeTex);

      var nextBuffer = _commandBuffers[_currentDoubleBufferIndex];
      ARSessionBuffersHelper.AddAfterRenderingBuffer(camera, nextBuffer);
    }
#endif

    private void ArSessionDeinitialized(ARSessionDeinitializedArgs args)
    {
      _arSession.Deinitialized -= ArSessionDeinitialized;
      Dispose();
    }

    private void OnFrameUpdated(FrameUpdatedArgs args)
    {
      GrabCameraFeed();
    }

    ~ARCameraFeed()
    {
      ReleaseUnmanagedResources();
    }

    private void CorrectToActualResolution()
    {
      switch (_cameraFeedResolution.Mode)
      {
        case CameraFeedResolutionMode.Custom:
        case CameraFeedResolutionMode.Fixed:
          // don't change anything
          break;

        case CameraFeedResolutionMode.FromHardware:
        {
          var frame = _arSession.CurrentFrame;

          if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Playback || isStandAloneDesktop || isUnityEditor)
          {
            _cameraFeedResolution.Width = frame.Camera.CPUImageResolution.width;
            _cameraFeedResolution.Height = frame.Camera.CPUImageResolution.height;
          }
          else
          {
            _cameraFeedResolution.Width = frame.Camera.ImageResolution.width;
            _cameraFeedResolution.Height = frame.Camera.ImageResolution.height;
          }
          
          break;
        }

        case CameraFeedResolutionMode.Default:
        case CameraFeedResolutionMode.Screen:
          _cameraFeedResolution.Width = Screen.width;
          _cameraFeedResolution.Height = Screen.height;
          break;

        default:
          var message = "_cameraFeedResolution.Mode is invalid: " + _cameraFeedResolution.Mode;
          throw new InvalidEnumArgumentException(message);
      }
    }

    private void GrabCameraFeed()
    {
      // TODO(bpeake): Handle this as a part of the command buffer, rather than independently.

      var frame = _arSession.CurrentFrame;
      if (frame == null || frame.Camera == null || frame.CapturedImageTextures == null)
        return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
      if (_arSession.RuntimeEnvironment == RuntimeEnvironment.LiveDevice && frame.CapturedImageTextures.Length == 0)
        return;
#endif

      if (!_hasSetupRenderTexture)
      {
        _hasSetupRenderTexture = true;

#if DOUBLE_BUFFER_VIDEO_FEED
        for (int i = 0; i < 2; i++)
        {
          _videoFullDoubleBuffer[i].Release();

          CorrectToActualResolution();
          _videoFullDoubleBuffer[i].width = _cameraFeedResolution.Width;
          _videoFullDoubleBuffer[i].height = _cameraFeedResolution.Height;
          _videoFullDoubleBuffer[i].Create();
        }

        _videoFullAlias.Resize(_videoFullDoubleBuffer[0].width, _videoFullDoubleBuffer[0].height);
        _videoFullAlias.UpdateExternalTexture(_videoFullDoubleBuffer[0].GetNativeTexturePtr());
        // The placeholder is no longer necessary, so clean it up.
        UnityEngine.Object.Destroy(_videoFullAliasPlaceholder);
        _videoFullAliasPlaceholder = null;
#else
        _videoFull.Release();

        CorrectToActualResolution();

        _videoFull.width = _cameraFeedResolution.Width;
        _videoFull.height = _cameraFeedResolution.Height;
        _videoFull.Create();
#endif

        SetupVideoTextures(frame);
      }

      CorrectCameraMatrices(frame);

    if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Playback || isStandAloneDesktop || isUnityEditor)
    {
      if (frame.CapturedImageBuffer == null)
        return;

      switch (TextureMode)
      {
        case TextureType.YCbCr:
          VideoTextureY.LoadRawTextureData(frame.CapturedImageBuffer.Planes[0].Data);
          VideoTextureCbCr.LoadRawTextureData(frame.CapturedImageBuffer.Planes[1].Data);
          VideoTextureY.Apply();
          VideoTextureCbCr.Apply();
          break;

        case TextureType.BGRA:
          VideoTextureBGRA.LoadRawTextureData(frame.CapturedImageBuffer.Planes[0].Data);
          VideoTextureBGRA.Apply();
          break;

        default:
          break;
      }
    }
    else
    {
      // Update the native pointer and setup material properties.
      switch (TextureMode)
      {
        case TextureType.YCbCr:
          VideoTextureY.UpdateExternalTexture(frame.CapturedImageTextures[0]);
          VideoTextureCbCr.UpdateExternalTexture(frame.CapturedImageTextures[1]);
          break;

        case TextureType.BGRA:
          VideoTextureBGRA.UpdateExternalTexture(frame.CapturedImageTextures[0]);
          break;

        default:
          var message = "Cannot get camera feed from unknown texture mode: " + TextureMode;
          throw new InvalidEnumArgumentException(message);
      }
    }
      UpdateMaterials();
      _hasGottenFirstFrame = true;
    }

    private void SetupVideoTextures(IARFrame frame)
    {
      switch (TextureMode)
      {
        case TextureType.YCbCr:
          if (VideoTextureY == null)
            CreateYTexture(frame);

          if (VideoTextureCbCr == null)
            CreateCbCrTexture(frame);

          break;

        case TextureType.BGRA:
          if (VideoTextureBGRA == null)
            CreateBGRATexture(frame);

          break;

        default:
          var message = "Cannot get camera feed from unknown texture mode: " + TextureMode;
          throw new InvalidEnumArgumentException(message);
      }
    }

    private void CreateBGRATexture(IARFrame frame)
    {
      if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Playback || isStandAloneDesktop || isUnityEditor)
      {

        VideoTextureBGRA =
          new Texture2D
          (
            frame.Camera.CPUImageResolution.width,
            frame.Camera.CPUImageResolution.height,
            TextureFormat.BGRA32,
            false,
            true
          );
      }
      else
      {
        VideoTextureBGRA =
          Texture2D.CreateExternalTexture
          (
            frame.Camera.ImageResolution.width,
            frame.Camera.ImageResolution.height,
            TextureFormat.BGRA32,
            false,
            false,
            frame.CapturedImageTextures[0]
          );
      }

      VideoTextureBGRA.filterMode = FilterMode.Bilinear;
      VideoTextureBGRA.wrapMode = TextureWrapMode.Repeat;
    }

    private void CreateYTexture(IARFrame frame)
    {
      if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Playback || isStandAloneDesktop || isUnityEditor)
      {
        VideoTextureY =
          new Texture2D
          (
            frame.Camera.CPUImageResolution.width,
            frame.Camera.CPUImageResolution.height,
            TextureFormat.R8,
            false,
            true
          );
      }
      else
      {
        VideoTextureY =
          Texture2D.CreateExternalTexture
          (
            frame.Camera.ImageResolution.width,
            frame.Camera.ImageResolution.height,
            TextureFormat.R8,
            false,
            false,
            frame.CapturedImageTextures[0]
          );
      }

      VideoTextureY.filterMode = FilterMode.Bilinear;
      VideoTextureY.wrapMode = TextureWrapMode.Repeat;
    }

    private void CreateCbCrTexture(IARFrame frame)
    {
      if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Playback || isStandAloneDesktop || isUnityEditor)
      {
        VideoTextureCbCr =
          new Texture2D
          (
            frame.Camera.CPUImageResolution.width / 2,
            frame.Camera.CPUImageResolution.height / 2,
            TextureFormat.RG16,
            false,
            true
          );
      }
      else
      {
        VideoTextureCbCr =
          Texture2D.CreateExternalTexture
          (
            frame.Camera.ImageResolution.width / 2,
            frame.Camera.ImageResolution.height / 2,
            TextureFormat.RG16,
            false,
            false,
            frame.CapturedImageTextures[1]
          );

      }

      VideoTextureCbCr.filterMode = FilterMode.Bilinear;
      VideoTextureCbCr.wrapMode = TextureWrapMode.Repeat;
    }

    private void CorrectCameraMatrices(IARFrame frame)
    {
      switch (_cameraFeedResolution.Mode)
      {
        case CameraFeedResolutionMode.Custom:
        {
          var shouldInvertResolutionParams = ShouldInvertResolutionParams();
          var orientedWidth =
            shouldInvertResolutionParams
              ? _cameraFeedResolution.Height
              : _cameraFeedResolution.Width;

          var orientedHeight =
            shouldInvertResolutionParams
              ? _cameraFeedResolution.Width
              : _cameraFeedResolution.Height;

          ProjectionTransform =
            frame.Camera.CalculateProjectionMatrix
            (
              Screen.orientation,
              orientedWidth,
              orientedHeight,
              _nearClipPlane,
              _farClipPlane
            );

          DisplayTransform =
            frame.CalculateDisplayTransform(Screen.orientation, orientedWidth, orientedHeight);

          break;
        }

        case CameraFeedResolutionMode.Fixed:
          ProjectionTransform =
            frame.Camera.CalculateProjectionMatrix
            (
              _originalOrientation,
              _cameraFeedResolution.Width,
              _cameraFeedResolution.Height,
              _nearClipPlane,
              _farClipPlane
            );

          DisplayTransform =
            frame.CalculateDisplayTransform
            (
              _originalOrientation,
              _cameraFeedResolution.Width,
              _cameraFeedResolution.Height
            );

          break;

        default:
        {
          if (_cameraFeedResolution.Mode != CameraFeedResolutionMode.FromHardware)
          {
            ProjectionTransform =
              frame.Camera.CalculateProjectionMatrix
              (
                Screen.orientation,
                Screen.width,
                Screen.height,
                _nearClipPlane,
                _farClipPlane
              );

            DisplayTransform =
              frame.CalculateDisplayTransform
              (
                Screen.orientation,
                Screen.width,
                Screen.height
              );
          }
          else
          {
            ProjectionTransform =
              frame.Camera.CalculateProjectionMatrix
              (
                Screen.orientation,
                VideoFull.width,
                VideoFull.height,
                _nearClipPlane,
                _farClipPlane
              );

            DisplayTransform = Matrix4x4.identity;
          }

          break;
        }
      }
    }

    private bool ShouldInvertResolutionParams()
    {
      var orientation = Screen.orientation;
      if (orientation == ScreenOrientation.AutoRotation)
        return false;

      var isOrientationPortrait =
        (orientation == ScreenOrientation.Portrait ||
          orientation == ScreenOrientation.PortraitUpsideDown);

      if (isOrientationPortrait)
      {
        return
          _originalOrientation == ScreenOrientation.LandscapeLeft ||
          _originalOrientation == ScreenOrientation.LandscapeRight;
      }

      return
        _originalOrientation == ScreenOrientation.Portrait ||
        _originalOrientation == ScreenOrientation.PortraitUpsideDown;
    }

    private void UpdateMaterials()
    {
      switch (TextureMode)
      {
        case TextureType.YCbCr:
          _yuvMaterial.SetTexture(TextureY, VideoTextureY);
          _yuvMaterial.SetTexture(TextureCbCr, VideoTextureCbCr);
          _yuvMaterial.SetMatrix(TextureTransform, DisplayTransform);
          break;

        case TextureType.BGRA:
          _bgraMaterial.SetTexture(Texture, VideoTextureBGRA);
          _bgraMaterial.SetMatrix(TextureTransform, DisplayTransform);
          break;
      }
    }

    private void ReleaseUnmanagedResources()
    {
#if DOUBLE_BUFFER_VIDEO_FEED
      _UpdateLoop.Tick -= SwapVideoBuffer;
#endif

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          _arSession.FrameUpdated -= OnFrameUpdated;

          if (VideoTextureY)
          {
            // This check is to allow unit tests to run this code
            if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Playback || isStandAloneDesktop || isUnityEditor)
            {
                UnityEngine.Object.DestroyImmediate(VideoTextureY);
                UnityEngine.Object.DestroyImmediate(VideoTextureCbCr);
            }

            else
            {
                UnityEngine.Object.Destroy(VideoTextureY);
                UnityEngine.Object.Destroy(VideoTextureCbCr);
            }

          }

          if (VideoTextureBGRA)
          {
            if (_arSession.RuntimeEnvironment == RuntimeEnvironment.Playback || isStandAloneDesktop || isUnityEditor)
            {
                UnityEngine.Object.DestroyImmediate(VideoTextureBGRA);
            }
            else
            {
                UnityEngine.Object.Destroy(VideoTextureBGRA);
            }
          }

          _virtualCamera.Dispose();

#if DOUBLE_BUFFER_VIDEO_FEED
          for (int i = 0; i < 2; i++)
          {
            if (_videoFullDoubleBuffer[i] != null && _videoFullDoubleBuffer[i].IsCreated())
              _videoFullDoubleBuffer[i].Release();
          }

          if (_videoFullAlias) {
            UnityEngine.Object.Destroy(_videoFullAlias);
          }

          if (_videoFullAliasPlaceholder) {
            UnityEngine.Object.Destroy(_videoFullAliasPlaceholder);
          }
#else
          if (_videoFull != null && _videoFull.IsCreated())
            _videoFull.Release();
#endif
        }
      );
    }

    public void Dispose()
    {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }
  }

  public static class ARSessionCameraFeedExtension
  {
    private static readonly ConcurrentDictionary<IARSession, ARCameraFeed> _cameraFeeds =
      new ConcurrentDictionary<IARSession, ARCameraFeed>();

    private static IARSession _session;

    private static int _initializedValue;
    private static void _InitializeIfNeeded()
    {
      if (Interlocked.CompareExchange(ref _initializedValue, 1, 0) != 0)
        return;

      _CallbackQueue.ApplicationWillQuit += _Deinitialize;
      ARSessionFactory.SessionInitialized += _SessionInitialized;
    }

    internal static void _Deinitialize()
    {
      _CallbackQueue.ApplicationWillQuit -= _Deinitialize;
      ARSessionFactory.SessionInitialized -= _SessionInitialized;

      var oldSession = _session;
      if (oldSession != null)
      {
        _session = null;
        oldSession.Deinitialized -= _RemoveCameraFeed;
        _cameraFeeds.TryRemove(oldSession, out _);
      }

      _initializedValue = 0;
    }

    private static void _SessionInitialized(AnyARSessionInitializedArgs args)
    {
      var oldSession = _session;
      if (oldSession != null)
      {
        oldSession.Deinitialized -= _RemoveCameraFeed;
        _cameraFeeds.TryRemove(oldSession, out _);
      }

      _session = args.Session;
      _session.Deinitialized += _RemoveCameraFeed;
    }

    private static void _RemoveCameraFeed(ARSessionDeinitializedArgs args)
    {
      var session = _session;
      if (session == null)
        return;

      _session = null;
      _cameraFeeds.TryRemove(session, out _);
    }

    /// Creates or gets an ARCameraFeed that outputs textures using the given ARSession's
    /// frame updates. Not all ARSessions need to have an ARCameraFeed; only get one if you
    /// need the output textures.
    /// @note
    ///   Only a single ARCameraFeed instance will ever be created for each ARSession.
    ///   This means that if you call this method multiple times for the same session, the
    ///   returned ARCameraFeed will always have the original resolution and textureType.
    /// @param arSession The ARSession to get the feed for.
    /// @param resolution The resolution info of the ARCameraFeed.
    /// @returns An ARCameraFeed for the given ARSession.
    public static ARCameraFeed GetCameraFeed
    (
      this IARSession arSession,
      ARCameraFeed.CameraFeedResolution resolution,
      ARCameraFeed.TextureType textureType = ARCameraFeed.TextureType.Platform
    )
    {
      _InitializeIfNeeded();

      var feed =
        _cameraFeeds.GetOrAdd
        (
          arSession,
          (key) =>
          {
            var newFeed = new ARCameraFeed
            (
              key,
              textureType,
              cameraFeedResolution: resolution,
              autoDisposeOnDeinitialize: true
            );

            return newFeed;
          }
        );

      return feed;
    }
  }
}
