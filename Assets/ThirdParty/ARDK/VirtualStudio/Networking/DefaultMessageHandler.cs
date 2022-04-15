// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

using UnityEngine;

namespace Niantic.ARDK.VirtualStudio.Networking
{
  public class DefaultMessageHandler :
    MessageHandlerBase
  {
    public override void HandleMessage(PeerDataReceivedArgs args)
    {
      Debug.LogFormat
      (
        "[Message Received] Tag: {0}, Sender: {1}, Data Length: {2}",
        args.Tag,
        args.Peer,
        args.DataLength
      );
    }
  }
}