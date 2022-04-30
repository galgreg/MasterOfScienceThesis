using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using VehiclePhysics;

public class AdvancedCarAgent : Agent
{
// ------------ Public data members (to set from Unity editor). ------------- //
    [Header("Car agent related values")]
    public GameObject CarAgentObject;
    public List<CarLocation> StartCarLocations;

    [Header("Others")]
    public GameObject InvisibleWall;
    public GameObject FinishCheckpoint;
    public Light DirectionalLight;

// -------- Methods overriden from UnityEngine.MonoBehaviour class. --------- //
    // Start is called before the first frame update.
    void Start() {
        // Get vehicle controller component.
        m_vehicleController = CarAgentObject.GetComponent<VPVehicleController>();
    }

    // When car collides with barriers.
    void OnCollisionEnter(Collision other) {
        // Set true on m_doesCollide.
        m_doesCollide = true;

        // Add collision enter penalty.
        _addReward(RW_COLLISION_ENTER * Mathf.Pow(m_currentNormSpeed, 2));
    }

    // When collision with barriers curretly occurs.
    void OnCollisionStay(Collision other) {
        // Add collision stay penalty
        _addReward(RW_COLLISION_STAY);
    }

    // When collision with barriers does end.
    void OnCollisionExit(Collision other) {
        // Set false on m_doesCollide.
        m_doesCollide = false;
    }

    // When car drives through finish line checkpoint.
    void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent(out FinishLine finishLine)) {
            // Print lap time for current episode.
            float endLapTime = Time.time;
            string lapTime = (endLapTime - m_beginLapTime).ToString("0.00");
            Debug.Log("Lap time for episode " + CompletedEpisodes + ": " + lapTime + " secs.");

            // End episode.
            EndEpisode();
        }
    }

// ---------- Methods overriden from Unity.MLAgents.Agent class. ------------ //
    // It executes before each agent's episode begin.
    public override void OnEpisodeBegin() {
        _resetCarPosition();
        _changeLightProperties();
    }

    // Collect vector observations.
    public override void CollectObservations(VectorSensor sensor) {
        // Compute current normalized car speed.
        float carSpeed = m_vehicleController.data.Get(Channel.Vehicle, VehicleData.Speed);
        m_currentNormSpeed = carSpeed / MAX_SPEED;

        // Add current normalized speed as an observation.
        sensor.AddObservation(m_currentNormSpeed);

        // Add to observations information, if car does currently collide with barriers.
        sensor.AddObservation(m_doesCollide ? 1.0f : 0.0f); 
    }

    // Defines what means output returned by neural network.
    // Here also are computed rewards usual for each step.
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        // Get values for throttle and steeringAngle.
        var continuousActions = actionBuffers.ContinuousActions;
        float throttle = Mathf.Clamp(continuousActions[0], -1.0f, 1.0f);
        float steeringAngle = Mathf.Clamp(continuousActions[1], -1.0f, 1.0f);
        
        // Set vehicle input (it means throttle and steering angle).
        _setVehicleInput(throttle, steeringAngle);

        // Add speed reward
        _addReward(RW_SPEED * m_currentNormSpeed);
    }

    // Heuristic is used for testing purposes
    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;
        // Throttle (positive) and brake/backward (negative).
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        // Left (negative) and right (positive).
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

// --------------------------- Private methods. ----------------------------- //
    // Set new random properties for directional light.
    private void _changeLightProperties() {
        if (DirectionalLight != null) {
            var lightTransf = DirectionalLight.transform;
            lightTransf.position = new Vector3(
                Random.Range(-220.0f, 200.0f),
                Random.Range(6.0f, 50.0f),
                Random.Range(-304.0f, -63.0f)
            );
            lightTransf.rotation = Quaternion.Euler(new Vector3(
                Random.Range(0.0f, 180.0f),
                Random.Range(0.0f, 360.0f),
                0.0f
            ));
            DirectionalLight.intensity = Random.Range(0.0f, 2.0f);
            DirectionalLight.color = Random.ColorHSV();
        }
    }

    // Set vehicle input from given throttle and steeringAngle value.
    private void _setVehicleInput(float throttle, float steeringAngle) {
        const int MAX_VAL = 10000;
        m_vehicleController.data.Set(Channel.Input, InputData.Steer, (int)(steeringAngle * MAX_VAL));

        if (m_currentNormSpeed < 0.0f) {
            throttle = -throttle;
        } else if (m_currentNormSpeed == 0.0f) {
            if (throttle > 0 && m_isGearReverse) {
                m_vehicleController.data.Set(Channel.Input, InputData.AutomaticGear, 4);
                m_isGearReverse = false;
            } else if (throttle < 0) {
                if (!m_isGearReverse) {
                    m_vehicleController.data.Set(Channel.Input, InputData.AutomaticGear, 2);
                    m_isGearReverse = true;
                }
                throttle = -throttle;
            }
        }

        m_vehicleController.data.Set(Channel.Input, InputData.Throttle,
                throttle > 0.0f ? (int)(throttle * MAX_VAL) : 0);
        m_vehicleController.data.Set(Channel.Input, InputData.Brake,
                throttle < 0.0f ? (int)(-throttle * MAX_VAL) : 0);
    }

    // Reset car to its start position.
    private void _resetCarPosition() {
        // Reset car location.
        int locationIndex = Random.Range(0, StartCarLocations.Count);
        var carLocation = StartCarLocations[locationIndex];
        var quatRotation = Quaternion.Euler(carLocation.Rotation);
        m_vehicleController.HardReposition(carLocation.Position, quatRotation, true);
        
        // Reset invisible wall location.
        var carBackward = -CarAgentObject.transform.forward;
        InvisibleWall.transform.position = carLocation.Position + carBackward * 2;
        InvisibleWall.transform.rotation = quatRotation;

        // Reset finish checkpoint location.
        FinishCheckpoint.transform.position = carLocation.Position + carBackward * 3;
        FinishCheckpoint.transform.rotation = quatRotation;

        // Set ignition key position on start.
        m_vehicleController.data.Set(Channel.Input, InputData.Key, 1);
        // Set automatic gear on drive mode.
        m_vehicleController.data.Set(Channel.Input, InputData.AutomaticGear, 4);
        // Reset begin lap time.
        m_beginLapTime = Time.time;
    }

    // Add reward, clamped to range [-1.0f : 1.0f].
    private void _addReward(float reward) {
        AddReward(Mathf.Clamp(reward, -1.0f, 1.0f));
    }

// ------------------------ Private data members. --------------------------- //
    // Reference to vehicle controller.
    private VPVehicleController m_vehicleController;
    // Quaternion object for start agent rotation.
    private Quaternion m_quatStartAgentRotation;
    // Used to check if reverse gear is set.
    private bool m_isGearReverse = false;
    // Current speed at normalized form - it must be value between -1 to 1.
    private float m_currentNormSpeed = 0.0f;
    // Time of lap begin.
    private float m_beginLapTime = 0.0f;
    // Checks if car is currently colliding or not.
    private bool m_doesCollide = false;

// ------------------------- Private constants. ----------------------------- //
    // Max speed possible to achieve by car on this track.
    private const float MAX_SPEED = 35000.0f;

//---------------------- Reward weight (RW) constants. ---------------------- //
    private const float RW_SPEED = 0.75f;
    private const float RW_COLLISION_ENTER = -1.0f;
    private const float RW_COLLISION_STAY = -0.5f;
}
