using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCommandAnimator : MonoBehaviour
{
    [System.Serializable]
    private struct PendingProjectileData
    {
        public GameObject prefab;
        public Vector2 spawnPosition;
        public Vector2 direction;
        public string targetName;
        public float lifetime;

        public PendingProjectileData(
            GameObject prefab,
            Vector2 spawnPosition,
            Vector2 direction,
            string targetName,
            float lifetime
        )
        {
            this.prefab = prefab;
            this.spawnPosition = spawnPosition;
            this.direction = direction;
            this.targetName = targetName;
            this.lifetime = lifetime;
        }
    }

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string spawnProjectileTrigger = "SpawnProjectile";

    [Tooltip("Si true, le projectile attend l'event d'animation SpawnPendingProjectile.")]
    [SerializeField] private bool useAnimationEvent = true;

    [Tooltip("Sécurité : si l'event d'animation ne se lance pas, on spawn quand même.")]
    [SerializeField] private bool useFailsafeSpawn = true;

    [SerializeField] private float failsafeDelay = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly List<PendingProjectileData> pendingProjectiles = new List<PendingProjectileData>();

    private bool animationAlreadyRequested;
    private Coroutine failsafeRoutine;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void RequestProjectileSpawn(
        GameObject projectilePrefab,
        Vector2 spawnPosition,
        Vector2 direction,
        string targetName,
        float lifetime
    )
    {
        if (projectilePrefab == null)
            return;

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        direction.Normalize();

        PendingProjectileData data = new PendingProjectileData(
            projectilePrefab,
            spawnPosition,
            direction,
            targetName,
            lifetime
        );

        pendingProjectiles.Add(data);

        if (debugLog)
            Debug.Log("[PlayerCommandAnimator] Projectile ajouté. Total pending : " + pendingProjectiles.Count);

        if (!useAnimationEvent)
        {
            SpawnPendingProjectile();
            return;
        }

        if (!animationAlreadyRequested)
        {
            animationAlreadyRequested = true;
            PlaySpawnProjectileAnimation();
            StartFailsafe();
        }
    }

    private void PlaySpawnProjectileAnimation()
    {
        if (animator == null)
        {
            Debug.LogWarning("[PlayerCommandAnimator] Aucun Animator trouvé. Spawn direct.");
            SpawnPendingProjectile();
            return;
        }

        if (!animator.isActiveAndEnabled)
        {
            Debug.LogWarning("[PlayerCommandAnimator] Animator désactivé. Spawn direct.");
            SpawnPendingProjectile();
            return;
        }

        if (debugLog)
            Debug.Log("[PlayerCommandAnimator] Animation projectile déclenchée.");

        animator.ResetTrigger(spawnProjectileTrigger);
        animator.SetTrigger(spawnProjectileTrigger);
    }

    private void StartFailsafe()
    {
        if (!useFailsafeSpawn)
            return;

        if (failsafeRoutine != null)
            StopCoroutine(failsafeRoutine);

        failsafeRoutine = StartCoroutine(FailsafeSpawnRoutine());
    }

    private IEnumerator FailsafeSpawnRoutine()
    {
        yield return new WaitForSeconds(failsafeDelay);

        if (pendingProjectiles.Count <= 0)
        {
            ResetRequestState();
            yield break;
        }

        Debug.LogWarning("[PlayerCommandAnimator] Failsafe : l'event d'animation n'a pas été appelé. Spawn forcé.");

        SpawnPendingProjectile();
    }

    // À appeler par StateMachineBehaviour (l'animation dans la StateMachine)
    public void SpawnPendingProjectile()
    {
        if (pendingProjectiles.Count <= 0)
        {
            ResetRequestState();
            return;
        }

        if (failsafeRoutine != null)
        {
            StopCoroutine(failsafeRoutine);
            failsafeRoutine = null;
        }

        int count = pendingProjectiles.Count;

        for (int i = 0; i < pendingProjectiles.Count; i++)
        {
            SpawnProjectile(pendingProjectiles[i]);
        }

        pendingProjectiles.Clear();
        ResetRequestState();

        if (debugLog)
            Debug.Log("[PlayerCommandAnimator] Projectiles spawn : " + count);
    }

    private void SpawnProjectile(PendingProjectileData data)
    {
        if (data.prefab == null)
            return;

        GameObject projectile = Instantiate(
            data.prefab,
            data.spawnPosition,
            Quaternion.identity
        );

        TargetObject targetObject = projectile.GetComponent<TargetObject>();

        if (targetObject == null)
            targetObject = projectile.AddComponent<TargetObject>();

        targetObject.SetName(data.targetName);

        Projectile2D proj = projectile.GetComponent<Projectile2D>();

        if (proj == null)
            proj = projectile.GetComponentInChildren<Projectile2D>();

        if (proj != null)
        {
            proj.Init(data.direction);
        }
        else
        {
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.linearVelocity = data.direction * 12f;
            }
        }

        if (data.lifetime > 0f)
            Destroy(projectile, data.lifetime);
    }

    private void ResetRequestState()
    {
        animationAlreadyRequested = false;

        if (failsafeRoutine != null)
        {
            StopCoroutine(failsafeRoutine);
            failsafeRoutine = null;
        }
    }

    public void ForceClearProjectileQueue()
    {
        pendingProjectiles.Clear();
        ResetRequestState();

        if (debugLog)
            Debug.Log("[PlayerCommandAnimator] File projectile reset.");
    }
}