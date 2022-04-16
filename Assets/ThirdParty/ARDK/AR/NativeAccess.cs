// Copyright 2021 Niantic, Inc. All Rights Reserved.

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
#define AR_NATIVE_ONLY
#endif

using UnityEngine;

namespace Niantic.ARDK.AR
{
  internal static class NativeAccess
  {
    public enum ModeType
    {
      // This means that native code should be run.
      Native,

      // This means that instead of native code running, shims meant for unit tests should be used instead.
      Testing,

      // This means that instead of native code running, nothing should be done, and if necessary a default value will be returned.
      Disabled,
    }

    // On production devices (android/iOS phones) only native methods are supported.
    // Because this is a const variable, if statements checking it can be optimized out, meaning
    // there is no overhead for using it to control whether native methods are called on those
    // platforms. On other platforms we default to not making any native calls, but can switch to
    // other behaviors when necessary. Unit tests can switch to the testing shims, and playback code
    // can switch to native calls once playback has been set up.
#if AR_NATIVE_ONLY
    public const ModeType Mode = ModeType.Native;
#else
    private static ModeType _mode = ModeType.Disabled;
    public static ModeType Mode
    {
      get
      {
        return _mode;
      }
    }
#endif

    public static void SwitchToNativeImplementation()
    {
#if !AR_NATIVE_ONLY
      _mode = ModeType.Native;
#endif
    }

    public static void SwitchToTestingImplementation()
    {
#if !AR_NATIVE_ONLY
      _mode = ModeType.Testing;
#endif
    }

    public static void SwitchToDefaultImplementation()
    {
#if !AR_NATIVE_ONLY
      _mode = ModeType.Disabled;
#endif
    }
  }
}
