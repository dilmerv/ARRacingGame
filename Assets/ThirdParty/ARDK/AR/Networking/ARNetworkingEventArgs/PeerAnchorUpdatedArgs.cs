// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections.ObjectModel;

using Niantic.ARDK.AR.Networking.NetworkAnchors;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;

namespace Niantic.ARDK.AR.Networking.ARNetworkingEventArgs
{
  /// @note This is currently in internal development, and not useable.
  public struct PeerAnchorUpdatedArgs:
    IArdkEventArgs
  {
    public PeerAnchorUpdatedArgs(IARPeerAnchor anchor)
    {
      Anchor = anchor;
    }

    public readonly IARPeerAnchor Anchor;
  }
}
