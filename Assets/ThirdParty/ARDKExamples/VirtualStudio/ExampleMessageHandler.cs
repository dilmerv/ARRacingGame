// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.VirtualStudio.Networking;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Networking.MultipeerNetworkingEventArgs;

using UnityEngine;

namespace Niantic.ARDKExamples.VirtualStudio
{
  public enum ExampleMessageTag
  {
    AppleCount,
    OrangeCount
  }

  public class ExampleMessageHandler : MessageHandlerBase
  {
    public override void HandleMessage(PeerDataReceivedArgs args)
    {
      var messageTag = (ExampleMessageTag) args.Tag;
      var number = BitConverter.ToInt32(args.CopyData(), 0);;

      Debug.LogFormat
      (
        "Peer {0} wants to buy {1} {2} from peer {3}",
        args.Peer.Identifier,
        number,
        messageTag == ExampleMessageTag.AppleCount ? "apples" : "oranges",
        OwningPlayer.Networking.Self.Identifier
      );
    }
  }
}