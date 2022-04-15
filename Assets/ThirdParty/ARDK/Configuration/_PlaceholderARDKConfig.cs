using System;

using Niantic.ARDK.Networking;
using Niantic.ARDK.Utilities.Logging;

namespace Niantic.ARDK.Configuration
{
  // <summary>
  // Temporary ardk config class while proper support for other Operating Systems and architecture is being added.
  // </summary>
  internal sealed class _PlaceholderArkdConfig :
    _IArdkConfig
  {
    private string _dbowUrl;
    private string _contextAwarenessUrl;
    private string _apiKey;
    private string _authenticationUrl;

    public _PlaceholderArkdConfig()
    {
      ARLog._Debug($"Using config: {nameof(_PlaceholderArkdConfig)}");
    }
    public bool SetDbowUrl(string url)
    {
      if (string.IsNullOrWhiteSpace(url))
        throw new ArgumentException($"{nameof(url)} is null or whitespace.");
      
      _dbowUrl = url;
      return true;
    }

    public string GetDbowUrl()
    {
      return _dbowUrl ?? string.Empty;
    }

    public string GetContextAwarenessUrl()
    {
      return _contextAwarenessUrl ?? string.Empty;
    }

    public bool SetContextAwarenessUrl(string url)
    {
      if (url == null)
        throw new ArgumentException($"{nameof(url)} is null.");
      
      _contextAwarenessUrl = url;
      return true;
    }

    public bool SetApiKey(string key)
    {
      if (string.IsNullOrWhiteSpace(key))
        throw new ArgumentException($"{nameof(key)} is null or whitespace.");
      
      _apiKey = key;
      return true;
    }

    public string GetAuthenticationUrl()
    {
      return _authenticationUrl ?? string.Empty;
    }

    public bool SetAuthenticationUrl(string url)
    {
      if (string.IsNullOrWhiteSpace(url))
        throw new ArgumentException($"{nameof(url)} is null or whitespace.");
      
      _authenticationUrl = url;
      return true;
    }

    public NetworkingErrorCode VerifyApiKeyWithFeature(string feature)
    {
      return NetworkingErrorCode.Ok;
    }
  }
}
