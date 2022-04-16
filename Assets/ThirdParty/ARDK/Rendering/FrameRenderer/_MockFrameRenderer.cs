using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Rendering;

namespace Niantic.ARDK.Rendering
{
  internal sealed class _MockFrameRenderer: 
    ARFrameRenderer
  {
    private CommandBuffer _commandBuffer;
    private Texture2D _texture;
    protected override Shader Shader { get; }

    public _MockFrameRenderer(RenderTarget target)
      : base(target)
    {
      Shader = Resources.Load<Shader>("EditorFrame");
    }

    public _MockFrameRenderer
    (
      RenderTarget target,
      float near,
      float far,
      Shader customShader = null
    ) : base(target, near, far)
    {
      Shader = customShader ? customShader : Resources.Load<Shader>("EditorFrame");
      ARLog._Debug("Loaded: " + (Shader != null ? Shader.name : null));
    }

    protected override GraphicsFence? OnConfigurePipeline
    (
      RenderTarget target,
      Resolution targetResolution,
      Resolution sourceResolution,
      Material renderMaterial
    )
    {
      _commandBuffer = new CommandBuffer
      {
        name = "MockFrameRenderer"
      };
      
      _commandBuffer.ClearRenderTarget(true, true, Color.clear);
      _commandBuffer.Blit(null, target.Identifier, renderMaterial);

#if UNITY_2019_1_OR_NEWER
      return _commandBuffer.CreateAsyncGraphicsFence();
#else
      return _commandBuffer.CreateGPUFence();
#endif
    }

    protected override bool OnUpdateState
    (
      IARFrame frame,
      Matrix4x4 projectionTransform,
      Matrix4x4 displayTransform,
      Material material
    )
    {
      if (frame.CapturedImageBuffer == null)
        return false;

      if (_texture == null)
      {
        var resolution = frame.Camera.CPUImageResolution;
        _texture = new Texture2D(resolution.width, resolution.height, TextureFormat.BGRA32, false);
      }

      // Update source textures
      _texture.LoadRawTextureData(frame.CapturedImageBuffer.Planes[0].Data);
      _texture.Apply();

      // Bind the texture and the display transform
      material.SetTexture(PropertyBindings.FullImage, _texture);
      material.SetMatrix(PropertyBindings.DisplayTransform, displayTransform);

      return true;
    }

    protected override void OnAddToCamera(Camera camera)
    {
      ARSessionBuffersHelper.AddBackgroundBuffer(camera, _commandBuffer);
    }

    protected override void OnRemoveFromCamera(Camera camera)
    {
      ARSessionBuffersHelper.RemoveBackgroundBuffer(camera, _commandBuffer);
    }

    protected override void OnIssueCommands()
    {
      Graphics.ExecuteCommandBuffer(_commandBuffer);
    }

    protected override void OnRelease()
    {
      if (_texture != null)
        Object.Destroy(_texture);

      _commandBuffer?.Dispose();
    }
  }
}