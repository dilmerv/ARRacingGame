#define AUTH_REQUIRED
// Copyright 2021 Niantic, Inc. All Rights Reserved.
using System;

namespace Niantic.ARDK.Networking
{
  [Serializable]
  public struct ServerConfiguration
  {
    public static readonly int DefaultHeartbeatFrequency = 1000;

#if AUTH_REQUIRED
    public static readonly bool AuthRequired = true;
#else
    public static readonly bool AuthRequired = false;
#endif

    /// <summary>
    /// Endpoint that serves an ARBE address + public key, and is used to connect in native.
    /// </summary>
    public static readonly string ARBEEndpoint = "https://ardk.eng.nianticlabs.com:8084/publickey";
    
    /// <summary>
    /// API key to use for authenticating the application. Create a Resources/ARDK directory and
    ///   add an ArdkAuthConfig scriptable object with your API key to ensure that authentication
    ///   automatically happens when your application is loaded.
    /// </summary>
    public static string ApiKey { get; set; }

    /// <summary>
    /// URL at which the API key will be authenticated.
    /// @note This can only be set for internal testing. Other attempts to set it will no-op. 
    /// </summary>
    public static string AuthenticationUrl { get; set; }

    /// <summary>
    /// Generates a ServerConfiguration pointed at ARBEs, with no defined ClientMetadata. If the
    ///   ClientMetadata is not defined when this ServerConfiguration is used to generate an
    ///   IMultipeerNetworking, a random Guid will be generated
    /// </summary>
    public static ServerConfiguration ARBE
    {
      get
      {
        return
          new ServerConfiguration
          (
            DefaultHeartbeatFrequency,
            ARBEEndpoint
          );
      }
    }

    public int HeartbeatFrequency;
    public string Endpoint;
    public byte[] ClientMetadata;

    public ServerConfiguration(string endpoint)
      : this
      (
        DefaultHeartbeatFrequency,
        endpoint
      )
    {
    }
    public ServerConfiguration
    (
      int heartbeatFrequency,
      string endpoint
    )
      : this()
    {
      HeartbeatFrequency = heartbeatFrequency;
      Endpoint = endpoint;
    }
    public ServerConfiguration
    (
      int heartbeatFrequency,
      string endpoint,
      byte[] clientMetadata
    )
      : this()
    {
      HeartbeatFrequency = heartbeatFrequency;
      ClientMetadata = clientMetadata;
      Endpoint = endpoint;
    }

    // Populate the ClientId with a random Guid
    internal void GenerateRandomClientId()
    {
      ClientMetadata = Guid.NewGuid().ToByteArray();
    }
  }
}
