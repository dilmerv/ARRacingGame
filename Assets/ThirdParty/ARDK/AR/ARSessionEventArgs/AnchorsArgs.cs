// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.ObjectModel;

using Niantic.ARDK.AR.Anchors;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public struct AnchorsArgs:
    IArdkEventArgs
  {
    public AnchorsArgs(IARAnchor[] anchors):
      this()
    {
      Anchors = new ReadOnlyCollection<IARAnchor>(anchors);
    }

    public ReadOnlyCollection<IARAnchor> Anchors { get; private set; }    
  }
}
