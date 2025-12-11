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
    [Range(0.1f, 1f)] public float triggerThreshold = 0.5f;

    [Header("Attachment Settings")]
    public string attachTag = "Bat";
    public bool snapPosition = true;
    public bool snapRotation = true;

    // State tracking
    private bool _isHolding = false;
    private bool _wasPressed = false; // To track "GetDown" manually for axis
    private GameObject _currentHeldObject;
    private Rigidbody _currentHeldRb;

    // Cache
    private readonly Collider[] _buffer = new Collider[16];

    void Update()
    {

        float triggerValue = OVRInput.Get(triggerAxis, controller);
        bool isPressed = triggerValue >= triggerThreshold;


        if (isPressed && !_wasPressed)
        {
            if (_isHolding)
            {
                DropObject();
            }
            else
            {
                TryPickup();
            }
        }

        _wasPressed = isPressed;
    }

    private void TryPickup()
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


        if (_currentHeldRb != null)
        {
            _currentHeldRb.isKinematic = true;

            _currentHeldRb.velocity = Vector3.zero;
            _currentHeldRb.angularVelocity = Vector3.zero;
        }


        _currentHeldObject.transform.SetParent(transform);

        if (snapPosition) _currentHeldObject.transform.localPosition = Vector3.zero;
        if (snapRotation) _currentHeldObject.transform.localRotation = Quaternion.identity;

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

        Debug.Log($"[BatManager] Dropped: {_currentHeldObject.name}");

        _currentHeldObject = null;
        _currentHeldRb = null;
        _isHolding = false;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;
        Gizmos.color = _isHolding ? Color.green : Color.cyan; // Change color if holding
        Gizmos.DrawWireSphere(transform.position, sphereRadius);
    }
}