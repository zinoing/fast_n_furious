using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public float vertical;
    public float horizontal;
    public bool handBrake;
    public bool boosting;

    void Start()
    {
        
    }

    void FixedUpdate()
    {
        vertical = Input.GetAxisRaw("Vertical");
        horizontal = Input.GetAxisRaw("Horizontal");
        handBrake = (Input.GetAxis("Jump") != 0) ? true : false;
        boosting = (Input.GetKey(KeyCode.LeftShift)) ? true : false;
    }
}
