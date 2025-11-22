using UnityEngine;

public class GhostCollision : MonoBehaviour
{
    private Collider _col;

    private void Awake()
    {
        _col = GetComponent<Collider>();

        // Ensure it's trigger-only
        _col.isTrigger = true;
    }

    public void KillGhost()
    {
        // You can still disable trigger or whole ghost here
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            KillGhost();
        }
    }
}