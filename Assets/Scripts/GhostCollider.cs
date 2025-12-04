using UnityEngine;
using NaughtyAttributes;

public class GhostCollision : MonoBehaviour
{
    private Collider _col;

    private void Awake()
    {
        _col = GetComponentInChildren<Collider>();
        // Ensure it's trigger-only
        _col.isTrigger = true;
    }

    [Button]
    public void KillGhost()
    {
        // Play dissolve instead of instantly turning off the object
        GhostDissolve dissolve = GetComponent<GhostDissolve>();
        if (dissolve != null)
        {
            dissolve.PlayDissolveAndDisable();
        }
        else
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddHit();
            }
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            KillGhost();
        }
    }
}