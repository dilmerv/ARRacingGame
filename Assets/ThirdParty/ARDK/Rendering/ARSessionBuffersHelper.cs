// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Awareness.Depth;

#if ARDK_HAS_URP
using Niantic.ARDK.Rendering.SRP;
#endif

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.Rendering
{
  public static class ARSessionBuffersHelper
  {
    /// Adds an IssuePluginEventAndData step to the provided commandBuffer. This method must
    /// be called and the given command buffer must be executed in order to receive AR updates
    /// on Android devices.
    /// @param commandBuffer The command buffer to add the IssuePluginEventAndData step to.
    /// @param arSession The AR session to fetch updates for.
    public static void IssuePluginEventAndData
    (
      this CommandBuffer commandBuffer,
      IARSession arSession
    )
    {
      if (arSession is _NativeARSession nativeSession)
        nativeSession.SetupCommandBuffer(commandBuffer);
    }

    public static CommandBuffer ConstructBackgroundBuffer
    (
      ARCameraFeed cameraFeed,
      bool clearRenderTarget = true
    )
    {
      return ConstructBackgroundBuffer(cameraFeed, Vector2.one, Vector2.zero, clearRenderTarget);
    }

    public static CommandBuffer ConstructBackgroundBuffer
    (
      ARCameraFeed cameraFeed,
      Vector2 scale,
      Vector2 offset,
      bool clearRenderTargetDepth = true
    )
    {
      var cb = new CommandBuffer();
      // Label this pass in Unity's Frame Debug utility
      cb.name = "[ARDK] RenderARCamera";

      // Clear to a default background color.
      cb.ClearRenderTarget(clearRenderTargetDepth, true, Color.cyan);

      // Wait for GPUFence to pass (ie wait for ARCameraFeed to update)
      // then blit to screen.
#if UNITY_2019_1_OR_NEWER
      cb.WaitOnAsyncGraphicsFence(cameraFeed.VideoFeedFence);
#else
      cb.WaitOnGPUFence(cameraFeed.VideoFeedFence);
#endif

      if (scale != Vector2.one || offset != Vector2.zero)
      {
       cb.Blit
       (
         cameraFeed.VideoFull,
         BuiltinRenderTextureType.CurrentActive,
         scale,
         offset
       );
      }
      else
      {
       cb.Blit(cameraFeed.VideoFull, BuiltinRenderTextureType.CurrentActive);
      }

      return cb;
    }

    public static void AddBackgroundBuffer(Camera camera, CommandBuffer commandBuffer)
    {
      if (camera == null)
        throw new ArgumentNullException(nameof(camera));

      if (commandBuffer == null)
        throw new ArgumentNullException(nameof(commandBuffer));

#if ARDK_HAS_URP
      if (_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
      {
        var rendererFeature = GetFeature();

        if (rendererFeature != null)
        {
          rendererFeature.SetupBackgroundPass(camera, commandBuffer);

          // The render pass is enabled here to keep the effects of this
          // method the same whether the URP is enabled or not
          SetCameraRenderPassEnabled(true);
          rendererFeature.SetActive(true);
        }

        return;
      }
#endif

      camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, commandBuffer);
    }

    public static void AddAfterRenderingBuffer(Camera camera, CommandBuffer commandBuffer)
    {
      if (camera == null)
        throw new ArgumentNullException(nameof(camera));

      if (commandBuffer == null)
        throw new ArgumentNullException(nameof(commandBuffer));

#if ARDK_HAS_URP
      if (_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
      {
        var rendererFeature =
          _RenderPipelineInternals.GetFeatureOfType<ARSessionFeature>();

        if (rendererFeature != null)
          rendererFeature.SetupAfterRenderingPass(camera, commandBuffer);

        return;
      }
#endif

      camera.AddCommandBuffer(CameraEvent.AfterEverything, commandBuffer);
    }

    public static void RemoveBackgroundBuffer(Camera camera, CommandBuffer commandBuffer)
    {
#if ARDK_HAS_URP
      if (_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
      {
        var rendererFeature = GetFeature();

        if (rendererFeature != null)
          rendererFeature.RemoveBackgroundPass();

        return;
      }
#endif

      if (camera == null)
      {
        var msg =
          "Camera is null. If the camera was destroyed, you don't need to explicitly remove " +
          "this command buffer.";
        throw new ArgumentNullException(nameof(camera), msg);
      }

      if (commandBuffer == null)
        throw new ArgumentNullException(nameof(commandBuffer));

      var cbHandle = commandBuffer;
      if (cbHandle != null)
        camera.RemoveCommandBuffer(CameraEvent.BeforeForwardOpaque, cbHandle);
    }

    public static void RemoveAfterRenderingBuffer(Camera camera, CommandBuffer commandBuffer)
    {
#if ARDK_HAS_URP
      if (_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
      {
        var rendererFeature = GetFeature();

        if (rendererFeature != null)
          rendererFeature.RemoveNativePass();

        return;
      }
#endif

      if (camera == null)
      {
        var msg =
          "Camera is null. If the camera was destroyed, you don't need to explicitly remove " +
          "this command buffer.";
        throw new ArgumentNullException(nameof(camera), msg);
      }

      if (commandBuffer == null)
        throw new ArgumentNullException(nameof(commandBuffer));

      var cbHandle = commandBuffer;
      if (cbHandle != null)
        camera.RemoveCommandBuffer(CameraEvent.AfterEverything, cbHandle);
    }

#if ARDK_HAS_URP
    public static void SetCameraRenderPassEnabled(bool isEnabled)
    {
      var rendererFeature = GetFeature();

      if (rendererFeature != null)
        rendererFeature.settings.IsBackgroundPassEnabled = isEnabled;
    }

    private static ARSessionFeature GetFeature()
    {
      var feature =
        _RenderPipelineInternals.GetFeatureOfType<ARSessionFeature>();

      if (feature == null)
      {
        var message =
          "No ARSessionFeature was found added to the " +
          "active Universal Render Pipeline Renderer.";

        Debug.LogError(message);
        return null;
      }

      return feature;
    }
#endif
  }
}
