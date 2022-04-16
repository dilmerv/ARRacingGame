// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.AR.Awareness
{
  public enum AwarenessInitializationStatus
  {
    Unknown,
    Uninitialized,
    Initializing,
    Ready,
    Failed
  }

  public enum AwarenessInitializationError
  {
    // No error occured
    None,

    // Error is unknown
    Unknown,

    // Awareness features cannot initialize because of invalid inputs
    BadInput,

    // Awareness features cannot fetch data needed to initialize
    FetchFailed,

    // Awareness features cannot unpack downloaded data needed to initialize
    UnpackFailed,

    // Awareness features cannot decrypt downloaded data needed to initialize
    DecryptFailed,

    // Awareness features could not perform initialization
    InitializeFailed,

    // Awareness feature is currently initializing
    AlreadyRunning,

    // Awareness feature has bad metadata
    BadMetaData,
  }

  /// Possible error values output by an native AR session
  internal enum _NativeAwarenessInitializationCode
  {
    // No errors
    None = 0,

    // Error is unknown
    Unknown = 1,

    // Awareness features are not initialized yet
    Uninitialized = 2,

    // Awareness features are not ready yet
    NotReady = 3,

    // Awareness features cannot initialize because of invalid inputs
    BadInput = 4,

    // Awareness features cannot fetch data needed to initialize
    FetchFailed = 5,

    // Awareness features cannot unpack downloaded data needed to initialize
    UnpackFailed = 6,

    // Awareness features cannot decrypt downloaded data needed to initialize
    DecryptFailed = 7,

    // Awareness features could not perform initialization
    InitializeFailed = 8,

    // Awareness feature is currently initializing
    AlreadyRunning = 9,

    // Awareness feature has bad metadata
    BadMetaData = 10,
  }

  internal static class _AwarenessInitializationCodeTranslator
  {
    public static AwarenessInitializationStatus ToStatus(this _NativeAwarenessInitializationCode code)
    {
      switch (code)
      {
        case _NativeAwarenessInitializationCode.Uninitialized:
          return AwarenessInitializationStatus.Uninitialized;

        case _NativeAwarenessInitializationCode.NotReady:
          return AwarenessInitializationStatus.Initializing;

        case _NativeAwarenessInitializationCode.None:
          return AwarenessInitializationStatus.Ready;

        default:
          return AwarenessInitializationStatus.Failed;
      }
    }

    public static AwarenessInitializationError ToError(this _NativeAwarenessInitializationCode code)
    {
      switch (code)
      {
        case _NativeAwarenessInitializationCode.Unknown:
          return AwarenessInitializationError.Unknown;

        case _NativeAwarenessInitializationCode.BadInput:
          return AwarenessInitializationError.BadInput;

        case _NativeAwarenessInitializationCode.FetchFailed:
          return AwarenessInitializationError.FetchFailed;

        case _NativeAwarenessInitializationCode.UnpackFailed:
          return AwarenessInitializationError.UnpackFailed;

        case _NativeAwarenessInitializationCode.DecryptFailed:
          return AwarenessInitializationError.DecryptFailed;

        case _NativeAwarenessInitializationCode.InitializeFailed:
          return AwarenessInitializationError.InitializeFailed;

        case _NativeAwarenessInitializationCode.AlreadyRunning:
          return AwarenessInitializationError.AlreadyRunning;

        case _NativeAwarenessInitializationCode.BadMetaData:
          return AwarenessInitializationError.BadMetaData;

        default:
          // Case for codes Ready, Uninitialized, and NotReady
          return AwarenessInitializationError.None;
      }
    }
  }
}
