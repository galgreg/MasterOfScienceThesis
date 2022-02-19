using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CarAgent : Agent
{
    [Header("CarAgent transform")]
    public Vector3 StartAgentPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 StartAgentRotation = new Vector3(0.0f, 0.0f, 0.0f);
    public float CarAgentScale = 1.0f;
    
    // Start is called before the first frame update.
    void Start() {
        // TODO
    }

    // It executes before each agent's episode begin.
    public override void OnEpisodeBegin() {
        // TODO
    }

    // Define here, what means actions received from policy.
    public override void OnActionReceived(ActionBuffers actionBuffers) {
        // TODO
    }

    // Heuristic is used for testing purposes
    public override void Heuristic(in ActionBuffers actionsOut) {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}
