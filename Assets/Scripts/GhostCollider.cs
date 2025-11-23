using UnityEngine;
using NaughtyAttributes;

public class GhostCollision : MonoBehaviour
{
    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();

        // Ensure it's trigger-only
        _col.isTrigger = true;
    }

    [Button]
    public void KillGhost()
    {
        if (ScoreUI.Instance != null)
        {
            ScoreUI.Instance.AddHit();
        }
        // Play dissolve instead of instantly turning off the object
        GhostDissolve dissolve = GetComponent<GhostDissolve>();
        if (dissolve != null)
        {
            dissolve.PlayDissolveAndDisable();
        }
        else
        {
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