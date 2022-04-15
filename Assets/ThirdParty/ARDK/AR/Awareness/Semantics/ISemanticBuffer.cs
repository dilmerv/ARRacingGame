// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Semantics
{
  public interface ISemanticBuffer : IDataBuffer<UInt32>, IDisposable
  {
    /// The number of channels contained in this buffer.
    uint ChannelCount { get; }

    /// An array of semantic class names, in the order their channels appear in the data.
    string[] ChannelNames { get; }

    /// Get the channel index of a specified semantic class.
    /// @param channelName Name of semantic class.
    /// @returns The index of the specified semantic class, or -1 if the channel does not exist.
    int GetChannelIndex(string channelName);

    /// Get a mask with only the specified channel's bit enabled. Can be used to quickly check if
    /// a channel exists at a particular pixel in this semantic buffer.
    /// @param channelIndex Channel index of the semantic class to mask for.
    /// @returns A mask with only the specified channel's bit enabled.
    UInt32 GetChannelTextureMask(int channelIndex);

    /// Get a mask with only the specified channels' bits enabled. Can be used to quickly check if
    /// a set of channels exists at a particular pixel in this semantic buffer.
    /// @param channelIndices Channel indices of the semantic classes to mask for.
    /// @returns A mask with only the specified channels' bits enabled.
    UInt32 GetChannelTextureMask(int[] channelIndices);

    /// Get a mask with only the specified channel's bit enabled. Can be used to quickly check if
    /// a channel exists at a particular pixel in this semantic buffer.
    /// @param channelName Name of the semantic class to mask for.
    /// @returns A mask with only the specified channel's bit enabled.
    UInt32 GetChannelTextureMask(string channelName);

    /// Get a mask with only the specified channels' bits enabled. Can be used to quickly check if
    /// a set of channels exists at a particular pixel in this semantic buffer.
    /// @param channelNames Names of the semantic classes to mask for.
    /// @returns A mask with only the specified channels' bits enabled.
    UInt32 GetChannelTextureMask(string[] channelNames);

    /// Check if a pixel in this semantic buffer contains a certain channel.
    /// @param x Pixel position on the x-axis.
    /// @param y Pixel position on the y-axis.
    /// @param channelIndex Channel index of the semantic class to look for.
    /// @returns True if the channel exists at the given coordinates.
    bool DoesChannelExistAt(int x, int y, int channelIndex);

    /// Check if a pixel in this semantic buffer contains a certain channel.
    /// @param x Pixel position on the x-axis.
    /// @param y Pixel position on the y-axis.
    /// @param channelName Name of the semantic class to look for.
    /// @returns True if the channel exists at the given coordinates.
    bool DoesChannelExistAt(int x, int y, string channelName);

    /// Check if a pixel in this semantic buffer contains a certain channel.
    /// This method samples the semantics buffer using normalised texture coordinates.
    /// @param uv Normalised texture coordinates. The bottom-left is (0,1); the top-right is (1,0).
    /// @param channelIndex Channel index of the semantic class to look for.
    /// @returns True if the channel exists at the given coordinates.
    bool DoesChannelExistAt(Vector2 uv, int channelIndex);

    /// Check if a pixel in this semantic buffer contains a certain channel.
    /// This method samples the semantics buffer using normalised texture coordinates.
    /// @param uv Normalised texture coordinates. The bottom-left is (0,1); the top-right is (1,0).
    /// @param channelName Name of the semantic class to look for.
    /// @returns True if the channel exists at the given coordinates.
    bool DoesChannelExistAt(Vector2 uv, string channelName);

    /// Check if a pixel in this semantic buffer contains a certain channel.
    /// This method samples the semantics buffer using normalised viewport coordinates.
    /// @param point
    ///   Normalised viewport coordinates. The bottom-left is (0,0); the top-right is (1,1).
    /// @param viewportWidth
    ///   Width of the viewport. In most cases this equals to the rendering camera's pixel width.
    /// @param viewportHeight
    ///   Height of the viewport. In most cases this equals to the rendering camera's pixel height.
    /// @param channelIndex Channel index of the semantic class to look for.
    /// @returns True if the channel exists at the given coordinates.
    [Obsolete("Please use the ARSemanticSegmentationManager to sample the buffer using viewport coordinates.")]
    bool DoesChannelExistAt(Vector2 point, int viewportWidth, int viewportHeight, int channelIndex);

    /// Check if a pixel in this semantic buffer contains a certain channel.
    /// This method samples the semantics buffer using normalised viewport coordinates.
    /// @param point
    ///   Normalised viewport coordinates. The bottom-left is (0,0); the top-right is (1,1).
    /// @param viewportWidth
    ///   Width of the viewport. In most cases this equals to the rendering camera's pixel width.
    /// @param viewportHeight
    ///   Height of the viewport. In most cases this equals to the rendering camera's pixel height.
    /// @param channelName Name of the semantic class to look for.
    /// @returns True if the channel exists at the given coordinates.
    [Obsolete("Please use the ARSemanticSegmentationManager to sample the buffer using viewport coordinates.")]
    bool DoesChannelExistAt(Vector2 point, int viewportWidth, int viewportHeight, string channelName);

    /// Check if a certain channel exists anywhere in this buffer.
    /// @param channelIndex Channel index of the semantic class to look for.
    /// @returns True if the channel exists.
    bool DoesChannelExist(int channelIndex);

    /// Check if a certain channel exists anywhere in this buffer.
    /// @param channelName Name of the semantic class to look for.
    /// @returns True if the channel exists.
    bool DoesChannelExist(string channelName);

    /// Update (or create, if needed) a texture with data of one of this buffer's channels.
    /// @param texture
    ///   Reference to the texture to copy to. This method will create a texture if the reference
    ///   is null.
    /// @param channelIndex
    ///   Channel index of the semantic class to copy.
    /// @param filterMode
    ///   Texture filtering mode.
    /// @returns True if the buffer was successfully copied to the given texture.
    bool CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      int channelIndex,
      FilterMode filterMode = FilterMode.Point
    );
    
    /// Update (or create, if needed) a texture with data composited of multiple channels from this buffer.
    /// @param texture
    ///   Reference to the texture to copy to. This method will create a texture if the reference
    ///   is null.
    /// @param channels
    ///   Semantic channel indices to copy to this texture.
    /// @param filterMode
    ///   Texture filtering mode.
    /// @returns True if the buffer was successfully copied to the given texture.
    bool CreateOrUpdateTextureARGB32
    (
      ref Texture2D texture,
      int[] channels,
      FilterMode filterMode = FilterMode.Point
    );
    
    /// Update (or create, if needed) a texture with this data of the entire buffer.
    /// @param croppedRect
    ///   Rectangle defining how to crop the buffer's data before copying to the texture.
    /// @param texture
    ///   Reference to the texture to copy to. This method will create a texture if the reference
    ///   is null.
    /// @returns True if the buffer was successfully copied to the given texture.
    bool CreateOrUpdateTextureRFloat(ref Texture2D texture, FilterMode filterMode = FilterMode.Point);

    /// Rotates the semantic buffer so it is oriented to the screen
    /// @returns
    ///   A new semantic buffer rotated.
    [Obsolete("Please use the ARSemanticSegmentationManager to create a texture fitted to the screen.")]
    ISemanticBuffer RotateToScreenOrientation();

    /// Interpolate the semantic buffer using the given camera and viewport information. Since the
    /// semantic buffer served by an ARFrame was likely generated using a camera image from a previous
    /// frame, always interpolate the buffer in order to get the best semantic segmentation output.
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
    ///   in outputs more accurate for closer pixels, but pixels further away will move faster
    ///   than they should. Use 0.5f if your subject in the scene is always closer than ~2 meters
    ///   from the device, and use 1.0f if your subject is further away most of the time.
    /// @returns A new semantic buffer with data interpolated using the camera and viewport inputs.
    [Obsolete("Please use the ARSemanticSegmentationManager to create a texture fitted to the screen.")]
    ISemanticBuffer Interpolate
    (
      IARCamera arCamera,
      int viewportWidth,
      int viewportHeight,
      float backProjectionDistance = AwarenessParameters.DefaultBackProjectionDistance
    );

    /// Sizes the semantic buffer to the given dimensions.
    /// @param viewportWidth
    ///   Width of the viewport. In most cases this equals to the rendering camera's pixel width.
    /// @param viewportHeight
    ///   Height of the viewport. In most cases this equals to the rendering camera's pixel height.
    /// @returns
    ///   A new buffer sized to the given viewport dimensions,
    ///   and rotated to the screen rotation
    [Obsolete("Please use the ARSemanticSegmentationManager to create a texture fitted to the screen.")]
    ISemanticBuffer FitToViewport
    (
      int viewportWidth,
      int viewportHeight
    );
    
    /// Returns the nearest value to the specified normalized coordinates in the buffer.
    /// @param uv
    ///   Normalized coordinates.
    /// @returns
    ///   The value in the semantic buffer at the nearest location to the coordinates.
    UInt32 Sample(Vector2 uv);

    /// Returns the nearest value to the specified normalized coordinates in the buffer.
    /// @param uv
    ///   Normalized coordinates.
    /// @param transform
    ///   2D transformation applied to normalized coordinates before sampling.
    ///   This transformation should convert to the depth buffer's coordinate frame.
    /// @returns
    ///   The value in the semantic buffer at the nearest location to the
    ///   transformed coordinates.
    UInt32 Sample(Vector2 uv, Matrix4x4 transform);
  }
}
