using UnityEngine;

public class GhostPathFollower : MonoBehaviour
{
    [Header("Path Settings")]
    public Transform[] pathPoints;   // children of the ghost (for organization)

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float waitTimeAtPoint = 0.3f;

    // These store the path we actually follow
    private Vector3[] cachedWorldPositions;

    private int currentIndex = 0;
    private int direction = 1;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    private void Start()
    {
        if (pathPoints == null || pathPoints.Length == 0)
        {
            Debug.LogWarning("[GhostPathFollower] No path points assigned.");
            enabled = false;
            return;
        }

        cachedWorldPositions = new Vector3[pathPoints.Length + 1];
        cachedWorldPositions[0] = transform.position; // start position
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null)
            {
                Debug.LogWarning("[GhostPathFollower] Path point " + i + " is null.");
                enabled = false;
                return;
            }

            cachedWorldPositions[i+1] = pathPoints[i].position; // world-space
        }

        // Optional: you can now ignore the transforms entirely
        // pathPoints = null;

    }

    private void Update()
    {
        if (cachedWorldPositions == null || cachedWorldPositions.Length == 0)
            return;

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
                isWaiting = false;

            return;
        }

        MoveAlongCachedPath();
    }

    private void MoveAlongCachedPath()
    {
        Vector3 target = cachedWorldPositions[currentIndex];
        Vector3 toTarget = target - transform.position;
        float distSqr = toTarget.sqrMagnitude;

        if (distSqr < 0.0001f)
        {
            transform.position = target;
            OnReachPoint();
            return;
        }

        Vector3 step = toTarget.normalized * moveSpeed * Time.deltaTime;

        if (step.sqrMagnitude >= distSqr)
        {
            transform.position = target;
            OnReachPoint();
        }
        else
        {
            transform.position += step;
        }
    }

    private void OnReachPoint()
    {
        if (waitTimeAtPoint > 0f)
        {
            isWaiting = true;
            waitTimer = waitTimeAtPoint;
        }

        if (currentIndex == cachedWorldPositions.Length - 1)
            direction = -1;
        else if (currentIndex == 0)
            direction = 1;

        currentIndex += direction;
    }
}
