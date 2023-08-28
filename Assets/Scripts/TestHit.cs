using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestHit : MonoBehaviour
{
    public float hitForce = 10f;  // Set your initial test force
    private Rigidbody cueBallRigidbody;

    private void Start()
    {
        cueBallRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Listen for a mouse click or a key press
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            // debug
            Debug.Log("Space bar pressed or mouse clicked!");

            // Apply the force on mouse click or Space key press
            Vector3 hitDirection = new Vector3(1, 0, 0);  // Change this to your desired direction
            cueBallRigidbody.AddForce(hitDirection * hitForce, ForceMode.Impulse);
        }
    }
}
