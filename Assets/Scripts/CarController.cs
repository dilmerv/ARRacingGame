using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField]
    private float speed = 1.0f;

    [SerializeField]
    private float torque = 1.0f;

    [SerializeField]
    private float minSpeedBeforeTorque = 0.3f;

    [SerializeField]
    private float minSpeedBeforeIdle = 0.2f;

    public Direction CurrentDirection { get; set; } = Direction.Idle;

    [SerializeField]
    private Rigidbody carRigidBody;

    private CarWheel[] wheels;

    public enum Direction
    {
        Idle,
        MoveForward,
        MoveBackward,
        TurnLeft,
        TurnRight
    }

    private void Awake()
    {
        wheels = GetComponentsInChildren<CarWheel>();
    }

    void Update()
    {
        if (carRigidBody.velocity.magnitude <= minSpeedBeforeIdle)
        {
            CurrentDirection = Direction.Idle;
            AddWheelsSpeed(0);
        }
    }

    void FixedUpdate() => ApplyMovement();

    public void ApplyMovement()
    {
        if (Input.GetKey(KeyCode.UpArrow) || CurrentDirection == Direction.MoveForward)
        {
            carRigidBody.AddForce(transform.forward * speed, ForceMode.VelocityChange);
            AddWheelsSpeed(speed);
        }

        if (Input.GetKey(KeyCode.DownArrow) || CurrentDirection == Direction.MoveBackward)
        {
            carRigidBody.AddForce(-transform.forward * speed, ForceMode.VelocityChange);
            AddWheelsSpeed(-speed);
        }

        if ((Input.GetKey(KeyCode.LeftArrow) && canApplyTorque()) || CurrentDirection == Direction.TurnLeft)
        {
            carRigidBody.AddTorque(transform.up * -torque);
        }

        if (Input.GetKey(KeyCode.RightArrow) && canApplyTorque() || CurrentDirection == Direction.TurnRight)
        {
            carRigidBody.AddTorque(transform.up * torque);
        }
    }

    void AddWheelsSpeed(float speed)
    {
        foreach (var wheel in wheels)
        {
            wheel.WheelSpeed = speed;
        }
    }


    public bool canApplyTorque()
    {
        Vector3 velocity = carRigidBody.velocity;
        return Mathf.Abs(velocity.x) >= minSpeedBeforeTorque || Mathf.Abs(velocity.z) >= minSpeedBeforeTorque;
    }
}