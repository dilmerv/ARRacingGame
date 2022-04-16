// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct PeerRemovedArgs:
    IArdkEventArgs
  {
    public PeerRemovedArgs(IPeer peer):
      this()
    {
      Peer = peer;
    }
    
    public IPeer Peer { get; private set; }
  }
}
