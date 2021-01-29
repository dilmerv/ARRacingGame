using DilmerGames.Core.Singletons;
using TMPro;
using UnityEngine;
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
}
