using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BatManager : MonoBehaviour
{
    [Header("Detection Settings")]
    public float sphereRadius = 0.15f;
    public LayerMask detectionLayers = ~0;
    public bool drawGizmos = true;

    [Header("Input Settings")]
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;
    public OVRInput.Axis1D triggerAxis = OVRInput.Axis1D.PrimaryIndexTrigger;
    [Range(0f, 1f)] public float triggerThreshold = 0.5f;

    [Header("Attachment Settings")]
    public string attachTag = "Bat";
    
    // --- POSITION & ROTATION FIXES ---
    [Header("Adjustments")]
    [Tooltip("Move the bat UP/DOWN/FORWARD to fit your hand. Try Y = -0.1 or Z = 0.1")]
    public Vector3 fixPosition = new Vector3(0, -0.1f, 0); 

    [Tooltip("Rotate to fix 'Torch' or 'Frying Pan'. Try (90, 0, 90) or (0, 0, 0)")]
    public Vector3 fixRotation = new Vector3(90, 0, 90); 
    // ---------------------------------

    // State tracking
    private bool _isHolding = false;
    private bool _wasPressed = false; 
    private GameObject _currentHeldObject;
    private Rigidbody _currentHeldRb;
    private HoverRespawnObject _hoverScript; 

    private readonly Collider[] _buffer = new Collider[16];

    void Update()
    {
        float triggerValue = OVRInput.Get(triggerAxis, controller);
        bool isPressed = triggerValue >= triggerThreshold;

        // Press once to Grab, Press again to Drop
        if (isPressed && !_wasPressed)
        {
            if (_isHolding) DropObject();
            else DoSphereDetection();
        }

        _wasPressed = isPressed;
    }

    private void DoSphereDetection()
    {
        Vector3 origin = transform.position;
        int count = Physics.OverlapSphereNonAlloc(origin, sphereRadius, _buffer, detectionLayers, QueryTriggerInteraction.Ignore);

        Collider bestTarget = null;
        float closestDistSqr = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Collider col = _buffer[i];
            if (col == null) continue;
            if (!col.CompareTag(attachTag)) continue;

            float dSqr = (col.transform.position - origin).sqrMagnitude;
            if (dSqr < closestDistSqr)
            {
                closestDistSqr = dSqr;
                bestTarget = col;
            }
        }

        if (bestTarget != null)
        {
            AttachObject(bestTarget.gameObject);
        }
    }

    private void AttachObject(GameObject obj)
    {
        _currentHeldObject = obj;
        _currentHeldRb = obj.GetComponent<Rigidbody>();
        _hoverScript = obj.GetComponent<HoverRespawnObject>();

        // 1. Disable Physics
        if (_currentHeldRb != null)
        {
            _currentHeldRb.isKinematic = true; 
            _currentHeldRb.velocity = Vector3.zero; 
            _currentHeldRb.angularVelocity = Vector3.zero;
        }

        // 2. Notify Hover Script (Fixes Floating)
        if (_hoverScript != null)
        {
            _hoverScript.OnGrab();
        }

        // 3. Parent to Hand
        _currentHeldObject.transform.SetParent(transform);

        _currentHeldObject.transform.localPosition = fixPosition;

        _currentHeldObject.transform.localRotation = Quaternion.Euler(fixRotation);
        
        _isHolding = true;
        Debug.Log($"[BatManager] Picked up: {obj.name}");
    }

    private void DropObject()
    {
        if (_currentHeldObject == null) return;

        _currentHeldObject.transform.SetParent(null);

        if (_currentHeldRb != null)
        {
            _currentHeldRb.isKinematic = false;
            Vector3 throwVel = OVRInput.GetLocalControllerVelocity(controller);
            _currentHeldRb.velocity = throwVel;
        }

        if (_hoverScript != null)
        {
            _hoverScript.OnRelease();
            _hoverScript = null;
        }

        Debug.Log($"[BatManager] Dropped: {_currentHeldObject.name}");

        _currentHeldObject = null;
        _currentHeldRb = null;
        _isHolding = false;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = _isHolding ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }
}