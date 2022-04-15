using System;

using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace Niantic.ARDK.Rendering
{
  internal sealed class _ARCoreFrameRenderer: 
    ARFrameRenderer
  {
    // Rendering resources
    private CommandBuffer _commandBuffer;
    private Texture2D _nativeTexture;
    private RenderTexture _cachedTexture;
    protected override Shader Shader { get; }
    
    // Resources for caching external textures
    private readonly Shader _blitShader;
    private Material _blitMaterial;

    public _ARCoreFrameRenderer(RenderTarget target)
      : base(target)
    {
      // The blitting shader used for caching
      _blitShader = Resources.Load<Shader>("ExternalBlit");
      
      // The main shader used for rendering the background
      Shader = Resources.Load<Shader>("ARCoreFrame");
    }

    public _ARCoreFrameRenderer
    (
      RenderTarget target,
      float near,
      float far,
      Shader customShader = null
    ) : base(target, near, far)
    {
      // The blitting shader used for caching
      _blitShader = Resources.Load<Shader>("ExternalBlit");
      
      // The main shader used for rendering the background
      Shader = customShader ? customShader : Resources.Load<Shader>("ARCoreFrame");
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
      _blitMaterial = new Material(_blitShader)
      {
        hideFlags = HideFlags.HideAndDontSave
      };
      
      // Allocate the texture cache for double buffering
      _cachedTexture = new RenderTexture
      (
        sourceResolution.width,
        sourceResolution.height,
        0,
        RenderTextureFormat.ARGB32
      )
      {
        useMipMap = false, autoGenerateMips = false, filterMode = FilterMode.Point, anisoLevel = 0
      };
      
      _cachedTexture.Create();
      
      _commandBuffer = new CommandBuffer
      {
        name = "ARCoreFrameRenderer"
      };
      
      _commandBuffer.ClearRenderTarget(true, true, Color.clear);
      _commandBuffer.Blit(null, Target.Identifier, renderMaterial);

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

    protected override bool OnUpdateState
    (
      IARFrame frame,
      Matrix4x4 projectionTransform,
      Matrix4x4 displayTransform,
      Material material
    )
    {
      // We require a single plane image as source
      if (frame.CapturedImageTextures.Length < 1 || frame.CapturedImageTextures[0] == IntPtr.Zero)
        return false;

      // Update the native texture
      CreateOrUpdateExternalTexture
      (
        ref _nativeTexture,
        frame.Camera.ImageResolution,
        TextureFormat.ARGB32,
        frame.CapturedImageTextures[0]
      );
      
      // On Android, the native texture is prone to change
      // during rendering, so we work with a copy that we own.
      _blitMaterial.SetTexture(PropertyBindings.FullImage, _nativeTexture);
      var prevTarget = RenderTexture.active;
      Graphics.Blit(null, _cachedTexture, _blitMaterial);
      RenderTexture.active = prevTarget;

      // Bind texture and the display transform
      material.SetTexture(PropertyBindings.FullImage, _cachedTexture);
      material.SetMatrix(PropertyBindings.DisplayTransform, displayTransform);

      return true;
    }

    protected override void OnIssueCommands()
    {
      Graphics.ExecuteCommandBuffer(_commandBuffer);
    }

    protected override void OnRelease()
    {
      _commandBuffer?.Dispose();
      
      if (_cachedTexture != null)
        Object.Destroy(_cachedTexture);

      if (_nativeTexture != null)
        Object.Destroy(_nativeTexture);
      
      if (_blitMaterial != null)
        Object.Destroy(_blitMaterial);
    }
  }
}
