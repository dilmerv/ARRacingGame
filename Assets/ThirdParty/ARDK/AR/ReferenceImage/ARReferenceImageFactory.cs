// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;

using AOT;

using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.AR.ReferenceImage
{
  /// <summary>
  /// This class contains methods to create IARReferenceImage instances.
  /// </summary>
  public static class ARReferenceImageFactory
  {
    /// Creates a new reference image from raw image buffer (RGBA, BGRA, etc), physical size,
    ///   and orientation.
    /// @param name The name of the image (for identifying unique images upon detection)
    /// @param rawBytes The base image from which to create the reference image
    /// @param width The width of the image in pixels
    /// @param height The height of the image in pixels
    /// @param byteOrderInfo The endianness of each pixel (See ByteOrderInfo)
    /// @param alphaInfo The location of the alpha channel in each pixel (See AlphaInfo)
    /// @param physicalWidth The physical width of the image in meters.
    /// @param orientation The orientation of the provided image. (See Orientation for more
    ///   information)
    /// @note Not supported in Editor.
    /// @note The ARReferenceImage will contain the full image buffer until it is destroyed.
    ///   Unless reuse of the constructed ARReferenceImage is required in the near future, it is
    ///   recommended to destroy images after adding them to a configuration.
    /// @note Currently, only Orientation.Up is supported
    /// @note May return null if creating the reference image fails (not enough features, not supported)
    public static IARReferenceImage Create
    (
      string name,
      byte[] rawBytes,
      int width,
      int height,
      ByteOrderInfo byteOrderInfo,
      AlphaInfo alphaInfo,
      int componentsPerPixel,
      float physicalWidth,
      Orientation orientation = Orientation.Up
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var result =
          _NARReferenceImage_Init
          (
            name,
            rawBytes,
            (UInt64)width,
            (UInt64)height,
            (UInt64)8 /* hard coded 8 bits per component */,
            (UInt64)(8 * componentsPerPixel),
            (UInt64)(width * componentsPerPixel),
            (UInt32)byteOrderInfo,
            (UInt32)alphaInfo,
            (UInt32)orientation,
            physicalWidth
          );

        if (result == IntPtr.Zero)
        {
          ARLog._Error("Failed to create reference image, returning null");
          return null;
        }

        return _NativeARReferenceImage._FromNativeHandle(result);
      }
      #pragma warning disable 0162
      else
      {
        throw new InvalidOperationException("This operation is not supported on this platform.");
      }
      #pragma warning restore 0162
    }

    /// Creates a new reference image from the contents of a JPG image (in byte[] form),
    ///   physical size, and orientation.
    /// @param name The name of the image (for identifying unique images upon detection)
    /// @param rawBytes The JPG image from which to create the reference image
    /// @param physicalWidth The physical width of the image in meters.
    /// @param orientation The orientation of the image (Currently only Up is supported)
    /// @note Not supported in Editor.
    /// @note The ARReferenceImage will contain the full image buffer until it is destroyed.
    ///   Unless reuse of the constructed ARReferenceImage is required in the near future, it is
    ///   recommended to destroy images after adding them to a configuration.
    /// @note WARNING: JPG decompression contains a security vulnerability, only use this constructor
    ///   on a byte buffer that can be verified to be a JPG image (manually uploaded data, server
    ///   verification, etc).
    /// @note May return null if creating the reference image fails (not enough features, not supported)
    public static IARReferenceImage Create
    (
      string name,
      byte[] rawBytesOfJpg,
      int bufferSize,
      float physicalWidth,
      Orientation orientation = Orientation.Up
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var result =
          _NARReferenceImage_InitWithJPG
          (
            name,
            rawBytesOfJpg,
            (UInt64)bufferSize,
            (UInt32)orientation,
            physicalWidth
          );

        if (result == IntPtr.Zero)
        {
          ARLog._Error("Failed to create reference image, returning null");
          return null;
        }

        return _NativeARReferenceImage._FromNativeHandle(result);
      }
      #pragma warning disable 0162
      else
      {
        throw new InvalidOperationException("This operation is not supported on this platform.");
      }
      #pragma warning restore 0162
    }

    /// Creates a new reference image from a JPG file and the physical width
    /// @param name The name of the image (for identifying unique images upon detection)
    /// @param filePath The JPG image from which to create the reference image
    /// @param physicalWidth The physical width of the image in meters.
    /// @param orientation The orientation of the image (Currently only Up is supported)
    /// @note Not supported in Editor.
    /// @note The ARReferenceImage will contain the full image buffer until it is destroyed.
    ///   Unless reuse of the constructed ARReferenceImage is required in the near future, it is
    ///   recommended to destroy images after adding them to a configuration.
    /// @note WARNING: JPG decompression contains a security vulnerability, only use this constructor
    ///   on a file that can be verified to be a JPG image (manually uploaded data, server
    ///   verification, etc).
    /// @note May return null if creating the reference image fails (not enough features, not supported)
    public static IARReferenceImage Create
    (
      string name,
      string filePath,
      float physicalWidth,
      Orientation orientation = Orientation.Up
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var rawBytes = _ReadBytesFromFile(filePath);
        var result =
          _NARReferenceImage_InitWithJPG
          (
            name,
            rawBytes,
            (UInt64)rawBytes.Length,
            (UInt32)orientation,
            physicalWidth
          );

        if (result == IntPtr.Zero)
        {
          ARLog._Error("Failed to create reference image, returning null");
          return null;
        }

        return _NativeARReferenceImage._FromNativeHandle(result);
      }
      #pragma warning disable 0162
      else
      {
        throw new InvalidOperationException("This operation is not supported on this platform.");
      }
      #pragma warning restore 0162
    }

    /// Creates a new reference image from raw image buffer (RGBA, BGRA, etc), physical size,
    ///   and orientation in an async manner.
    /// @param name The name of the image (for identifying unique images upon detection)
    /// @param rawBytes The base image from which to create the reference image
    /// @param width The width of the image in pixels
    /// @param height The height of the image in pixels
    /// @param byteOrderInfo The endianness of each pixel (See ByteOrderInfo)
    /// @param alphaInfo The location of the alpha channel in each pixel (See AlphaInfo)
    /// @param physicalWidth The physical width of the image in meters.
    /// @param completionHandler The action to run upon creating the image
    /// @param orientation The orientation of the provided image. (See Orientation for more
    ///   information)
    /// @note Not supported in Editor.
    /// @note The ARReferenceImage will contain the full image buffer until it is destroyed.
    ///   Unless reuse of the constructed ARReferenceImage is required in the near future, it is
    ///   recommended to destroy images after adding them to a configuration.
    /// @note Currently, only Orientation.Up is supported
    /// @note May execute the handler with a null object if creation fails
    public static void CreateAsync
    (
      string name,
      byte[] rawBytes,
      int width,
      int height,
      ByteOrderInfo byteOrderInfo,
      AlphaInfo alphaInfo,
      int componentsPerPixel,
      float physicalWidth,
      Action<IARReferenceImage> completionHandler,
      Orientation orientation = Orientation.Up
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        if (orientation != Orientation.Up)
          throw new Exception("ARReferenceImage only supports Orientation.Up at the moment");

        _NARReferenceImage_CreateAsync
        (
          name,
          rawBytes,
          (UInt64)width,
          (UInt64)height,
          (UInt64)8 /* hard coded 8 bits per component */,
          (UInt64)(8 * componentsPerPixel),
          (UInt64)(width * componentsPerPixel),
          (UInt32)byteOrderInfo,
          (UInt32)alphaInfo,
          (UInt32)orientation,
          physicalWidth,
          SafeGCHandle.AllocAsIntPtr(completionHandler),
          ARReferenceImageCreateAsyncCallback
        );
      }
      #pragma warning disable 0162
      else
      {
        throw new InvalidOperationException("This operation is not supported on this platform.");
      }
      #pragma warning restore 0162
    }


    /// Creates a new reference image from the contents of a JPG image (in byte[] form),
    ///   physical size, and orientation in an async manner.
    /// @param name The name of the image (for identifying unique images upon detection)
    /// @param rawBytes The JPG image from which to create the reference image
    /// @param physicalWidth The physical width of the image in meters.
    /// @param completionHandler The action to run upon creating the image
    /// @param orientation The orientation of the image (Currently only Up is supported)
    /// @note Not supported in Editor.
    /// @note The ARReferenceImage will contain the full image buffer until it is destroyed.
    ///   Unless reuse of the constructed ARReferenceImage is required in the near future, it is
    ///   recommended to destroy images after adding them to a configuration.
    /// @note WARNING: JPG decompression contains a security vulnerability, only use this constructor
    ///   on a byte buffer that can be verified to be a JPG image (manually uploaded data, server
    ///   verification, etc).
    /// @note May execute the handler with a null object if creation fails
    public static void CreateAsync
    (
      string name,
      byte[] rawBytesOfJpg,
      int bufferSize,
      float physicalWidth,
      Action<IARReferenceImage> completionHandler,
      Orientation orientation = Orientation.Up
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        _NARReferenceImage_CreateAsyncWithJPG
        (
          name,
          rawBytesOfJpg,
          (UInt64)bufferSize,
          (UInt32)orientation,
          physicalWidth,
          SafeGCHandle.AllocAsIntPtr(completionHandler),
          ARReferenceImageCreateAsyncCallback
        );
      }
      #pragma warning disable 0162
      else
      {
        throw new InvalidOperationException("This operation is not supported on this platform.");
      }
      #pragma warning restore 0162
    }

    /// Creates a new reference image from a JPG file and the physical width in an async manner
    /// @param name The name of the image (for identifying unique images upon detection)
    /// @param filePath The JPG image from which to create the reference image
    /// @param physicalWidth The physical width of the image in meters.
    /// @param completionHandler The action to run upon creating the image
    /// @param orientation The orientation of the image (Currently only Up is supported)
    /// @note Not supported in Editor.
    /// @note The ARReferenceImage will contain the full image buffer until it is destroyed.
    ///   Unless reuse of the constructed ARReferenceImage is required in the near future, it is
    ///   recommended to destroy images after adding them to a configuration.
    /// @note WARNING: JPG decompression contains a security vulnerability, only use this constructor
    ///   on a file that can be verified to be a JPG image (manually uploaded data, server
    ///   verification, etc).
    /// @note May execute the handler with a null object if creation fails
    public static void CreateAsync
    (
      string name,
      string filePath,
      float physicalWidth,
      Action<IARReferenceImage> completionHandler,
      Orientation orientation = Orientation.Up
    )
    {
      if (NativeAccess.Mode == NativeAccess.ModeType.Native)
      {
        var rawBytes = _ReadBytesFromFile(filePath);
        _NARReferenceImage_CreateAsyncWithJPG
        (
          name,
          rawBytes,
          (UInt64)rawBytes.Length,
          (UInt32)orientation,
          physicalWidth,
          SafeGCHandle.AllocAsIntPtr(completionHandler),
          ARReferenceImageCreateAsyncCallback
        );
      }
      #pragma warning disable 0162
      else
      {
        throw new InvalidOperationException("This operation is not supported on this platform.");
      }
      #pragma warning restore 0162
    }

    internal static _SerializableARReferenceImage _AsSerializable(this IARReferenceImage source)
    {
      var possibleResult = source as _SerializableARReferenceImage;
      if (possibleResult != null)
        return possibleResult;

      var result = new _SerializableARReferenceImage(source.Name, source.PhysicalSize);
      return result;
    }

    private static byte[] _ReadBytesFromFile(string filePath)
    {
#if UNITY_ANDROID
      byte[] rawBytes;

      // This case happens with files built into the apk (ie StreamingAssets), where the file path
      //   takes the format 'jar:file:///data/xxx'. A UnityWebRequest is required to properly handle
      //   the 'jar' prefix. In other cases (ie temporaryCachePath), System.IO.File can be used.
      if (filePath.Contains("://"))
      {
        var www = UnityEngine.Networking.UnityWebRequest.Get(filePath);
        var request = www.SendWebRequest();

        while (!request.isDone)
        {
          // TODO: Shouldn't we add a sleep or something here???
        }

        rawBytes = www.downloadHandler.data;
      }
      else
      {
        rawBytes = System.IO.File.ReadAllBytes(filePath);
      }

      return rawBytes;
#elif UNITY_IOS
      return System.IO.File.ReadAllBytes(filePath);
#else
      throw new NotSupportedException("The current platform is not supported.");
#endif
    }
    
    [MonoPInvokeCallback(typeof(_NARReferenceImage_CreateAsync_Callback))]
    private static void ARReferenceImageCreateAsyncCallback
    (
      IntPtr context,
      IntPtr referenceImageHandle
    )
    {
      var callbackHandle = SafeGCHandle<Action<IARReferenceImage>>.FromIntPtr(context);
      var callback = callbackHandle.TryGetInstance();
      callbackHandle.Free();

      // Happens when callback was deallocated
      if (callback == null)
        return;

      IARReferenceImage referenceImage = null;

      if (referenceImageHandle != IntPtr.Zero)
        referenceImage = _NativeARReferenceImage._FromNativeHandle(referenceImageHandle);

      _CallbackQueue.QueueCallback(() => { callback(referenceImage); });
    }

    private delegate void _NARReferenceImage_CreateAsync_Callback
    (
      IntPtr context,
      IntPtr referenceImageHandle
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARReferenceImage_Init
    (
      string name,
      byte[] imageBytes,
      UInt64 imageWidth,
      UInt64 imageHeight,
      UInt64 bitsPerComponent,
      UInt64 bitsPerPixel,
      UInt64 bytesPerRow,
      UInt32 byteOrderInfo,
      UInt32 alphaInfo,
      UInt32 orientation,
      float physicalWidth
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARReferenceImage_InitWithJPG
    (
      string name,
      byte[] jpgBytes,
      UInt64 jpgBufferSize,
      UInt32 orientation,
      float physicalWidth
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARReferenceImage_CreateAsync
    (
      string name,
      byte[] imageBytes,
      UInt64 imageWidth,
      UInt64 imageHeight,
      UInt64 bitsPerComponent,
      UInt64 bitsPerPixel,
      UInt64 bytesPerRow,
      UInt32 byteOrderInfo,
      UInt32 alphaInfo,
      UInt32 orientation,
      float physicalWidth,
      IntPtr applicationContext,
      _NARReferenceImage_CreateAsync_Callback callback
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NARReferenceImage_CreateAsyncWithJPG
    (
      string name,
      byte[] jpgBytes,
      UInt64 jpgBufferSize,
      UInt32 orientation,
      float physicalWidth,
      IntPtr applicationContext,
      _NARReferenceImage_CreateAsync_Callback callback
    );
  }
}
