// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;

namespace Niantic.ARDK.VirtualStudio.Networking.Mock
{
  /// <summary>
  /// A remote representation of a peer.
  /// </summary>
  internal sealed class _MockPeer:
    _Peer
  {
    public Guid StageIdentifier { get; private set; }

    public _MockPeer(Guid peerIdentifier, Guid stageIdentifier):
      base(peerIdentifier)
    {
      StageIdentifier = stageIdentifier;
    }
  }
}
