// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.Networking.ARNetworkingEventArgs
{
  public struct PeerStateReceivedArgs:
    IArdkEventArgs
  {
    public PeerStateReceivedArgs(IPeer peer, PeerState state):
      this()
    {
      Peer = peer;
      State = state;
    }
    
    public IPeer Peer { get; private set; }
    public PeerState State { get; private set; }
  }
}
