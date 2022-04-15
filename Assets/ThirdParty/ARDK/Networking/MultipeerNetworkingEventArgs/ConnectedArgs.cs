// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct ConnectedArgs:
    IArdkEventArgs
  {
    public ConnectedArgs(IPeer self, IPeer host):
      this()
    {
      Self = self;
      Host = host;
    }
    public IPeer Self { get; private set; }
    public IPeer Host { get; private set; }

    public bool IsHost
    {
      get
      {
        return Self.Equals(Host);
      }
    }
  }
}
