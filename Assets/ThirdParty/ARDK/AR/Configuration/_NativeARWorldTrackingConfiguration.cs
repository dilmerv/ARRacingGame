// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;

using AOT;

using Niantic.ARDK.AR.Awareness.Depth.Generators;
using Niantic.ARDK.AR.ReferenceImage;
using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.AR.VideoFormat;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.AR.Configuration
{
  internal sealed class _NativeARWorldTrackingConfiguration:
    _NativeARConfiguration,
    IARWorldTrackingConfiguration
  {
    /// Perform an asynchronous check as to whether the hardware and software are capable of and
    /// support the ARWorldTrackingConfiguration.
    internal static void _CheckCapabilityAndSupport
    (
      Action<ARHardwareCapability, ARSoftwareSupport> callback
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        try
        {
          _NARConfiguration_CheckCapabilityAndSupport
          (
            1,
            SafeGCHandle.AllocAsIntPtr(callback),
            ConfigurationCheckCapabilityAndSupportCallback
          );
        }
        catch (DllNotFoundException e)
        {
          ARLog._DebugFormat("Failed to check ARDK system compatibility: {0}", false, e);
          callback(ARHardwareCapability.NotCapable, ARSoftwareSupport.NotSupported);
        }
      }
    }

    internal static bool _CheckLidarDepthSupport()
    {
      try
      {
        return _NARConfiguration_CheckLidarDepthSupport();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to check ARDK system compatibility: {0}", false, e);
        return false;
      }
    }

    internal static bool _CheckDepthEstimationSupport()
    {
      try
      {
        return _NARConfiguration_CheckDepthEstimationSupport();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to check ARDK system compatibility: {0}", false, e);
        return false;
      }
    }

    internal static bool _CheckDepthSupport()
    {
      try
      {
        return _NARConfiguration_CheckDepthSupport();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to check ARDK system compatibility: {0}", false, e);
        return false;
      }
    }

    internal static bool _CheckSemanticSegmentationSupport()
    {
      try
      {
        return _NARConfiguration_CheckSemanticSegmentationSupport();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to check ARDK system compatibility: {0}", false, e);
        return false;
      }
    }

    internal static bool _CheckMeshingSupport()
    {
      try
      {
        return _NARConfiguration_CheckMeshingSupport();
      }
      catch (DllNotFoundException e)
      {
        ARLog._DebugFormat("Failed to check ARDK system compatibility: {0}", false, e);
        return false;
      }
    }

    private static IntPtr InitNativeHandle(bool playbackEnabled)
    {
#pragma warning disable 0429
      IntPtr result = new IntPtr(0);
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        result = playbackEnabled ? _NARPlaybackWorldTrackingConfiguration_Init() : _NARWorldTrackingConfiguration_Init();
      }
      return result;
#pragma warning restore 0429
    }

    internal _NativeARWorldTrackingConfiguration(bool playbackEnabled=false):
      this(InitNativeHandle(playbackEnabled))
    {
    }

    internal _NativeARWorldTrackingConfiguration(IntPtr nativeHandle):
      base(nativeHandle)
    {
    }

    protected override long _MemoryPressure
    {
      get { return base._MemoryPressure + 8; }
    }

    public override ReadOnlyCollection<IARVideoFormat> SupportedVideoFormats
    {
      get
      {
        var rawFormats = EmptyArray<IntPtr>.Instance;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            var obtained =
              _NARWorldTrackingConfiguration_GetSupportedVideoFormats
              (
                _NativeHandle,
                rawFormats.Length,
                rawFormats
              );

            if (obtained == rawFormats.Length)
              break;

            rawFormats = new IntPtr[Math.Abs(obtained)];
          }
        }

        var count = rawFormats.Length;

        if (count == 0)
          return EmptyReadOnlyCollection<IARVideoFormat>.Instance;

        var formats = new IARVideoFormat[count];

        for (var i = 0; i < count; i++)
          formats[i] = _NativeARVideoFormat._FromNativeHandle(rawFormats[i]);

        return new ReadOnlyCollection<IARVideoFormat>(formats);
      }
    }

    public PlaneDetection PlaneDetection
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (PlaneDetection)_NARWorldTrackingConfiguration_GetPlaneDetection(_NativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARWorldTrackingConfiguration_SetPlaneDetection(_NativeHandle, (UInt64)value);
      }
    }

    public IReadOnlyCollection<IARReferenceImage> DetectionImages
    {
      get
      {
        var rawImages = EmptyArray<IntPtr>.Instance;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          while (true)
          {
            var obtained =
              _NARWorldTrackingConfiguration_GetDetectionImages
              (
                _NativeHandle,
                rawImages.Length,
                rawImages
              );

            if (obtained == rawImages.Length)
              break;

            rawImages = new IntPtr[Math.Abs(obtained)];
          }
        }

        var images = new HashSet<IARReferenceImage>();
        var count = rawImages.Length;

        for (var i = 0; i < count; i++)
          images.Add(_NativeARReferenceImage._FromNativeHandle(rawImages[i]));

        return images.AsArdkReadOnly();
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        {
          var rawImages =
            (from image in value select ((_NativeARReferenceImage)image)._NativeHandle).ToArray();

          _NARWorldTrackingConfiguration_SetDetectionImages
          (
            _NativeHandle,
            rawImages,
            (UInt64)rawImages.Length
          );
        }
      }
    }

    public bool IsAutoFocusEnabled
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARWorldTrackingConfiguration_IsAutoFocusEnabled(_NativeHandle) != 0;

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARWorldTrackingConfiguration_SetAutoFocusEnabled(_NativeHandle, value ? 1 : (UInt32)0);
      }
    }

    public bool IsSharedExperienceEnabled
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
         return _NARWorldTrackingConfiguration_IsCollaborationEnabled(_NativeHandle) != 0;

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        UInt32 uintValue = value ? 1 : (UInt32)0;

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARWorldTrackingConfiguration_SetCollaborationEnabled(_NativeHandle, uintValue);
      }
    }

    public MappingRole MappingRole
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return (MappingRole) _NARWorldTrackingConfiguration_GetMappingRole(_NativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARWorldTrackingConfiguration_SetMappingRole(_NativeHandle, Convert.ToUInt32(value));
      }
    }

    public MapLayerIdentifier MapLayerIdentifier
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return new MapLayerIdentifier
            (_NARWorldTrackingConfiguration_GetMapLayerIdentifier(_NativeHandle));

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARWorldTrackingConfiguration_SetMapLayerIdentifier(_NativeHandle, value._ToGuid());
      }
    }

    public uint DepthTargetFrameRate
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_GetDepthTargetFrameRate(_NativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetDepthTargetFrameRate(_NativeHandle, value);
      }
    }

    public uint SemanticTargetFrameRate
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_GetSemanticTargetFrameRate(_NativeHandle);

        #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
        #pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetSemanticTargetFrameRate(_NativeHandle, value);
      }
    }

    public bool IsDepthEnabled
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_IsDepthEnabled(_NativeHandle) != 0;

