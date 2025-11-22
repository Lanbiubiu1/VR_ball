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

    private Rigidbody _rb;
    private Grabbable _grabbable;             // may be null for Ball

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private bool _isHeld = false;             // only meaningful for Bat
    private bool _isRespawning = false;

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

    // ---- BALL: called when bat hits it ----
    public void ActivatePhysicsFromHit()
    {
        if (kind != ObjectKind.Ball) return;

        _rb.isKinematic = false;
        _rb.useGravity = true;
    }

    // ---- FLOOR COLLISION → RESPAWN (both) ----
    private void OnCollisionEnter(Collision collision)
    {
        if (_isRespawning) return;

        // Prevent bat from respawning while still held
        if (kind == ObjectKind.Bat && _isHeld) return;

        if (collision.collider.gameObject.name == floorObjectName)
        {
            StartCoroutine(RespawnAfterDelay());
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
    }
}
