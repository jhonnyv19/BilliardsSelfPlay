using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CueBallCollisionHandler : MonoBehaviour
{

    public PoolEnvController envController;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check if cue ball was pocketed
        if (collision.gameObject.tag == "Pocket")
        {
            Debug.Log("CueBall has been pocketed");

        }
    }
}
