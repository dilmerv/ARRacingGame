using DilmerGames.Core.Singletons;
using UnityEngine.InputSystem;

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

    public void OnTurnLeft(InputValue inputValue) => turnLeft = inputValue.isPressed;

    public void OnTurnRight(InputValue inputValue) => turnRight = inputValue.isPressed;

    public void OnAccelerate(InputValue inputValue) => accelerate = inputValue.isPressed;

    public void OnReverse(InputValue inputValue) => reverse = inputValue.isPressed;
}
