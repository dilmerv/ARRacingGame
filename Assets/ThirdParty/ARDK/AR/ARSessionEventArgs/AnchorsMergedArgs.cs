// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.ObjectModel;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public struct AnchorsMergedArgs:
    IArdkEventArgs
  {
    public AnchorsMergedArgs(IARAnchor parent, IARAnchor[] children):
      this()
    { 
      Parent = parent;
      Children = new ReadOnlyCollection<IARAnchor>(children);
    }

    public IARAnchor Parent { get; private set; }
    public ReadOnlyCollection<IARAnchor> Children { get; private set; }
  }
}
