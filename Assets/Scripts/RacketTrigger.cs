using UnityEngine;

public class RacketTrigger : MonoBehaviour
{
    [Header("Setup")]
    public Transform racketPlane;      // The visual mesh or center of the strings
    public Transform sweetSpot;        // Assign a transform at the perfect center of the racket
    public Collider racketCollider;    // The collider on this object

    [Header("Physics Settings")]
    public float forceMultiplier = 1.5f;   // How much force to add based on swing speed
    public float baseBounce = 2.0f;        // Minimum bounce even if racket is still
    [Range(0, 1)] public float swingInfluence = 0.8f; // 1.0 = Ball goes exactly where you swing. 0.0 = Mirror reflection.

    [Header("Precision")]
    public bool useHitPointOffset = true;  // If true, hitting edge of racket angles the ball
    public float offCenterAngleFactor = 30f; // How much the ball angles if hit at the edge

    
    public enum NormalAxis { Forward, Up, Right }
    [Tooltip("Which local axis is the face normal of your 'bat' (pan)?")]
    public NormalAxis hitNormalAxis = NormalAxis.Up;

    [Header("Debug")]
    public bool debugVisuals = true;

    // State
    private Vector3 previousPos;
    private Vector3 currentVelocity;
    private float hitCooldown = 0.0f; // Prevent double hits in the same swing

    void Start()
    {
        previousPos = transform.position;
        if (racketCollider == null) racketCollider = GetComponent<Collider>();
        if (sweetSpot == null) sweetSpot = transform;
        if (racketPlane == null) racketPlane = transform;
    }

    void Update()
    {
        Vector3 displacement = transform.position - previousPos;
        currentVelocity = displacement / Mathf.Max(Time.deltaTime, 0.0001f);
        previousPos = transform.position;

        if (hitCooldown > 0) hitCooldown -= Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (hitCooldown > 0) return;

        if (other.CompareTag("Ball"))
        {
            HandleCollision(other);
            hitCooldown = 0.2f;
        }
    }

    void HandleCollision(Collider ballCollider)
    {
        Rigidbody ballRb = ballCollider.attachedRigidbody;
        if (ballRb == null) return;

        Vector3 hitPoint = racketCollider.ClosestPoint(ballCollider.transform.position);

       
        Vector3 localNormal;
        switch (hitNormalAxis)
        {
            case NormalAxis.Up:
                localNormal = Vector3.up;
                break;
            case NormalAxis.Right:
                localNormal = Vector3.right;
                break;
            default:
                localNormal = Vector3.forward;
                break;
        }
        // Convert local axis to world-space:
        Vector3 faceNormal = racketPlane.TransformDirection(localNormal);
        // ---------------------------------------------------------

        Vector3 swingDirection = currentVelocity.normalized;
        float swingSpeed = currentVelocity.magnitude;

        Vector3 incomingVel = ballRb.velocity;

        // Relative velocity between ball and racket
        Vector3 relativeVelocity = incomingVel - currentVelocity;

        // Mirror reflection using racket face normal
        Vector3 reflectionDir = Vector3.Reflect(relativeVelocity, faceNormal).normalized;

        // Blend between reflection and swing direction
        Vector3 finalDirection = Vector3.Lerp(reflectionDir, swingDirection, swingInfluence).normalized;

        // Optionally angle based on off-center hit
        if (useHitPointOffset)
        {
            Vector3 offsetVector = hitPoint - sweetSpot.position;
            Vector3 planeOffset = Vector3.ProjectOnPlane(offsetVector, faceNormal);

            float deviationAngle = planeOffset.magnitude * offCenterAngleFactor;

            Vector3 rotationAxis = Vector3.Cross(faceNormal, planeOffset).normalized;
            if (rotationAxis.sqrMagnitude > 0.001f)
            {
                Quaternion deflection = Quaternion.AngleAxis(deviationAngle, rotationAxis);
                finalDirection = deflection * finalDirection;
            }
        }

        float totalForce = baseBounce + (swingSpeed * forceMultiplier);
        //totalForce += incomingVel.magnitude * 0.2f;

        ballRb.velocity = finalDirection * totalForce;

        var hoverObj = ballRb.GetComponent<HoverRespawnObject>();
        if (hoverObj != null) hoverObj.ActivatePhysicsFromHit();

        if (debugVisuals)
        {
            Debug.DrawLine(hitPoint, hitPoint + finalDirection * 2f, Color.green, 2f);
            Debug.DrawLine(hitPoint, hitPoint + faceNormal, Color.blue, 2f);
            Debug.Log($"Hit! Speed: {totalForce:F1} | Off-Center: {Vector3.Distance(hitPoint, sweetSpot.position):F3}");
        }
    }
}
