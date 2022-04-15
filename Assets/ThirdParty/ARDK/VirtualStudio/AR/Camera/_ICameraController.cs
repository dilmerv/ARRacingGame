// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using Niantic.ARDK.AR;
using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR
{
  internal interface _ICameraController:
    IDisposable
  {
    Guid StageIdentifier { get; }

    bool RequiresUpdate { get; }

    /// <summary>
    /// Camera in scene that captures the augmented reality environment.
    /// </summary>
    Camera ARSceneCamera { get; }

    void Move();

    IARFrame CreateFrame();
  }
}
