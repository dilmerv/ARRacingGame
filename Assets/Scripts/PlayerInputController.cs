using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputController : MonoBehaviour
{
    private bool turnLeft, turnRight, accelerate, reverse;

    void FixedUpdate() 
    {
        if(accelerate)
        {
            CarController.Instance.Accelerate();
        }
        if(reverse)
        {
            CarController.Instance.Reverse();
        }
        if(turnLeft)
        {
            CarController.Instance.TurnLeft();
        }
        if(turnRight)
        {
            CarController.Instance.TurnRight();
        }
    }

    public void OnTurnLeft(InputValue inputValue) => turnLeft = inputValue.isPressed;

    public void OnTurnRight(InputValue inputValue) => turnRight = inputValue.isPressed;

    public void OnAccelerate(InputValue inputValue) => accelerate = inputValue.isPressed;
    public void OnReverse(InputValue inputValue) => reverse = inputValue.isPressed;
}
