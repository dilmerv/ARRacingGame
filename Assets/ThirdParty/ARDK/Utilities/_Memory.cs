// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_WIN
#define UNITY_STANDALONE_DESKTOP
#endif
#if (UNITY_IOS || UNITY_ANDROID || UNITY_STANDALONE_DESKTOP) && !UNITY_EDITOR
#define AR_NATIVE_SUPPORT
#endif

using System;
using System.Runtime.InteropServices;
using Niantic.ARDK.Internals;

namespace Niantic.ARDK.Utilities
{
  internal static class _Memory
  {
    /// Free the specified buffer.
    public static void Free(IntPtr buffer)
    {
#if AR_NATIVE_SUPPORT
      _NARMemory_Free(buffer);
#endif
    }

    // TODO: this is really specialized...make generic
    /// Copies the source into the destination using the size specified in bytes.
    public static void Copy(IntPtr source, UInt64[] destination, UInt64 size)
    {
#if AR_NATIVE_SUPPORT
      _NARMemory_Copy(source, destination, size);
#endif
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMemory_Free(IntPtr buffer);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NARMemory_Copy(IntPtr source, UInt64[] destination, UInt64 size);
  }
}
