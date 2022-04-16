// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Networking;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.VirtualStudio.AR.Networking.Mock;

namespace Niantic.ARDK.VirtualStudio.AR.Networking
{
  internal interface _IEditorARNetworkingSessionMediator:
    ISessionMediator
  {
    _MockARNetworking CreateNonLocalSession(Guid stageIdentifier);

    IReadOnlyCollection<_MockARNetworking> GetConnectedSessions(Guid stageIdentifier);

    _MockARNetworking GetSession(Guid stageIdentifier);
  }
}