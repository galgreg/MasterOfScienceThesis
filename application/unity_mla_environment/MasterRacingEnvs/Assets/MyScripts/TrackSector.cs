using UnityEngine;

public class TrackSector {
    public TrackSector(
            CheckpointSingle nextCheckpoint,
            Vector2 expectedDir,
            int expectedSpeed) {
        m_nextCheckpoint = nextCheckpoint;
        m_expectedDir = expectedDir;
        m_expectedSpeed = expectedSpeed;
    }
    public CheckpointSingle nextCheckpoint() {
        return m_nextCheckpoint;
    }
    public Vector2 expectedDir() {
        return m_expectedDir;
    }
    public int expectedSpeed() {
        return m_expectedSpeed;
    }

    /*
        Checkpoint which begins this sector.
    */
    private CheckpointSingle m_nextCheckpoint;
    /*
        Expected direction. It's defined as a XZ-plane 2D vector.
    */
    private Vector2 m_expectedDir;
    /*
        Speed scalar is integer value equal speed in m/s * 1000 (e.g. 14 m/s = 14500).
    */
    private int m_expectedSpeed = 0;
}