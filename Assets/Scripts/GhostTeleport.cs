using System.Collections;
using UnityEngine;

public class GhostTeleportAndPathFollower : MonoBehaviour
{
    [Header("Teleport Timing")]
    [SerializeField] private float minTeleportInterval = 5f;
    [SerializeField] private float maxTeleportInterval = 8f;
    [SerializeField] private float prePortalLeadTime = 1f;
    [SerializeField] private float portalSpawnStagger = 0.2f;
    [SerializeField] private float waitAtIncomingPortal = 0.5f;
    [SerializeField] private float waitAtOutgoingPortal = 0.5f;

    [Header("Teleport Destinations")]
    [SerializeField] private Transform[] teleportDestinations;

    [Header("Portal VFX")]
    [SerializeField] private GameObject portalPrefab;
    [SerializeField] private float portalLifetimeAfterTeleport = 0.3f;
    [SerializeField] private float portalHeightOffset = 0.3f;

    [Header("Path Following")]
    [SerializeField] private PathDefinition[] paths;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float waitTimeAtPoint = 0.3f;
    [SerializeField] private bool loopPath = true;

    [System.Serializable]
    public class PathDefinition
    {
        public Transform[] pathPoints;
    }

    private int _currentPathIndex = 0;
    private int _currentPointIndex = 0;

    private bool _isPathWaiting = false;
    private float _pathWaitTimer = 0f;

    private bool _isTeleportFrozen = false;

    private int _lastDestinationIndex = -1;
    private int _pathDirection = 1;

    private Coroutine _teleportRoutine;

    private GameObject _activeOriginPortal = null;
    private GameObject _activeDestPortal = null;

    private void Start()
    {
        if (teleportDestinations == null || teleportDestinations.Length == 0) { enabled = false; return; }
        if (paths == null || paths.Length != teleportDestinations.Length) { enabled = false; return; }

        transform.position = teleportDestinations[0].position;
        transform.rotation = teleportDestinations[0].rotation;

        SetCurrentPath(0);

        _teleportRoutine = StartCoroutine(TeleportLoop());
    }

    private void Update()
    {
        if (_isTeleportFrozen) return;
        if (paths == null || paths.Length == 0) return;
        FollowPath();
    }

    private void FollowPath()
    {
        PathDefinition path = paths[_currentPathIndex];
        int userCount = (path != null && path.pathPoints != null) ? path.pathPoints.Length : 0;
        int totalPoints = userCount + 1;
        if (totalPoints <= 0) return;

        if (_isPathWaiting)
        {
            _pathWaitTimer -= Time.deltaTime;
            if (_pathWaitTimer <= 0f)
            {
                _isPathWaiting = false;
                AdvancePoint(userCount);
            }
            return;
        }

        Vector3 targetPos;
        Quaternion targetRot;

        if (_currentPointIndex == 0)
        {
            targetPos = teleportDestinations[_currentPathIndex].position;
            targetRot = teleportDestinations[_currentPathIndex].rotation;
        }
        else
        {
            int userIndex = _currentPointIndex - 1;
            if (path.pathPoints[userIndex] == null) return;
            targetPos = path.pathPoints[userIndex].position;
            targetRot = path.pathPoints[userIndex].rotation;
        }

        float dist = Vector3.Distance(transform.position, targetPos);

        if (dist <= 0.05f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
            _isPathWaiting = true;
            _pathWaitTimer = waitTimeAtPoint;
            return;
        }

        Vector3 dir = (targetPos - transform.position).normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;

        if (rotationSpeed > 0f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    private void AdvancePoint(int userPointCount)
    {
        int totalPoints = userPointCount + 1;
        if (totalPoints <= 1) return;

        if (_pathDirection > 0)
        {
            if (_currentPointIndex >= totalPoints - 1)
            {
                _pathDirection = -1;
                _currentPointIndex = totalPoints - 2;
            }
            else
            {
                _currentPointIndex++;
            }
        }
        else
        {
            if (_currentPointIndex <= 0)
            {
                _pathDirection = 1;
                _currentPointIndex = 1;
            }
            else
            {
                _currentPointIndex--;
            }
        }
    }

    private void SetCurrentPath(int index)
    {
        _currentPathIndex = Mathf.Clamp(index, 0, paths.Length - 1);
        _currentPointIndex = 0;
        _isPathWaiting = false;
        _pathWaitTimer = 0f;
        _pathDirection = 1;
    }

    public void StopTeleportAndCleanup()
    {
        if (_teleportRoutine != null)
        {
            StopCoroutine(_teleportRoutine);
            _teleportRoutine = null;
        }

        _isTeleportFrozen = false;

        if (_activeOriginPortal != null)
        {
            Destroy(_activeOriginPortal);
            _activeOriginPortal = null;
        }
        if (_activeDestPortal != null)
        {
            Destroy(_activeDestPortal);
            _activeDestPortal = null;
        }
    }

    private IEnumerator TeleportLoop()
    {
        Quaternion xTiltRotation = Quaternion.Euler(-90f, 0f, 0f);

        while (true)
        {
            float interval = Random.Range(minTeleportInterval, maxTeleportInterval);
            float waitBeforePortal = Mathf.Max(0f, interval - prePortalLeadTime);
            yield return new WaitForSeconds(waitBeforePortal);

            int count = teleportDestinations.Length;
            int destIndex;

            do
            {
                destIndex = Random.Range(0, count);
            }
            while (count > 1 && destIndex == _lastDestinationIndex);

            Transform dest = teleportDestinations[destIndex];

            if (portalPrefab != null)
            {
                Quaternion destYRotation = Quaternion.Euler(0f, dest.rotation.eulerAngles.y, 0f);
                Quaternion finalDestRotation = destYRotation * xTiltRotation;
                Vector3 destPos = dest.position + Vector3.up * portalHeightOffset;
                _activeDestPortal = Instantiate(portalPrefab, destPos, finalDestRotation);
            }

            yield return new WaitForSeconds(portalSpawnStagger);

            if (portalPrefab != null)
            {
                Quaternion originYRotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f);
                Quaternion finalOriginRotation = originYRotation * xTiltRotation;
                Vector3 originPos = transform.position + Vector3.up * portalHeightOffset;
                _activeOriginPortal = Instantiate(portalPrefab, originPos, finalOriginRotation);
            }

            _isTeleportFrozen = true;

            yield return new WaitForSeconds(waitAtOutgoingPortal);

            transform.position = dest.position;
            transform.rotation = dest.rotation;

            SetCurrentPath(destIndex);

            _lastDestinationIndex = destIndex;

            yield return new WaitForSeconds(waitAtIncomingPortal);

            if (_activeOriginPortal != null) Destroy(_activeOriginPortal, portalLifetimeAfterTeleport);
            if (_activeDestPortal != null) Destroy(_activeDestPortal, portalLifetimeAfterTeleport);

            _activeOriginPortal = null;
            _activeDestPortal = null;

            _isTeleportFrozen = false;
        }
    }
}
