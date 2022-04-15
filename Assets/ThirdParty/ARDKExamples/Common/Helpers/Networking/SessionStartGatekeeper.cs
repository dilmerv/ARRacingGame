using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Helpers
{
  /// <summary>
  /// Small class to disallow users from starting a session
  /// without having filled the session id field.
  /// </summary>
  public class SessionStartGatekeeper : MonoBehaviour
  {
    [SerializeField]
    private InputField idField;
    [SerializeField]
    private Button startButton;

    protected void Update()
    {
      startButton.interactable = !string.IsNullOrEmpty(idField.text);
    }
  }
}