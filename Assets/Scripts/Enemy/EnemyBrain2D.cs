using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBrain2D : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }

    [Header("Preset (optional)")]
    public EnemyPreset2D preset;

    [Header("Facing")]
    public Transform visualRoot;
    public bool useVisualRootRotationInsteadOfFlip = true;

    private float facingSign = 1f;

    [Header("Target")]
    public Transform target;                 // assigne le Player (ou auto-find tag)
    public string targetTag = "Player";

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float chaseSpeed = 3.2f;
    public float stopDistance = 1.1f;        // stop avant d'attaquer
    public bool faceTarget = true;

    [Header("Detection")]
    public float detectRange = 8f;           // cercle jaune
    public float loseRange = 10f;
    public LayerMask obstacleMask;           // obstacles pour LineOfSight (optionnel)
    public bool requireLineOfSight = false;

    [Header("Line Of Sight")]
    public Vector2 sightOriginOffset = new Vector2(0f, 0.6f);
    public Vector2 sightTargetOffset = new Vector2(0f, 0.6f);

    [Header("Enemy Avoidance")]
    public bool avoidOtherEnemies = true;
    public LayerMask enemyMask;              // layer Enemy
    public float enemyCheckDistance = 0.35f;
    public Vector2 enemyCheckOffset = new Vector2(0.4f, 0.0f);

    [Header("Patrol (No Points)")]
    public bool patrol = true;
    public bool useRaycastPatrol = true;
    public LayerMask groundMask;             // sol + murs (pour les raycasts)
    public float patrolWait = 0.25f;

    [Tooltip("Raycast horizontal: mur devant")]
    public float wallCheckDistance = 0.35f;
    public Vector2 wallCheckOffset = new Vector2(0.4f, 0.0f);

    [Tooltip("Raycast vertical: sol devant (éviter le vide)")]
    public float ledgeCheckDistance = 0.7f;
    public Vector2 ledgeCheckOffset = new Vector2(0.4f, -0.2f);

    [Header("Attack (Melee)")]
    public float attackRange = 1.2f;         // cercle rouge
    public float attackCooldown = 0.9f;
    public int damage = 1;
    public Vector2 attackBoxSize = new Vector2(1.2f, 1.0f);     // carré rouge
    public Vector2 attackBoxOffset = new Vector2(0.7f, 0f);
    public LayerMask damageMask;             // layer du Player

    [Header("Attack (Shooter)")]
    public GameObject projectilePrefab;
    public float shootCooldown = 0.8f;
    public float projectileSpeed = 12f;
    public float shootRange = 7f;
    public float preferredDistance = 4f;
    public float minDistance = 2f;
    public Transform shootMuzzle;

    // ---- internal
    State state;
    Rigidbody2D rb;
    SpriteRenderer sr;

    float waitTimer;
    float attackTimer;
    int patrolDir = 1; // 1 right, -1 left

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

    }

    void Start()
    {
        ApplyPreset();

        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go != null) target = go.transform;
        }

        state = patrol ? State.Patrol : State.Idle;
        FaceDir(1f);
    }

    void Update()
    {
        if (target == null) return;

        attackTimer -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, target.position);

        bool seesTarget = dist <= detectRange && (!requireLineOfSight || HasLineOfSight());
        bool lostTarget = dist >= loseRange;

        // Selon le type: la "portée d'attaque" n'est pas la même
        float engageRange = GetEngageRange();

        switch (state)
        {
            case State.Idle:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (seesTarget) state = State.Chase;
                break;

            case State.Patrol:
                if (seesTarget) { state = State.Chase; break; }
                PatrolUpdate();
                break;

            case State.Chase:
                if (lostTarget) { state = patrol ? State.Patrol : State.Idle; break; }

                // Kamikaze : on utilise la vraie hitbox pour savoir quand exploser
                if (preset != null && preset.type == EnemyPreset2D.EnemyType.Kamikaze)
                {
                    float facing = GetFacingSign();
                    Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);

                    Collider2D hit = Physics2D.OverlapBox(center, attackBoxSize, 0f, damageMask);

                    if (hit != null)
                    {
                        state = State.Attack;
                        break;
                    }

                    ChaseUpdate();
                    break;
                }

                // Si on est à portée d'engagement -> Attack
                if (dist <= engageRange) { state = State.Attack; break; }

                ChaseUpdate();
                break;

            case State.Attack:
                if (lostTarget) { state = patrol ? State.Patrol : State.Idle; break; }

                // Kamikaze: on le laisse gérer lui-même son contact / explosion
                if (preset != null && preset.type == EnemyPreset2D.EnemyType.Kamikaze)
                {
                    AttackUpdate(dist);
                    break;
                }

                // Si on n'est plus à portée -> re-chase
                if (dist > engageRange + 0.35f) { state = State.Chase; break; }

                AttackUpdate(dist);
                break;
        }
    }

    float GetEngageRange()
    {
        if (preset != null)
        {
            if (preset.type == EnemyPreset2D.EnemyType.Shooter)
                return shootRange;

            if (preset.type == EnemyPreset2D.EnemyType.Kamikaze)
                return attackRange;
        }

        return attackRange;
    }

    void ApplyPreset()
    {
        if (preset == null) return;

        patrol = preset.patrol;
        moveSpeed = preset.moveSpeed;
        chaseSpeed = preset.chaseSpeed;
        stopDistance = preset.stopDistance;

        detectRange = preset.detectRange;
        loseRange = preset.loseRange;
        requireLineOfSight = preset.requireLineOfSight;

        // melee
        attackRange = preset.attackRange;
        attackCooldown = preset.attackCooldown;
        damage = preset.damage;
        attackBoxSize = preset.attackBoxSize;
        attackBoxOffset = preset.attackBoxOffset;

        // shooter
        projectilePrefab = preset.projectilePrefab;
        shootCooldown = preset.shootCooldown;
        projectileSpeed = preset.projectileSpeed;
        shootRange = preset.shootRange;
        preferredDistance = preset.preferredDistance;
        minDistance = preset.minDistance;
    }

    // ---------------------------
    // PATROL (no points)
    // ---------------------------
    void PatrolUpdate()
    {
        if (!useRaycastPatrol)
        {
            MoveX(patrolDir, moveSpeed);
            FaceDir(patrolDir);
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dir = patrolDir;

        // Mur devant ?
        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(wallCheckOffset.x * dir, wallCheckOffset.y);
        var wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * dir, wallCheckDistance, groundMask);

        // Sol devant ?
        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(ledgeCheckOffset.x * dir, ledgeCheckOffset.y);
        var groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance, groundMask);

        bool willHitWall = wallHit.collider != null;
        bool noGroundAhead = groundHit.collider == null;

        // Enemy devant ?
        bool enemyAhead = false;
        if (avoidOtherEnemies && enemyMask.value != 0)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(enemyCheckOffset.x * dir, enemyCheckOffset.y);

            // cercle de détection devant
            float radius = 0.2f;
            Collider2D hit = Physics2D.OverlapCircle(origin, radius, enemyMask);

            if (hit != null)
            {
                // ignorer soi-même (ou enfants)
                if (!hit.transform.IsChildOf(transform) && !transform.IsChildOf(hit.transform))
                    enemyAhead = true;
            }
        }


        // On flip aussi si un enemy est devant
        if (willHitWall || noGroundAhead || enemyAhead)
        {
            patrolDir *= -1;
            waitTimer = patrolWait;   // petite pause pour éviter jitter
            dir = patrolDir;
        }

        MoveX(dir, moveSpeed);
        FaceDir(dir);
    }


    // ---------------------------
    // CHASE
    // ---------------------------
    void ChaseUpdate()
    {
        float dx = target.position.x - transform.position.x;

        // Shooter: logique de distance (il ne veut pas coller)
        if (preset != null && preset.type == EnemyPreset2D.EnemyType.Shooter)
        {
            ShooterReposition(dx);
            return;
        }

        // Kamikaze: il continue à foncer, il ne s'arrête pas à stopDistance
        if (preset != null && preset.type == EnemyPreset2D.EnemyType.Kamikaze)
        {
            float dirK = Mathf.Sign(dx);

            if (dirK == 0f)
                dirK = GetFacingSign();

            MoveX(dirK, chaseSpeed);
            if (faceTarget) FaceDir(dirK);
            return;
        }

        // Melee classique
        if (Mathf.Abs(dx) <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            if (faceTarget) FaceDir(Mathf.Sign(dx));
            return;
        }

        float dir = Mathf.Sign(dx);
        MoveX(dir, chaseSpeed);
        if (faceTarget) FaceDir(dir);
    }

    void ShooterReposition(float dx)
    {
        float abs = Mathf.Abs(dx);
        float dirToPlayer = Mathf.Sign(dx);

        // trop proche: reculer
        if (abs < minDistance)
        {
            float dirAway = -dirToPlayer;
            MoveX(dirAway, chaseSpeed);
            if (faceTarget) FaceDir(dirToPlayer);
            return;
        }

        // un peu trop proche: reculer doucement
        if (abs < preferredDistance)
        {
            float dirAway = -dirToPlayer;
            MoveX(dirAway, moveSpeed);
            if (faceTarget) FaceDir(dirToPlayer);
            return;
        }

        // trop loin: s'approcher
        if (abs > shootRange)
        {
            MoveX(dirToPlayer, chaseSpeed);
            if (faceTarget) FaceDir(dirToPlayer);
            return;
        }

        // bonne distance: stop
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (faceTarget) FaceDir(dirToPlayer);
    }

    // ---------------------------
    // ATTACK
    // ---------------------------
    void AttackUpdate(float dist)
    {
        if (preset != null)
        {
            if (preset.type == EnemyPreset2D.EnemyType.Shooter)
            {
                ShooterAttackUpdate();
                return;
            }

            if (preset.type == EnemyPreset2D.EnemyType.Kamikaze)
            {
                KamikazeAttackUpdate();
                return;
            }
        }

        MeleeAttackUpdate();
    }

    void MeleeAttackUpdate()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (attackTimer > 0f) return;
        attackTimer = attackCooldown;

        float facing = GetFacingSign();
        Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);

        Collider2D hit = Physics2D.OverlapBox(center, attackBoxSize, 0f, damageMask);
        if (hit != null)
        {
            var dmg = hit.GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damage);
        }
    }

    void KamikazeAttackUpdate()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        float facing = GetFacingSign();
        Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackBoxSize, 0f, damageMask);

        Debug.Log("Kamikaze ATTACK");
        Debug.Log("Hits trouvés : " + hits.Length);

        bool hasHitTarget = false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            var dmg = hit.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                Debug.Log("Kamikaze a touché : " + hit.name);
                dmg.TakeDamage(damage);
                hasHitTarget = true;
            }
        }

        if (hasHitTarget)
        {
            Debug.Log("Kamikaze exploded");
            Destroy(gameObject);
            return;
        }

        // sécurité
        state = State.Chase;
    }

    void ShooterAttackUpdate()
    {
        float dx = target.position.x - transform.position.x;
        ShooterReposition(dx);

        if (attackTimer > 0f) return;
        attackTimer = shootCooldown;

        if (projectilePrefab == null) return;

        float facing = GetFacingSign(); // -1 gauche, +1 droite

        Vector2 spawnPos = shootMuzzle != null
            ? (Vector2)shootMuzzle.position
            : (Vector2)transform.position + new Vector2(0.6f * facing, 0.2f);

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);


        Vector2 dir = facing < 0 ? Vector2.left : Vector2.right;

        var laser = go.GetComponent<LaserProjectile2D>();
        if (laser != null)
        {
            laser.SetDirection(dir);
        }
        else
        {
            var proj = go.GetComponent<Projectile2D>();
            if (proj != null)
            {
                proj.Init(dir);
            }
            else
            {
                var prb = go.GetComponent<Rigidbody2D>();
                if (prb != null)
                    prb.linearVelocity = new Vector2(projectileSpeed * facing, 0f);
            }
        }
    }


    // ---------------------------
    // LOS
    // ---------------------------
    bool HasLineOfSight()
    {
        if (requireLineOfSight && obstacleMask.value == 0)
            return false;

        Vector2 from = (Vector2)transform.position + sightOriginOffset;
        Vector2 to = (Vector2)target.position + sightTargetOffset;

        Vector2 dir = (to - from).normalized;
        float dist = Vector2.Distance(from, to);

        // RaycastAll + ignore player/self (ta version actuelle)
        RaycastHit2D[] hits = Physics2D.RaycastAll(from, dir, dist, obstacleMask);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;

            if (hit.collider.transform.IsChildOf(target))
                continue;

            if (hit.collider.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }

    // ---------------------------
    // Helpers
    // ---------------------------
    void MoveX(float dir, float speed)
    {
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    void FaceDir(float dir)
    {
        if (dir == 0f) return;

        facingSign = dir < 0f ? -1f : 1f;

        if (useVisualRootRotationInsteadOfFlip)
        {
            if (visualRoot != null)
            {
                visualRoot.localRotation = facingSign < 0f
                    ? Quaternion.Euler(0f, 180f, 0f)
                    : Quaternion.Euler(0f, 0f, 0f);
            }
        }
        else
        {
            if (sr != null)
                sr.flipX = facingSign < 0f;
        }
    }

    float GetFacingSign()
    {
        return facingSign;
    }

    // ---------------------------
    // Gizmos
    // ---------------------------
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetEngageRange());

        float facing = Application.isPlaying ? facingSign : 1f;

        // melee hitbox preview
        Vector2 center = (Vector2)transform.position + new Vector2(attackBoxOffset.x * facing, attackBoxOffset.y);
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(center, attackBoxSize);

        if (useRaycastPatrol)
        {
            float dir = facing;

            Gizmos.color = Color.cyan;
            Vector3 wallOrigin = transform.position + (Vector3)new Vector2(wallCheckOffset.x * dir, wallCheckOffset.y);
            Gizmos.DrawLine(wallOrigin, wallOrigin + Vector3.right * dir * wallCheckDistance);

            Gizmos.color = Color.green;
            Vector3 ledgeOrigin = transform.position + (Vector3)new Vector2(ledgeCheckOffset.x * dir, ledgeCheckOffset.y);
            Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + Vector3.down * ledgeCheckDistance);
        }

        // enemy avoid preview
        if (avoidOtherEnemies)
        {
            Gizmos.color = Color.white;
            float dir = facing;
            Vector3 p = transform.position + (Vector3)new Vector2(enemyCheckOffset.x * dir, enemyCheckOffset.y);
            Gizmos.DrawWireSphere(p, 0.2f);
        }

        if (requireLineOfSight && target != null)
        {
            Gizmos.color = Color.magenta;
            Vector3 from = transform.position + (Vector3)sightOriginOffset;
            Vector3 to = target.position + (Vector3)sightTargetOffset;
            Gizmos.DrawLine(from, to);
        }
    }
}
