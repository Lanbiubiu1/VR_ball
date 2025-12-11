using System.Collections;
using UnityEngine;
using Oculus.Interaction; 

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class HoverRespawnObject : MonoBehaviour
{
    public enum ObjectKind
    {
        Bat,
        Ball
    }

    [Header("Type")]
    public ObjectKind kind = ObjectKind.Bat; 

    [Header("Respawn Settings")]
    public float respawnDelay = 0.2f;
    public Transform respawnPoint;            

    [Header("Floor Settings")]
    public string floorObjectName = "Floor"; 

    [Header("Out Of Bounds (fallback)")]
    public float minYForRespawn = -10f;       

    [Header("Player / Camera")]
    public Transform playerCamera;

    private Rigidbody _rb;
    private Grabbable _grabbable;            

    // Fallback absolute spawn
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // Camera-relative spawn data
    private Vector3 _localOffsetXZ;           
    private float _spawnY;                    
    private float _yawOffset;                 
    private bool _hasCameraOffset = false;

    private bool _isHeld = false;             
    private bool _isRespawning = false;

    private int _ballFloorHitCount = 0;

    // COMBO
    private int _currentCombo = 0;            
    private int _maxCombo = 0;                

    [Header("Combo Settings")]
    [SerializeField]
    private float comboHitCooldown = 0.1f;    

    private float _lastComboHitTime = -999f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _grabbable = GetComponent<Grabbable>();  

        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        // FIX: Listen to events for BOTH Bat and Ball
        if (_grabbable != null)
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
        if (playerCamera != null)
        {
            Vector3 camPos = playerCamera.position;
            Vector3 objPos = transform.position;

            float camYaw = playerCamera.eulerAngles.y;
            Quaternion camYawRot = Quaternion.Euler(0f, camYaw, 0f);

            Vector3 worldOffset = objPos - camPos;
            Vector3 localOffset = Quaternion.Inverse(camYawRot) * worldOffset;

            _localOffsetXZ = new Vector3(localOffset.x, 0f, localOffset.z);
            _spawnY = objPos.y;

            float objYaw = transform.eulerAngles.y;
            _yawOffset = Mathf.DeltaAngle(camYaw, objYaw);

            _hasCameraOffset = true;
        }

        FreezeAtRespawnPoint();

        if (kind == ObjectKind.Ball && ScoreUI.Instance != null)
        {
            ScoreUI.Instance.UpdateComboText(_maxCombo);
        }
    }

    private void Update()
    {
        // Don't auto-respawn if we are holding it!
        if (kind == ObjectKind.Ball && !_isRespawning && !_isHeld)
        {
            if (transform.position.y < minYForRespawn)
            {
                _ballFloorHitCount = 0;
                _currentCombo = 0; 
                StartCoroutine(RespawnAfterDelay());
            }
        }
    }

    // ---- GRAB / RELEASE (FIXED FOR BOTH) ----
    private void HandlePointerEvent(PointerEvent evt)
    {
        // FIX: Removed the "if (kind != ObjectKind.Bat) return;" check
        // Now this logic runs for the Ball too.

        if (evt.Type == PointerEventType.Select)
        {
            OnGrab();
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            OnRelease();
        }
    }

    public void OnGrab()
    {
        _isHeld = true;
        _isRespawning = false;

        // While holding, physics is usually handled by Grabbable, 
        // but we ensure clean state here.
        _rb.useGravity = false;
        _rb.isKinematic = false; // Grabbable usually sets this to true anyway
        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    public void OnRelease()
    {
        _isHeld = false;

        // FIX: This forces physics ON when you throw the ball.
        // It overrides the "restore kinematic state" behavior of the Grabbable script.
        _rb.useGravity = true;
        _rb.isKinematic = false;
        
        // Optional: If you want to impart extra throw velocity manually, do it here.
        // But "Throw When Unselected" in Grabbable usually handles it.
    }

    // ---- BALL: called when bat hits it ----
    public void ActivatePhysicsFromHit()
    {
        if (kind != ObjectKind.Ball) return;

        // Force physics on (in case it was hovering)
        _rb.isKinematic = false;
        _rb.useGravity = true;

        //play audio
        GetComponent<AudioSource>()?.Play();

        _ballFloorHitCount = 0;

        if (Time.time - _lastComboHitTime < comboHitCooldown)
        {
            return;
        }

        _lastComboHitTime = Time.time;
        _currentCombo++;  

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

        if (otherName == floorObjectName)
        {
            if (kind == ObjectKind.Bat)
            {
                if (_isHeld) return; 
                StartCoroutine(RespawnAfterDelay());
            }
            else if (kind == ObjectKind.Ball)
            {
                _ballFloorHitCount++;

                if (_ballFloorHitCount >= 2)
                {
                    _ballFloorHitCount = 0;
                    _currentCombo = 0;   
                    StartCoroutine(RespawnAfterDelay());
                }
            }
        }
        else
        {
            // Reset floor hits if we hit a wall/racket
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

        // If user grabbed it during the delay, cancel respawn
        if (_isHeld)
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

            Vector3 worldOffset = camYawRot * _localOffsetXZ;

            targetPos = new Vector3(
                camPos.x + worldOffset.x,
                _spawnY,
                camPos.z + worldOffset.z
            );

            if (kind == ObjectKind.Ball)
            {
                targetRot = camYawRot;
            }
            else 
            {
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

        // Reset to hovering state
        _rb.useGravity = false;
        _rb.isKinematic = true;

        if (kind == ObjectKind.Ball)
        {
            _currentCombo = 0;
            _ballFloorHitCount = 0;
        }
    }
    
    public void RespawnToInitialScenePosition()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;

        _rb.velocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.useGravity = false;
        _rb.isKinematic = true;

        if (kind == ObjectKind.Ball)
        {
            _currentCombo = 0;
            _ballFloorHitCount = 0;
        }

        _isRespawning = false;
    }
}