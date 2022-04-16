using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples.Helpers
{
  /// <summary>
  /// Class that organizes the session id field and the random id button.
  /// Is it's own seperate class in order to manage button life cycle and
  /// add extra polish not feasible within UnityEvent configuration.
  /// </summary>
  public class SessionIDGenerator : MonoBehaviour
  {
    [SerializeField]
    private Button generateButton;

    [SerializeField]
    private SessionIDField idField; 

    // Generates a string of 6 random capital letters
    private string GenerateRandomText()
    {
      string builder = "";

      for (int i = 0; i < 6; ++i)
      {
        int r = Random.Range(0, 26); // [0, 26)
        builder += (char)('A' + r);
      }

      return builder;
    }

    public void AssignRandomText()
    {
      idField.text = GenerateRandomText();

      // Necessary in order to integrate seemlessly with the input field.
      // For most networking examples, this invokes a necessary callback
      // in NetworkSessionManager.
      idField.onEndEdit.Invoke(idField.text);
    }

    protected void Update()
    {
      generateButton.gameObject.SetActive(idField.interactable);
    }
  }
}