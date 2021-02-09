using DilmerGames.Core.Singletons;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class GameManager : Singleton<GameManager>
{
    [SerializeField]
    private GlobalGameSettings globalGameSettings;

    public GlobalGameSettings GlobalGameSettings
    {
        get
        {
            return globalGameSettings;
        }
    }
    
    void Awake() 
    {
        EnhancedTouchSupport.Enable();
    }
}
