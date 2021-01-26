using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOptions : MonoBehaviour
{
    private bool meshVisibilityOn = true;

    [SerializeField]
    private Material meshMaterial;

    public void ToggleMeshVisibility(Button button)
    {
        meshVisibilityOn = !meshVisibilityOn;
        
        button.GetComponentInChildren<TextMeshProUGUI>().text = meshVisibilityOn ? 
            "MESHING VISIBILITY OFF" : "MESHING VISIBILITY ON";

        meshMaterial.color = meshVisibilityOn ? new Color(meshMaterial.color.r, meshMaterial.color.g, meshMaterial.color.b, 1)
        : new Color(meshMaterial.color.r, meshMaterial.color.g, meshMaterial.color.b, 0);
    }
}
