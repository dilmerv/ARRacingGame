using DilmerGames.Core.Singletons;
using Niantic.ARDK.Extensions.Meshing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOptions : Singleton<GameOptions>
{
    private bool meshVisibilityOn = true;

    [SerializeField]
    private ARMeshManager arMeshManager;

    public void ToggleMeshVisibility(Button button)
    {
        arMeshManager.SetUseInvisibleMaterial(meshVisibilityOn);

        meshVisibilityOn = !meshVisibilityOn;
        
        button.GetComponentInChildren<TextMeshProUGUI>().text = meshVisibilityOn ? 
            "MESHING ON" : "MESHING OFF";
    }
}
