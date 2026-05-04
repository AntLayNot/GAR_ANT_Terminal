using UnityEngine;

public class BossArenaTrigger : MonoBehaviour
{
    [SerializeField] private BossArenaController arenaController;
    [SerializeField] private bool triggerOnlyOncePerEntry = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (triggerOnlyOncePerEntry && hasTriggered)
            return;

        if (arenaController != null)
            arenaController.StartFight();

        hasTriggered = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        hasTriggered = false;
    }
}