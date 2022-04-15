// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Camera;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public struct CameraTrackingStateChangedArgs:
    IArdkEventArgs
  {
    public CameraTrackingStateChangedArgs(IARCamera camera, TrackingState trackingState):
      this()
    {
      Camera = camera;
      TrackingState = trackingState;
    }

    public IARCamera Camera { get; private set; }
    public TrackingState TrackingState { get; private set; }
  }
}
