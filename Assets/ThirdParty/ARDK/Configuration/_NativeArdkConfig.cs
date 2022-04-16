// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;
using System.Runtime.InteropServices;
using System.Text;

using Niantic.ARDK.AR;
using Niantic.ARDK.Internals;
using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Configuration
{
  internal sealed class _NativeArdkConfig:
    _IArdkConfig
  {
    private string _dbowUrl;

    public _NativeArdkConfig()
    {
        ARLog._Debug($"Using config: {nameof(_NativeArdkConfig)}");
    }
    
    public bool SetDbowUrl(string url)
    {
      if (!_NAR_ARDKGlobalConfigHelper_SetDBoWUrl(url))
      {
        ARLog._Warn("Failed to set the DBoW URL. It may have already been set.");
        return false;
      }

      // The C++ side actually changes the provided url to include some version information.
      // So, here we just want to clear the cache. On a future get we will get the C++ provided
      // value.
      _dbowUrl = null;
      return true;
    }

    public string GetDbowUrl()
    {
      var result = _dbowUrl;
      if (result != null)
        return result;

      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetDBoWUrl(stringBuilder, (ulong)stringBuilder.Capacity);

      result = stringBuilder.ToString();
      _dbowUrl = result;
      return result;
    }

    private string _contextAwarenessUrl;
    public bool SetContextAwarenessUrl(string url)
    {
      if (!_NAR_ARDKGlobalConfigHelper_SetContextAwarenessUrl(url))
      {
        ARLog._Warn("Failed to set the Context Awareness URL.");
        return false;
      }

      _contextAwarenessUrl = url;
      return true;
    }
    public string GetContextAwarenessUrl()
    {
      /// For security reasons, we will not exposed the default URL
      return _contextAwarenessUrl;
    }

    public string GetAuthenticationUrl()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetAuthURL(stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    public bool SetAuthenticationUrl(string url)
    {
      if (!_NAR_ARDKGlobalConfigHelper_SetAuthURL(url))
      {
        ARLog._Warn("Failed to set the Authentication URL.");
        return false;
      }

      return true;
    }

    public NetworkingErrorCode VerifyApiKeyWithFeature(string feature)
    {
      var error = 
        (NetworkingErrorCode) _NAR_ARDKGlobalConfigHelper_ValidateApiKeyWithFeature(feature);

      return error;
    }

    public bool SetApiKey(string key)
    {
      if (!_NAR_ARDKGlobalConfigHelper_SetApiKey(key))
      {
        ARLog._Warn("Failed to set the API Key.");
        return false;
      }

      return true;
    }

    internal string GetApiKey()
    {
      var stringBuilder = new StringBuilder(512);
      _NAR_ARDKGlobalConfigHelper_GetApiKey(stringBuilder, (ulong)stringBuilder.Capacity);

      var result = stringBuilder.ToString();
      return result;
    }

    // Set DBoW URL
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ARDKGlobalConfigHelper_SetDBoWUrl(string url);

    // Get DBoW URL
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKGlobalConfigHelper_GetDBoWUrl
    (
      StringBuilder outUrl,
      ulong maxUrlSize
    );

    // Set ContextAwareness URL
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ARDKGlobalConfigHelper_SetContextAwarenessUrl(string url);

    // Set Api Key
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ARDKGlobalConfigHelper_SetApiKey(string key);

    // Get Api Key
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKGlobalConfigHelper_GetApiKey
    (
      StringBuilder outKey,
      ulong maxKeySize
    );

    // Set Auth URL
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern bool _NAR_ARDKGlobalConfigHelper_SetAuthURL(string key);
    
    // Attempt to validate the specified feature, with a previously set Api Key. 
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern Int32 _NAR_ARDKGlobalConfigHelper_ValidateApiKeyWithFeature(string feature);

    // Get Auth URL
    [DllImport(_ARDKLibrary.libraryName)]
    private static extern void _NAR_ARDKGlobalConfigHelper_GetAuthURL
    (
      StringBuilder outKey,
      ulong maxKeySize
    );

  }
}
