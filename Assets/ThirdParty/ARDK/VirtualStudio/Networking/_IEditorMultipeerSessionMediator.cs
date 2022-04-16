// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.VirtualStudio.Networking.Mock;

namespace Niantic.ARDK.VirtualStudio.Networking
{
  internal interface _IEditorMultipeerSessionMediator:
    ISessionMediator
  {
    _MockMultipeerNetworking CreateNonLocalSession(Guid stageIdentifier, RuntimeEnvironment source);

    IReadOnlyCollection<_MockMultipeerNetworking> GetConnectedSessions(Guid stageIdentifier);

    _MockMultipeerNetworking GetSession(Guid stageIdentifier);

    IPeer GetHostIfSet(byte[] sessionMetadata);
  }
}