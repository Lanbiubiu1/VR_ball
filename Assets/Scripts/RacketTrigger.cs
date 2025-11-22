using UnityEngine;

public class RacketTrigger : MonoBehaviour
{
    public Transform racketPlane;  // 球拍平面（球拍面）
    public Transform ball;
    public float minHitSpeed = 6f;
    public float speedMultiplier = 1.2f; // 击球时使用的倍率
    public float velocitySmoothing = 0.2f; // 速度平滑插值系数 (0-1)
    public bool debugVelocity = false;

    private Vector3 previousPos;
    private Vector3 smoothedVelocity;

    private Vector3 ballLastPos;

    void Start()
    {
        ballLastPos = ball.position;
        previousPos = transform.position;
    }

    void Update()
    {
        // 原始速度（世界空间）
        Vector3 rawVel = (transform.position - previousPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        previousPos = transform.position;

        // 简单平滑
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVel, velocitySmoothing);

        if (debugVelocity)
        {
            Debug.DrawLine(transform.position, transform.position + smoothedVelocity * 0.05f, Color.yellow);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            HitBall(racketPlane.forward, other);
        }
    }

    void HitBall(Vector3 normal, Collider other)
    {
        // 优先使用触发到的球体刚体
        Rigidbody rb = other.attachedRigidbody != null ? other.attachedRigidbody : ball.GetComponent<Rigidbody>();

        var hoverObj = rb.GetComponent<HoverRespawnObject>();
        if (hoverObj != null)
        {
            hoverObj.ActivatePhysicsFromHit();
        }

        // 反弹方向 = 平面法线
        Vector3 dir = normal;
        float racketSpeed = smoothedVelocity.magnitude;
        float appliedSpeed = Mathf.Max(minHitSpeed, racketSpeed * speedMultiplier);
        rb.velocity = dir.normalized * appliedSpeed;

        if (debugVelocity)
        {
            Debug.Log($"Hit Ball | RacketSpeed={racketSpeed:F2} Applied={appliedSpeed:F2}");
        }
    }
}
