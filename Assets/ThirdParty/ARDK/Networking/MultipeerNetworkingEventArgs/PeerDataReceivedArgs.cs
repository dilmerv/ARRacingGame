// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.IO;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct PeerDataReceivedArgs:
    IArdkEventArgs
  {
    public PeerDataReceivedArgs(IPeer peer, uint tag, TransportType transportType, byte[] data):
      this()
    {
      Peer = peer;
      Tag = tag;
      TransportType = transportType;
      _data = data;
    }

    public IPeer Peer { get; private set; }
    public uint Tag { get; private set; }
    public TransportType TransportType { get; private set; }

    private readonly byte[] _data;
    public int DataLength
    {
      get { return _data.Length; }
    }

    public MemoryStream CreateDataReader()
    {
      return new MemoryStream(_data, false);
    }

    public byte[] CopyData()
    {
      var result = new byte[_data.Length];
      Buffer.BlockCopy(_data, 0, result, 0, _data.Length);
      return result;
    }
  }
}
