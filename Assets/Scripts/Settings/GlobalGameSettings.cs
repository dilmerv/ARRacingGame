using UnityEngine;

[CreateAssetMenu(fileName = "GlobalGameSettings", menuName = "Create Game Settings", order = 0)]
public class GlobalGameSettings : ScriptableObject 
{
    public Material AvailableReticleMaterial;

    public Material UnavailableReticleMaterial;  
}