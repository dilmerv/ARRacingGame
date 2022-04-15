using System;
using System.Runtime.CompilerServices;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.Rendering
{
  /// A render target can either be a camera or an offscreen texture.
  public readonly struct RenderTarget: 
    IEquatable<RenderTarget>
  {
    /// The actual camera as a render target, if any.
    public readonly Camera Camera;

    /// The actual GPU texture as a render target, if any.
    public readonly RenderTexture RenderTexture;

    /// The identifier of this render target.
    public readonly RenderTargetIdentifier Identifier;

    // Whether the target is a Unity camera
    public readonly bool IsTargetingCamera;
    
    // Whether the target is a RenderTexture
    public readonly bool IsTargetingTexture;

    private readonly ScreenOrientation _defaultOrientation;

    /// Creates a render target from the specified camera.
    public RenderTarget(Camera cam)
    {
      Camera = cam;
      IsTargetingCamera = true;

      RenderTexture = null;
      IsTargetingTexture = false;

      Identifier = Camera.targetTexture == null
        ? BuiltinRenderTextureType.CurrentActive  // TODO: what if this is a secondary camera?
        : BuiltinRenderTextureType.CameraTarget;

      _defaultOrientation = Screen.orientation;
    }

    /// Creates a render target from the specified texture.
    public RenderTarget(RenderTexture texture)
    {
      Camera = null;
      IsTargetingCamera = false;

      RenderTexture = texture;
      IsTargetingTexture = true;

      Identifier = new RenderTargetIdentifier(texture);
      
      _defaultOrientation = RenderTexture.width > RenderTexture.height
        ? ScreenOrientation.LandscapeLeft
        : ScreenOrientation.Portrait;
    }

    /// Returns a the resolution of the target, in the function of
    /// the specified screen orientation.
    public Resolution GetResolution(ScreenOrientation forOrientation)
    {
      if (IsTargetingCamera)
      {
        // The camera's resolution is automatically being rotated by Unity,
        // therefore we only need to swap the params if the requested
        // orientation is different than the screen orientation.
#if UNITY_EDITOR
        return new Resolution
        {
          width = Camera.pixelWidth, height = Camera.pixelHeight
        };
#else
        var shouldRotate = !UIOrientationsEqual(forOrientation, Screen.orientation);
        return new Resolution
        {
          width = shouldRotate ? Camera.pixelHeight : Camera.pixelWidth,
          height = shouldRotate ? Camera.pixelWidth : Camera.pixelHeight
        };
#endif
      }
      else
      {
        // If the target is a texture, the resolution params need to be swapped
        // if the specified orientation is different from the texture's native
        // orientation.
        var shouldRotate = !UIOrientationsEqual(forOrientation, _defaultOrientation);
        return new Resolution
        {
          width = shouldRotate ? RenderTexture.height : RenderTexture.width,
          height = shouldRotate ? RenderTexture.width : RenderTexture.height
        };
      }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool UIOrientationsEqual(ScreenOrientation a, ScreenOrientation b)
    {
      if (a == b)
        return true;

      switch (a)
      {
        case ScreenOrientation.Portrait:
        case ScreenOrientation.PortraitUpsideDown:
          return b == ScreenOrientation.Portrait || b == ScreenOrientation.PortraitUpsideDown;

        case ScreenOrientation.LandscapeLeft:
        case ScreenOrientation.LandscapeRight:
          return b == ScreenOrientation.LandscapeLeft || b == ScreenOrientation.LandscapeRight;

        default:
          return false;
      }
    }
    
    public static implicit operator RenderTarget(Camera cam)
    {
      return new RenderTarget(cam);
    }

    public static implicit operator RenderTarget(RenderTexture texture)
    {
      return new RenderTarget(texture);
    }
    
    public bool Equals(RenderTarget other)
    {
      return Identifier.Equals(other.Identifier);
    }

    public override bool Equals(object obj)
    {
      return obj is RenderTarget other && Equals(other);
    }

    public override int GetHashCode()
    {
      return Identifier.GetHashCode();
    }
    
    /// Returns the current screen orientation. When called in the editor,
    /// this property infers the orientation from the screen resolution.
    public static ScreenOrientation ScreenOrientation
    {
      get
      {
#if UNITY_EDITOR
        return Screen.width > Screen.height
          ? UnityEngine.ScreenOrientation.LandscapeLeft
          : UnityEngine.ScreenOrientation.Portrait;
#else
        return Screen.orientation;
#endif
      }
    }
  }
}
