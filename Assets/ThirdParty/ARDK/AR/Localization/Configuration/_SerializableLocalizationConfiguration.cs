// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

namespace Niantic.ARDK.AR.Localization
{
  internal sealed class _SerializableLocalizationConfiguration:
    ILocalizationConfiguration
  {
    public string MapIdentifier { get; set; }

    public float LocalizationTimeout { get; set; }

    public float RequestTimeLimit { get; set; }

    public string LocalizationEndpoint { get; set; }

    void IDisposable.Dispose()
    {
      // Do nothing. This implementation of ILocalizationConfiguration is fully managed.
    }
  }
}
