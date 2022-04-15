// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.ComponentModel;

using Niantic.ARDK.AR;
using Niantic.ARDK.Utilities.Logging;
using Niantic.ARDK.VirtualStudio.Remote;

namespace Niantic.ARDK.Utilities.Preloading
{
  public static class FeaturePreloaderFactory
  {
    public static IFeaturePreloader Create()
    {
      return _Create();
    }

    public static IFeaturePreloader Create(RuntimeEnvironment env)
    {
      switch (env)
      {
        case RuntimeEnvironment.Mock:
          return new _MockFeaturePreloader();

        case RuntimeEnvironment.Remote:
          if (!_RemoteConnection.IsEnabled)
            return null;

          ARLog._Warn
          (
            "Preloading is not yet supported over Remote. Required features will be downloaded " +
            "to the ARSession when it is run on device."
          );

          return new _MockFeaturePreloader();

        case RuntimeEnvironment.LiveDevice:
#pragma warning disable CS0162
          if (NativeAccess.Mode != NativeAccess.ModeType.Native &&
            NativeAccess.Mode != NativeAccess.ModeType.Testing)
            return null;
#pragma warning restore CS0162

          return new _NativeFeaturePreloader();
      }

      return null;
    }

    private static readonly RuntimeEnvironment[] _bestMatches =
      { RuntimeEnvironment.LiveDevice, RuntimeEnvironment.Remote, RuntimeEnvironment.Mock};

    internal static IFeaturePreloader _Create
    (
      IEnumerable<RuntimeEnvironment> sources = null
    )
    {
      bool triedAtLeast1 = false;

      if (sources != null)
      {
        foreach (var source in sources)
        {
          var possibleResult = Create(source);
          if (possibleResult != null)
            return possibleResult;

          triedAtLeast1 = true;
        }
      }

      if (!triedAtLeast1)
        return _Create(_bestMatches);

      throw new NotSupportedException("None of the provided sources are supported by this build.");
    }
  }
}
