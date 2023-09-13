using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum Player
{
    SolidPlayer = 0,
    StripedPlayer = 1,
}

public class PoolEnvController : MonoBehaviour
{
    public PoolAgent solidPlayer;
    public PoolAgent stripedPlayer;
    public Transform cueBallTransform;
    public GameObject cueBallPrefab;
    // public GameObject solidBallPrefab;
    // public GameObject stripedBallPrefab;
    // public GameObject blackBallPrefab;


    public float hitForce = 5.0f;
    public float maxActions = 100;

    private GameObject[] pockets;

    private GameObject[] solidBalls_arr;
    private GameObject[] stripedBalls_arr;

    private List<GameObject> solidBalls;  // Keep track of the solid balls that are not pocketed
    private List<GameObject> stripedBalls;  // Keep track of the striped balls that are not pocketed
    private List<GameObject> pocketedSolidBalls;  // Keep track of the solid balls that are pocketed
    private List<GameObject> pocketedStripedBalls;  // Keep track of the striped balls that are pocketed
    private GameObject blackBall;
    private GameObject cueBall;

    private Vector3[] solidBallStartingPositions;
    private Vector3[] stripedBallStartingPositions;
    private Vector3 blackBallStartingPosition;

    private float velocityThreshold = 0.1f;
    private float yThreshold = 0.01f;

    // min and max x coordinates
    private float x_min = -48.2f;
    private float x_max = 48.0f;

    // min and max y coordinates
    private float y_min = -9.0f;
    private float y_max = 2.125f;

    // min and max z coordinates
    private float z_min = -11.5f;
    private float z_max = 43.01f;
    
    // variable for ball y position
    private float ballY = 1.3f;

    private float impactReward = 0.25f;

    private bool hasHitBall = false;

    private bool actionWasTaken = false;

    private Vector3 previousCueBallLocalPosition; 

    private bool needToResetScene = false;

    public bool needToMoveCueBall = false;

    // Current player
    private Player currentPlayer;

    void Awake() {

        // Slow down time to make it easier to see what's happening
        // Time.timeScale = 0.2f;

        // Initialize arrays
        solidBalls_arr = GameObject.FindGameObjectsWithTag("SolidBall");
        stripedBalls_arr = GameObject.FindGameObjectsWithTag("StripedBall");

        // Initialize the lists
        solidBalls = new List<GameObject>();
        stripedBalls = new List<GameObject>();
        pocketedSolidBalls = new List<GameObject>();
        pocketedStripedBalls = new List<GameObject>();

        // Add each ball to the appropriate list
        foreach (GameObject ball in GameObject.FindGameObjectsWithTag("SolidBall"))
        {
            solidBalls.Add(ball);
        }

        foreach (GameObject ball in GameObject.FindGameObjectsWithTag("StripedBall"))
        {
            stripedBalls.Add(ball);
        }

        // Initialize game object references
        pockets = GameObject.FindGameObjectsWithTag("PocketEdge");
        blackBall = GameObject.FindGameObjectWithTag("BlackBall");
        cueBall = GameObject.FindGameObjectWithTag("CueBall");

        // Sanity checks
        if (solidBalls.Count != 7)
        {
            Debug.Log("Error: There are not 7 solid colored balls");
        }

        if (stripedBalls.Count != 7)
        {
            Debug.Log("Error: There are not 7 striped colored balls");
        }

        // Check if black ball exists
        if (blackBall == null)
        {
            Debug.Log("Error: There is no black ball");
        }

        // Check if cue ball exists
        if (cueBall == null)
        {
            Debug.Log("Error: There is no cue ball");
        }

        // Set the current player, solid player goes first
        currentPlayer = Player.SolidPlayer;

        // Move solid player's transform to cue ball and set rotation to 0
        solidPlayer.transform.position = cueBallTransform.position;
        solidPlayer.transform.rotation = Quaternion.identity;

        resetScene();
        previousCueBallLocalPosition = cueBallTransform.localPosition;

        // Save the starting positions of the solid balls
        solidBallStartingPositions = new Vector3[solidBalls.Count];
        for (int i = 0; i < solidBalls.Count; i++)
        {
            solidBallStartingPositions[i] = solidBalls[i].transform.localPosition;
        }

        // Save the starting positions of the striped balls
        stripedBallStartingPositions = new Vector3[stripedBalls.Count];
        for (int i = 0; i < stripedBalls.Count; i++)
        {
            stripedBallStartingPositions[i] = stripedBalls[i].transform.localPosition;
        }

        // Save the starting position of the black ball
        blackBallStartingPosition = blackBall.transform.localPosition;

    }

