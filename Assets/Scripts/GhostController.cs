using UnityEngine;

public class GhostPathFollower : MonoBehaviour
{
    [Header("Path Settings")]
    public Transform[] pathPoints; 

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float waitTimeAtPoint = 0.3f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;

    private Vector3[] cachedWorldPositions;
    private Quaternion[] cachedWorldRotations;

    private int currentIndex = 0;   
    private int direction = 1;      // 1 = forward, -1 = backward
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

        int count = pathPoints.Length + 1;

        cachedWorldPositions = new Vector3[count];
        cachedWorldRotations = new Quaternion[count];

        // Index 0: starting transform
        cachedWorldPositions[0] = transform.position;
        cachedWorldRotations[0] = transform.rotation;

        // Indices 1..N: path points as placed in the scene
        for (int i = 0; i < pathPoints.Length; i++)
        {
            if (pathPoints[i] == null)
            {
                Debug.LogWarning("[GhostPathFollower] Path point " + i + " is null.");
                enabled = false;
                return;
            }

            cachedWorldPositions[i + 1] = pathPoints[i].position;
            cachedWorldRotations[i + 1] = pathPoints[i].rotation;
        }

        transform.position = cachedWorldPositions[0];
        transform.rotation = cachedWorldRotations[0];
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
        Vector3 targetPos = cachedWorldPositions[currentIndex];
        Quaternion targetRot = cachedWorldRotations[currentIndex];

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationSpeed * Time.deltaTime
        );

        Vector3 toTarget = targetPos - transform.position;
        float distSqr = toTarget.sqrMagnitude;

        // snap to target and handle arrival
        if (distSqr < 0.0001f)
        {
            transform.position = targetPos;
            OnReachPoint();
            return;
        }

        Vector3 step = toTarget.normalized * moveSpeed * Time.deltaTime;

        if (step.sqrMagnitude >= distSqr)
        {
            transform.position = targetPos;
            OnReachPoint();
        }
        else
        {
            transform.position += step;
        }
    }

    private void OnReachPoint()
    {
        // hard-snap to remove any tiny interpolation error
        transform.rotation = cachedWorldRotations[currentIndex];

        if (waitTimeAtPoint > 0f)
        {
            isWaiting = true;
            waitTimer = waitTimeAtPoint;
        }

        // return trip lol
        if (currentIndex == cachedWorldPositions.Length - 1)
            direction = -1;
        else if (currentIndex == 0)
            direction = 1;

        currentIndex += direction;
    }
}
