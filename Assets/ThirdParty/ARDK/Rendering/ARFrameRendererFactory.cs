using System;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.AR;
using Niantic.ARDK.VirtualStudio.Remote;

using UnityEngine;

namespace Niantic.ARDK.Rendering
{
  public static class ARFrameRendererFactory
  {
    // Recommended near and far clipping settings for AR rendering
    private const float NEAR_CLIPPING = 0.1f;
    private const float FAR_CLIPPING = 100f;

    /// Create an ARFrameRenderer appropriate for the current device.
    ///
    /// On a mobile device, the attempted order will be LiveDevice, Remote, and finally Mock.
    /// In the Unity Editor, the attempted order will be Remote, then Mock.
    ///
    /// @returns The created renderer, or throws if it was not possible to create a renderer.
    public static ARFrameRenderer Create
    (
      RenderTarget target,
      float nearClipping = NEAR_CLIPPING,
      float farClipping = FAR_CLIPPING
    )
    {
      return _Create(target, null, nearClipping, farClipping);
    }

    private static ARFrameRenderer _Create
    (
      RenderTarget target,
      IEnumerable<RuntimeEnvironment> envs,
      float nearClipping,
      float farClipping
    )
    {
      bool triedAtLeast1 = false;

      if (envs != null)
      {
        foreach (var env in envs)
        {
          var possibleResult = Create(target, env, nearClipping, farClipping);
          if (possibleResult != null)
            return possibleResult;

          triedAtLeast1 = true;
        }
      }

      if (!triedAtLeast1)
        return _Create(target, ARSessionFactory._defaultBestMatches, nearClipping, farClipping);

      throw new NotSupportedException("None of the provided envs are supported by this build.");
    }

    /// Create an ARFrameRenderer with the specified RuntimeEnvironment.
    /// @param env The runtime environment to create the renderer for.
    /// @returns The created renderer, or null if it was not possible to create a renderer.
    public static ARFrameRenderer Create
    (
      RenderTarget target,
      RuntimeEnvironment env,
      float nearClipping = NEAR_CLIPPING,
      float farClipping = FAR_CLIPPING
    )
    {
      ARFrameRenderer result;
      switch (env)
      {
        case RuntimeEnvironment.Default:
          return Create(target, nearClipping, farClipping);

        case RuntimeEnvironment.LiveDevice:
#pragma warning disable CS0162
          if (NativeAccess.Mode != NativeAccess.ModeType.Native && NativeAccess.Mode != NativeAccess.ModeType.Testing)
            return null;

#if UNITY_IOS
          result = new _ARKitFrameRenderer(target, nearClipping, farClipping);
#elif UNITY_ANDROID
          result = new _ARCoreFrameRenderer(target, nearClipping, farClipping);
#else
          return null;
#endif
          break;
#pragma warning restore CS0162

        case RuntimeEnvironment.Playback:
            result = new _ARPlaybackFrameRenderer(target,nearClipping, farClipping);
          break;

        case RuntimeEnvironment.Remote:
          if (!_RemoteConnection.IsEnabled)
            return null;

          result = new _RemoteFrameRenderer(target, nearClipping, farClipping);
          break;

        case RuntimeEnvironment.Mock:
          result = new _MockFrameRenderer(target, nearClipping, farClipping);
          break;

        default:
          throw new InvalidEnumArgumentException(nameof(env), (int)env, env.GetType());
      }

      return result;
    }

    [Obsolete("This method is deprecated, use Create without a resolution specified")]
    public static ARFrameRenderer Create
    (
      RenderTarget target,
      Resolution resolution,
      float nearClipping = NEAR_CLIPPING,
      float farClipping = FAR_CLIPPING
    )
    {
      return _Create(target, null, nearClipping, farClipping);
    }

    [Obsolete("This method is deprecated, use Create without a resolution specified")]
    public static ARFrameRenderer Create
    (
      RenderTarget target,
      Resolution resolution,
      RuntimeEnvironment env,
      float nearClipping = NEAR_CLIPPING,
      float farClipping = FAR_CLIPPING
    )
    {
      return Create(target, env, nearClipping, farClipping);
    }

  }
}
