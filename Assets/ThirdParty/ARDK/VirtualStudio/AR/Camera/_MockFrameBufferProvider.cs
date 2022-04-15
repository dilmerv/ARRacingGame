// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.ARSessionEventArgs;
using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.AR.Awareness.Depth;
using Niantic.ARDK.AR.Awareness.Semantics;
using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.AR.Image;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;
using UnityEngine.Rendering;

#if ARDK_HAS_URP
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using Niantic.ARDK.Rendering.SRP;

using UnityEngine.Rendering.Universal;
#endif

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  internal sealed class _MockFrameBufferProvider:
    IDisposable
  {
    // ARSession data
    private readonly _MockARSession _arSession;
    private readonly float _timeBetweenFrames;
    private float _timeSinceLastFrame;
    private _SerializableARCamera _cachedSerializedCamera;
    private readonly Transform _camerasRoot;

    // Image buffer
    private Camera _imageCamera;
    private CameraIntrinsics _imageIntrinsics;
    private RenderTexture _imageRT;

    // Depth buffer
    private bool _generateDepth;
    private readonly float _timeBetweenDepthUpdates;
    private float _timeSinceLastDepthUpdate;
    private Camera _depthCamera;
    private CameraIntrinsics _depthIntrinsics;
    private RenderTexture _depthRT;
    private RenderTexture _depthOnlyRT;
    private Shader _depthToDisparityShader;
    private Material _depthToDisparityMaterial;

    // Semantics buffer
    private bool _generateSemantics;
    private readonly float _timeBetweenSemanticsUpdates;
    private float _timeSinceLastSemanticsUpdate;
    private Camera _semanticsCamera;
    private CameraIntrinsics _semanticsIntrinsics;
    private Shader _semanticsShader;
    private RenderTexture _semanticsRT;
    private Texture2D _semanticsTex;
    private string[] _channelNames;

    // Magic numbers to define the image resolution of mock frames
    internal const int _ARImageWidth = 1920;
    internal const int _ARImageHeight = 1440;
    internal const int _SensorFocalLength = 26;

    // Awareness Model Params (updated ARDK 0.10):
    private const int _ModelWidth = 256;
    private const int _ModelHeight = 144;
    private const float _ModelNearDistance = 0.2f;
    private const float _ModelFarDistance = 100f;

    public const string MOCK_LAYER_NAME = "ARDK_MockWorld";

    public const string MOCK_LAYER_MISSING_MSG =
      "Add the ARDK_MockWorld layer to the Layers list (Edit > ProjectSettings > Tags and Layers)" +
      " in order to render in Mock AR sessions.";

    public _MockFrameBufferProvider(_MockARSession mockARSession, Transform camerasRoot)
    {
      _arSession = mockARSession;
      _arSession.Ran += CheckRunConfiguration;
      _timeBetweenFrames = 1f / _MockCameraConfiguration.FPS;

      if (mockARSession.Configuration is _SerializableARWorldTrackingConfiguration worldTrackingConfiguration)
      {
        _timeBetweenDepthUpdates = 1f / worldTrackingConfiguration.DepthTargetFrameRate;
        _timeBetweenSemanticsUpdates = 1f / worldTrackingConfiguration.SemanticTargetFrameRate;
      }

      _camerasRoot = camerasRoot;
      InitializeImageGeneration();

      _UpdateLoop.Tick += Update;
    }

    internal static void RemoveLayerFromCamera(Camera cam, string layerName)
    {
      // get the input layer name's index, which should range from 0 to Unity's max # of layers minus 1
      int layerIndex = LayerMask.NameToLayer(layerName);
        
      if (layerIndex > -1)
      {
        // perform a guardrail check to see if the mock layer
        // is included in the ar camera's culling mask
        if ((cam.cullingMask & (1 << layerIndex)) != 0)
        {
          // in the case that the mock layer is included, remove it from the culling mask
          cam.cullingMask &= ~(1 << layerIndex);
        }
      }
      else
      {
        ARLog._Error($"Layer {layerName} does not exist");
      }
    }

    private void CheckRunConfiguration(ARSessionRanArgs args)
    {
      if (_arSession.Configuration is IARWorldTrackingConfiguration worldTrackingConfiguration)
      {
        _generateDepth = worldTrackingConfiguration.IsDepthEnabled;
        _generateSemantics = worldTrackingConfiguration.IsSemanticSegmentationEnabled;
      }
      else
      {
        _generateDepth = false;
        _generateSemantics = false;
      }

      if (_generateDepth && _depthCamera == null)
        InitializeDepthGeneration();

      if (_generateSemantics && _semanticsCamera == null)
        InitializeSemanticsGeneration();

      if (_depthCamera != null)
      {
        _depthCamera.enabled = _generateDepth;
        _depthCamera.depthTextureMode =
          _generateDepth ? DepthTextureMode.Depth : DepthTextureMode.None;
      }

      if (_semanticsCamera != null)
      {
        _semanticsCamera.enabled = _generateSemantics;
      }
    }

    private void InitializeImageGeneration()
    {
      // Instantiate a new Unity camera
      _imageCamera = CreateCameraBase("Image");
      
      // Configure the camera to use physical properties
      _imageCamera.usePhysicalProperties = true;
      _imageCamera.focalLength = _SensorFocalLength;
      _imageCamera.nearClipPlane = 0.1f;
      _imageCamera.farClipPlane = 100f;

      // Infer the orientation of the editor
      var editorOrientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Rotate the 'device' to the UI orientation
      _imageCamera.transform.localRotation = MathUtils.CalculateViewRotation
      (
        from: ScreenOrientation.LandscapeLeft,
        to: editorOrientation
      ).ToRotation();
      
      // Set up rendering offscreen to render texture.
      _imageRT = new RenderTexture
      (
        _ARImageWidth,
        _ARImageHeight,
        16,
        RenderTextureFormat.BGRA32
      );
      _imageRT.Create();
      _imageCamera.targetTexture = _imageRT;

      _imageIntrinsics = MathUtils.CalculateIntrinsics(_imageCamera);

      // Reading this property's value is equivalent to calling
      // the CalculateProjectionMatrix method, using the camera's
      // imageResolution and intrinsics properties to derive size
      // and orientation, and passing default values of 0.001 and
      // 1000.0 for the near and far clipping planes.
      var projection = MathUtils.CalculateProjectionMatrix
      (
        _imageIntrinsics,
        _ARImageWidth,
        _ARImageHeight,
        _ARImageWidth,
        _ARImageHeight,
        _ARImageWidth > _ARImageHeight
          ? ScreenOrientation.LandscapeLeft
          : ScreenOrientation.Portrait,
        0.001f,
        1000.0f
      );

      // Initialize the view matrix.
      // This will be updated in every frame.
      var initialView = GetMockViewMatrix(_imageCamera);
      
      var imageResolution = new Resolution
      {
        width = _ARImageWidth, height = _ARImageHeight
      };

      _cachedSerializedCamera = new _SerializableARCamera
      (
        TrackingState.Normal,
        TrackingStateReason.None,
        imageResolution,
        imageResolution,
        _imageIntrinsics,
        _imageIntrinsics,
        initialView.inverse,
        projectionMatrix: projection,
        estimatedViewMatrix: initialView,
        worldScale: 1.0f
      );
    }

    private void InitializeDepthGeneration()
    {
      _depthCamera = CreateAwarenessCamera("Depth");
      _depthCamera.depthTextureMode = DepthTextureMode.Depth;
      
      var editorOrientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Rotate the 'device' to the UI orientation
      _depthCamera.transform.localRotation = MathUtils.CalculateViewRotation
      (
        from: ScreenOrientation.LandscapeLeft,
        to: editorOrientation
      ).ToRotation();

      _depthRT =
      new RenderTexture
      (
        _ModelWidth,
        _ModelHeight,
        16,
        RenderTextureFormat.Depth
      );

    _depthOnlyRT =
      new RenderTexture
      (
        _ModelWidth,
        _ModelHeight,
        0,
        RenderTextureFormat.RFloat
      );

      _depthToDisparityShader = Resources.Load<Shader>("UnityToMetricDepth");
      _depthToDisparityMaterial = new Material(_depthToDisparityShader);

      var farDividedByNear = _ModelFarDistance / _ModelNearDistance;
      _depthToDisparityMaterial.SetFloat("_ZBufferParams_Z", (-1 + farDividedByNear) / _ModelFarDistance);
      _depthToDisparityMaterial.SetFloat("_ZBufferParams_W", 1 / _ModelFarDistance);

      _depthCamera.targetTexture = _depthRT;
      _depthIntrinsics = MathUtils.CalculateIntrinsics(_depthCamera);
    }

    private void InitializeSemanticsGeneration()
    {
      _semanticsCamera = CreateAwarenessCamera("Semantics");
      _semanticsCamera.clearFlags = CameraClearFlags.SolidColor;
      _semanticsCamera.backgroundColor = new Color(0, 0, 0, 0);
      
      var editorOrientation = Screen.width > Screen.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Rotate the 'device' to the UI orientation
      _semanticsCamera.transform.localRotation = MathUtils.CalculateViewRotation
        (
          from: ScreenOrientation.LandscapeLeft,
          to: editorOrientation
        ).ToRotation();

      _semanticsRT =
        new RenderTexture
        (
          _ModelWidth,
          _ModelHeight,
          16,
          RenderTextureFormat.ARGB32
        );

      _semanticsRT.Create();
      _semanticsCamera.targetTexture = _semanticsRT;

      _semanticsShader = Resources.Load<Shader>("Segmentation");
      _semanticsCamera.SetReplacementShader(_semanticsShader, String.Empty);

      _semanticsTex = new Texture2D(_ModelWidth, _ModelHeight, TextureFormat.ARGB32, false);

      _semanticsIntrinsics = MathUtils.CalculateIntrinsics(_semanticsCamera);

      SetupReplacementRenderer();

      _channelNames = Enum.GetNames(typeof(MockSemanticLabel.ChannelName));
    }

    private void SetupReplacementRenderer()
    {
#if ARDK_HAS_URP
      if (!_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
        return;

      var rendererIndex =
        _RenderPipelineInternals.GetRendererIndex
        (
          _RenderPipelineInternals.REPLACEMENT_RENDERER_NAME
        );

      if (rendererIndex < 0)
      {
        ARLog._Error
        (
          "Cannot generate mock semantic segmentation buffers unless the ArdkUrpAssetRenderer" +
          " is added to the Renderer List."
        );

        return;
      }

      _semanticsCamera.GetUniversalAdditionalCameraData().SetRenderer(rendererIndex);
#endif
    }

    private Camera CreateCameraBase(string name)
    {
      var cameraObject = new GameObject(name);
      cameraObject.transform.SetParent(_camerasRoot);

      var camera = cameraObject.AddComponent<Camera>();
      camera.depth = int.MinValue;

      if (LayerMask.NameToLayer(MOCK_LAYER_NAME) < 0)
      {
        ARLog._Error(MOCK_LAYER_MISSING_MSG);
        return null;
      }

      camera.cullingMask = LayerMask.GetMask(MOCK_LAYER_NAME);

      return camera;
    }

    private Camera CreateAwarenessCamera(string name)
    {
      var camera = CreateCameraBase(name);

      camera.nearClipPlane = _ModelNearDistance;
      camera.farClipPlane = _ModelFarDistance;
      camera.usePhysicalProperties = true;
      camera.focalLength = _SensorFocalLength;

      return camera;
    }

    private bool _isDisposed;
    public void Dispose()
    {
      if (_isDisposed)
        return;

      _isDisposed = true;

      _imageRT.Release();

      if (_depthCamera != null)
      {
        GameObject.Destroy(_depthCamera.gameObject);
        _depthRT.Release();
        _depthOnlyRT.Release();
      }

      if (_semanticsCamera != null)
      {
        GameObject.Destroy(_semanticsCamera.gameObject);
        _semanticsRT.Release();
      }
    }

    private static Matrix4x4 GetMockViewMatrix(Camera serializedCamera)
    {
      var rotation = MathUtils.CalculateViewRotation
      (
        from: ScreenOrientation.LandscapeLeft,
        to: Screen.width > Screen.height
          ? ScreenOrientation.LandscapeLeft
          : ScreenOrientation.Portrait
      );

      var narView = serializedCamera.worldToCameraMatrix.ConvertViewMatrixBetweenNarAndUnity();
      
      return rotation * narView;
    }

    private void Update()
    {
      if (_arSession != null && _arSession.State == ARSessionState.Running)
      {
        _timeSinceLastFrame += Time.deltaTime;
        if (_timeSinceLastFrame >= _timeBetweenFrames)
        {
          _timeSinceLastFrame = 0;

          var mockViewMatrix = GetMockViewMatrix(_imageCamera);
          _cachedSerializedCamera._estimatedViewMatrix = mockViewMatrix;
          _cachedSerializedCamera.Transform = mockViewMatrix.inverse;

          _SerializableDepthBuffer depthBuffer = null;
          if (_generateDepth && Time.time - _timeSinceLastDepthUpdate > _timeBetweenDepthUpdates)
          {
            depthBuffer = _GetDepthBuffer();
            _timeSinceLastDepthUpdate = Time.time;
          }
          
          _SerializableSemanticBuffer semanticBuffer = null;
          if (_generateSemantics && Time.time - _timeSinceLastSemanticsUpdate > _timeBetweenSemanticsUpdates)
          {
            semanticBuffer = _GetSemanticBuffer();
            _timeSinceLastSemanticsUpdate = Time.time;
          }

          var serializedFrame = new _SerializableARFrame
          (
            capturedImageBuffer: _GetImageBuffer(),
            depthBuffer: depthBuffer,
            semanticBuffer: semanticBuffer,
            camera: _cachedSerializedCamera,
            lightEstimate: null,
            anchors: null,
            maps: null,
            worldScale: 1.0f,
            estimatedDisplayTransform: MathUtils.CalculateDisplayTransform
            (
              _ARImageWidth,
              _ARImageHeight,
              Screen.width,
              Screen.height,
              Screen.width > Screen.height
                ? ScreenOrientation.LandscapeLeft
                : ScreenOrientation.Portrait,
              invertVertically: false
            )
          );

          _arSession.UpdateFrame(serializedFrame);
        }
      }
    }

    private _SerializableImageBuffer _GetImageBuffer()
    {
      var imageData =
        new NativeArray<byte>
        (
          _ARImageWidth * _ARImageHeight * 4,
          Allocator.Persistent,
          NativeArrayOptions.UninitializedMemory
        );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref imageData, AtomicSafetyHandle.Create());
#endif

      AsyncGPUReadback.RequestIntoNativeArray(ref imageData, _imageRT).WaitForCompletion();

      var plane =
        new _SerializableImagePlane
        (
          imageData,
          _ARImageWidth,
          _ARImageHeight,
          _ARImageWidth * 4,
          4
        );

      var buffer =
        new _SerializableImageBuffer
        (
          ImageFormat.BGRA,
          new _SerializableImagePlanes(new[] { plane }),
          75
        );

      return buffer;
    }

    private _SerializableDepthBuffer _GetDepthBuffer()
    {
      Graphics.Blit(_depthRT, _depthOnlyRT, _depthToDisparityMaterial);

      var depthData = new NativeArray<float>
      (
        _ModelWidth * _ModelHeight,
        Allocator.Persistent,
        NativeArrayOptions.UninitializedMemory
      );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref depthData, AtomicSafetyHandle.Create());
#endif

      AsyncGPUReadback.RequestIntoNativeArray(ref depthData, _depthOnlyRT).WaitForCompletion();

      var buffer = new _SerializableDepthBuffer
      (
        _ModelWidth,
        _ModelHeight,
        true,
        GetMockViewMatrix(_imageCamera),
        depthData,
        _ModelNearDistance,
        _ModelFarDistance,
        _depthIntrinsics
      )
      {
        IsRotatedToScreenOrientation = true
      };

      return buffer;
    }

    private _SerializableSemanticBuffer _GetSemanticBuffer()
    {
       var data = new NativeArray<uint>
       (
         _ModelWidth * _ModelHeight,
         Allocator.Persistent,
         NativeArrayOptions.UninitializedMemory
       );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref data, AtomicSafetyHandle.Create());
#endif

      // Doing this in the CPU is slower, but I couldn't figure out how to get
      // the correct uint value out of a shader. Performance is sufficient.
      var currRT = RenderTexture.active;
      RenderTexture.active = _semanticsRT;

      _semanticsTex.ReadPixels(new Rect(0, 0, _ModelWidth, _ModelHeight), 0, 0);
      _semanticsTex.Apply();

      RenderTexture.active = currRT;

      var byteArray = _semanticsTex.GetPixels32();
      for (var i = 0; i < byteArray.Length; i++)
      {
        data[i] = MockSemanticLabel.ToInt(byteArray[i]);
      }

      var buffer = new _SerializableSemanticBuffer
      (
        _ModelWidth,
        _ModelHeight,
        true,
        GetMockViewMatrix(_imageCamera),
        data,
        _channelNames,
        _semanticsIntrinsics
      )
      {
        IsRotatedToScreenOrientation = true
      };

      return buffer;
    }
  }
}