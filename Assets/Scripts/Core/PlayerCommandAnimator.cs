using UnityEngine;

public class PlayerCommandAnimator : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string spawnProjectileTrigger = "SpawnProjectile";

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private GameObject pendingProjectilePrefab;
    private Vector2 pendingSpawnPosition;
    private Vector2 pendingDirection;
    private string pendingTargetName;
    private float pendingLifetime;
    private bool hasPendingProjectile;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void RequestProjectileSpawn(GameObject projectilePrefab, Vector2 spawnPosition, Vector2 direction, string targetName, float lifetime)
    {
        if (projectilePrefab == null)
            return;

        pendingProjectilePrefab = projectilePrefab;
        pendingSpawnPosition = spawnPosition;
        pendingDirection = direction.normalized;
        pendingTargetName = targetName;
        pendingLifetime = lifetime;

        if (pendingDirection == Vector2.zero)
            pendingDirection = Vector2.right;

        hasPendingProjectile = true;

        PlaySpawnProjectileAnimation();
    }

    private void PlaySpawnProjectileAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("[PlayerCommandAnimator] Aucun Animator trouvé.");
            return;
        }

        if (debugLog)
            Debug.Log("[PlayerCommandAnimator] Animation projectile déclenchée.");

        animator.ResetTrigger(spawnProjectileTrigger);
        animator.SetTrigger(spawnProjectileTrigger);
    }

    // Appelée par le StateMachineBehaviour au bon moment
    public void SpawnPendingProjectile()
    {
        if (!hasPendingProjectile)
            return;

        hasPendingProjectile = false;

        GameObject projectile = Instantiate(
            pendingProjectilePrefab,
            pendingSpawnPosition,
            Quaternion.identity
        );

        var targetObject = projectile.GetComponent<TargetObject>();
        if (targetObject == null)
            targetObject = projectile.AddComponent<TargetObject>();

        targetObject.SetName(pendingTargetName);

        Projectile2D proj = projectile.GetComponent<Projectile2D>();

        if (proj == null)
            proj = projectile.GetComponentInChildren<Projectile2D>();

        if (proj != null)
        {
            proj.Init(pendingDirection);
        }
        else
        {
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = pendingDirection * 12f;
            }
        }

        if (pendingLifetime > 0f)
            Destroy(projectile, pendingLifetime);

        if (debugLog)
            Debug.Log("[PlayerCommandAnimator] Projectile spawn au bon moment.");
    }
}