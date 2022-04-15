// Copyright 2021 Niantic, Inc. All Rights Reserved.

namespace Niantic.ARDK.Utilities.VersionUtilities
{
  public static class ARDKGlobalVersion
  {
    private static _IARDKVersion _impl;

    private static _IARDKVersion _Impl
    {
      get
      {
        if (_impl == null)
        {
          _impl = new _NativeARDKVersion();
        }

        return _impl;
      }
    }

    /// @returns
    ///   The ARDK version number.
    public static string GetARDKVersion()
    {
      return _Impl.GetARDKVersion();
    }

    /// @returns
    ///   The name of the ARdk BackEnd (ARBE) server, if a networking session
    ///   is currently successfully connected to one that has made that information
    ///   available.
    public static string GetARBEVersion()
    {
      return _Impl.GetARBEVersion();
    }
  }
}
