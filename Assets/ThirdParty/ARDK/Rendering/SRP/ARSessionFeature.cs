// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if ARDK_HAS_URP
using System;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Niantic.ARDK.Rendering.SRP;

namespace Niantic.ARDK.Rendering.SRP
{
  public class ARSessionFeature: ScriptableRendererFeature
  {
    [Serializable]
    public class ARSessionFeatureSettings
    {
      public bool IsBackgroundPassEnabled;
    }

    // Must be named "settings" (lowercase) to be shown in the Render Features inspector
    public ARSessionFeatureSettings settings = new ARSessionFeatureSettings();

    private _CommandBufferPass _backgroundPass;
    private _CommandBufferPass _afterRenderingPass;

    public override void Create()
    {
    }

    public void SetupBackgroundPass(Camera camera, CommandBuffer commandBuffer)
    {
      if (camera == null)
        throw new ArgumentNullException(nameof(camera));

      if (commandBuffer == null)
        throw new ArgumentNullException(nameof(commandBuffer));

      _backgroundPass = new _CommandBufferPass(RenderPassEvent.BeforeRenderingOpaques);
      _backgroundPass.Setup(camera, commandBuffer);
    }

    public void SetupAfterRenderingPass(Camera camera, CommandBuffer commandBuffer)
    {
      if (camera == null)
        throw new ArgumentNullException(nameof(camera));

      if (commandBuffer == null)
        throw new ArgumentNullException(nameof(commandBuffer));

      _afterRenderingPass = new _CommandBufferPass(RenderPassEvent.AfterRendering);
      _afterRenderingPass.Setup(camera, commandBuffer);
    }

    public void RemoveBackgroundPass()
    {
      settings.IsBackgroundPassEnabled = false;
      _backgroundPass = null;
    }

    public void RemoveNativePass()
    {
      _afterRenderingPass = null;
    }

    public override void AddRenderPasses
    (
      ScriptableRenderer renderer,
      ref RenderingData renderingData
    )
    {
      if (settings.IsBackgroundPassEnabled)
      {
        if (_backgroundPass != null &&
            renderingData.cameraData.camera == _backgroundPass.TargetCamera)
        {
          renderer.EnqueuePass(_backgroundPass);
        }
      }

      if (_afterRenderingPass != null &&
          renderingData.cameraData.camera == _afterRenderingPass.TargetCamera)
      {
        renderer.EnqueuePass(_afterRenderingPass);
      }
    }
  }
}

#endif
