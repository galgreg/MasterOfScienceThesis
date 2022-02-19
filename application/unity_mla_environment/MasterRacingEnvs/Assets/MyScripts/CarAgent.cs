using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CarAgent : Agent
{
    [Header("CarAgent object")]
    public GameObject CarAgentObject;

    [Header("CarAgent transform")]
    public Vector3 StartAgentPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 StartAgentRotation = new Vector3(0.0f, 0.0f, 0.0f);
    
    // Start is called before the first frame update.
    void Start() {
        // TODO
    }

    // It executes before each agent's episode begin.
    public override void OnEpisodeBegin() {
        CarAgentObject.transform.position = StartAgentPosition;
        CarAgentObject.transform.eulerAngles = StartAgentRotation;
    }

    // Define here, what means actions received from policy.
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        var continuousActions = actionBuffers.ContinuousActions;
        float throttle = continuousActions[0];
        float steeringAngle = continuousActions[1];

        // TODO - set throttle and steering angle on CarAgentObject input.
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
        // TODO
    }
    public void RaceFinishEvent() {
        // TODO
    }
    public void WrongCheckpointEvent() {
        // TODO
    }
}
