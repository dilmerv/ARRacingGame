using DilmerGames.Core.Singletons;
using UnityEngine.InputSystem;
using UnityEngine;

public class PlayerInputController : Singleton<PlayerInputController>
{
    private bool turnLeft, turnRight, accelerate, reverse;
    
    private CarController carController;

    public void Bind(CarController carController)
    {
        this.carController = carController;
    }

    void FixedUpdate() 
    {
        if(carController == null) 
        {
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

    public void OnTurnLeft(InputValue inputValue) 
    { 
        turnLeft = inputValue.isPressed;
        Logger.Instance.LogInfo($"OnTurnLeft...{turnLeft}");
    }

    public void OnTurnRight(InputValue inputValue) 
    { 
        turnRight = inputValue.isPressed;
        Logger.Instance.LogInfo($"OnTurnRight...{turnRight}");
    }

    public void OnAccelerate(InputValue inputValue) 
    {
        accelerate = inputValue.isPressed;
        Logger.Instance.LogInfo($"OnAccelerate...{accelerate}");
    } 

    public void OnReverse(InputValue inputValue) 
    {
        reverse = inputValue.isPressed;
        Logger.Instance.LogInfo($"OnReverse...{reverse}");
    }
}
