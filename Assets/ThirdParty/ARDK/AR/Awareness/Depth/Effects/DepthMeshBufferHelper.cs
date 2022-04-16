// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using UnityEngine;
using UnityEngine.Rendering;

#if ARDK_HAS_URP
using Niantic.ARDK.Rendering.SRP;
#endif

using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.AR.Awareness.Depth.Effects
{
  public static class DepthMeshBufferHelper
  {
    public static void AddCommandBuffer(UnityEngine.Camera camera, CommandBuffer commandBuffer)
    {
      if (camera == null)
        throw new ArgumentNullException(nameof(camera));

      if (commandBuffer == null)
        throw new ArgumentNullException(nameof(commandBuffer));

#if ARDK_HAS_URP
      if (_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
      {
        var feature = GetFeature();
        if (feature != null)
          feature.SetupMeshPass(camera, commandBuffer);
        return;
      }
#endif

      camera.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, commandBuffer);
    }

    public static void RemoveCommandBuffer(UnityEngine.Camera camera, CommandBuffer commandBuffer)
    {
#if ARDK_HAS_URP
      if (_RenderPipelineInternals.IsUniversalRenderPipelineEnabled)
      {
        var feature = GetFeature();
        if (feature != null)
          feature.RemoveMeshPass();
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

#if ARDK_HAS_URP
    private static DepthMeshRendererFeature GetFeature()
    {
      var feature =
        _RenderPipelineInternals.GetFeatureOfType<DepthMeshRendererFeature>();

      if (feature == null)
      {
        var message =
          "No DepthMeshRendererFeature was found added to the " +
          "active Universal Render Pipeline Renderer.";

        ARLog._Error(message);
        return null;
      }

      return feature;
    }
#endif
  }
}
