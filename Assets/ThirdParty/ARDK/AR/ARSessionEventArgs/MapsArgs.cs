// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.ObjectModel;

using Niantic.ARDK.AR.SLAM;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public struct MapsArgs:
    IArdkEventArgs
  {
    public MapsArgs(IARMap[] maps):
      this()
    {
      Maps = new ReadOnlyCollection<IARMap>(maps);
    }

    public ReadOnlyCollection<IARMap> Maps { get; private set; }
  }
}
