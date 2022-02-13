using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    private TrackCheckpoints m_trackCheckpoints;
    
    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent<CarComponent>(out CarComponent car)) {
            m_trackCheckpoints.CarThroughCheckpoint(this);
        }
    }

    public void SetTrackCheckpoints(TrackCheckpoints trackCheckpoints) {
        m_trackCheckpoints = trackCheckpoints;
    }
}
