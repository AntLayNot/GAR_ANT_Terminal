using UnityEngine;

public class PlayerCommandProjectileSpawnBehaviour : StateMachineBehaviour
{
    [Header("Spawn Timing")]
    [Range(0f, 1f)]
    [SerializeField] private float spawnNormalizedTime = 0.45f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool hasSpawned;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasSpawned = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasSpawned)
            return;

        float normalizedTime = stateInfo.normalizedTime % 1f;

        if (normalizedTime < spawnNormalizedTime)
            return;

        hasSpawned = true;

        PlayerCommandAnimator commandAnimator = animator.GetComponentInParent<PlayerCommandAnimator>();

        if (commandAnimator == null)
        {
            if (debugLog)
                Debug.LogWarning("[PlayerCommandProjectileSpawnBehaviour] Aucun PlayerCommandAnimator trouvé.");

            return;
        }

        if (debugLog)
            Debug.Log("[PlayerCommandProjectileSpawnBehaviour] Spawn projectile déclenché.");

        commandAnimator.SpawnPendingProjectile();
    }
}