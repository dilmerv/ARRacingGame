using TMPro;
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

    [SerializeField]
    private Rigidbody carRigidBody = null;

    #region Stats

    [SerializeField]
    private bool showStats = false;

    [SerializeField]
    private TextMeshProUGUI speedText = null;

    #endregion

    private CarWheel[] wheels;

    private int targetsCollected = 0;

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
#if UNITY_EDITOR
        PlayerInputController.Instance.Bind(this);
#endif
    }

    void Update()
    {
        if (carRigidBody.velocity.magnitude <= minSpeedBeforeIdle)
        {
            AddWheelsSpeed(0);
        }

        if(showStats)
        {
            if(speedText != null)
            {
                //magnitude is in 1 meter per second
                //convert it to miles per hour by * 2.236936
                speedText.text = $"{string.Format("{0:0.#}", carRigidBody.velocity.magnitude * 2.236936)} Mph";
            }
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