    void Start(){
        requestDecisionFromCurrentPlayer();
    }

    private void FixedUpdate() {
        if(needToResetScene){
            needToResetScene = false;
            resetScene();
            // Debug.Log("Scene reset");
        }

        if(cueBall.transform.localPosition.y < yThreshold){
            needToMoveCueBall = true;

        }
        
        // Check if cue ball was pocketed, which should not happen
        if (needToMoveCueBall)
        {

            if (currentPlayer == Player.SolidPlayer)
            {
                // Debug.Log("Solid player pocketed the cue ball");
                solidPlayer.SetReward(-0.5f);
            }
            else
            {
                // Debug.Log("Striped player pocketed the cue ball");
                stripedPlayer.SetReward(-0.5f);
            }

            // Debug.Log("Cue ball was pocketed, negative reward applied");

            // If it was pocketed, move it back to its previous position and clear forces
            // ResetCueBall(previousCueBallLocalPosition);

            MoveCueBall(previousCueBallLocalPosition);
            needToMoveCueBall = false;

        }
    }

    private void Update()
    {   

        // Check if balls have stopped moving from the previous action
        if (!BallsAreMoving() && actionWasTaken)
        {
            
            // if (!hasHitBall)
            // {
            //     // Apply a penalty if the cue ball didn't hit another ball
            //     if (currentPlayer == Player.SolidPlayer)
            //     {
            //         solidPlayer.AddReward(-0.25f);
            //     }
            //     else
            //     {
            //         stripedPlayer.AddReward(-0.25f);
            //     }
            // }

            // Check for pocketed solid balls
            foreach (GameObject ball in solidBalls.ToList())  // Iterate over a copy of the list to avoid modifying the list while iterating
            {
                if (ball.transform.localPosition.y < yThreshold)
                {
                    // Debug.Log("Solid ball pocketed");
                    pocketedSolidBalls.Add(ball);
                    solidBalls.Remove(ball);
                    // Assign reward to the solid player
                    // solidPlayer.AddReward((1 - solidPlayer.getActionsTaken() / maxActions) * pocketedSolidBalls.Count);
                    solidPlayer.AddReward(0.5f);

                }
            }

            // Check for pocketed striped balls
            foreach (GameObject ball in stripedBalls.ToList())  // Iterate over a copy of the list to avoid modifying the list while iterating
            {
                if (ball.transform.localPosition.y < yThreshold)
                {
                    // Debug.Log("Striped ball pocketed");
                    pocketedStripedBalls.Add(ball);
                    stripedBalls.Remove(ball);

                    // Assign reward to the striped player
                    // stripedPlayer.AddReward((1 - stripedPlayer.getActionsTaken() / maxActions) * pocketedStripedBalls.Count);
                    stripedPlayer.AddReward(0.5f);
                }
            }

            // Check if solid player won
            if (pocketedSolidBalls.Count == 7 && blackBall.transform.localPosition.y < yThreshold)
            {
                solidPlayer.SetReward(1.0f);
                stripedPlayer.SetReward(-1.0f);

                Debug.Log("Solid player won!");

                stripedPlayer.EndEpisode();
                solidPlayer.EndEpisode();
                needToResetScene = true;

            } 
            // Check if striped player won
            else if (pocketedStripedBalls.Count == 7 && blackBall.transform.localPosition.y < yThreshold)
            {
                stripedPlayer.SetReward(1.0f);
                solidPlayer.SetReward(-1.0f);

                Debug.Log("Striped player won!");

                stripedPlayer.EndEpisode();
                solidPlayer.EndEpisode();
                needToResetScene = true;

            } 
            // Check if players have reached the maximum number of actions
            else if (stripedPlayer.getActionsTaken() == maxActions) {
                
                // Assign victory to the player with the most balls pocketed
                if (pocketedSolidBalls.Count > pocketedStripedBalls.Count)
                {
                    solidPlayer.SetReward(1.0f);
                    stripedPlayer.SetReward(-1.0f);
                }
                else if (pocketedSolidBalls.Count < pocketedStripedBalls.Count)
                {
                    stripedPlayer.SetReward(1.0f);
                    solidPlayer.SetReward(-1.0f);
                }
                else
                {
                    // If the number of pocketed balls is the same, assign 0 to both players
                    solidPlayer.SetReward(0.0f);
                    stripedPlayer.SetReward(0.0f);
                }

                solidPlayer.EndEpisode();
                stripedPlayer.EndEpisode();
                needToResetScene = true;

            }

            // Reset collision boolean
            hasHitBall = false;

            actionWasTaken = false;

            // Switch the current player
            switchPlayer();
            requestDecisionFromCurrentPlayer();

        } 
        else if (!BallsInBounds())
        {
            
            // Debug.Log("Ball was not in bounds");

            // If the solid player is the current player, the solid player loses
            if (currentPlayer == Player.SolidPlayer)
            {
                solidPlayer.SetReward(-1.0f);
            }
            else
            {
                stripedPlayer.SetReward(-1.0f);
            }

            // End the episode
            solidPlayer.EndEpisode();
            stripedPlayer.EndEpisode();

            actionWasTaken = false;

            hasHitBall = false;

            needToResetScene = true;
            switchPlayer();
            requestDecisionFromCurrentPlayer();
        }

        // Print location of cue ball
        // Debug.Log("Cue ball position: " + cueBall.transform.localPosition);

    }

