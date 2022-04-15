// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Anchors;

namespace Niantic.ARDK.VirtualStudio.AR.Mock
{
  /// Base class for mocked anchors. Mocked anchors are only detected in the local ARSession.
  public abstract class MockAnchorBase:
    MockDetectableBase
  {
    private HashSet<Guid> _discoveredInSessions = new HashSet<Guid>();

    internal abstract void CreateAndAddAnchorToSession(_IMockARSession arSession);

    internal abstract void RemoveAnchorFromSession(_IMockARSession arSession);

    protected abstract IARAnchor AnchorData { get; }

    protected abstract bool UpdateAnchorData();

    internal sealed override void BeDiscovered(_IMockARSession arSession, bool isLocal)
    {
      if (!_discoveredInSessions.Contains(arSession.StageIdentifier))
      {
        _discoveredInSessions.Add(arSession.StageIdentifier);
        CreateAndAddAnchorToSession(arSession);
      }
    }

    internal override void OnSessionRanAgain(_IMockARSession arSession)
    {
      if ((arSession.RunOptions & ARSessionRunOptions.RemoveExistingAnchors) != 0)
      {
        _discoveredInSessions.Remove(arSession.StageIdentifier);
        RemoveAnchorFromSession(arSession);
      }
    }

    private void Update()
    {
      // Check every frame to see if anything has changed in this anchor's transform.

      if (AnchorData != null && transform.hasChanged)
      {
        // If something has changed, recalculate and apply the anchor data
        // to reflect the current transform state.
        if (UpdateAnchorData())
        {
          // Now that the anchor data has been updated, notify the session
          // that an update has occurred to this anchor
          UpdateAnchorInSessions(AnchorData);
        }
      }
    }
  }
}