using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using VehiclePhysics;

public class CarAgent : Agent
{
    [Header("CarAgent object")]
    public GameObject CarAgentObject;

    [Header("CarAgent transform")]
    public Vector3 StartAgentPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 StartAgentRotation = new Vector3(0.0f, 0.0f, 0.0f);

    // Reference to vehicle controller 
    private VPVehicleController m_vehicleController;
    // Quaternion object for start agent rotation.
    private Quaternion m_quatStartAgentRotation;
    // It states if should be reposition done on episode begin.
    private bool m_reposOnEpisodeBegin = true;
    private const float CORRECT_CHECKPOINT_REWARD = 1.0f;
    private const float WRONG_CHECKPOINT_PENALTY = -1.0f;
    private const float RACE_FINISHED_REWARD = 1.0f;
    private const float SMALL_STEP_PENALTY = -0.005f;

    // Start is called before the first frame update.
    void Start() {
        m_quatStartAgentRotation = Quaternion.Euler(
                StartAgentRotation.x,
                StartAgentRotation.y,
                StartAgentRotation.z);
        m_vehicleController = CarAgentObject.GetComponent<VPVehicleController>();
        // Set ignition key position on start.
        m_vehicleController.data.Set(Channel.Input, InputData.Key, 1);
    }

    // It executes before each agent's episode begin.
    public override void OnEpisodeBegin() {
        if (m_reposOnEpisodeBegin) {
            _doCarReposition();
            m_reposOnEpisodeBegin = false;
        }
    }

    // Define here, what means actions received from policy.
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        var continuousActions = actionBuffers.ContinuousActions;
        float throttle = continuousActions[0];
        float steeringAngle = continuousActions[1];
        _setVehicleInput(throttle, steeringAngle);
        AddReward(SMALL_STEP_PENALTY);
    }

    // Heuristic is used for testing purposes
    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;
        // Throttle (positive) and brake/backward (negative).
        continuousActionsOut[0] = Input.GetAxis("Vertical");
        // Left (negative) and right (positive).
        continuousActionsOut[1] = Input.GetAxis("Horizontal");
    }

    public void CorrectCheckpointEvent() {
        SetReward(CORRECT_CHECKPOINT_REWARD);
    }
    public void RaceFinishEvent() {
        SetReward(RACE_FINISHED_REWARD);
        EndEpisode();
    }
    public void WrongCheckpointEvent() {
        SetReward(WRONG_CHECKPOINT_PENALTY);
        m_reposOnEpisodeBegin = true;
        EndEpisode();
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
        m_vehicleController.HardReposition(StartAgentPosition, m_quatStartAgentRotation);
        // Set manual gear position on first.
        m_vehicleController.data.Set(Channel.Input, InputData.ManualGear, 1);
    }
}
