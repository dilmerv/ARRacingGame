// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if ARDK_HAS_URP
using System;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Niantic.ARDK.Rendering.SRP;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.AR.Awareness.Depth.Effects
{
  public sealed class DepthMeshRendererFeature: ScriptableRendererFeature
  {
    private _CommandBufferPass _pass;

    public override void Create()
    {
    }

    public void SetupMeshPass(UnityEngine.Camera camera, CommandBuffer commandBuffer)
    {
      if (camera == null)
        throw new ArgumentNullException("camera");

      if (commandBuffer == null)
        throw new ArgumentNullException("commandBuffer");

      _pass = new _CommandBufferPass(RenderPassEvent.BeforeRenderingOpaques);
      _pass.Setup(camera, commandBuffer);
      SetActive(true);
    }

    public void RemoveMeshPass()
    {
      _pass = null;
      SetActive(false);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
      if (_pass != null && renderingData.cameraData.camera == _pass.TargetCamera)
        renderer.EnqueuePass(_pass);
    }
  }
}
#endif
