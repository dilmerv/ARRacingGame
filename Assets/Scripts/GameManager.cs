using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

public class GameManager : MonoBehaviour
{
    void Awake() 
    {
        EnhancedTouchSupport.Enable();
    }
}