    public void HandleCueBallCollision(string ballTag)
    {
        if (ballTag == "SolidBall")
        {
            // The cue ball has collided with a solid ball
            if (currentPlayer == Player.SolidPlayer)
            {
                // The solid player hit the correct ball
                solidPlayer.AddReward(impactReward - (impactReward / maxActions) * solidPlayer.getActionsTaken());
                // Debug.Log("Solid player hit the correct ball");
            }
            else
            {
                // The striped player hit the wrong ball
                // stripedPlayer.AddReward(-0.5f);  // Replace with the actual penalty value
            }
        }
        else if (ballTag == "StripedBall")
        {
            // The cue ball has collided with a striped ball
            if (currentPlayer == Player.StripedPlayer)
            {
                // The striped player hit the correct ball
                stripedPlayer.AddReward(impactReward - (impactReward / maxActions) * stripedPlayer.getActionsTaken());
                // Debug.Log("Striped player hit the correct ball");
            }
            else
            {
                // The solid player hit the wrong ball
                // solidPlayer.AddReward(-0.5f);  // Replace with the actual penalty value
            }
        }

        hasHitBall = true;
    }

    public void setPreviousCueBallLocalPosition(Vector3 position)
    {
        previousCueBallLocalPosition = position;
    }

    public void actionTaken() {
        actionWasTaken = true;
    }

    private void MoveCueBall(Vector3 position) {
        cueBallTransform.localPosition = position;

        // Reset the cue ball's velocities
        cueBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
        cueBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }

    private void ResetCueBall(Vector3 newPosition)
    {
        // Destroy the current cue ball
        if (cueBall != null)
        {
            Destroy(cueBall);
        }

        // Instantiate a new cue ball from the prefab
        cueBall = Instantiate(cueBallPrefab, newPosition, Quaternion.identity);
        cueBallTransform = cueBall.transform;

        // Reset agent reference to the cue ball
        solidPlayer.cueBall = cueBall;
        solidPlayer.cueBallRigidBody = cueBall.GetComponent<Rigidbody>();
        stripedPlayer.cueBall = cueBall;
        stripedPlayer.cueBallRigidBody = cueBall.GetComponent<Rigidbody>();

        // // Attach script to cue ball
        // cueBall.AddComponent<CueBallCollisionDetector>();
        // cueBall.GetComponent<CueBallCollisionDetector>().envController = this;

        // cueBall.transform.localPosition = newPosition;
        // cueBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
        // cueBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

    }
    



    // private void resetBlackBall(float ballY)
    // {
    //     // Destroy the existing black ball
    //     Destroy(blackBall);

    //     // Instantiate a new black ball from the prefab at the initial position
    //     blackBall = Instantiate(blackBallPrefab, blackBallStartingPosition, Quaternion.identity);
    // }



