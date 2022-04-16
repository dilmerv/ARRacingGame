// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Depth
{
  // The value of position [x, y] in the Data buffer equals how many meters away
  // from the camera the surface in that pixel is, clamped to the range of NearDistance
  // to FarDistance.
  public interface IDepthBuffer: IDataBuffer<float>, IDisposable
  {
    /// The minimum distance from the camera (in meters) captured by this depth buffer.
    /// Depths closer in will be assigned this distance.
    float NearDistance { get; }

    /// The maximum distance from the camera (in meters) captured by this depth buffer.
    /// Depths farther out will be assigned this distance.
    float FarDistance { get; }

    /// Update (or create, if needed) a texture with this depth buffer's data.
    /// @param texture
    ///   Reference to the texture to copy to. This method will create a texture if the reference
    ///   is null.
    /// @param valueConverter
    ///   Defines a function to perform additional processing on the values before pushing
    ///   to the GPU. This is usually used to normalize values for ARGB32 textures.
    /// @returns True if the buffer was successfully copied to the given texture.
    bool CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      FilterMode filterMode = FilterMode.Point,
      Func<float, float> valueConverter = null
    );

    /// Update (or create, if needed) a texture with this depth buffer's data.
    /// @param texture
    ///   Reference to the texture to copy to. This method will create a texture if the reference
    ///   is null.
    /// @returns True if the buffer was successfully copied to the given texture.
    bool CreateOrUpdateTextureRFloat
    (
      ref Texture2D texture,
      FilterMode filterMode = FilterMode.Point
    );

    /// Rotates the depth buffer so it is oriented to the screen.
    /// @note
    ///   The raw buffer, not yet rotated to screen orientation, will be oriented the same as the
    ///   device's raw camera image. In most cases, gravity points to the right.
    /// @returns
    ///   A new depth buffer rotated.
    /// @note
    ///   This method is deprecated and will be removed in a future update.
    ///   Use DepthBufferProcessor's CopyToAlignedTexture to get a texture fitted to the screen.
    IDepthBuffer RotateToScreenOrientation();

    /// Interpolate the depth buffer using the given camera and viewport information. Since the
    /// depth buffer served by an ARFrame was likely generated using a camera image from a previous
    /// frame, always interpolate the buffer in order to get the best depth estimation.
    /// @param arCamera
    ///   ARCamera with the pose to interpolate this buffer to.
    /// @param viewportWidth
    ///   Width of the viewport. In most cases this equals to the rendering camera's pixel width.
    ///   This is used to calculate the new projection matrix.
    /// @param viewportHeight
    ///   Height of the viewport. In most cases this equals to the rendering camera's pixel height.
    ///   This is used to calculate the new projection matrix.
    /// @param backProjectionDistance
    ///   This value sets the normalized distance of the back-projection plane. Lower values result
    ///   in depths more accurate for closer pixels, but pixels further away will move faster
    ///   than they should. Use 0.5f if your subject in the scene is always closer than ~2 meters
    ///   from the device, and use 1.0f if your subject is further away most of the time.
    /// @returns A new IDepthBuffer with data interpolated using the camera and viewport inputs.
    /// @note
    ///   This method is deprecated and will be removed in a future update.
    ///   Use DepthBufferProcessor's CopyToAlignedTexture to get a texture fitted to the screen.
    IDepthBuffer Interpolate
    (
      IARCamera arCamera,
      int viewportWidth,
      int viewportHeight,
      float backProjectionDistance = AwarenessParameters.DefaultBackProjectionDistance
    );

    /// Fits the depth buffer to the given dimensions.
    /// @note
    ///   The returned depth buffer will be rotated to match the screen orientation,
    ///   if it has not been already.
    /// @param viewportWidth
    ///   Width of the viewport. In most cases this equals the screen resolution's width.
    /// @param viewportHeight
    ///   Height of the viewport. In most cases this equals the screen resolution's height.
    /// @returns
    ///   A new buffer sized to the given viewport dimensions,
    ///   and rotated to the screen rotation.
    /// @note
    ///   This method is deprecated and will be removed in a future update.
    ///   Use DepthBufferProcessor's CopyToAlignedTexture to get a texture fitted to the screen.
    IDepthBuffer FitToViewport
    (
      int viewportWidth,
      int viewportHeight
    );

    /// Returns the nearest value to the specified normalized coordinates in the buffer.
    /// @param uv
    ///   Normalized coordinates.
    /// @returns
    ///   The value in the depth buffer at the nearest location to the coordinates.
    float Sample(Vector2 uv);

    /// Returns the nearest value to the specified normalized coordinates in the buffer.
    /// @param uv
    ///   Normalized coordinates.
    /// @param transform
    ///   2D transformation applied to normalized coordinates before sampling.
    ///   This transformation should convert to the depth buffer's coordinate frame.
    /// @returns
    ///   The value in the depth buffer at the nearest location to the
    ///   transformed coordinates.
    float Sample(Vector2 uv, Matrix4x4 transform);
  }
}
