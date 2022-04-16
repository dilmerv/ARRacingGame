using UnityEngine;

namespace Niantic.ARDK.Configuration.Authentication
{
  [CreateAssetMenu(fileName = "ArdkAuthConfig", menuName = "ARDK/ArdkAuthConfig", order = 1)]
  public class ArdkAuthConfig : ScriptableObject
  {
    [SerializeField]
    [Tooltip("Developers should obtain their API key from Niantic and put it in here")]
    private string _apiKey = "";
    
    public string ApiKey {
      get
      {
        return !string.IsNullOrEmpty(_overrideApiKey) ? _overrideApiKey : _apiKey;
      }
    }
    
    // Override API key for internal testing. You probably shouldn't touch this
    private readonly string _overrideApiKey = string.Empty;
  }
}
