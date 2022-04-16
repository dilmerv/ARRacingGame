// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.IO;

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct SessionResultReceivedFromArmArgs:
    IArdkEventArgs
  {
    public SessionResultReceivedFromArmArgs(uint outcome, byte[] details):
      this()
    {
      Outcome = outcome;
      _details = details;
    }

    public uint Outcome { get; private set; }
    
    private readonly byte[] _details;
    public MemoryStream CreateDetailsReader()
    {
      return new MemoryStream(_details, false);
    }
  }
}
