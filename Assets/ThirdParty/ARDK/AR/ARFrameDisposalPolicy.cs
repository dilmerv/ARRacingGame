// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR
{
  // Maybe this could become a Flags enum about what to retain in the future.
  public enum ARFrameDisposalPolicy
  {
    DisposeOldFrames,
    ReleaseImageAndTexturesOfOldFrames,
    KeepOldFrames
  }
}