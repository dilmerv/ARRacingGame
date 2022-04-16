// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Networking.HLAPI.Data
{
  public struct PacketInfo
  {
    public object Data { get; private set; }
    public IPeer Sender { get; private set; }
    public ReplicationMode ReplicationMode { get; private set; }

    public PacketInfo(object data, IPeer sender, ReplicationMode replicationMode):
      this()
    {
      Data = data;
      Sender = sender;
      ReplicationMode = replicationMode;
    }
  }
}
