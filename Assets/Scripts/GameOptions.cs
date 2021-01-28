using DilmerGames.Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOptions : Singleton<GameOptions>
{
    private bool meshVisibilityOn = true;

    [SerializeField]
    private Material meshMaterial;

    public void ToggleMeshVisibility(Button button)
    {
        meshVisibilityOn = !meshVisibilityOn;
        
        button.GetComponentInChildren<TextMeshProUGUI>().text = meshVisibilityOn ? 
            "MESHING ON" : "MESHING OFF";

        meshMaterial.color = meshVisibilityOn ? new Color(meshMaterial.color.r, meshMaterial.color.g, meshMaterial.color.b, 1)
        : new Color(meshMaterial.color.r, meshMaterial.color.g, meshMaterial.color.b, 0);
    }

    public void RestartSession()
    {
        //TO DO for now I will restart the scene but I will implement
        //a good way to restart the AR Session and player mission
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);
    }
}
