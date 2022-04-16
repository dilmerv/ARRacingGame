// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR
{
  internal static class _SessionFrameSharedLogic
  {
    internal static void _MakeSessionFrameBecomeNonCurrent(IARSession session)
    {
      var frame = session.CurrentFrame;
      if (frame == null)
        return;

      var nullableFrameDisposalPolicy = frame.DisposalPolicy;

      ARFrameDisposalPolicy frameDisposalPolicy;
      if (nullableFrameDisposalPolicy.HasValue)
        frameDisposalPolicy = nullableFrameDisposalPolicy.Value;
      else
        frameDisposalPolicy = session.DefaultFrameDisposalPolicy;

      switch (frameDisposalPolicy)
      {
        case ARFrameDisposalPolicy.DisposeOldFrames:
          frame.Dispose();
          break;

        case ARFrameDisposalPolicy.ReleaseImageAndTexturesOfOldFrames:
          frame.ReleaseImageAndTextures();
          break;

        case ARFrameDisposalPolicy.KeepOldFrames:
          break;

        default:
          throw new InvalidOperationException("Unknown ARFrameDisposalPolicy: " + frameDisposalPolicy);
      }
    }
  }
}