using Niantic.ARDK.AR;

using UnityEngine;
using UnityEngine.Rendering;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Rendering
{
    internal sealed class _ARPlaybackFrameRenderer :
      ARFrameRenderer
    {
        private CommandBuffer _commandBuffer;
        private Texture2D _textureY;
        private Texture2D _textureCbCr;

        protected override Shader Shader { get; }

        private Shader PlatformShader
        {
          get =>
            Application.platform == RuntimePlatform.Android
              ? Resources.Load<Shader>("ARCoreFrameBiplanar")
              : Resources.Load<Shader>("ARKitFrame");
        }

        public _ARPlaybackFrameRenderer(RenderTarget target)
          : base(target)
        {
            Shader = PlatformShader;
        }

        public _ARPlaybackFrameRenderer
        (
          RenderTarget target,
          float near,
          float far,
          Shader customShader = null
        ) : base(target, near, far)
        {
          Shader = customShader ? customShader : PlatformShader;
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
                name = "PlaybackFrameRenderer"
            };

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

            if (_textureY == null)
            {
                var resolution = frame.Camera.CPUImageResolution;
                _textureY = new Texture2D(resolution.width, resolution.height, TextureFormat.R8, false);
                _textureCbCr =
                  new Texture2D(resolution.width / 2, resolution.height / 2, TextureFormat.RG16, false);
            }

            // Update source textures
            _textureY.LoadRawTextureData(frame.CapturedImageBuffer.Planes[0].Data);
            _textureCbCr.LoadRawTextureData(frame.CapturedImageBuffer.Planes[1].Data);
            _textureY.Apply();
            _textureCbCr.Apply();

            // Bind the texture and the display transform
            material.SetTexture(PropertyBindings.YChannel, _textureY);
            material.SetTexture(PropertyBindings.CbCrChannel, _textureCbCr);
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
            if (_textureY != null)
                Object.Destroy(_textureY);

            if (_textureCbCr != null)
                Object.Destroy(_textureCbCr);

            _commandBuffer?.Dispose();
        }
    }
}