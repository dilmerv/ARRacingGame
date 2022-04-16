// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Recording
{
  public enum ARRecordingStatus:
    byte
  {
    Uninitialized = 0,
    Processing = 1,
    Completed = 2,
    Canceled = 3,
    FileCorrupted = 4,
    ConfigError = 5,
    RecordingError = 6,
    EncoderError = 7,
  }
}