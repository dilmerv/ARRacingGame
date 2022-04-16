// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Niantic.ARDK.AR;
using Niantic.ARDK.AR.Configuration;
using Niantic.ARDK.Configuration;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Utilities.Logging;

using UnityEngine;

namespace Niantic.ARDK.Utilities.Preloading
{
  internal sealed class _NativeFeaturePreloader:
    IFeaturePreloader
  {
    private IntPtr _nativeHandle;

    private const string _dbowUrl = "https://bowvocab.eng.nianticlabs.com/dbow_b50_l3.bin";

    internal _NativeFeaturePreloader()
    {
      _nativeHandle = _NAR_ARDKFilePreloader_Init();
    }

    ~_NativeFeaturePreloader()
    {
      _NAR_ARDKFilePreloader_Release(_nativeHandle);
    }

    public void Dispose()
    {
      GC.SuppressFinalize(this);

      var nativeHandle = _nativeHandle;
      if (nativeHandle != IntPtr.Zero)
      {
        _nativeHandle = IntPtr.Zero;

        _NAR_ARDKFilePreloader_Release(nativeHandle);
      }
    }

    public float GetProgress(Feature feature)
    {
      return _NAR_ARDKFilePreloader_CurrentProgress(_nativeHandle, (UInt32)feature);
    }

    public PreloadedFeatureState GetStatus(Feature feature)
    {
      return (PreloadedFeatureState)_NAR_ARDKFilePreloader_GetStatus(_nativeHandle, (UInt32)feature);
    }

    public bool ExistsInCache(Feature feature)
    {
      return _NAR_ARDKFilePreloader_ExistsInCache(_nativeHandle, (UInt32)feature);
    }

    public void Download(Feature[] features)
    {
      // Todo: This should really be done in native
      if (features.Contains(Feature.Dbow) && string.IsNullOrEmpty(ArdkGlobalConfig.GetDbowUrl()))
        ArdkGlobalConfig.SetDbowUrl(ArdkGlobalConfig._DBOW_URL);

      UInt32[] featuresInts = Array.ConvertAll(features, value => (UInt32) value);
      _NAR_ARDKFilePreloader_Download(_nativeHandle, featuresInts, featuresInts.Length);
    }

    public void ClearCache(Feature feature)
    {
      _NAR_ARDKFilePreloader_ClearFromCache(_nativeHandle, (UInt32)feature);
    }

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern IntPtr _NAR_ARDKFilePreloader_Init();

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKFilePreloader_Download
    (
      IntPtr ptr,
      UInt32[] features,
      int numFeatures
    );

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKFilePreloader_Release(IntPtr ptr);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern float _NAR_ARDKFilePreloader_CurrentProgress(IntPtr ptr, UInt32 feature);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern byte _NAR_ARDKFilePreloader_GetStatus(IntPtr ptr, UInt32 feature);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ARDKFilePreloader_ExistsInCache(IntPtr ptr, UInt32 feature);

    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKFilePreloader_ClearFromCache(IntPtr ptr, UInt32 feature);

    // Returns true if the status is Finished or Failed
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ARDKFilePreloader_IsDownloadFinished(IntPtr ptr, UInt32 feature);
  }
}