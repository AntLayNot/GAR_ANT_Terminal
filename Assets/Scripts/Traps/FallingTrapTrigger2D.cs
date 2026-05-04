using UnityEngine;

public class FallingTrapTrigger2D : MonoBehaviour
{
    [SerializeField] private FallingTrap2D trap;
    [SerializeField] private string targetTag = "Player";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag))
            return;

        if (trap != null)
            trap.TriggerFall();
    }
}