    private void switchPlayer() {
        // Switch the current player
        currentPlayer = (currentPlayer == Player.SolidPlayer) ? Player.StripedPlayer : Player.SolidPlayer;

        // Move next player's transform to the cue ball's position and set the rotation to 0
        if (currentPlayer == Player.SolidPlayer)
        {
            solidPlayer.transform.position = cueBall.transform.position;
            solidPlayer.transform.rotation = Quaternion.identity;
        }
        else
        {
            stripedPlayer.transform.position = cueBall.transform.position;
            stripedPlayer.transform.rotation = Quaternion.identity;
        }
    }

    private void requestDecisionFromCurrentPlayer()
    {
        // Request a decision from the appropriate player
        if (currentPlayer == Player.SolidPlayer)
        {
            solidPlayer.RequestDecision();
        }
        else
        {
            stripedPlayer.RequestDecision();
        }
    }

    public void resetScene()
    {
        // Adjust solid and striped ball positions and clear forces
        resetSolidBallPositions(solidBalls_arr, ballY);
        resetStripedBallPositions(stripedBalls_arr, ballY);

        // Adjust the 8 ball and clear forces
        blackBall.transform.localPosition = new Vector3(28.05f, ballY, 16.44f);
        blackBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
        blackBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        // Adjust the cue ball and clear forces
        MoveCueBall(new Vector3(-29.75f, ballY, 16.44f));

        // Restore solidBalls and stripedBalls lists to their original state
        // solidBalls.Clear();
        // solidBalls.AddRange(solidBalls_arr);
        
        // stripedBalls.Clear();
        // stripedBalls.AddRange(stripedBalls_arr);

        // // Clear pocketedSolidBalls and pocketedStripedBalls lists
        // pocketedSolidBalls.Clear();
        // pocketedStripedBalls.Clear();
    }


    private void resetSceneWhenCueBallPocketed()
    {
        // Adjust the cue ball and clear forces
        cueBallTransform.localPosition = new Vector3(-29.75f, ballY, 16.44f);
        cueBall.GetComponent<Rigidbody>().velocity = Vector3.zero;
        cueBall.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }



    // Helper function, checks that all balls have a velocity less than a specified threshold
    // Returns true if all balls have stopped moving
    private bool BallsAreMoving()
    {
        // Check if solid balls have stopped moving
        foreach (GameObject ball in solidBalls)
        {
            float y = ball.transform.localPosition.y;
            // If they are still moving in the pocket, that is okay
            if (ball.GetComponent<Rigidbody>().velocity.magnitude > velocityThreshold && y > yThreshold)
            {

                return true;
            }
        }

        // Check if striped balls have stopped moving
        foreach (GameObject ball in stripedBalls)
        {
            float y = ball.transform.localPosition.y;

            // If they are still moving in the pocket, that is okay
            if (ball.GetComponent<Rigidbody>().velocity.magnitude > velocityThreshold && y > yThreshold)
            {

                return true;
            }
        }

        // Check if cue ball has stopped moving
        if (cueBall.GetComponent<Rigidbody>().velocity.magnitude > velocityThreshold && cueBall.transform.localPosition.y > yThreshold)
        {

            return true;
        }

        // Check if black ball has stopped moving
        if (blackBall.GetComponent<Rigidbody>().velocity.magnitude > velocityThreshold && blackBall.transform.localPosition.y > yThreshold)
        {

            return true;
        }


        return false;

    }

    private Vector3 normalizeVector3(Vector3 vec)
    {
        // normalizedValue = (currentValue - minValue)/(maxValue - minValue)
        vec.x = (vec.x - x_min) / (x_max - x_min);
        vec.y = (vec.y - y_min) / (y_max - y_min);
        vec.z = (vec.z - z_min) / (z_max - z_min);

        return vec;
    }

