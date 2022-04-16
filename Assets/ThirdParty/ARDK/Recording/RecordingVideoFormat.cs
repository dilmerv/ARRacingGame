// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Recording
{
  public enum RecordingVideoFormat: byte
  {
    None = 0,
    VP8 = 1,
    VP9 = 2,
    VP9TwoPass = 3,
    VP9Lossless = 4,
    VP9LosslessTwoPass = 5,
  }
}