// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.Networking.ARNetworkingEventArgs
{
  public struct AnyARNetworkingInitializedArgs:
    IArdkEventArgs
  {
    public AnyARNetworkingInitializedArgs(IARNetworking arNetworking):
      this()
    {
      ARNetworking = arNetworking;
    }

    public IARNetworking ARNetworking { get; private set; }
  }
}
