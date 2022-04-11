using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using VehiclePhysics;

public class CarAgent : Agent
{
// ------------------------- Public data members. --------------------------- //
    [Header("Car agent related values")]
    public GameObject CarAgentObject;
    public Vector3 StartAgentPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 StartAgentRotation = new Vector3(0.0f, 0.0f, 0.0f);

    [Header("Other values")]
    public Transform Checkpoints;

// ------------------------------ Methods. ---------------------------------- //
    // Constructor
    CarAgent() {
        RAY_ANGLES = new float[NUM_OF_RAY_ANGLES];
        float angleInterval = (MAX_RAY_ANGLE - MIN_RAY_ANGLE) / (NUM_OF_RAY_ANGLES - 1);
        for (int i = 0; i < NUM_OF_RAY_ANGLES; ++i) {
            RAY_ANGLES[i] = MIN_RAY_ANGLE + i * angleInterval;
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
        
        // Create temp arrays (needed to simplify m_trackSectors initialization)
        Vector2[] checkptPos = new Vector2[Checkpoints.childCount];
        CheckpointSingle[] checkpts = new CheckpointSingle[Checkpoints.childCount];
        int idx = 0;
        foreach (Transform checkpoint in Checkpoints) {
            checkpts[idx] = checkpoint.GetComponent<CheckpointSingle>();
            checkptPos[idx] = new Vector2(checkpoint.position.x, checkpoint.position.z);
            ++idx;
        }
        
        // Initialize list of track sectors.
        m_trackSectors = new List<TrackSector>();
        for (int i = 0; i < checkpts.Length; ++i) {
            int nextPosIdx = (i+1) % checkpts.Length;
            m_trackSectors.Add(new TrackSector(
                    checkpts[nextPosIdx],
                    checkptPos[nextPosIdx] - checkptPos[i],
                    checkpts[i].SectorSpeed));
        }

        // Get vehicle controller component.
        m_vehicleController = CarAgentObject.GetComponent<VPVehicleController>();
    }

    // It executes before each agent's episode begin.
    public override void OnEpisodeBegin() {
        if (m_reposOnEpisodeBegin) {
            _doCarReposition();
            m_curSectorIdx = _lastSectorIdx();
        }
        m_reposOnEpisodeBegin = true;
    }

    void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out CheckpointSingle checkpointSingle)) {
            if (m_trackSectors[m_curSectorIdx].nextCheckpoint() == checkpointSingle) {
                if (m_curSectorIdx == _lastSectorIdx()) {
                    if (m_endEpisodeOnCrossTheLine) {
                        m_reposOnEpisodeBegin = false;
                        EndEpisode();
                    } else {
                        m_endEpisodeOnCrossTheLine = true;
                    }
                }
                m_curSectorIdx = (m_curSectorIdx + 1) % m_trackSectors.Count;
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
        _computeStepRewards(throttle);
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

    private void _computeStepRewards(float throttle) {
        if (_isOutOfRoad()) {
            SetReward(-1.0f);
            EndEpisode();
        } else {
            // Add velocity reward.
            float velocityReward = _speedReward() * _directionReward();
            AddReward(RW_VELOCITY * velocityReward);
            // Add throttle reward.
            AddReward(RW_THROTTLE * throttle);
            // Add road center reward.
            AddReward(RW_ROAD_CENTER * (1.0f / RAY_ANGLES.Length) * _numOfRaysOnTheRoad());
        }
    }

    /*
        Get number of side car rays, which intersect with the road.
    */
    private int _numOfRaysOnTheRoad() {
        int numOfRays = 0;
        for (int i = 0; i < RAY_ANGLES.Length; ++i) {
            float angle = RAY_ANGLES[i];
            Vector3 leftRayDir =
                    Quaternion.AngleAxis(angle, m_comTransform.forward) * -(m_comTransform.up);
            Vector3 rightRayDir =
                    Quaternion.AngleAxis(-angle, m_comTransform.forward) * -(m_comTransform.up);
            if (Physics.Raycast(
                    m_comTransform.position,
                    leftRayDir,
                    5.0f,
                    RACE_TRACK_LAYER_MASK)) {
                numOfRays += 1;
            }
            if (Physics.Raycast(
                    m_comTransform.position,
                    rightRayDir,
                    5.0f,
                    RACE_TRACK_LAYER_MASK)) {
                numOfRays += 1;
            }
        }
        return numOfRays;
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
        It returns value from the range <-1:1>, where -1 means the worst and 1
        the best car speed possible. 
    */
    private float _speedReward() {
        // Get expected speed.
        int expSpeed = m_trackSectors[m_curSectorIdx].expectedSpeed();
        // Get actual speed.
        int actSpeed = m_vehicleController.data.Get(Channel.Vehicle, VehicleData.Speed);
        // Compute both parts of the fraction.
        float numerator = -2 * Mathf.Abs(expSpeed - actSpeed);
        float denominator = Mathf.Max(expSpeed, MAX_SPEED - expSpeed);
        // Return result.
        return numerator / denominator + 1;
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
    private float _directionReward() {
        // Get expected car direction.
        Vector2 expectedDir = m_trackSectors[m_curSectorIdx].expectedDir();
        
        // Get actual car direction.
        Vector2 actualDir = new Vector2(m_carTransform.forward.x, m_carTransform.forward.z);

        // Compute angle between expectedDir and actualDir.
        float angle = Vector2.Angle(expectedDir, actualDir);

        // Compute cosinus for angle and return as result.
        return Mathf.Cos(Mathf.Deg2Rad * angle);
    }

    private int _lastSectorIdx() {
        return m_trackSectors.Count - 1;
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

    /*
        List of track sectors. Track sector is a road segment placed between two
        consecutive checkpoints. Each track sector has reference to the next checkpoint
        (the checkpoint which ends the sector) as well as expected driving direction vector
        and expected speed scalar.
    */
    private List<TrackSector> m_trackSectors;
    /*
        Index of track sector where currently the car is.
    */
    private int m_curSectorIdx = 0;

    // Array of angles between car rays and -(m_comTransform.up) direction vector.
    private readonly float[] RAY_ANGLES;
    private const int NUM_OF_RAY_ANGLES = 14;
    private const float MIN_RAY_ANGLE = 72.5f;
    private const float MAX_RAY_ANGLE = 85.5f;
    
    // Race track layer mask constant.
    private const int RACE_TRACK_LAYER_MASK = 1 << 8;

    // Max speed possible to achieve by car on this track.
    private const int MAX_SPEED = 34000;

// --------------------- Reward and penalty constants. ---------------------- //
    /*
        Weight of the velocity reward. Velocity reward encourage agent to keep
        the car driving with the right speed toward the right direction
        (because Velocity = Speed * Direction).
    */
    private const float RW_VELOCITY = 0.5f;
    /*
        Encourage to accelerate rather than brake - especially needed
        at the beginning of the training.
    */
    private const float RW_THROTTLE = 0.25f;
    /*
        Encourage to keep car driving on the middle of the road.
    */
    private const float RW_ROAD_CENTER = 0.25f;
    
}
