using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using VehiclePhysics;

public class CarAgentSimple : Agent
{
    [Header("Car agent related values")]
    public GameObject CarAgentObject;

    public Vector3 StartCarPosition;
    public Vector3 StartCarRotation;

    // Start is called before the first frame update.
    void Start() {
        // Create quaternion from StartCarRotation.
        m_quatStartAgentRotation = Quaternion.Euler(StartCarRotation);

        // Get vehicle controller component.
        m_vehicleController = CarAgentObject.GetComponent<VPVehicleController>();
    }

    // It executes before each agent's episode begin.
    public override void OnEpisodeBegin() {
        _doCarReposition();
    }

    void OnCollisionEnter(Collision other) {
        m_doesCollide = true;
    }
    void OnCollisionExit(Collision other) {
        m_doesCollide = false;
    }

    public override void OnActionReceived(ActionBuffers actionBuffers) {
        var continuousActions = actionBuffers.ContinuousActions;
        float throttle = Mathf.Clamp(continuousActions[0], -1.0f, 1.0f);
        float steeringAngle = Mathf.Clamp(continuousActions[1], -1.0f, 1.0f);
        _setVehicleInput(throttle, steeringAngle);
        _computeStepRewards();
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
        
        int carSpeed = m_vehicleController.data.Get(Channel.Vehicle, VehicleData.Speed);
        if (carSpeed < 0) {
            throttle = -throttle;
        } else if (carSpeed == 0) {
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

    private void _doCarReposition() {
        // Reset car to its start position and start rotation.
        m_vehicleController.HardReposition(
                StartCarPosition, m_quatStartAgentRotation, true);
        // Set ignition key position on start.
        m_vehicleController.data.Set(Channel.Input, InputData.Key, 1);
        // Set automatic gear on drive mode.
        m_vehicleController.data.Set(Channel.Input, InputData.AutomaticGear, 4);
    }

    private void _computeStepRewards() {
        // TODO
    }

// ------------------------- Private data members. -------------------------- //
    // Reference to vehicle controller 
    private VPVehicleController m_vehicleController;
    // Quaternion object for start agent rotation.
    private Quaternion m_quatStartAgentRotation;
    // Used to check if car collides with wall or not.
    private bool m_doesCollide = false;
    // Used to check if reverse gear is set
    private bool m_isGearReverse = false;
}
