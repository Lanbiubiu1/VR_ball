using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostTriggerForwarder : MonoBehaviour
{
    public GhostCollision parent;   // assign in Inspector

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            parent.KillGhost();
        }
    }
}
