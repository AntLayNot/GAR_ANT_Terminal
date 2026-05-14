using UnityEngine;

public class EnemyAttackHitBehaviour : StateMachineBehaviour
{
    [Header("Hit Timing")]
    [Range(0f, 1f)]
    [SerializeField] private float hitNormalizedTime = 0.45f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private bool hasHit;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        hasHit = false;
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (hasHit)
            return;

        float normalizedTime = stateInfo.normalizedTime;

        // Si l'animation loop, on garde seulement la partie 0 -> 1
        normalizedTime = normalizedTime % 1f;

        if (normalizedTime < hitNormalizedTime)
            return;

        hasHit = true;

        EnemyBrain2D enemyBrain = animator.GetComponentInParent<EnemyBrain2D>();

        if (enemyBrain == null)
        {
            if (debugLog)
                Debug.LogWarning("[EnemyAttackHitBehaviour] Aucun EnemyBrain2D trouvé dans les parents.");

            return;
        }

        if (debugLog)
            Debug.Log("[EnemyAttackHitBehaviour] Hit déclenché à " + hitNormalizedTime);

        enemyBrain.AnimationDealDamage();
    }
}