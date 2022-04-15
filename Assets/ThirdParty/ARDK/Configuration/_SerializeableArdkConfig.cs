// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Configuration
{
  internal sealed class _SerializeableArdkConfig:
    _IArdkConfig
  {
    private string _dbowUrl;
    private string _apiKey;
    private string _contextAwarenessUrl = "";
    private string _authenticationUrl = "";

    public _SerializeableArdkConfig()
    {
      ARLog._Debug($"Using config: {nameof(_SerializeableArdkConfig)}");
    }
    
    public bool SetDbowUrl(string url)
    {
      _dbowUrl = url;

      return true;
    }

    public string GetDbowUrl()
    {
      return _dbowUrl;
    }

    public bool SetContextAwarenessUrl(string url)
    {
      _contextAwarenessUrl = url;

      return true;
    }

    public string GetContextAwarenessUrl()
    {
      return _contextAwarenessUrl;
    }

    public bool SetApiKey(string apiKey)
    {
      _apiKey = apiKey;
      return true;
    }

    public string GetAuthenticationUrl()
    {
      return _authenticationUrl;
    }

    public bool SetAuthenticationUrl(string url)
    {
      _authenticationUrl = url;
      return true;
    }

    public NetworkingErrorCode VerifyApiKeyWithFeature(string feature)
    {
      if (String.IsNullOrEmpty(_apiKey))
        return NetworkingErrorCode.ApiKeyNotSet;

      return NetworkingErrorCode.Ok;
    }
  }
}