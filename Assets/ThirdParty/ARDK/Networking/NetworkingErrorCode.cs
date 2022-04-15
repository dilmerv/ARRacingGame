using System;

namespace Niantic.ARDK.Networking
{
  // Possible networking error codes when authenticating or connecting to ARBEs
  public enum NetworkingErrorCode : Int32
  {
    Unknown = 0,
    // 1-100 - Handshake errors
    
    // OK
    Ok = 200,

    // ARBE-specific
    ARBENotSet = 500,
    ARBEConnection,
    ARBEHttp,
    ARBEResponse,
    ARBEVersion,

    // Auth server
    AuthNotSet = 600,
    AuthConnection,
    AuthHttp,
    AuthResponse,

    // Api key
    ApiKeyInvalid = 700,
    ApiKeyNotSet,

    // UDP errors
    UdpInvalid = 800,

    // Unexpected
    Unexpected = 911,

  }
}
