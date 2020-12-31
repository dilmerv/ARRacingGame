using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarWheel : MonoBehaviour
{
    [SerializeField]
    private float SpeedOffset = 10.0f;

    public float WheelSpeed { get; set; }

    void Update()
    {
        transform.Rotate(Vector3.right, SpeedOffset * WheelSpeed * Time.deltaTime);    
    }
}
