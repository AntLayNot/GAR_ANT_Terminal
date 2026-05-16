using UnityEngine;

public class FallingTrapTrigger2D : MonoBehaviour
{
    [SerializeField] private FallingTrap2D trap;
    [SerializeField] private string targetTag = "Player";
    [SerializeField] private bool destroyTriggerAfterActivation = true;

    private bool activated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated)
            return;

        if (!other.CompareTag(targetTag))
            return;

        activated = true;

        if (trap != null)
            trap.TriggerFall();

        if (destroyTriggerAfterActivation)
            Destroy(gameObject);
    }
}