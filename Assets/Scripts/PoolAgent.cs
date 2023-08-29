using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using Unity.Barracuda;
using System;
using Grpc.Core;
using System.Linq;

public class PoolAgent : Agent
{
    public PoolEnvController envController;

    public GameObject cueBall;

    private int actionsTaken;

    public Rigidbody cueBallRigidBody;

    // min and max x coordinates
    private float x_min = -48.2f;
    private float x_max = 48.0f;

    // min and max y coordinates
    private float y_min = -9.0f;
    private float y_max = 2.125f;

    // min and max z coordinates
    private float z_min = -11.5f;
    private float z_max = 43.01f;

    protected override void Awake() {
        cueBallRigidBody = cueBall.GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        actionsTaken = 0;

    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Apply a force to the cue ball
        float moveX = Mathf.Clamp(actionBuffers.ContinuousActions[0], -1f, 1f);
        float moveY = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        // Debug.Log("Action taken: " + moveX + ", " + moveY);

        // Calculate the direction of the hit
        Vector3 hitDirection = new Vector3(moveX, 0, moveY).normalized;

        // Record the cue ball's position before the hit
        envController.setPreviousCueBallLocalPosition(cueBall.transform.localPosition);

        // Apply the hit force in the hit direction
        // Debug.Log("Location of cue ball when hit: " + cueBall.transform.localPosition);
        cueBallRigidBody.AddForce(hitDirection * envController.hitForce, ForceMode.Impulse);

        // Increment action counter
        actionsTaken++;

        // Only apply life penalty based on turns
        // AddReward(-1 / envController.maxActions);

        envController.actionTaken();

        // Print accumulated reward
        Debug.Log("Accumulated reward for player: " + GetCumulativeReward());

        // Debug.Log(gameObject.name + " hit cue ball");
        // Debug.Log("Location of cue ball after hit: " + cueBall.transform.localPosition);

        // // Add a penalty if the action's magnitude is too small
        // float smallActionThreshold = 0.2f;  // Adjust this value as needed
        // float smallActionPenalty = -0.1f;  // Adjust this value as needed

        // if (hitDirection.magnitude < smallActionThreshold)
        // {
        //     AddReward(smallActionPenalty);
        //     Debug.Log("Small action penalty");
        // }
    }


    // void OnCollisionEnter(Collision collision)
    // {
    //     // Unset flag if cue ball hits another ball
    //     if (collision.gameObject.tag == "SolidBall" || collision.gameObject.tag == "BlackBall")
    //     {
    //         cueBallHitOthers = false;
    //     }
    //     // Debug.Log("Detected collision");
    //     if (collision.gameObject.tag == "SolidBall")
    //     {
    //         // Assign gradually decreasing reward for hitting a solid ball
    //         AddReward(impactFactor - (impactFactor / maxActions) * actionsTaken);

    //         // Debug.Log("Solid ball hit reward assigned!");
    //     }
    //     else if (collision.gameObject.tag == "StripedBall")
    //     {
    //         AddReward(-0.15f);
    //         // Debug.Log("Striped ball hit, negative reward");
    //     }

    // }

    public int getActionsTaken()
    {
        return actionsTaken;
    }


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }

    private Vector3 normalizeVector3(Vector3 vec)
    {
        // normalizedValue = (currentValue - minValue)/(maxValue - minValue)
        vec.x = (vec.x - x_min) / (x_max - x_min);
        vec.y = (vec.y - y_min) / (y_max - y_min);
        vec.z = (vec.z - z_min) / (z_max - z_min);

        return vec;
    }


    //private void collectPositionalObservations(VectorSensor sensor)
    //{
    //    sensor.AddObservation(normalizeVector3(blackBall.transform.localPosition));
    //    sensor.AddObservation(normalizeVector3(cueBall.transform.localPosition));

    //    // Add position of all balls
    //    foreach (GameObject ball in solidBalls)
    //    {
    //        sensor.AddObservation(normalizeVector3(ball.transform.localPosition));
    //    }

    //    foreach (GameObject ball in stripedBalls)
    //    {
    //        sensor.AddObservation(normalizeVector3(ball.transform.localPosition));
    //    }

    //    // Add the locations of all six pockets
    //    float pocketY = 1.125f;
    //    sensor.AddObservation(normalizeVector3(new Vector3(-45.5f, pocketY, 40.49f)));
    //    sensor.AddObservation(normalizeVector3(new Vector3(0.75f, pocketY, 40.49f)));
    //    sensor.AddObservation(normalizeVector3(new Vector3(47.0f, pocketY, 40.49f)));

    //    sensor.AddObservation(normalizeVector3(new Vector3(47.0f, pocketY, -8.0f)));
    //    sensor.AddObservation(normalizeVector3(new Vector3(0.88f, pocketY, -8.0f)));
    //    sensor.AddObservation(normalizeVector3(new Vector3(45.5f, pocketY, -8.0f)));

    //}

    //private void collectRelativeObservations(VectorSensor sensor)
    //{

    //    sensor.AddObservation(normalizeVector3(blackBall.transform.localPosition - cueBall.transform.localPosition));

    //    // Add relative positions of solid and striped balls
    //    foreach (GameObject ball in solidBalls)
    //    {
    //        sensor.AddObservation(normalizeVector3(ball.transform.localPosition - cueBall.transform.localPosition));
    //    }

    //    foreach (GameObject ball in stripedBalls)
    //    {
    //        sensor.AddObservation(normalizeVector3(ball.transform.localPosition - cueBall.transform.localPosition));
    //    }

    //    // Add the relative locations of all six pockets
    //    float pocketY = 1.125f;
    //    sensor.AddObservation(normalizeVector3(new Vector3(-45.5f, pocketY, 40.49f) - cueBall.transform.localPosition));
    //    sensor.AddObservation(normalizeVector3(new Vector3(0.75f, pocketY, 40.49f) - cueBall.transform.localPosition));
    //    sensor.AddObservation(normalizeVector3(new Vector3(47.0f, pocketY, 40.49f) - cueBall.transform.localPosition));

    //    sensor.AddObservation(normalizeVector3(new Vector3(47.0f, pocketY, -8.0f) - cueBall.transform.localPosition));
    //    sensor.AddObservation(normalizeVector3(new Vector3(0.88f, pocketY, -8.0f) - cueBall.transform.localPosition));
    //    sensor.AddObservation(normalizeVector3(new Vector3(45.5f, pocketY, -8.0f) - cueBall.transform.localPosition));

    //}

}
