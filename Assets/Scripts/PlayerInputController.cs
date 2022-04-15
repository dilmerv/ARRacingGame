using UnityEngine;
using UnityEngine.InputSystem;
using LearnXR.Controllers;

public class PlayerInputController : MonoBehaviour, PlayerControls.IPlayerActions
{
    private bool turnLeft, turnRight, accelerate, reverse;
    
    private CarController carController;

    private PlayerControls playerControls;

    public void Bind(CarController carController)
    {
        this.carController = carController;
    }

    private void Awake()
    {
        playerControls = new PlayerControls();
        playerControls.Enable();
        playerControls.Player.SetCallbacks(this);
    }

    void FixedUpdate() 
    {
        if(carController == null) 
        {
            Logger.Instance.LogInfo("CarController is null...");
            return;
        }

        if(accelerate)
        {
            carController.Accelerate();
        }
        if(reverse)
        {
            carController.Reverse();
        }
        if(turnLeft)
        {
            carController.TurnLeft();
        }
        if(turnRight)
        {
            carController.TurnRight();
        }
    }

    public void OnAccelerate(InputAction.CallbackContext context)
    {
        accelerate = context.ReadValueAsButton();
        Logger.Instance.LogInfo($"OnAccelerate...{accelerate}");
    }

    public void OnReverse(InputAction.CallbackContext context)
    {
        reverse = context.ReadValueAsButton();
        Logger.Instance.LogInfo($"OnReverse...{reverse}");
    }

    public void OnTurnLeft(InputAction.CallbackContext context)
    {
        turnLeft = context.ReadValueAsButton();
        Logger.Instance.LogInfo($"OnTurnLeft...{turnLeft}");
    }

    public void OnTurnRight(InputAction.CallbackContext context)
    {
        turnRight = context.ReadValueAsButton();
        Logger.Instance.LogInfo($"OnTurnRight...{turnRight}");
    }
}
