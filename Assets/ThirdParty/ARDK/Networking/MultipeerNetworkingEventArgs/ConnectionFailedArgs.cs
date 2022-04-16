// Copyright 2021 Niantic, Inc. All Rights Reserved.

using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.Networking.MultipeerNetworkingEventArgs
{
  public struct ConnectionFailedArgs:
    IArdkEventArgs
  {
    public ConnectionFailedArgs(uint errorCode):
      this()
    {
      ErrorCode = errorCode;
    }
    public uint ErrorCode { get; private set; }
  }
}
