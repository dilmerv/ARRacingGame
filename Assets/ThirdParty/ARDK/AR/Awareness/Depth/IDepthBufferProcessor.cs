using UnityEngine;

namespace Niantic.ARDK.AR.Awareness.Depth
{
  /// An interface that extracts information from a raw depth buffer for game code.
  /// @note Requires that the ARSession is run with IARWorldTrackingConfiguration.DepthFeatures
  ///   set to DepthFeatures.Depth.
  public interface IDepthBufferProcessor: 
    IAwarenessBufferProcessor
  {
    /// The CPU copy of the latest awareness buffer.
    IDepthBuffer AwarenessBuffer { get; }
    
    /// The closest perpendicular depth value to be considered meaningful.
    float MinDepth { get; }

    /// The farthest perpendicular depth value to be considered meaningful.
    float MaxDepth { get; }
    
    /// Returns the eye depth of the specified pixel.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @returns The perpendicular depth from the camera plane if exists or float.PositiveInfinity
    ///   if the depth information is unavailable.
    float GetDepth(int viewportX, int viewportY);

    /// Returns the distance of the specified pixel from the camera origin.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @returns The distance from the camera if exists or float.PositiveInfinity if the depth
    ///   information is unavailable.
    float GetDistance(int viewportX, int viewportY);

    /// Returns the world position of the specified pixel.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @returns World position if exists or Vector3.zero if the depth information is unavailable.
    Vector3 GetWorldPosition(int viewportX, int viewportY);

    /// Returns the surface normal of the specified pixel.
    /// @param viewportX Horizontal coordinate in viewport space.
    /// @param viewportY Vertical coordinate in viewport space.
    /// @returns Normal if exists or Vector3.up if the depth information is unavailable.
    Vector3 GetSurfaceNormal(int viewportX, int viewportY);
    
    /// Pushes the current state of the depth buffer to the
    /// specified target texture. The resulting texture will
    /// contain a display aligned representation of normalized
    /// depth values.
    /// @note Only use this call if you absolutely need the
    ///   texture to be display aligned. It is faster to
    ///   create a texture from the awareness buffer itself.
    /// @param texture The target texture (ARGB32). If this
    ///   texture does not exist, it will be created. It is
    ///   the responsibility of the caller to release this texture.
    /// @param orientation The target orientation of the texture.
    ///   This determines the resolution of the container. This has
    ///   to be either landscape or portrait.
    void CopyToAlignedTextureARGB32(ref Texture2D texture, ScreenOrientation orientation);
    
    /// Pushes the current state of the depth buffer to the
    /// specified target texture. The resulting texture will
    /// contain a display aligned representation of the raw
    /// depth values.
    /// @note Only use this call if you absolutely need the
    ///   texture to be display aligned. It is faster to
    ///   create a texture from the awareness buffer itself.
    /// @param texture The target texture (RFloat). If this
    ///   texture does not exist, it will be created. It is
    ///   the responsibility of the caller to release this texture.
    /// @param orientation The target orientation of the texture.
    ///   This determines the resolution of the container. This has
    ///   to be either landscape or portrait.
    void CopyToAlignedTextureRFloat(ref Texture2D texture, ScreenOrientation orientation);
  }
}
