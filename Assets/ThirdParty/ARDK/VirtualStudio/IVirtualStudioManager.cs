// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities;
using Niantic.ARDK.VirtualStudio.AR;
using Niantic.ARDK.VirtualStudio.AR.Networking;
using Niantic.ARDK.VirtualStudio.Networking;

namespace Niantic.ARDK.VirtualStudio
{
  internal interface _IVirtualStudioManager: IDisposable
  {
    _IEditorARSessionMediator ArSessionMediator { get;  }

    _IEditorMultipeerSessionMediator MultipeerMediator { get; }

    _IEditorARNetworkingSessionMediator ArNetworkingMediator { get; }

    MockPlayConfiguration PlayConfiguration { get; }

    MockPlayer LocalPlayer { get; }

    MockPlayer GetPlayer(string playerName);

    MockPlayer GetPlayer(Guid stageIdentifier);

    MockPlayer GetPlayerWithPeer(IPeer peer);

    void InitializeForConfiguration(MockPlayConfiguration playConfiguration);
  }
}