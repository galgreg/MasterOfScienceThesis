using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrackCheckpoints : MonoBehaviour
{
    public CarAgent CarAgentObject;
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
            m_nextCheckpointSingleIdx += 1;
            CarAgentObject.CorrectCheckpointEvent();

            if (m_nextCheckpointSingleIdx == m_checkpointSingleList.Count) {
                CarAgentObject.RaceFinishEvent();
                m_nextCheckpointSingleIdx = 0;
            }
        } else {
            CarAgentObject.WrongCheckpointEvent();
            m_nextCheckpointSingleIdx = 0;
        }
    }
}
