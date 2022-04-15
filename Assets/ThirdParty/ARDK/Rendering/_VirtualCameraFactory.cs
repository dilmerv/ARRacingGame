// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using UnityEngine.Rendering;

namespace Niantic.ARDK.Rendering
{
  /// <summary>
  /// Helper for creating a <see cref="_VirtualCamera"/>.
  /// </summary>
  internal static class _VirtualCameraFactory
  {
    /// <summary>
    /// Creates a virtual camera that will automatically render every unity frame.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to execute on the camera.</param>
    /// <param name="renderOrder">When in the render stack this virtual camera should be rendered.</param>
    /// <returns>The newly created <see cref="_VirtualCamera"/></returns>
    public static _VirtualCamera CreateContinousVirtualCamera
      (CommandBuffer commandBuffer, int renderOrder = Int32.MinValue)
    {
      return new _VirtualCamera(camera => {}, commandBuffer, renderOrder);
    }

    /// <summary>
    /// Creates a virtual camera that will need to be told explicitly when to render by the user.
    /// </summary>
    /// <param name="commandBuffer">The command buffer to run on render.</param>
    /// <param name="renderOrder">When in the render stack this virtual camera should be rendered.</param>
    /// <returns>The newly created <see cref="_VirtualCamera"/></returns>
    public static _VirtualCamera CreateSnapshotVirtualCamera
      (CommandBuffer commandBuffer, int renderOrder = Int32.MinValue)
    {
      return new _VirtualCamera(camera => camera.enabled = false, commandBuffer, renderOrder);
    }
  }
}
