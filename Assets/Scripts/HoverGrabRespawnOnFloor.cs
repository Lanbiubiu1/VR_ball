using System.Collections;
using UnityEngine;
using Oculus.Interaction;  // for Grabbable & PointerEvent

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class HoverRespawnObject : MonoBehaviour
{
    public enum ObjectKind
    {
        Bat,
        Ball
    }

    [Header("Type")]
    public ObjectKind kind = ObjectKind.Bat;   // dropdown in Inspector

    [Header("Respawn Settings")]
    public float respawnDelay = 0.2f;
    public Transform respawnPoint;            // optional override

    [Header("Floor Settings")]
    public string floorObjectName = "Floor";  // your MeshCollider object name

    [Header("Out Of Bounds (fallback)")]
    public float minYForRespawn = -10f;       // if ball.y < this → emergency respawn

    [Header("Player / Camera")]
    [Tooltip("Player head / main camera (e.g. CenterEyeAnchor)")]
    public Transform playerCamera;

    private Rigidbody _rb;
    private Grabbable _grabbable;             // may be null for Ball

    // Fallback absolute spawn
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // Camera-relative spawn data
    private Vector3 _localOffsetXZ;           // offset around camera in its yaw-space
    private float _spawnY;                    // fixed world Y height
    private float _yawOffset;                 // object yaw relative to camera yaw (for bat)
    private bool _hasCameraOffset = false;

    private bool _isHeld = false;             // only meaningful for Bat
    private bool _isRespawning = false;

    private int _ballFloorHitCount = 0;

    // COMBO
    private int _currentCombo = 0;            // current combo for this ball life
    private int _maxCombo = 0;                // max combo across the run

    [Header("Combo Settings")]
    [SerializeField]
    private float comboHitCooldown = 0.1f;    // ignore repeated hits within this time window

    private float _lastComboHitTime = -999f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grabbable = GetComponent<Grabbable>();  // will be null for Ball

        // Store absolute pose as final fallback
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        // Only Bats are grabbable
        if (kind == ObjectKind.Bat && _grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += HandlePointerEvent;
        }
    }

    private void OnDestroy()
    {
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= HandlePointerEvent;
        }
    }

    private void Start()
    {
        // Record camera-relative offset based on how you placed camera + bat + ball in the scene
        if (playerCamera != null)
        {
            Vector3 camPos = playerCamera.position;
            Vector3 objPos = transform.position;

            // Use yaw-only rotation for local offset
            float camYaw = playerCamera.eulerAngles.y;
            Quaternion camYawRot = Quaternion.Euler(0f, camYaw, 0f);

            Vector3 worldOffset = objPos - camPos;
            // Convert to camera-yaw-local space
            Vector3 localOffset = Quaternion.Inverse(camYawRot) * worldOffset;

            _localOffsetXZ = new Vector3(localOffset.x, 0f, localOffset.z);
            _spawnY = objPos.y;

            // Yaw offset between object and camera (used for bat)
            float objYaw = transform.eulerAngles.y;
            _yawOffset = Mathf.DeltaAngle(camYaw, objYaw);

            _hasCameraOffset = true;
        }

        // All objects start hovering/frozen
        FreezeAtRespawnPoint();

        // Initialize combo UI once (ball only)
        if (kind == ObjectKind.Ball && ScoreUI.Instance != null)
        {
            ScoreUI.Instance.UpdateComboText(_maxCombo);
        }
    }

    // Optional: emergency out-of-bounds respawn for the ball
    private void Update()
    {
        if (kind == ObjectKind.Ball && !_isRespawning)
        {
            if (transform.position.y < minYForRespawn)
            {
                _ballFloorHitCount = 0;
                _currentCombo = 0; // reset combo on emergency respawn
                StartCoroutine(RespawnAfterDelay());
            }
        }
    }

    // ---- GRAB / RELEASE (Bat only) ----
    private void HandlePointerEvent(PointerEvent evt)
    {
        if (kind != ObjectKind.Bat) return;

        if (evt.Type == PointerEventType.Select)
        {
            OnGrab();
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            OnRelease();
        }
    }

    private void OnGrab()
    {
        _isHeld = true;
        _isRespawning = false;

        _rb.useGravity = false;
        _rb.isKinematic = false;
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    private void OnRelease()
    {
        _isHeld = false;

        // After release, bat behaves like normal physics object
        _rb.useGravity = true;
        _rb.isKinematic = false;
    }

    // ---- BALL: called when bat hits it (from RacketTrigger) ----
    public void ActivatePhysicsFromHit()
    {
        if (kind != ObjectKind.Ball) return;

        _rb.isKinematic = false;
        _rb.useGravity = true;

        // hitting / serving the ball breaks the floor-hit chain
        _ballFloorHitCount = 0;

        // --- COMBO LOGIC WITH COOLDOWN ---
        if (Time.time - _lastComboHitTime < comboHitCooldown)
        {
            return;
        }

        _lastComboHitTime = Time.time;

        _currentCombo++;  // one more bat-ball collision in this life

        if (_currentCombo > _maxCombo)
        {
            _maxCombo = _currentCombo;

            if (ScoreUI.Instance != null)
            {
                ScoreUI.Instance.UpdateComboText(_maxCombo);
            }
        }
    }

    // ---- COLLISION LOGIC ----
    private void OnCollisionEnter(Collision collision)
    {
        if (_isRespawning) return;

        string otherName = collision.collider.gameObject.name;

        // floor logic
        if (otherName == floorObjectName)
        {
            if (kind == ObjectKind.Bat)
            {
                // Bat: respawn immediately on floor
                if (_isHeld) return; // still in hand: ignore
                StartCoroutine(RespawnAfterDelay());
            }
            else if (kind == ObjectKind.Ball)
            {
                // Ball: only respawn after TWO consecutive floor hits
                _ballFloorHitCount++;

                if (_ballFloorHitCount >= 2)
                {
                    _ballFloorHitCount = 0;
                    _currentCombo = 0;   // reset combo on respawn
                    StartCoroutine(RespawnAfterDelay());
                }
            }
        }
        else
        {
            // Hit something that is NOT the floor (wall, racket, ghost...)
            if (kind == ObjectKind.Ball)
            {
                _ballFloorHitCount = 0;
            }
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        _isRespawning = true;

        yield return new WaitForSeconds(respawnDelay);

        // If bat got grabbed during delay, don't snap it
        if (kind == ObjectKind.Bat && _isHeld)
        {
            _isRespawning = false;
            yield break;
        }

        FreezeAtRespawnPoint();
        _isRespawning = false;
    }

    private void FreezeAtRespawnPoint()
    {
        Vector3 targetPos;
        Quaternion targetRot;

        if (_hasCameraOffset && playerCamera != null)
        {
            Vector3 camPos = playerCamera.position;
            float camYaw = playerCamera.eulerAngles.y;
            Quaternion camYawRot = Quaternion.Euler(0f, camYaw, 0f);

            // Rotate the stored local offset with the current camera yaw
            Vector3 worldOffset = camYawRot * _localOffsetXZ;

            targetPos = new Vector3(
                camPos.x + worldOffset.x,
                _spawnY,
                camPos.z + worldOffset.z
            );

            if (kind == ObjectKind.Ball)
            {
                // Ball always faces camera forward
                targetRot = camYawRot;
            }
            else // Bat
            {
                // Bat keeps its original yaw offset relative to camera
                targetRot = Quaternion.Euler(0f, camYaw + _yawOffset, 0f);
            }
        }
        else if (respawnPoint != null)
        {
            targetPos = respawnPoint.position;
            targetRot = respawnPoint.rotation;
        }
        else
        {
            targetPos = _initialPosition;
            targetRot = _initialRotation;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        // Hover / freeze in air
        _rb.useGravity = false;
        _rb.isKinematic = true;

        if (kind == ObjectKind.Ball)
        {
            _currentCombo = 0;
            _ballFloorHitCount = 0;
        }
    }
}
