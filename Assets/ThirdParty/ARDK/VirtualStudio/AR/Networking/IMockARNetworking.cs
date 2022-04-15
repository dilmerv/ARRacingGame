// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Networking;
using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.AR.Networking.Mock
{
  public interface IMockARNetworking:
    IARNetworking
  {
    void BroadcastPose(Matrix4x4 pose, float deltaTime);
  }
}