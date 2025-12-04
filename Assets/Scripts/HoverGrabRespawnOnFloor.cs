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

    private Rigidbody _rb;
    private Grabbable _grabbable;             // may be null for Ball

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

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
        // Ignore repeated calls from OnTriggerStay within a tiny time window
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
            // → break the "consecutive floor hit" chain for the ball
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
        Vector3 targetPos = respawnPoint != null ? respawnPoint.position : _initialPosition;
        Quaternion targetRot = respawnPoint != null ? respawnPoint.rotation : _initialRotation;

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
