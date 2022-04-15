// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

namespace Niantic.ARDK.VirtualStudio.Networking
{
  public abstract class MessageHandlerBase
  {
    public MockPlayer OwningPlayer
    {
      get { return _owningPlayer; }
    }

    private MockPlayer _owningPlayer;

    internal void SetOwningPlayer(MockPlayer player)
    {
      _owningPlayer = player;
    }

    public abstract void HandleMessage(PeerDataReceivedArgs args);
  }
}