// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Niantic.ARDK.Utilities;
using Niantic.ARDK.Utilities.Logging;

using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness
{
  /// <summary>
  /// Common functions to be used for inference buffers used by the native and serialized code.
  /// NOTE:
  /// - if these functions are templated, iOS increases in 10% cpu usage
  /// - if raw pointers (unsafe) is used instead of arrays, memory usage significantly increases
  /// </summary>
  internal static class _AwarenessBufferHelper
  {
    // Buffer used to prepare values for depth textures
    [ThreadStatic]
    private static Color[] _bufferCache;

    internal static NativeArray<T> RotateToScreenOrientation<T>
    (
      NativeArray<T> src,
      int srcWidth,
      int srcHeight,
      out int newWidth,
      out int newHeight
    )
    where T: struct, IComparable
    {
      newWidth = srcWidth;
      newHeight = srcHeight;

      // Rotate and/or crop

      Func<int, int, int, int, int> dstIdxFn;
      switch (Screen.orientation)
      {
        case ScreenOrientation.Portrait:
          // CW
          dstIdxFn = (w, _, x, y) => x * w + (w - 1 - y);
          newWidth = srcHeight;
          newHeight = srcWidth;
          break;

        case ScreenOrientation.PortraitUpsideDown:
          // CCW
          dstIdxFn = (w, h, x, y) => (h - 1 - x) * w + y;
          newWidth = srcHeight;
          newHeight = srcWidth;
          break;

        case ScreenOrientation.LandscapeLeft:
          // 180a
          dstIdxFn = (w, h, x, y) => (h - 1 - y) * w + (w - 1 - x);
          break;

        default:
          return new NativeArray<T>(src, Allocator.Persistent);
      }

      var rotatedData =
        new NativeArray<T>
        (
          newWidth * newHeight,
          Allocator.Persistent,
          NativeArrayOptions.UninitializedMemory
        );

      for (var y = 0; y < srcHeight; y++)
      {
        for (var x = 0; x < srcWidth; x++)
        {
          var srcIdx = y * srcWidth + x;
          var dstIdx = dstIdxFn(newWidth, newHeight, x, y);
          rotatedData[dstIdx] = src[srcIdx];
        }
      }

      return rotatedData;
    }

    internal static NativeArray<T> _FitToViewport<T>
    (
      NativeArray<T> src,
      int srcWidth,
      int srcHeight,
      int viewportWidth,
      int viewportHeight,
      out int newWidth,
      out int newHeight
    )
    where T: struct, IComparable
    {
      // Ideally this code is shared between native and serializeable
      var srcRatio = srcWidth / (float)srcHeight;
      var trgRatio = viewportWidth / (float)viewportHeight;

      int cropStartX = 0, cropStartY = 0;

      newWidth = (int)srcWidth;
      newHeight = (int)srcHeight;

      if (srcRatio > trgRatio)
      {
        // Portrait: crop along the width
        newWidth = Mathf.FloorToInt(srcHeight * trgRatio);
        if (newWidth < srcWidth)
          cropStartX = Mathf.FloorToInt((srcWidth - newWidth) / 2f);
      }
      else if (srcRatio != trgRatio)
      {
        // Landscape: crop along the height
        newHeight = Mathf.FloorToInt(srcWidth / trgRatio);
        if (newHeight < srcHeight)
          cropStartY = Mathf.FloorToInt((srcHeight - newHeight) / 2f);
      }

      var newData =
        new NativeArray<T>
        (
          newWidth * newHeight,
          Allocator.Persistent,
          NativeArrayOptions.UninitializedMemory
        );

#if ENABLE_UNITY_COLLECTIONS_CHECKS
      NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref newData, AtomicSafetyHandle.Create());
#endif

      // Crop by copying data with offset
      for (var y = 0; y < newHeight; y++)
      {
        for (var x = 0; x < newWidth; x++)
        {
          var srcIdx = (y + cropStartY) * srcWidth + x + cropStartX;
          var dstIdx = (newHeight - 1 - y) * newWidth + x;

          newData[dstIdx] = src[srcIdx];
        }
      }

      return newData;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureARGB32
    (
      NativeArray<float> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode,
      Func<float, float> valueConverter = null
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.ARGB32)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }
      
      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }
      
      if (texture.width != width || texture.height != height)
        texture.Reinitialize(width, height);

      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // Copy to texture
      _SetColorBuffer(ref _bufferCache, src, valueConverter);
      texture.SetPixels(_bufferCache, 0);

      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureRFloat
    (
      NativeArray<float> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.RFloat)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }

      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.RFloat, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }
      
      if (texture.width != width || texture.height != height)
        texture.Reinitialize(width, height);
      
      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // Copy to texture
      texture.SetPixelData(src, 0);
      
      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureARGB32
    (
      NativeArray<UInt32> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode,
      Func<UInt32, float> valueConverter = null
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.ARGB32)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }

      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.ARGB32, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }

      if (texture.width != width || texture.height != height)
        texture.Reinitialize(width, height);

      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // 32-bit pixel size, in this case use the value converter
      // or fall back to the default ushort to float conversion
      _SetColorBuffer(ref _bufferCache, src, valueConverter);
      texture.SetPixels(_bufferCache, 0);

      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Returns a boolean if input texture has a copy of the observation image buffer.
    /// The texture if successfully copied needs to be deallocated.
    /// Only call on Unity thread.
    /// </summary>
    internal static bool _CreateOrUpdateTextureRFloat
    (
      NativeArray<UInt32> src,
      int width,
      int height,
      ref Texture2D texture,
      FilterMode filterMode
    )
    {
      if (width * height != src.Length)
      {
        ARLog._Error("The specified pixel buffer must match the size of the texture.");
        return false;
      }

      if (texture != null && texture.format != TextureFormat.RFloat)
      {
        ARLog._Error("The texture has already been allocated with a different pixel format.");
        return false;
      }

      if (texture == null)
      {
        texture = new Texture2D(width, height, TextureFormat.RFloat, false, false)
        {
          filterMode = filterMode, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0
        };
      }

      if (texture.width != width || texture.height != height)
        texture.Reinitialize(width, height);

      if (texture.filterMode != filterMode)
        texture.filterMode = filterMode;

      // 32-bit pixel size, copy straight to texture
      texture.SetPixelData(src, 0);

      // Push top GPU
      texture.Apply(false);

      // Success
      return true;
    }

    /// <summary>
    /// Sets the internal color color cache used for texture creation using a source buffer of type Float32.
    /// </summary>
    private static void _SetColorBuffer
    (
      ref Color[] destination,
      NativeArray<float> source,
      Func<float, float> valueConverter = null
    )
    {
      var length = source.Length;
      if (destination == null || destination.Length != length)
        destination = new Color[length];

      var isConversionDefined = valueConverter != null;
      for (var idx = 0; idx < length; idx++)
      {
        var val = isConversionDefined ? valueConverter(source[idx]) : source[idx];
        destination[idx] = new Color(val, val, val, 1);
      }
    }

    /// <summary>
    /// Sets the internal color color cache used for texture creation using a source buffer of type Int16.
    /// If valueConverter is defined, it'll be used to convert the values to 32 bit, otherwise
    /// the method falls back to Convert.ToSingle().
    /// </summary>
    private static void _SetColorBuffer
    (
      ref Color[] destination,
      NativeArray<UInt32> source,
      Func<UInt32, float> valueConverter = null
    ) 
    {
      var length = source.Length;
      if (destination == null || destination.Length != length)
        destination = new Color[length];

      var isConversionDefined = valueConverter != null;
      for (var idx = 0; idx < length; idx++)
      {
        var val = isConversionDefined 
          ? valueConverter(source[idx]) 
          : Convert.ToInt32(source[idx]);
        destination[idx] = new Color(val, val, val, 1);
      }
    }

    #region Math Extensions
    
    /// Produces a matrix that transforms a 3D point from the awareness buffer's
    /// local coordinate space to the world.
    /// @note The matrix returned by this call is essentially the same matrix as
    /// the camera.cameraToWorldMatrix, but the UI rotation is excluded.
    public static Matrix4x4 CalculateCameraToWorldTransform
    (
      this IAwarenessBuffer forBuffer,
      IARCamera camera
    )
    {
      // Infer buffer orientation
      var bufferOrientation = forBuffer.Width > forBuffer.Height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Acquire the view matrix in the orientation of the buffer
      var view = MathUtils.CalculateNarViewMatrix(camera, bufferOrientation);
      
      // The buffer's native coordinate system is upside down compared to
      // Unity's 2D coordinate space.
      InvertVerticalAxis(ref view);

      // Invert to produce a matrix that transforms from camera to world
      return view.inverse;
    }

    /// Calculates a display resolution for the buffer to preserve square pixels.
    /// The result is the smallest resolution to fit the buffer and keep the aspect
    /// ratio defined by the viewport resolution.
    /// @returns
    ///  The display resolution for the buffer, adjusted to viewport orientation.
    ///  The result might be a cropped or padded resolution.
    public static Resolution CalculateDisplayFrame
    (
      this IAwarenessBuffer forBuffer,
      float viewportWidth,
      float viewportHeight
    )
    {
      return MathUtils.CalculateDisplayFrame
      (
        forBuffer.Width,
        forBuffer.Height,
        viewportWidth,
        viewportHeight
      );
    }

    /// Calculates an affine transformation matrix to fit the specified buffer to the specified resolution
    /// @note The buffer's container can be landscape or portrait, but the content of the
    ///       buffer needs to be sensor oriented.
    /// @param forBuffer The buffer to fit to screen.
    /// @param width Target width.
    /// @param height Target height.
    /// @param invertVertically Whether vertical flipping is required (e.g. when the target is the screen).
    /// @returns An affine matrix to be applied to normalized image coordinates.
    public static Matrix4x4 CalculateDisplayTransform
    (
      this IAwarenessBuffer forBuffer,
      int width,
      int height,
      bool invertVertically = false
    )
    {
      // Infer target orientation
      var orientation = width > height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
      
      return MathUtils.CalculateDisplayTransform
      (
        imageWidth: (int)forBuffer.Width,
        imageHeight: (int)forBuffer.Height,
        viewportWidth: width,
        viewportHeight: height,
        viewportOrientation: orientation,
        invertVertically: invertVertically
      );
    }
    
    /// Calculates an affine transformation matrix to fit the specified buffer to the specified resolution
    /// @note The buffer's container can be landscape or portrait, but the content of the
    ///       buffer needs to be sensor oriented.
    /// @param forBuffer The buffer to fit to screen.
    /// @param width Target width.
    /// @param height Target height.
    /// @param orientation Target orientation.
    /// @param invertVertically Whether vertical flipping is required (e.g. when the target is the screen).
    /// @returns An affine matrix to be applied to normalized image coordinates.
    public static Matrix4x4 CalculateDisplayTransform
    (
      this IAwarenessBuffer forBuffer,
      int width,
      int height,
      ScreenOrientation orientation,
      bool invertVertically = false
    )
    {
      return MathUtils.CalculateDisplayTransform
      (
        imageWidth: (int)forBuffer.Width,
        imageHeight: (int)forBuffer.Height,
        viewportWidth: width,
        viewportHeight: height,
        viewportOrientation: orientation,
        invertVertically: invertVertically
      );
    }

    /// Calculates a transformation matrix for the buffer to synchronize its contents
    /// with the camera pose. The transformation produced by this function is agnostic
    /// to the presentation parameters. To also fit the buffer to the rendering viewport,
    /// combine this transform with the one returned by CalculateDisplayTransform().
    /// @param forBuffer The buffer to fit to screen.
    /// @param camera The AR camera.
    /// @param viewOrientation The orientation of the viewport.
    /// @param backProjectionDistance The normalized distance between the near and far view.
    /// @returns An projective matrix to be applied to normalized image coordinates.
    public static Matrix4x4 CalculateInterpolationTransform
    (
      this IAwarenessBuffer forBuffer,
      IARCamera camera,
      ScreenOrientation viewOrientation,
      float backProjectionDistance = 0.9f
    )
    {
      // Inspect buffer
      var aspectRatio = (float)forBuffer.Width / forBuffer.Height;
      var bufferOrientation = aspectRatio > 1.0f
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;

      // Calculate fov
      var fov = 2.0f *
        Mathf.Atan(forBuffer.Height / (2.0f * forBuffer.Intrinsics.FocalLength.y)) *
        Mathf.Rad2Deg;

      // To keep the homography agnostic to the screen, we need to create a
      // projection with a view aspect ratio being the same as the buffer container.
      var projection = Matrix4x4.Perspective
      (
        fov,
        aspectRatio,
        AwarenessParameters.DefaultNear,
        AwarenessParameters.DefaultFar
      );

      // Since the view matrices are rotated to match the interface orientation,
      // we need to rotate them back to match the buffer's orientation.
      var rotation = MathUtils.CalculateViewRotation(from: viewOrientation, to: bufferOrientation);

      // Acquire the reference pose to warp the image from
      var referencePose = (rotation * forBuffer.ViewMatrix).ConvertViewMatrixBetweenNarAndUnity();

      // Acquire the target pose to warp the image to
      var targetPose = MathUtils.CalculateUnityViewMatrix(camera, bufferOrientation);
      
      // We need to flip the vertical axis, because we invert it in the display
      // transform matrix, since Unity's 2D coordinate system starts from the
      // bottom rather than from the top.
      InvertVerticalAxis(ref referencePose);
      InvertVerticalAxis(ref targetPose);

      return MathUtils.CalculateHomography
      (
        referencePose,
        targetPose,
        projection,
        backProjectionDistance
      );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void InvertVerticalAxis(ref Matrix4x4 matrix)
    {
      matrix.m10 *= -1.0f;
      matrix.m11 *= -1.0f;
      matrix.m12 *= -1.0f;
      matrix.m13 *= -1.0f;
    }

    #endregion
  }
}
