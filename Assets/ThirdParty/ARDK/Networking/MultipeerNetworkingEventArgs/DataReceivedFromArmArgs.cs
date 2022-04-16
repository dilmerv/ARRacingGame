// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.IO;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct DataReceivedFromArmArgs:
    IArdkEventArgs
  {
    public DataReceivedFromArmArgs(uint tag, byte[] data):
      this()
    {
      Tag = tag;
      _data = data;
    }

    public uint Tag { get; private set; }
    
    private readonly byte[] _data;
    public MemoryStream CreateDataReader()
    {
      return new MemoryStream(_data, false);
    }
  }
}
