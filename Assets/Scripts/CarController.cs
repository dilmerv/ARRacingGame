using DilmerGames.Core.Singletons;
using UnityEngine;

public class CarController : Singleton<CarController>
{
    [SerializeField]
    private float speed = 1.0f;

    [SerializeField]
    private float torque = 1.0f;

    [SerializeField]
    private float minSpeedBeforeTorque = 0.3f;

    [SerializeField]
    private float minSpeedBeforeIdle = 0.2f;

    [SerializeField]
    private Rigidbody carRigidBody = null;

    private CarWheel[] wheels;

    public enum Direction
    {
        Idle,
        MoveForward,
        MoveBackward,
        TurnLeft,
        TurnRight
    }

    void Awake()
    {
        wheels = GetComponentsInChildren<CarWheel>();
    }

    void Update()
    {
        if (carRigidBody.velocity.magnitude <= minSpeedBeforeIdle)
        {
            AddWheelsSpeed(0);
        }
    }

    public void Accelerate()
    {
        carRigidBody.AddForce(transform.forward * speed, ForceMode.Acceleration);
        AddWheelsSpeed(speed);
    }

    public void Reverse()
    {
        carRigidBody.AddForce(-transform.forward * speed, ForceMode.Acceleration);;
        AddWheelsSpeed(-speed);
    }

    public void TurnLeft()
    {
        if(canApplyTorque())
            carRigidBody.AddTorque(transform.up * -torque);
    }

    public void TurnRight()
    {
        if(canApplyTorque())
            carRigidBody.AddTorque(transform.up * torque);
    }

    void AddWheelsSpeed(float speed)
    {
        foreach (var wheel in wheels)
        {
            wheel.WheelSpeed = speed;
        }
    }

    bool canApplyTorque()
    {
        Vector3 velocity = carRigidBody.velocity;
        return Mathf.Abs(velocity.x) >= minSpeedBeforeTorque || Mathf.Abs(velocity.z) >= minSpeedBeforeTorque;
    }
}