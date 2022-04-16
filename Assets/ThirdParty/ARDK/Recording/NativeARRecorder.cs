// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif
#if (UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_DESKTOP) && !UNITY_EDITOR
#define AR_NATIVE_SUPPORT
#endif

using System;
using System.Runtime.InteropServices;
using System.Security;

using AOT;

using Niantic.ARDK.AR;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// A native implementation of the <see cref="IARRecorder"/>
  /// </summary>
  public sealed class NativeARRecorder:
    IARRecorder,
    IDisposable
  {
    private HandleRef _nativeHandle;

    public NativeARRecorder(Guid stageIdentifier)
    {
      #if AR_NATIVE_SUPPORT
      unsafe
      {
        var stageIdentifierBytes = stageIdentifier.ToByteArray();

        fixed (byte* stageIdentifierPtr = stageIdentifierBytes)
        {
          var nativePtr = PInvoke.Ctor(stageIdentifierPtr);
          _nativeHandle = new HandleRef(this, nativePtr);
        }
      }
      #endif
    }

    ~NativeARRecorder()
    {
      ReleaseUnmanagedResources();
    }

    internal NativeARRecorder(_NativeARSession arSession)
      : this(arSession.StageIdentifier)
    {
    }

    /// <summary>
    /// Starts recording an AR session.
    /// </summary>
    /// <param name="recordingConfig">The configs to use for this recording.</param>
    public void Start(ARRecorderConfig recordingConfig)
    {
      PInvoke.Start(_nativeHandle, recordingConfig);
    }

    /// <summary>
    /// Stops recording a session.
    /// </summary>
    /// <param name="previewConfig">Preview config for recordings.</param>
    /// <param name="previewCallback">
    /// The callback to call once preview post-processing on the recording has finished.
    /// </param>
    /// <param name="researchConfig">Research config for recordings.</param>
    /// <param name="researchCallback">
    /// The callback to call once research post-processing on the recording has finished.
    /// </param>
    public void Stop(
      ARRecordingPreviewConfig previewConfig,
      Action<ARRecordingPreviewResults> previewCallback,
      ARRecordingResearchConfig researchConfig,
      Action<ARRecordingResearchResults> researchCallback,
      ARRecordingUnpackConfig unpackConfig,
      Action<ARRecordingUnpackResults> unpackCallback)
    {
      PInvoke.Stop(
        _nativeHandle,
        previewConfig,
        previewCallback,
        researchConfig,
        researchCallback,
        unpackConfig,
        unpackCallback);
    }

    /// <summary>
    /// Get the progress for processing the preview video for an AR Recording.
    /// </summary>
    /// <returns>a value between 0 and 1</returns>
    public float PreviewProgress()
    {
      return PInvoke.PreviewProgress(_nativeHandle);
    }

    /// <summary>
    /// Get the progress for processing research data for an AR Recording.
    /// </summary>
    /// <returns>a value between 0 and 1</returns>
    public float ResearchProgress()
    {
      return PInvoke.ResearchProgress(_nativeHandle);
    }

    /// <summary>
    /// Get the progress of unpacking the raw frames.
    /// </summary>
    public float UnpackProgress()
    {
      return PInvoke.UnpackProgress(_nativeHandle);
    }

    /// <summary>
    /// Cancel processing the preview video for an AR recording
    /// Causes the preview callback to be called immediately
    /// </summary>
    public void CancelPreview()
    {
      PInvoke.CancelPreview(_nativeHandle);
    }

    /// <summary>
    /// Cancel processing the research data for an AR recording.
    /// Causes the researchs callback to be called immediately
    /// </summary>
    public void CancelResearch()
    {
      PInvoke.CancelResearch(_nativeHandle);
    }

    /// <summary>
    /// Cancel the processing of unpacking the raw frames.
    /// </summary>
    public void CancelUnpack()
    {
      PInvoke.CancelUnpack(_nativeHandle);
    }

    /// <summary>
    /// Archives temporary AR recording directories into a gzipped .tar
    /// </summary>
    /// <param name="sourceDirectoryPath">
    /// The source directory path to archive.
    /// </param>
    /// <param name="destinationArchivePath">
    /// Target destination archive path.
    /// </param>
    public void ArchiveWorkingDirectory(
      String sourceDirectoryPath,
      String destinationArchivePath)
    {
      ArchiveConfig config = new ArchiveConfig(sourceDirectoryPath, destinationArchivePath);
      PInvoke.ArchiveWorkingDirectory(config);
    }

    /// <summary>
    /// Stores the name of the application.
    /// Calling this method multiple times will add multiple entries to recording data
    /// The recorder *must* be started before calling this method
    /// </summary>
    public void SetApplicationName(String applicationName) {
        ARSetApplicationInfoConfig config = new ARSetApplicationInfoConfig();
        config.ApplicationName = applicationName;
        PInvoke.SetApplicationName(_nativeHandle, config);
    }

    /// <summary>
    /// Stores the point of interest, represented as a string.
    /// Calling this method multiple times will add multiple entries to recording data
    /// The recorder *must* be started before calling this method
    /// </summary>
    public void SetPointOfInterest(String pointOfInterest) {
        ARSetPointOfInterestConfig config = new ARSetPointOfInterestConfig();
        config.PointOfInterest = pointOfInterest;
        PInvoke.SetPointOfInterest(_nativeHandle, config);
    }

    private void ReleaseUnmanagedResources()
    {
      if (_nativeHandle.Handle != IntPtr.Zero)
      {
        PInvoke.Dtor(_nativeHandle);
        _nativeHandle = new HandleRef();
      }
    }

    public void Dispose()
    {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }

    #pragma warning disable
    /// <summary>
    /// The pinvokes that call down to the native side of the code.
    /// </summary>
    private static class PInvoke
    {
      /// <summary>
      /// Constructs the recorder.
      /// </summary>
      /// <param name="uuidPtr">Ptr to the UUID in memory.</param>
      /// <returns>
      /// The native ptr to this object (or a ptr that can be used with the native side code).
      /// </returns>
      public static unsafe IntPtr Ctor(void* uuidPtr)
      {
        return _NARRecorder_Init(uuidPtr);
      }

      /// <summary>
      /// Destroys the recorder.
      /// </summary>
      /// <param name="nativeHandle">The handle to the native object.</param>
      public static void Dtor(HandleRef nativeHandle)
      {
        _NARRecorder_Release(nativeHandle.Handle);
      }

      /// <summary>
      /// Starts recording on the given recorder.
      /// </summary>
      /// <param name="nativeHandle">The native handle to the recorder.</param>
      /// <param name="config">The config to use for recording.</param>
      public static void Start(HandleRef nativeHandle, ARRecorderConfig config)
      {
        _NARRecorder_Start(nativeHandle.Handle, config);
      }

      [SuppressUnmanagedCodeSecurity]
      [MonoPInvokeCallback(typeof(_NARRecorder_Preview_Callback))]
      private unsafe static void PreviewCallbackDispatch(
        IntPtr callbackPtr,
        ARRecordingPreviewResults results)
      {
        // This method will be called as the dispatcher for the stop callback. It will convert the
        // callback ptr that was round tripped through native into the correct callback type again.
        var safeHandle = SafeGCHandle<Action<ARRecordingPreviewResults>>.FromIntPtr(callbackPtr);
        var callback = safeHandle.TryGetInstance();

        if (callback == null)
        {
          throw new InvalidOperationException(
            "Callback was called with an invalid callback dispatch");
        }

        // Free the handle to the callback to prevent memory leaks.
        safeHandle.Free();

        ARRecordingPreviewResults resultsCopy = results;

        // Callback needs to happen on the unity thread, so enqueue it on the callback queue.
        _CallbackQueue.QueueCallback(() => callback(resultsCopy));
      }

      [SuppressUnmanagedCodeSecurity]
      [MonoPInvokeCallback(typeof(_NARRecorder_Research_Callback))]
      private unsafe static void ResearchCallbackDispatch(
        IntPtr callbackPtr,
        ARRecordingResearchResults results)
      {
        // This method will be called as the dispatcher for the stop callback. It will convert the
        // callback ptr that was round tripped through native into the correct callback type again.
        var safeHandle = SafeGCHandle<Action<ARRecordingResearchResults>>.FromIntPtr(callbackPtr);
        var callback = safeHandle.TryGetInstance();

        if (callback == null)
        {
          throw new InvalidOperationException(
            "Callback was called with an invalid callback dispatch");
        }

        // Free the handle to the callback to prevent memory leaks.
        safeHandle.Free();

        ARRecordingResearchResults resultsCopy = results;

        // Callback needs to happen on the unity thread, so enqueue it on the callback queue.
        _CallbackQueue.QueueCallback(() => callback(resultsCopy));
      }

      [SuppressUnmanagedCodeSecurity]
      [MonoPInvokeCallback(typeof(_NARRecorder_Research_Callback))]
      private unsafe static void UnpackCallbackDispatch(
        IntPtr callbackPtr,
        ARRecordingUnpackResults results)
      {
        // This method will be called as the dispatcher for the stop callback. It will convert the
        // callback ptr that was round tripped through native into the correct callback type again.
        var safeHandle = SafeGCHandle<Action<ARRecordingUnpackResults>>.FromIntPtr(callbackPtr);
        var callback = safeHandle.TryGetInstance();

        if (callback == null)
        {
          throw new InvalidOperationException(
            "Callback was called with an invalid callback dispatch");
        }

        // Free the handle to the callback to prevent memory leaks.
        safeHandle.Free();

        ARRecordingUnpackResults resultsCopy = results;

        // Callback needs to happen on the unity thread, so enqueue it on the callback queue.
        _CallbackQueue.QueueCallback(() => callback(resultsCopy));
      }

      /// <summary>
      /// Stops recording on the native side code.
      /// </summary>
      /// <param name="nativeHandle">The native recorder to stop recording on.</param>
      /// <param name="previewConfig">Preview config for recordings.</param>
      /// <param name="previewCallback">
      /// The callback to call once preview post-processing on the recording has finished.
      /// </param>
      /// <param name="researchConfig">Research config for recordings.</param>
      /// <param name="researchCallback">
      /// The callback to call once research post-processing on the recording has finished.
      /// </param>
      public static void Stop(
        HandleRef nativeHandle,
        ARRecordingPreviewConfig previewConfig,
        Action<ARRecordingPreviewResults> previewCallback,
        ARRecordingResearchConfig researchConfig,
        Action<ARRecordingResearchResults> researchCallback,
        ARRecordingUnpackConfig unpackConfig,
        Action<ARRecordingUnpackResults> unpackCallback)
      {
        // Grab a GC handle to the callback and pass it through the dispatch callback version.
        var previewCallbackGCHandle = SafeGCHandle.Alloc(previewCallback);
        var researchCallbackGCHandle = SafeGCHandle.Alloc(researchCallback);
        var unpackCallbackGCHandle = SafeGCHandle.Alloc(unpackCallback);

        unsafe
        {
          _NARRecorder_Stop(
            nativeHandle.Handle,
            previewConfig,
            PreviewCallbackDispatch,
            previewCallbackGCHandle.ToIntPtr(),
            researchConfig,
            ResearchCallbackDispatch,
            researchCallbackGCHandle.ToIntPtr(),
            unpackConfig,
            UnpackCallbackDispatch,
            unpackCallbackGCHandle.ToIntPtr());
        }
      }

      /// <summary>
      /// Get the progress for processing the preview video for an AR Recording.
      /// </summary>
      /// <returns>a value between 0 and 1</returns>
      public static float PreviewProgress(HandleRef nativeHandle)
      {
        return _NARRecorder_PreviewProgress(nativeHandle.Handle);
      }

      /// <summary>
      /// Get the progress for processing research data for an AR Recording.
      /// </summary>
      /// <returns>a value between 0 and 1</returns>
      public static float ResearchProgress(HandleRef nativeHandle)
      {
        return _NARRecorder_ResearchProgress(nativeHandle.Handle);
      }

      /// <summary>
      /// Get the progress of unpacking frames into a directory.
      /// </summary>
      /// <returns>a value between 0 and 1</returns>
      public static float UnpackProgress(HandleRef nativeHandle)
      {
        return _NARRecorder_UnpackProgress(nativeHandle.Handle);
      }

      /// <summary>
      /// Cancel processing the preview video for an AR recording
      /// Causes the preview callback to be called immediately
      /// </summary>
      public static void CancelPreview(HandleRef nativeHandle)
      {
        _NARRecorder_CancelPreview(nativeHandle.Handle);
      }

      /// <summary>
      /// Cancel processing the research data for an AR recording.
      /// Causes the researchs callback to be called immediately
      /// </summary>
      public static void CancelResearch(HandleRef nativeHandle)
      {
        _NARRecorder_CancelResearch(nativeHandle.Handle);
      }

      /// <summary>
      /// Cancel the processing of unpacking AR frames into a directory.
      /// </summary>
      public static void CancelUnpack(HandleRef nativeHandle)
      {
        _NARRecorder_CancelUnpack(nativeHandle.Handle);
      }

      /// <summary>
      /// Archives temporary AR recording directories into a gzipped .tar
      /// </summary>
      public static void ArchiveWorkingDirectory(ArchiveConfig config)
      {
        _NARRecorder_ArchiveWorkingDirectory(config);
      }

      /// <summary>
      /// Stores the name of the application.
      /// Calling this method multiple times will add multiple entries to recording data
      /// The recorder *must* be started before calling this method
      /// </summary>
      public static void SetApplicationName(HandleRef nativeHandle, ARSetApplicationInfoConfig config) {
        _NARRecorder_SetApplicationName(nativeHandle.Handle, config);
      }

      /// <summary>
      /// Stores the point of interest, represented as a string.
      /// Calling this method multiple times will add multiple entries to recording data
      /// The recorder *must* be started before calling this method
      /// </summary>
      public static void SetPointOfInterest(HandleRef nativeHandle, ARSetPointOfInterestConfig config) {
        _NARRecorder_SetPointOfInterest(nativeHandle.Handle, config);
      }

      [SuppressUnmanagedCodeSecurity]
      private unsafe delegate void _NARRecorder_Preview_Callback(
        IntPtr context,
        ARRecordingPreviewResults results);

      [SuppressUnmanagedCodeSecurity]
      private unsafe delegate void _NARRecorder_Research_Callback(
        IntPtr context,
        ARRecordingResearchResults results);

      [SuppressUnmanagedCodeSecurity]
      private unsafe delegate void _NARRecorder_Unpack_Callback(
        IntPtr context,
        ARRecordingUnpackResults results);

      [DllImport(_ARDKLibrary.libraryName)]
      public static extern unsafe IntPtr _NARRecorder_Init(void* uuidPtr);

      [DllImport(_ARDKLibrary.libraryName)]
      public static extern void _NARRecorder_Release(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      [SuppressUnmanagedCodeSecurity]
      public static extern void _NARRecorder_Start(IntPtr nativeHandle, ARRecorderConfig config);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARRecorder_Stop(
        IntPtr nativeHandle,
        ARRecordingPreviewConfig previewConfig,
        _NARRecorder_Preview_Callback previewCallback,
        IntPtr previewApplicationContext,
        ARRecordingResearchConfig researchConfig,
        _NARRecorder_Research_Callback researchCallback,
        IntPtr researchApplicationContext,
        ARRecordingUnpackConfig unpackConfig,
        _NARRecorder_Unpack_Callback unpackCallback,
        IntPtr unpackApplicationContext);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern float _NARRecorder_PreviewProgress(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern float _NARRecorder_ResearchProgress(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern float _NARRecorder_UnpackProgress(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARRecorder_CancelPreview(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARRecorder_CancelResearch(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARRecorder_CancelUnpack(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARRecorder_ArchiveWorkingDirectory(ArchiveConfig archiveConfig);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARRecorder_SetApplicationName(IntPtr nativeHandle, ARSetApplicationInfoConfig config);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARRecorder_SetPointOfInterest(IntPtr nativeHandle, ARSetPointOfInterestConfig config);
    }
  }
}
#pragma warning enable
