// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Utilities;

using UnityEngine;
using UnityEngine.Rendering;

using Object = UnityEngine.Object;

namespace Niantic.ARDK.Rendering
{
  /// <summary>
  /// Helper for creating a virtual camera that doesn't render the game world, but instead can be
  /// used by the user to inject rendering commands into Unity's rendering pipeline.
  /// </summary>
  internal sealed class _VirtualCamera: IDisposable
  {
    ~_VirtualCamera()
    {
      ReleaseUnmanagedResources();
    }

    public event Action<_VirtualCamera> OnPreRender = camera => {};
    public event Action<_VirtualCamera> OnPostRender = camera => {};

    private Action<Camera> _setupRealCamera;
    private RealCamera _realCamera;

    private const int GpuCacheLineSize = 128;

    public Camera GetCamera()
    {
      return _realCamera._camera;
    }

    /// <summary>
    /// Creates a new virtual camera.
    /// </summary>
    /// <param name="setupRealCamera">A callback that is invoked to setup the UnityCamera before it is hidden.</param>
    /// <param name="commandBuffer">The command buffer to attach to the unity camera.</param>
    /// <param name="renderOrder">The order in the unity rending pipeline this should be executed in.</param>
    public _VirtualCamera
    (
      Action<Camera> setupRealCamera,
      CommandBuffer commandBuffer,
      int renderOrder = Int32.MinValue
    )
    {
      _realCamera =
        new GameObject("__real_camera__", typeof(RealCamera)).GetComponent<RealCamera>();

      _realCamera.Init();
      setupRealCamera(_realCamera._camera);

      ARSessionBuffersHelper.AddAfterRenderingBuffer(_realCamera._camera, commandBuffer);

      _realCamera.gameObject.hideFlags = HideFlags.HideInHierarchy;
      _realCamera._camera.depth = renderOrder;
      _realCamera._owner = this;

      if (Application.isPlaying)
      {
        Object.DontDestroyOnLoad(_realCamera.gameObject);
      }
    }

    private class RealCamera: MonoBehaviour
    {
      public Camera _camera;

      public _VirtualCamera _owner;

      internal void Init()
      {
        _camera = gameObject.AddComponent<Camera>();
        var _renderBuffer = new RenderTexture(GpuCacheLineSize, 1, 0, RenderTextureFormat.R8);
        _camera.targetTexture = _renderBuffer;
        _camera.cullingMask = 0;
      }

      private void OnPreRender()
      {
        _owner.OnPreRender(_owner);
      }

      private void OnPostRender()
      {
        _owner.OnPostRender(_owner);
      }
    }

    /// <summary>
    /// Forces the virtual camera to render. This command will halt the CPU until all rendering for
    /// this virtual camera is finished.
    /// </summary>
    public void ForceRender()
    {
      _realCamera._camera.Render();
    }

    private void ReleaseUnmanagedResources()
    {
      _CallbackQueue.QueueCallback
      (
        () =>
        {
          if (_realCamera != null)
          {
            // This check is to allow unit tests to run this code
            if (Application.isEditor)
            {
              Object.DestroyImmediate(_realCamera.gameObject);
            }
            else
            {
              Object.Destroy(_realCamera.gameObject);
            }
          }
        }
      );
    }

    public void Dispose()
    {
      ReleaseUnmanagedResources();
      GC.SuppressFinalize(this);
    }
  }
}
