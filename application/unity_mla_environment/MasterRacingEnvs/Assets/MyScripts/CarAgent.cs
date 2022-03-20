using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using VehiclePhysics;

/*
    TODO - dobrać właściwe wartości dla stałych wykorzystywanych przy wyliczaniu nagród i kar!
*/
public class CarAgent : Agent
{
// ------------------------- Public data members. --------------------------- //
    [Header("CarAgent object")]
    public GameObject CarAgentObject;

    [Header("CarAgent transform")]
    public Vector3 StartAgentPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 StartAgentRotation = new Vector3(0.0f, 0.0f, 0.0f);

    [Header("Track checkpoints object")]
    public Transform Checkpoints;

// ------------------------------ Methods. ---------------------------------- //
    // Constructor
    CarAgent() {
        RAYS_ANGLES = new float[NUM_OF_RAYS_ANGLES];
        float angleInterval = (MAX_RAY_ANGLE - MIN_RAY_ANGLE) / (NUM_OF_RAYS_ANGLES - 1);
        for (int i = 0; i < NUM_OF_RAYS_ANGLES; ++i) {
            RAYS_ANGLES[i] = MIN_RAY_ANGLE + i * angleInterval;
        }
    }

    // Start is called before the first frame update.
    void Start() {
        // Retrieve transform from CarAgentObject.
        m_carTransform = CarAgentObject.transform;
        m_comTransform = m_carTransform.Find("CoM");
        // Convert StartAgentRotation to quaternion.
        m_quatStartAgentRotation = Quaternion.Euler(
                StartAgentRotation.x,
                StartAgentRotation.y,
                StartAgentRotation.z);
        // Initialize checkpoints related members.
        m_checkpoints = new List<CheckpointSingle>();
        m_checkpointsPos = new List<Vector2>();
        foreach (Transform checkpoint in Checkpoints) {
            var checkpointSingle = checkpoint.GetComponent<CheckpointSingle>();
            m_checkpoints.Add(checkpointSingle);
            m_checkpointsPos.Add(new Vector2(checkpoint.position.x, checkpoint.position.z));
        }

        // Get vehicle controller component.
        m_vehicleController = CarAgentObject.GetComponent<VPVehicleController>();
    }

    // It executes before each agent's episode begin.
    public override void OnEpisodeBegin() {
        if (m_reposOnEpisodeBegin) {
            _doCarReposition();
            m_nextCheckpointIdx = 0;
            m_nextCheckpointPos = m_checkpointsPos[0];
            m_endEpisodeOnCrossTheLine = false;
        }
        m_reposOnEpisodeBegin = true;
    }

