using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointSingle : MonoBehaviour
{
    public void SetCarAgent(CarAgent carAgent) {
        m_carAgent = carAgent;
    }
    
    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent<CarComponent>(out CarComponent car)) {
            m_carAgent.OnCheckpointTrigger(this);
        }
    }

    private CarAgent m_carAgent;
}
