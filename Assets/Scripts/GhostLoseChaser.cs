using UnityEngine;

public class GhostLoseChaser : MonoBehaviour
{
    [Header("Chase Settings")]
    public float chaseSpeed = 3f;
    public float rotateSpeed = 10f;
    public Vector3 targetOffset = new Vector3(0, 0.0f, 0);

    public bool flipYRotation = false;

    [Header("Stop Condition")]
    public float stopDistance = 0.5f;

    private Transform _target;
    private bool _isChasing = false;

    public void StartChase(Transform target)
    {
        _target = target;
        _isChasing = true;
    }

    public void StopChase()
    {
        _isChasing = false;
    }

    private void Update()
    {
        if (!_isChasing || _target == null) return;

        Vector3 targetPos = _target.position + targetOffset;
        Vector3 currentPos = transform.position;

        Vector3 dir = targetPos - currentPos;

        if (dir.sqrMagnitude <= stopDistance * stopDistance)
        {
            _isChasing = false;
            gameObject.SetActive(false); 
            return;
        }

        dir.Normalize();

        transform.position += dir * chaseSpeed * Time.deltaTime;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

        if (flipYRotation)
        {
            targetRot *= Quaternion.Euler(0, 180f, 0);
        }

        // Smooth rotation
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotateSpeed * Time.deltaTime
        );
    }
}
