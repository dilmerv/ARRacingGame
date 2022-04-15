// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.ARSessionEventArgs
{
  public struct ARSessionFailedArgs:
    IArdkEventArgs
  {
    public readonly ARError Error;

    public ARSessionFailedArgs(ARError error)
    {
      Error = error;
    }
  }
}
