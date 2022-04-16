// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.AR;

namespace Niantic.ARDK.VirtualStudio.AR
{
  internal interface _IEditorARSessionMediator:
    ISessionMediator
  {
    IARSession CreateNonLocalSession(Guid stageIdentifier, RuntimeEnvironment runtimeEnvironment);

    IARSession GetSession(Guid stageIdentifier);
  }
}