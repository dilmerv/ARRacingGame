// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.AR.Frame;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public struct FrameUpdatedArgs:
    IArdkEventArgs
  {
    public FrameUpdatedArgs(IARFrame frame)
      : this()
    {
      Frame = frame;
    }

    public IARFrame Frame { get; private set; }
  }
}
