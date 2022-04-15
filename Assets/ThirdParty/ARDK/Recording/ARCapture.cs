// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif
#if (UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_DESKTOP) && !UNITY_EDITOR
#define AR_NATIVE_SUPPORT
#endif

using System;
using System.Security;
using System.Runtime.InteropServices;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.Internals;

using UnityEngine;

namespace Niantic.ARDK.Recording
{
  /// <summary>
  /// Native interface to the NAR POI AR Capture API (Recorder v2).
  /// </summary>
  public sealed class ARCapture:
    IDisposable
  {
    private HandleRef _nativeHandle;

    public ARCapture(Guid stageIdentifier)
    {
      unsafe
      {
        var stageIdentifierBytes = stageIdentifier.ToByteArray();

        fixed (byte* stageIdentifierPtr = stageIdentifierBytes)
        {
          var nativePtr = PInvoke.Ctor(stageIdentifierPtr);
          _nativeHandle = new HandleRef(this, nativePtr);
        }
      }
    }

    ~ARCapture()
    {
      ReleaseUnmanagedResources();
    }

    /// <summary>
    /// Starts capturing an AR session.
    /// </summary>
    public void Start(ARCaptureConfig captureConfig)
    {
      PInvoke.Start(_nativeHandle, captureConfig);
    }

    /// <summary>
    /// Stops capturing a session.
    /// </summary>
    public void Stop()
    {
      PInvoke.Stop(_nativeHandle);
    }

    /// <summary>
    /// Returns the capture's recording status.
    /// <returns>true if a recording session is active.</returns>
    /// </summary>
    public bool IsRecording()
    {
      return PInvoke.IsRecording(_nativeHandle);
    }

    /// <summary>
    /// Retrieves the capture's recording paths.
    /// The recorder *must* be started before calling this method.
    /// <returns>The recording path config used by the capture if a capture is running, empty paths otherwise</returns>
    /// </summary>
    public ARCaptureConfig GetCapturePaths()
    {
      
      if (IsRecording())
      {
        return PInvoke.GetPaths(_nativeHandle);
      }

      return ARCaptureConfig.Default;
    }

    /// <summary>
    /// Stores the name of the application.
    /// Calling this method multiple times will override previous calls.
    /// The recorder *must* be started before calling this method.
    /// <param name="applicationName">The name of the application.</param>
    /// </summary>
    public void SetApplicationName(String applicationName)
    {
      if (!IsRecording())
      {
        Debug.LogWarning("Calling SetApplicationName() while not recording!");
        return;
      }

      ARCaptureSetMetadataConfig config = new ARCaptureSetMetadataConfig
      {
        Metadata = "{ \"app\": \"" + applicationName + "\"}"
      };

      PInvoke.SetMetadata(_nativeHandle, config);
    }

    /// <summary>
    /// Stores the point of interest, represented as a string.
    /// Calling this method multiple times will override previous calls.
    /// The recorder *must* be started before calling this method.
    /// <param name="pointOfInterest">The identifier of the point of interest.</param>
    /// </summary>
    public void SetPointOfInterest(String pointOfInterest)
    {
      if (!IsRecording())
      {
        Debug.LogWarning("Calling SetPointOfInterest() while not recording!");
        return;
      }

      ARCaptureSetMetadataConfig config = new ARCaptureSetMetadataConfig
      {
        Metadata = "{ \"poi\": \"" + pointOfInterest + "\"}"
      };

      PInvoke.SetMetadata(_nativeHandle, config);
    }

    /// <summary>
    /// Stores free-form, application-specific metadata.
    /// Calling this method multiple times will overwrite elements previously set.
    /// The recorder *must* be started before calling this method.
    /// <param name="jsonMetadata">Well-formed JSON dictionary string.</param>
    /// </summary>
    public void SetJSONMetadata(String jsonMetadata)
    {
      if (!IsRecording())
      {
        Debug.LogWarning("Calling SetJSONMetadata() while not recording!");
        return;
      }

      ARCaptureSetMetadataConfig config = new ARCaptureSetMetadataConfig
      {
        Metadata = "{ \"metadata\": " + jsonMetadata + "}"
      };

      PInvoke.SetMetadata(_nativeHandle, config);
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
#if AR_NATIVE_SUPPORT
        return _NARCapture_Init(uuidPtr);
#else
        return IntPtr.Zero;
#endif
      }

      /// <summary>
      /// Destroys the recorder.
      /// </summary>
      /// <param name="nativeHandle">The handle to the native object.</param>
      public static void Dtor(HandleRef nativeHandle)
      {
#if AR_NATIVE_SUPPORT
        _NARCapture_Release(nativeHandle.Handle);
#endif
      }

      /// <summary>
      /// Starts recording on the given recorder.
      /// </summary>
      /// <param name="nativeHandle">The native handle to the recorder.</param>
      /// <param name="captureConfig">The config object containing paths to working directory and archive file.</param>
      public static void Start(HandleRef nativeHandle, ARCaptureConfig captureConfig)
      {
        
#if AR_NATIVE_SUPPORT
        _NARCapture_Start(nativeHandle.Handle, captureConfig);
#endif
      }

      /// <summary>
      /// Stops recording on the native side code.
      /// </summary>
      /// <param name="nativeHandle">The native recorder to stop recording on.</param>
      public static void Stop(HandleRef nativeHandle)
      {
#if AR_NATIVE_SUPPORT
        _NARCapture_Stop(nativeHandle.Handle);
#endif
      }

      /// <summary>
      /// Stores application-specific metadata.
      /// Calling this method multiple times will overwrite elements previously set.
      /// The recorder *must* be started before calling this method.
      /// <param name="nativeHandle">The native recorder to set metadata on.</param>
      /// <param name="config">The native recorder to stop recording on.</param>
      /// </summary>
      public static void SetMetadata(HandleRef nativeHandle, ARCaptureSetMetadataConfig config) {
#if AR_NATIVE_SUPPORT
        _NARCapture_SetMetadata(nativeHandle.Handle, config);
#endif
      }

      /// <summary>
      /// Returns the capture's recording status.
      /// <param name="nativeHandle">The native recorder to call.</param>
      /// <returns>true if a recording session is active for the provided handle.</returns>
      /// </summary>
      public static bool IsRecording(HandleRef nativeHandle)
      {
#if AR_NATIVE_SUPPORT
        return _NARCapture_IsRecording(nativeHandle.Handle);
#else
        return false;
#endif
      }

      /// <summary>
      /// Returns the capture's recording paths.
      /// The recorder *must* be started before calling this method.
      /// <param name="nativeHandle">The native recorder to call.</param>
      /// <returns>The paths used by the capture session.</returns>
      /// </summary>
      public static ARCaptureConfig GetPaths(HandleRef nativeHandle)
      {
#if AR_NATIVE_SUPPORT
        int bufferSize = 512;
        StringBuilder workingDirectoryPath = new StringBuilder(bufferSize);
        StringBuilder archivePath = new StringBuilder(bufferSize);
        int pathLength = _NARCapture_GetPaths(nativeHandle.Handle, workingDirectoryPath, archivePath, bufferSize);
        if (pathLength > bufferSize)
        {
          // try again with bigger buffer
          bufferSize = pathLength + 8;
          workingDirectoryPath = new StringBuilder(bufferSize);
          archivePath = new StringBuilder(bufferSize);
          _NARCapture_GetPaths(nativeHandle.Handle, workingDirectoryPath, archivePath, bufferSize);
        }
        return new ARCaptureConfig
        {
          WorkingDirectoryPath = workingDirectoryPath.ToString(),
          ArchivePath = archivePath.ToString()
        };
#else
        return ARCaptureConfig.Default;
#endif
      }

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern unsafe IntPtr _NARCapture_Init(void* uuidPtr);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARCapture_Release(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARCapture_Start(IntPtr nativeHandle, ARCaptureConfig captureConfig);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARCapture_Stop(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern void _NARCapture_SetMetadata(IntPtr nativeHandle, ARCaptureSetMetadataConfig config);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern bool _NARCapture_IsRecording(IntPtr nativeHandle);

      [DllImport(_ARDKLibrary.libraryName)]
      private static extern int _NARCapture_GetPaths(IntPtr nativeHandle, StringBuilder workingDirectoryOut, StringBuilder archiveOut, int bufferSize);
    }
  }
}
#pragma warning enable
