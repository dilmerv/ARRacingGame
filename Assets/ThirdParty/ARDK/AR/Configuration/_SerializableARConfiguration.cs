// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Niantic.ARDK.AR.Awareness.Depth.Generators;
using Niantic.ARDK.Utilities.Collections;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.AR.Configuration
{
  internal abstract class _SerializableARConfiguration:
    IARConfiguration
  {
    public bool IsLightEstimationEnabled { get; set; }
    public WorldAlignment WorldAlignment { get; set; }

    private const string _supportedVideoFormatsLogMessage =
      "ARWorldTrackingConfiguration.SupportedVideoFormats unsupported in Editor or Standalone!";

    public ReadOnlyCollection<IARVideoFormat> SupportedVideoFormats
    {
      get
      {
        ARLog._Error(_supportedVideoFormatsLogMessage);
        return EmptyReadOnlyCollection<IARVideoFormat>.Instance;
      }
    }

    public IARVideoFormat VideoFormat
    {
      get
      {
        ARLog._Error(_supportedVideoFormatsLogMessage);
        return null;
      }
      set
      {
        ARLog._Error(_supportedVideoFormatsLogMessage);
      }
    }



    public virtual void CopyTo(IARConfiguration target)
    {
      target.IsLightEstimationEnabled = IsLightEstimationEnabled;
      target.WorldAlignment = WorldAlignment;
    }

    void IDisposable.Dispose()
    {
      // Do nothing. This implementation of IARConfiguration is fully managed.
    }
  }
}

