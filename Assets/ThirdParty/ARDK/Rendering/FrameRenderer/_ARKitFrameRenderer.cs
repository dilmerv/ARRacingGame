using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace Niantic.ARDK.Rendering
{
  internal sealed class _ARKitFrameRenderer: 
    ARFrameRenderer
  {
    // Rendering resources
    private CommandBuffer _commandBuffer;
    private Texture2D _textureY, _textureCbCr;
    
    protected override Shader Shader { get; }

    public _ARKitFrameRenderer(RenderTarget target)
      : base(target)
    {
      Shader = Resources.Load<Shader>("ARKitFrame");
    }

    public _ARKitFrameRenderer
    (
      RenderTarget target,
      float near,
      float far,
      Shader customShader = null
    ) : base(target, near, far)
    {
      Shader = customShader ? customShader : Resources.Load<Shader>("ARKitFrame");
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
      if (target.IsTargetingTexture)
      {
        // When not targeting a camera, we do not create a command buffer.
        // This is because currently, executing the command buffer manually
        // causes the GPU to hang on iOS. Could be a driver or Unity issue.
        return null;
      }
      
      _commandBuffer = new CommandBuffer
      {
        name = "ARKitFrameRenderer"
      };
      
      _commandBuffer.ClearRenderTarget(true, true, Color.clear);
      _commandBuffer.Blit(null, target.Identifier, renderMaterial);

#if UNITY_2019_1_OR_NEWER
      return _commandBuffer.CreateAsyncGraphicsFence();
#else
      return _commandBuffer.CreateGPUFence();
#endif
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
      // This call raises GPU exceptions on iOS
      // Graphics.ExecuteCommandBuffer(_commandBuffer);

      // As an alternative, we blit to the target
      var target = Target.RenderTexture;
      BlitToTexture(ref target);
    }

    protected override bool OnUpdateState
    (
      IARFrame frame,
      Matrix4x4 projectionTransform,
      Matrix4x4 displayTransform,
      Material material
    )
    {
      // We require a biplanar input from ARKit
      if (frame.CapturedImageTextures.Length < 2)
        return false;

      var nativeResolution = frame.Camera.ImageResolution;
      var yResolution = nativeResolution;
      var uvResolution = new Resolution
      {
        width = nativeResolution.width / 2, height = nativeResolution.height / 2
      };

      // Update source textures
      CreateOrUpdateExternalTexture
        (ref _textureY, yResolution, TextureFormat.R8, frame.CapturedImageTextures[0]);

      CreateOrUpdateExternalTexture
        (ref _textureCbCr, uvResolution, TextureFormat.RG16, frame.CapturedImageTextures[1]);

      // Bind textures and the display transform
      material.SetTexture(PropertyBindings.YChannel, _textureY);
      material.SetTexture(PropertyBindings.CbCrChannel, _textureCbCr);
      material.SetMatrix(PropertyBindings.DisplayTransform, displayTransform);

      return true;
    }
    
    protected override void OnRelease()
    {
      _commandBuffer?.Dispose();

      if (_textureY != null)
        Object.Destroy(_textureY);

      if (_textureCbCr != null)
        Object.Destroy(_textureCbCr);
    }
  }
}
