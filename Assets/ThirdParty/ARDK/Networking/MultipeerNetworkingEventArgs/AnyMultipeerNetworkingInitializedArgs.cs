// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct AnyMultipeerNetworkingInitializedArgs:
    IArdkEventArgs
  {
    public readonly IMultipeerNetworking Networking;

    public AnyMultipeerNetworkingInitializedArgs(IMultipeerNetworking networking)
    {
      Networking = networking;
    }
  }
}
