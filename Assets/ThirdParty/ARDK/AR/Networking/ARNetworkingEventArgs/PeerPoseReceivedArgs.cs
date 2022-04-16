// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;

using UnityEngine;

namespace Niantic.ARDK.AR.Networking.ARNetworkingEventArgs
{
  public struct PeerPoseReceivedArgs:
    IArdkEventArgs
  {
    public PeerPoseReceivedArgs(IPeer peer, Matrix4x4 pose):
      this()
    {
      Peer = peer;
      Pose = pose;
    }
    
    public IPeer Peer { get; private set; }
    public Matrix4x4 Pose { get; private set; }
  }
}
