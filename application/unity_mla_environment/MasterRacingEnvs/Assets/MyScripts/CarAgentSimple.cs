using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using VehiclePhysics;

public class CarAgentSimple : Agent
{
// ------------ Public data members (to set from Unity editor). ------------- //
    [Header("Car agent related values")]
    public GameObject CarAgentObject;

    public Vector3 StartCarPosition;
    public Vector3 StartCarRotation;

// -------- Methods overriden from UnityEngine.MonoBehaviour class. --------- //
    // Start is called before the first frame update.
    void Start() {
        // Create quaternion from StartCarRotation.
        m_quatStartAgentRotation = Quaternion.Euler(StartCarRotation);

        // Get vehicle controller component.
        m_vehicleController = CarAgentObject.GetComponent<VPVehicleController>();
    }

    // When car collides with barriers.
    void OnCollisionEnter(Collision other) {
        // Add collision penalty.
        _addReward(RW_COLLISION * Mathf.Pow(m_currentNormSpeed, 2));
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
    }

    // Collect vector observations.
    public override void CollectObservations(VectorSensor sensor) {
        // Compute current normalized car speed.
        float carSpeed = m_vehicleController.data.Get(Channel.Vehicle, VehicleData.Speed);
        m_currentNormSpeed = carSpeed / MAX_SPEED;

        // Add current normalized speed as an observation.
        sensor.AddObservation(m_currentNormSpeed);
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
        // Reset car to its start position and start rotation.
        m_vehicleController.HardReposition(
                StartCarPosition, m_quatStartAgentRotation, true);
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

// ------------------------- Private constants. ----------------------------- //
    // Max speed possible to achieve by car on this track.
    private const float MAX_SPEED = 35000.0f;

//---------------------- Reward weight (RW) constants. ---------------------- //
    private const float RW_SPEED = 1.0f;
    private const float RW_COLLISION = -1.0f;
}