    void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out CheckpointSingle checkpointSingle)) {
            if (m_checkpoints.IndexOf(checkpointSingle) == m_nextCheckpointIdx) {
                if (m_nextCheckpointIdx == 0) {
                    if (m_endEpisodeOnCrossTheLine) {
                        m_reposOnEpisodeBegin = false;
                        EndEpisode();
                    } else {
                        m_endEpisodeOnCrossTheLine = true;
                    }
                }
                m_nextCheckpointIdx =
                        (m_nextCheckpointIdx + 1) % m_checkpoints.Count;
                m_nextCheckpointPos = m_checkpointsPos[m_nextCheckpointIdx];
            } else {
                EndEpisode();
            }
        }
    }

    // Define here, what means actions received from policy.
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        var continuousActions = actionBuffers.ContinuousActions;
        float throttle = continuousActions[0];
        float steeringAngle = continuousActions[1];
        _setVehicleInput(throttle, steeringAngle);
        _computeStepRewards(throttle, steeringAngle);
    }

    // Heuristic is used for testing purposes
    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;
        // Throttle (positive) and brake/backward (negative).
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        // Left (negative) and right (positive).
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }
    
    private void _setVehicleInput(float throttle, float steeringAngle) {
        const int MAX_VAL = 10000;
        m_vehicleController.data.Set(Channel.Input, InputData.Steer, (int)(steeringAngle * MAX_VAL));
        m_vehicleController.data.Set(Channel.Input, InputData.Throttle,
                throttle > 0.0f ? (int)(throttle * MAX_VAL) : 0);
        m_vehicleController.data.Set(Channel.Input, InputData.Brake,
                throttle < 0.0f ? (int)(-throttle * MAX_VAL) : 0);
    }

    private void _doCarReposition() {
        // Reset car to its start position and start rotation.
        m_vehicleController.HardReposition(StartAgentPosition, m_quatStartAgentRotation, true);
        // Set ignition key position on start.
        m_vehicleController.data.Set(Channel.Input, InputData.Key, 1);
        // Set manual gear position on first.
        m_vehicleController.data.Set(Channel.Input, InputData.ManualGear, 1);
    }

    private void _computeStepRewards(float throttle, float steeringAngle) {
        // Add throttle reward.
        AddReward(THROTTLE_REWARD * throttle);
        // Add steering angle penalty.
        AddReward(STEERING_ANGLE_PENALTY * System.Math.Abs(steeringAngle));
        
        // Get number of side rays intersecting with the road.
        int roadIntersectRaysCount = _getRoadIntersectRaysCount();
        
        // Add out-of-road penalty or road-center-distance reward.
        if (_isOutOfRoad()) {
            // Add penalty based on how far out of road the car is.
            int numOfSideRays = RAYS_ANGLES.Length / 2;
            AddReward(OUT_OF_ROAD_PENALTY * (1.0f / numOfSideRays) * (numOfSideRays - roadIntersectRaysCount));
        } else {
            // Add reward based on how close to the center of road the car is.
            AddReward(ROAD_CENTER_DISTANCE_REWARD * (1.0f / RAYS_ANGLES.Length) * roadIntersectRaysCount);
        }

        // Add driving direction reward (or penalty).
        AddReward(CAR_DIRECTION_REWARD * _getDirectionScalar());

        // Add step penalty.
        AddReward(STEP_PENALTY);
    }

    /*
        Get number of side car rays, which intersect with the road.
    */
    private int _getRoadIntersectRaysCount() {
        int intersectRaysCount = 0;
        for (int i = 0; i < RAYS_ANGLES.Length; ++i) {
            float angle = RAYS_ANGLES[i];
            Vector3 leftRayDir =
                    Quaternion.AngleAxis(angle, m_comTransform.forward) * -(m_comTransform.up);
            Vector3 rightRayDir =
                    Quaternion.AngleAxis(-angle, m_comTransform.forward) * -(m_comTransform.up);
            if (Physics.Raycast(
                    m_comTransform.position,
                    leftRayDir,
                    5.0f,
                    RACE_TRACK_LAYER_MASK)) {
                intersectRaysCount += 1;
            }
            if (Physics.Raycast(
                    m_comTransform.position,
                    rightRayDir,
                    5.0f,
                    RACE_TRACK_LAYER_MASK)) {
                intersectRaysCount += 1;
            }
        }

        return intersectRaysCount;
    }
    /*
        Check if car is outside of road - it's achieved by checking, if center
        car ray does intersect with the road.
    */
    private bool _isOutOfRoad() {
        return !Physics.Raycast(
                m_comTransform.position,
                -(m_comTransform.up),
                1.0f,
                RACE_TRACK_LAYER_MASK);
    }

    /*
        It returns value from the range <-1:1>, where -1 means the worst car driving
        direction (car drives in the opposite direction) and 1 means the best
        car driving direction (car drives exactly in the right direction).

        Algorithm used to compute result:
        1) Get car position (carPos) and next checkpoint position (nextCheckpointPos)
        2) Compute expected car direction vector (expectedDir) from equation: nextCheckpointPos - carPos.
        3) Get actual car direction vector (actualDir) by accessing car's Transform.forward vector.
        4) Compute angle (angle) between direction vectors, using Vector2.angle method.
        5) Compute cosinus for angle and return as result.

        All vectors are 2D, because only X and Z axes are needed.
    */
    private float _getDirectionScalar() {
        // Get car XZ position.
        Vector2 carPosXZ = new Vector2(m_carTransform.position.x, m_carTransform.position.z);

        // Compute expected car direction.
        Vector2 expectedDir = m_nextCheckpointPos - carPosXZ;
        
        // Get actual car direction.
        Vector2 actualDir = new Vector2(m_carTransform.forward.x, m_carTransform.forward.z);

        // Compute angle between expectedDir and actualDir.
        float angle = Vector2.Angle(expectedDir, actualDir);

        // Compute cosinus for angle and return as result.
        return Mathf.Cos(Mathf.Deg2Rad * angle);
    }

// ------------------------- Private data members. -------------------------- //
    // Reference to vehicle controller 
    private VPVehicleController m_vehicleController;
    // Quaternion object for start agent rotation.
    private Quaternion m_quatStartAgentRotation;
    // Used to check, if reposition should be done on episode begin.
    private bool m_reposOnEpisodeBegin = true;
    // Used to check, if episode should be ended on the cross of the lap line.
    private bool m_endEpisodeOnCrossTheLine = false;

    // Reference to car transform object.
    private Transform m_carTransform;
    // Reference to car's center-of-mass transform.
    private Transform m_comTransform;
    // Next checkpoint XZ position.
    private Vector2 m_nextCheckpointPos;

    /*
        List of CheckpointSingle objects. The order must match with the order of
        items being children of Checkpoints transform.
    */
    private List<CheckpointSingle> m_checkpoints;
    /*
        List of XZ checkpoint positions. The order must match with the order of
        items being children of Checkpoints transform.
    */
    private List<Vector2> m_checkpointsPos;
    // Index of next checkpoint index.
    private int m_nextCheckpointIdx = 0;
    // Race track layer mask constant.
    private const int RACE_TRACK_LAYER_MASK = 1 << 8;
    // Array of angles between car rays and -(m_comTransform.up) direction vector.
    private readonly float[] RAYS_ANGLES = { 72.5f, 73.5f, 74.5f, 75.5f, 76.5f, 77.5f, 78.5f, 79.5f, 80.5f, 81.5f, 82.5f, 83.5f, 84.5f, 85.5f };
    private const float MIN_RAY_ANGLE = 72.5f;
    private const float MAX_RAY_ANGLE = 85.5f;
    private const int NUM_OF_RAYS_ANGLES = 14;

// --------------------- Reward and penalty constants. ---------------------- //
    // Encourage to accelerate rather than brake.
    private const float THROTTLE_REWARD = 0.25f;
    // Discourage to do redundant steering wheel movements.
    private const float STEERING_ANGLE_PENALTY = -0.25f;
    // Discourage to pull off the road.
    private const float OUT_OF_ROAD_PENALTY = -0.25f;
    // Encourage to keep car on the center of the road.
    private const float ROAD_CENTER_DISTANCE_REWARD = 0.25f;
    // Encourage to keep the correct driving direction.
    private const float CAR_DIRECTION_REWARD = 0.25f;
    // Encourage to minimize steps needed to finish episode.
    private const float STEP_PENALTY = -0.1f;
}