#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetDepthEnabled(_NativeHandle, value ? 1 : (UInt32)0);
      }
    }

    private DepthPointCloudGenerator.Settings _depthPointCloudSettings;
    public DepthPointCloudGenerator.Settings DepthPointCloudSettings
    {
      get
      {
        var result = _depthPointCloudSettings;

        if (result == null)
        {
          result = new DepthPointCloudGenerator.Settings();
          _depthPointCloudSettings = result;
        }

        return result;
      }
      set
      {
        _depthPointCloudSettings = value;
      }
    }

    public bool IsSemanticSegmentationEnabled
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_IsSemanticSegmentationEnabled(_NativeHandle) != 0;
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetSemanticSegmentationEnabled(_NativeHandle, value ? 1 : (UInt32)0);
      }
    }

    public bool IsMeshingEnabled
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_IsMeshingEnabled(_NativeHandle) != 0;
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetMeshingEnabled(_NativeHandle, value ? 1 : (UInt32)0);
      }
    }

    public uint MeshingTargetFrameRate
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_GetMeshingTargetFrameRate(_NativeHandle);
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetMeshingTargetFrameRate(_NativeHandle, value);
      }
    }

    public float MeshingRadius
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_GetMeshingRadius(_NativeHandle);
    #pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
    #pragma warning restore 0162
      }
      set
      {
        if (value > 0 && value < 5)
        {
          ARLog._Error
          (
            "The smallest meshing radius possible is 5 meters. " +
            "Set the value to 0 for an infinite radius."
          );

          return;
        }

        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetMeshingRadius(_NativeHandle, value);
      }
    }

    public float MeshingTargetBlockSize
    {
      get
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          return _NARConfiguration_GetMeshingTargetBlockSize(_NativeHandle);
#pragma warning disable 0162
        throw new IncorrectlyUsedNativeClassException();
#pragma warning restore 0162
      }
      set
      {
        if (NativeAccess.Mode == NativeAccess.ModeType.Native)
          _NARConfiguration_SetMeshingTargetBlockSize(_NativeHandle, value);
      }
    }

    public void SetDetectionImagesAsync
    (
      IReadOnlyCollection<IARReferenceImage> detectionImages,
      Action completionHandler
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var rawImages =
          (from image in detectionImages select ((_NativeARReferenceImage)image)._NativeHandle)
          .ToArray();

        _NARWorldTrackingConfiguration_SetDetectionImagesAsync
        (
          _NativeHandle,
          rawImages,
          (UInt64)rawImages.Length,
          SafeGCHandle.AllocAsIntPtr(completionHandler),
          SetDetectionImagesAsyncCallback
        );
      }
    }

    public override void CopyTo(IARConfiguration target)
    {
      if (!(target is IARWorldTrackingConfiguration worldTarget))
      {
        var msg =
          "ARWorldTrackingConfiguration cannot be copied into a non-ARWorldTrackingConfiguration.";

        throw new ArgumentException(msg);
      }

      base.CopyTo(target);

      worldTarget.PlaneDetection = PlaneDetection;
      worldTarget.IsAutoFocusEnabled = IsAutoFocusEnabled;
      worldTarget.IsSharedExperienceEnabled = IsSharedExperienceEnabled;
      worldTarget.MappingRole = MappingRole;
      worldTarget.MapLayerIdentifier = MapLayerIdentifier;
      worldTarget.DetectionImages = DetectionImages;

      worldTarget.IsDepthEnabled = IsDepthEnabled;
      worldTarget.DepthTargetFrameRate = DepthTargetFrameRate;
      worldTarget.DepthPointCloudSettings = DepthPointCloudSettings.Copy();

      worldTarget.IsSemanticSegmentationEnabled = IsSemanticSegmentationEnabled;
      worldTarget.SemanticTargetFrameRate = SemanticTargetFrameRate;

      worldTarget.IsMeshingEnabled = IsMeshingEnabled;
      worldTarget.MeshingTargetFrameRate = MeshingTargetFrameRate;
      worldTarget.MeshingTargetBlockSize = MeshingTargetBlockSize;
      worldTarget.MeshingRadius = MeshingRadius;
    }

    [MonoPInvokeCallback(typeof(_ARWorldTrackingConfiguration_SetDetectionImagesAsync_Handler))]
    private static void SetDetectionImagesAsyncCallback(IntPtr context)
    {
      var actionHandle = SafeGCHandle<Action>.FromIntPtr(context);
      var callback = actionHandle.TryGetInstance();
      actionHandle.Free();

      if (callback != null)
        _CallbackQueue.QueueCallback(() => { callback(); });
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARWorldTrackingConfiguration_Init();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARPlaybackWorldTrackingConfiguration_Init();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NARWorldTrackingConfiguration_GetSupportedVideoFormats
    (
      IntPtr nativeHandle,
      Int32 lengthOfOutSupportedVideoFormats,
      IntPtr[] outSupportedVideoFormats
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt64 _NARWorldTrackingConfiguration_GetPlaneDetection
    (
      IntPtr nativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARWorldTrackingConfiguration_SetPlaneDetection
    (
      IntPtr nativeHandle,
      UInt64 planeDetection
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NARWorldTrackingConfiguration_GetDetectionImages
    (
      IntPtr nativeHandle,
      Int32 lengthOfOutDetectionImages,
      IntPtr[] outDetectionImages
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARWorldTrackingConfiguration_SetDetectionImages
    (
      IntPtr nativeHandle,
      IntPtr[] detectionImages,
      UInt64 detectionImageCount
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARWorldTrackingConfiguration_SetDetectionImagesAsync
    (
      IntPtr nativeHandle,
      IntPtr[] detectionImages,
      UInt64 detectionImageCount,
      IntPtr applicationContext,
      _ARWorldTrackingConfiguration_SetDetectionImagesAsync_Handler completionHandler
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARWorldTrackingConfiguration_IsAutoFocusEnabled
    (
      IntPtr nativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARWorldTrackingConfiguration_SetAutoFocusEnabled
    (
      IntPtr nativeHandle,
      UInt32 enabled
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARWorldTrackingConfiguration_IsCollaborationEnabled
    (
      IntPtr nativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARWorldTrackingConfiguration_SetCollaborationEnabled
    (
      IntPtr nativeHandle,
      UInt32 enabled
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARWorldTrackingConfiguration_GetMappingRole
    (
      IntPtr nativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARWorldTrackingConfiguration_SetMappingRole
    (
      IntPtr nativeHandle,
      UInt32 role
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Guid _NARWorldTrackingConfiguration_GetMapLayerIdentifier
    (
      IntPtr nativeHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARWorldTrackingConfiguration_SetMapLayerIdentifier
    (
      IntPtr nativeHandle,
      Guid identifier
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARConfiguration_IsDepthEnabled(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetDepthEnabled
    (
      IntPtr nativeHandle,
      UInt32 enabled
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARConfiguration_GetDepthTargetFrameRate(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetDepthTargetFrameRate
    (
      IntPtr nativeHandle,
      UInt32 targetFrameRate
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARConfiguration_IsSemanticSegmentationEnabled(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetSemanticSegmentationEnabled
    (
      IntPtr nativeHandle,
      UInt32 enabled
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARConfiguration_GetSemanticTargetFrameRate(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetSemanticTargetFrameRate
    (
      IntPtr nativeHandle,
      UInt32 targetFrameRate
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetSemanticThreshold
    (
      IntPtr nativeHandle,
      string channelName,
      float threshold
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARConfiguration_IsMeshingEnabled(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetMeshingEnabled
    (
      IntPtr nativeHandle,
      UInt32 enabled
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern UInt32 _NARConfiguration_GetMeshingTargetFrameRate(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetMeshingTargetFrameRate
    (
      IntPtr nativeHandle,
      UInt32 targetFrameRate
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NARConfiguration_GetMeshingRadius(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetMeshingRadius
    (
      IntPtr nativeHandle,
      float meshingRadius
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NARConfiguration_GetMeshingTargetBlockSize(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARConfiguration_SetMeshingTargetBlockSize
    (
      IntPtr nativeHandle,
      float targetFrameRate
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NARConfiguration_CheckLidarDepthSupport();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NARConfiguration_CheckDepthEstimationSupport();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NARConfiguration_CheckDepthSupport();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NARConfiguration_CheckSemanticSegmentationSupport();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NARConfiguration_CheckMeshingSupport();

    private delegate void _ARWorldTrackingConfiguration_SetDetectionImagesAsync_Handler
    (
      IntPtr context
    );
  }
}
