using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCheckpoints : MonoBehaviour
{
    private List<CheckpointSingle> m_checkpointSingleList;
    private int m_nextCheckpointSingleIdx;

    private void Awake() {
        m_checkpointSingleList = new List<CheckpointSingle>();
        Transform checkpointsTransform = transform.Find("Checkpoints");
        foreach (Transform checkpointSingleTransform in checkpointsTransform) {
            var checkpointSingle = checkpointSingleTransform.GetComponent<CheckpointSingle>();
            checkpointSingle.SetTrackCheckpoints(this);
            m_checkpointSingleList.Add(checkpointSingle);
        }
        
        m_nextCheckpointSingleIdx = 0;
    }

    public void CarThroughCheckpoint(CheckpointSingle checkpointSingle) {
        if (m_checkpointSingleList.IndexOf(checkpointSingle) == m_nextCheckpointSingleIdx) {
        // Correct index.
            m_nextCheckpointSingleIdx += 1;
            // TODO - emit normal reward signal.
            Debug.Log("Correct checkpoint!");

            if (m_nextCheckpointSingleIdx == m_checkpointSingleList.Count) {
                // All checkpoints passed, so add more reward and request to 
                // finish simulation.
                Debug.Log("Race finished!");
            }
        } else {
        // Invalid index.
            // TODO - emit big penalty signal and finish training.
            Debug.Log("Wrong checkpoint!");
        }
    }
}
