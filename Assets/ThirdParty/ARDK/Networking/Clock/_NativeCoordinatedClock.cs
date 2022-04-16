// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using Niantic.ARDK.Internals;

namespace Niantic.ARDK.Networking.Clock
{
  /// Wrapper around the native layer coordinated clock that gets updates from the multipeer
  /// networking systems.
  internal sealed class _NativeCoordinatedClock:
    ICoordinatedClock
  {
    // Private handles and code to deal with native callbacks and initialization
    private IntPtr _nativeHandle;

    internal _NativeCoordinatedClock(Guid stageIdentifier)
    {
      _nativeHandle = _NAR_CoordinatedClock_Init(stageIdentifier.ToByteArray());
    }

    ~_NativeCoordinatedClock()
    {
      if (_nativeHandle != IntPtr.Zero)
        _NAR_CoordinatedClock_Release(_nativeHandle);
    }

    public long CurrentCorrectedTime
    {
      get
      {
        return _NAR_CoordinatedClock_GetCurrentCorrectedTime(_nativeHandle);
      }
    }

    public CoordinatedClockTimestampQuality SyncStatus
    {
      get
      {
        return (CoordinatedClockTimestampQuality)_NAR_CoordinatedClock_GetSyncStatus(_nativeHandle);
      }
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NAR_CoordinatedClock_Init(byte[] stageIdentifier);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern long _NAR_CoordinatedClock_GetCurrentCorrectedTime(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_CoordinatedClock_Release(IntPtr nativeHandle);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern byte _NAR_CoordinatedClock_GetSyncStatus(IntPtr nativeHandle);
  }
}