    // Helper function, checks that all balls are within bounds of the pool table
    // Returns true if all balls are within the pool table
    private bool BallsInBounds()
    {
        float z_lower_threshold = z_min;
        float z_upper_threshold = z_max;

        float x_lower_threshold = x_min;
        float x_upper_threshold = x_max;

        foreach (GameObject ball in solidBalls)
        {
            float z = ball.transform.localPosition.z;
            float x = ball.transform.localPosition.x;

            if (z < z_lower_threshold || z > z_upper_threshold)
            {
                return false;
            }

            if (x < x_lower_threshold || x > x_upper_threshold)
            {

                return false;
            }

        }

        foreach (GameObject ball in stripedBalls)
        {
            float z = ball.transform.localPosition.z;
            float x = ball.transform.localPosition.x;

            if (z < z_lower_threshold || z > z_upper_threshold)
            {

                return false;
            }

            if (x < x_lower_threshold || x > x_upper_threshold)
            {

                return false;
            }

        }

        // Check cue balls
        float zCue = cueBall.transform.localPosition.z;
        float xCue = cueBall.transform.localPosition.x;

        if (zCue < z_lower_threshold || zCue > z_upper_threshold)
        {
            return false;
        }

        if (xCue < x_lower_threshold || xCue > x_upper_threshold)
        {

            return false;
        }

        // Check black ball
        float zBlack = blackBall.transform.localPosition.z;
        float xBlack = blackBall.transform.localPosition.x;

        if (zBlack < z_lower_threshold || zBlack > z_upper_threshold)
        {

            return false;
        }

        if (xBlack < x_lower_threshold || xBlack > x_upper_threshold)
        {

            return false;
        }


        return true;

    }

    private void resetSolidBallPositions(GameObject[] solidBalls, float ballY)
    {
        // Adjust solid ball positions
        solidBalls[0].transform.localPosition = new Vector3(23.7984f, ballY, 16.56083f);
        solidBalls[0].GetComponent<Rigidbody>().velocity = Vector3.zero;
        solidBalls[0].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        solidBalls[1].transform.localPosition = new Vector3(25.86276f, ballY, 17.93056f);
        solidBalls[1].GetComponent<Rigidbody>().velocity = Vector3.zero;
        solidBalls[1].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        solidBalls[2].transform.localPosition = new Vector3(29.72276f, ballY, 20.58056f);
        solidBalls[2].GetComponent<Rigidbody>().velocity = Vector3.zero;
        solidBalls[2].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        solidBalls[3].transform.localPosition = new Vector3(31.66275f, ballY, 22.06056f);
        solidBalls[3].GetComponent<Rigidbody>().velocity = Vector3.zero;
        solidBalls[3].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        solidBalls[4].transform.localPosition = new Vector3(27.9184f, ballY, 13.61083f);
        solidBalls[4].GetComponent<Rigidbody>().velocity = Vector3.zero;
        solidBalls[4].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        solidBalls[5].transform.localPosition = new Vector3(29.8784f, ballY, 14.84083f);
        solidBalls[5].GetComponent<Rigidbody>().velocity = Vector3.zero;
        solidBalls[5].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        solidBalls[6].transform.localPosition = new Vector3(31.79276f, ballY, 13.40056f);
        solidBalls[6].GetComponent<Rigidbody>().velocity = Vector3.zero;
        solidBalls[6].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

    }

    private void resetStripedBallPositions(GameObject[] stripedBalls, float ballY)
    {
        // Adjust striped ball positions
        stripedBalls[0].transform.localPosition = new Vector3(27.85241f, ballY, 19.22486f);
        stripedBalls[0].GetComponent<Rigidbody>().velocity = Vector3.zero;
        stripedBalls[0].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        stripedBalls[1].transform.localPosition = new Vector3(25.85276f, ballY, 14.87056f);
        stripedBalls[1].GetComponent<Rigidbody>().velocity = Vector3.zero;
        stripedBalls[1].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        stripedBalls[2].transform.localPosition = new Vector3(29.66276f, ballY, 12.06056f);
        stripedBalls[2].GetComponent<Rigidbody>().velocity = Vector3.zero;
        stripedBalls[2].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        stripedBalls[3].transform.localPosition = new Vector3(31.70275f, ballY, 10.85056f);
        stripedBalls[3].GetComponent<Rigidbody>().velocity = Vector3.zero;
        stripedBalls[3].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        stripedBalls[4].transform.localPosition = new Vector3(31.92276f, ballY, 15.93056f);
        stripedBalls[4].GetComponent<Rigidbody>().velocity = Vector3.zero;
        stripedBalls[4].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        stripedBalls[5].transform.localPosition = new Vector3(31.83276f, ballY, 18.99056f);
        stripedBalls[5].GetComponent<Rigidbody>().velocity = Vector3.zero;
        stripedBalls[5].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

        stripedBalls[6].transform.localPosition = new Vector3(29.84276f, ballY, 17.88056f);
        stripedBalls[6].GetComponent<Rigidbody>().velocity = Vector3.zero;
        stripedBalls[6].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

    }
}
