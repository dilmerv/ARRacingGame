// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.HLAPI.Object
{
  public struct MessageReceivedEventArgs<TMessage>:
    IArdkEventArgs
  {
    public readonly IPeer Sender;
    public readonly TMessage Message;

    public MessageReceivedEventArgs(IPeer sender, TMessage message)
    {
      Sender = sender;
      Message = message;
    }
  }
}
