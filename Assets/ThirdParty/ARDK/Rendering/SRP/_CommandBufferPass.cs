// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if ARDK_HAS_URP

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Niantic.ARDK.Rendering.SRP
{
  internal sealed class _CommandBufferPass: ScriptableRenderPass
  {
    public Camera TargetCamera
    {
      get
      {
        return _targetCamera;
      }
    }

    public CommandBuffer CommandBuffer
    {
      get
      {
        return _commandBuffer;
      }
    }

    private Camera _targetCamera;
    private CommandBuffer _commandBuffer;

    public _CommandBufferPass(RenderPassEvent renderPassEvent)
    {
      this.renderPassEvent = renderPassEvent;
    }

    public void Setup(Camera targetCamera, CommandBuffer commandBuffer)
    {
      _targetCamera = targetCamera;
      _commandBuffer = commandBuffer;
    }

    public override void Execute
    (
      ScriptableRenderContext context,
      ref RenderingData renderingData
    )
    {
      context.ExecuteCommandBuffer(_commandBuffer);
    }
  }
}

#endif