// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using AOT;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.AR.Localization
{
  /// @note This is an experimental feature, and may be changed or removed in a future release.
  ///   This feature is currently not functional or supported.
  internal sealed class _NativeLocalizer:
    _ThreadCheckedObject,
    ILocalizer
  {
    private readonly IARSession _arSession;
    private ArdkEventHandler<LocalizationProgressArgs> _localizationProgressUpdated;

    internal _NativeLocalizer(IARSession session)
    {
      _arSession = session;
      _nativeHandle = _NARVPS_Init(session.StageIdentifier.ToByteArray());
      if (_nativeHandle == IntPtr.Zero)
        throw new ArgumentException("nativeHandle can't be Zero.", nameof(_nativeHandle));
    }

    /// <inheritdoc />
    public event ArdkEventHandler<LocalizationProgressArgs> LocalizationProgressUpdated
    {
      add
      {
        _CheckThread();

        _SubscribeToDidUpdateLocalization();

        _localizationProgressUpdated += value;
      }
      remove
      {
        _localizationProgressUpdated -= value;
      }
    }


    public bool IsDestroyed
    {
      get => _nativeHandle == IntPtr.Zero;
    }
    
    public void Dispose()
    {
      _CheckThread();

      GC.SuppressFinalize(this);

      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _nativeHandle = IntPtr.Zero;

        _ReleaseImmediate(nativeHandle);
      }

      _cachedHandle.Free();
      _cachedHandleIntPtr = IntPtr.Zero;
    }
    
    private static void _ReleaseImmediate(IntPtr nativeHandle)
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARVPS_Release(nativeHandle);
      }
    }

    ~_NativeLocalizer()
    {
      _ReleaseImmediate(_nativeHandle);
    }

    /// <inheritdoc />
    public void StartLocalization(ILocalizationConfiguration config)
    {
      _CheckThread();

      if (config == null)
      {
        throw new ArgumentNullException(nameof(config));
      }

      if (!_ValidateARSessionIsAlive())
      {
        ARLog._Error("The ARSession is deinitialized, cannot start localization");
        return;
      }

      if (_arSession is _NativeARSession nativeSession)
      {
        var isLocationServiceInitializedForNoId =
          !nativeSession._IsLocationServiceInitialized() &&
          string.IsNullOrEmpty(config.MapIdentifier);
        
        if (isLocationServiceInitializedForNoId)
        {
          ARLog._Error
          (
            "SetupLocationService(locationService) must be called before attempting to localize" +
            " against any available world coordinate space."
          );
        
          return;
        }
      }

      if (!_ValidateLocalizationRequirements(config.LocalizationTimeout))
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        if (config is _NativeLocalizationConfiguration nativeConfig)
        {
          _NARVPS_StartLocalization
          (
            _nativeHandle,
            nativeConfig._NativeHandle
          );
        }
        else
        {
          var error = "Must use a _NativeLocalizationConfiguration with _NativeLocalizer";
          ARLog._Error(error);
        }
      }
    }

    /// <inheritdoc />
    public void StopLocalization()
    {
      _CheckThread();
      
      if (!_ValidateARSessionIsAlive())
      {
        ARLog._Error("The ARSession is deinitialized, cannot stop localization");
        return;
      }

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
        _NARVPS_StopLocalization(_nativeHandle);
    }

    private bool _ValidateLocalizationRequirements(float timeout)
    {
      if (timeout <= 0)
      {
        var warning = "Localization with a timeout value lesser than 0 will wait indefinitely " +
                      "for successful localization response";
        ARLog._Warn(warning);
      }

      if (_arSession.State != ARSessionState.Running)
      {
        ARLog._Error("Cannot start localization when ARSession is not running.");
        return false;
      }

      return true;
    }

    private bool _ValidateARSessionIsAlive()
    {
      if (_arSession is _NativeARSession nativeSession)
      {
        return !nativeSession.IsDestroyed;
      }
      
      var error = "Must use a _NativeLocalizationConfiguration with _NativeLocalizer";
      ARLog._Error(error);
      return false;
    }
    
    private void _SubscribeToDidUpdateLocalization()
    {
      _CheckThread();

      if (_updateLocalizationInitialized)
        return;

      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARVPS_Set_didUpdateLocalizationCallback
        (
          _handle,
          _nativeHandle,
          _onDidUpdateLocalizationNative
        );

        ARLog._Debug("Subscribed to native localization updated");
      }

      _updateLocalizationInitialized = true;
    }

    private bool _updateLocalizationInitialized;
    
    // Private handles and code to deal with native callbacks and initialization
    private IntPtr _nativeHandle;

    // Caching `this` for native device callbacks
    private IntPtr _cachedHandleIntPtr = IntPtr.Zero;
    private SafeGCHandle<_NativeLocalizer> _cachedHandle;

    private IntPtr _handle
    {
      get
      {
        _CheckThread();

        var cachedHandleIntPtr = _cachedHandleIntPtr;
        if (cachedHandleIntPtr != IntPtr.Zero)
          return cachedHandleIntPtr;

        _cachedHandle = SafeGCHandle.Alloc(this);
        cachedHandleIntPtr = _cachedHandle.ToIntPtr();
        _cachedHandleIntPtr = cachedHandleIntPtr;

        return cachedHandleIntPtr;
      }
    }

#region NativeCallbacks
    [MonoPInvokeCallback(typeof(_Localization_Callback))]
    private static void _onDidUpdateLocalizationNative
    (
      IntPtr context,
      UInt32 state,
      UInt32 failureReason,
      IntPtr transformPtr,
      string identifier,
      float confidence
    )
    {
      var localizer = SafeGCHandle.TryGetInstance<_NativeLocalizer>(context);
      if (localizer == null || localizer.IsDestroyed)
      {
        // localizer was deallocated
        ARLog._Debug("localizer is null in _onDidUpdateLocalizationNative()");
        return;
      }

      ARWorldCoordinateSpace coordinateSpace = null;
      if (transformPtr != IntPtr.Zero && !string.IsNullOrEmpty(identifier))
      {
        var parsedIdentifier = new ARWorldCoordinateSpace.Identifier(identifier);

        var transformSchema = new float[16];
        Marshal.Copy(transformPtr, transformSchema, 0, 16);
        // narTransform is expressed as TrackingToMap in CV coordinates
        var narTransform = _Convert.InternalToMatrix4x4(transformSchema);
        // unityTransform is expressed as MapToTracking in Unity coordinates
        var unityTransform = NARConversions.FromNARToUnity(narTransform.inverse);

        coordinateSpace = new ARWorldCoordinateSpace(parsedIdentifier, unityTransform);
      }

      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (localizer.IsDestroyed)
          {
            // localizer was deallocated
            return;
          }

          if (localizer._localizationProgressUpdated != null)
          {
            var args =
              new LocalizationProgressArgs
              (
                (LocalizationState)state,
                (LocalizationFailureReason)failureReason,
                confidence,
                coordinateSpace
              );

            localizer._localizationProgressUpdated(args);
          }
        }
      );
    }
#endregion

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARVPS_Init(byte[] stageIdentifier);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVPS_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NARVPS_StartLocalization
    (
      IntPtr nativeHandle,
      IntPtr nativeConfigHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NARVPS_StopLocalization(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARVPS_Set_didUpdateLocalizationCallback
    (
      IntPtr applicationSession,
      IntPtr platformSession,
      _Localization_Callback callback
    );

    private delegate void _Localization_Callback
    (
      IntPtr context,
      UInt32 state,
      UInt32 failureReason,
      IntPtr transformPtr,
      string identifier,
      float confidence
    );
  }
}
