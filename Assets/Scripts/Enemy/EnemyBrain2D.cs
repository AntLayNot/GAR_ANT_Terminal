using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBrain2D : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Attack }

    [Header("Preset (optional)")]
    public EnemyPreset2D preset;

    private NodeAllyPowerDrain2D allyPowerDrain;

    [Header("Facing")]
    public Transform visualRoot;
    public bool useVisualRootRotationInsteadOfFlip = true;

    private float facingSign = 1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string isMovingParam = "IsMoving";
    [SerializeField] private string attackTriggerParam = "Attack";
    [SerializeField] private float attackAnimLockTime = 0.25f;

    private float attackAnimTimer;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Attack Sounds")]
    [SerializeField] private AudioClip[] meleeAttackSounds;
    [SerializeField] private AudioClip[] shootAttackSounds;

    [SerializeField] private float meleeAttackVolume = 1f;
    [SerializeField] private float shootAttackVolume = 1f;

    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Target")]
    public Transform target;
    public string targetTag = "Player";

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float chaseSpeed = 3.2f;
    public float stopDistance = 1.1f;
    public bool faceTarget = true;

    [Header("Detection")]
    public float detectRange = 8f;
    public float loseRange = 10f;
    public LayerMask obstacleMask;
    public bool requireLineOfSight = false;

    [Header("Line Of Sight")]
    public Vector2 sightOriginOffset = new Vector2(0f, 0.6f);
    public Vector2 sightTargetOffset = new Vector2(0f, 0.6f);

    [Header("Enemy Avoidance")]
    public bool avoidOtherEnemies = true;
    public LayerMask enemyMask;
    public float enemyCheckDistance = 0.35f;
    public Vector2 enemyCheckOffset = new Vector2(0.4f, 0.0f);

    [Header("Patrol (No Points)")]
    public bool patrol = true;
    public bool useRaycastPatrol = true;
    public LayerMask groundMask;
    public float patrolWait = 0.25f;

    [Tooltip("Raycast horizontal: mur devant")]
    public float wallCheckDistance = 0.35f;
    public Vector2 wallCheckOffset = new Vector2(0.4f, 0.0f);

    [Tooltip("Raycast vertical: sol devant (éviter le vide)")]
    public float ledgeCheckDistance = 0.7f;
    public Vector2 ledgeCheckOffset = new Vector2(0.4f, -0.2f);

    [Header("Attack (Melee)")]
    public float attackRange = 1.2f;
    public float attackCooldown = 0.9f;
    public int damage = 1;
    public Vector2 attackBoxSize = new Vector2(1.2f, 1.0f);
    public Vector2 attackBoxOffset = new Vector2(0.7f, 0f);
    public LayerMask damageMask;

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
    int patrolDir = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();

        if (audioSource == null)
            audioSource = FindFirstObjectByType<AudioSource>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        allyPowerDrain = GetComponent<NodeAllyPowerDrain2D>();
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
        attackTimer -= Time.deltaTime;
        attackAnimTimer -= Time.deltaTime;

        if (target == null)
        {
            UpdateAnimator();
            return;
        }

        float dist = Vector2.Distance(transform.position, target.position);

        bool seesTarget = dist <= detectRange && (!requireLineOfSight || HasLineOfSight());
        bool lostTarget = dist >= loseRange;

        float engageRange = GetEngageRange();

        switch (state)
        {
            case State.Idle:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                if (seesTarget) state = State.Chase;
                break;

            case State.Patrol:
                if (seesTarget)
                {
                    state = State.Chase;
                    break;
                }

                PatrolUpdate();
                break;

            case State.Chase:
                if (lostTarget)
                {
                    state = patrol ? State.Patrol : State.Idle;
                    break;
                }

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

                if (dist <= engageRange)
                {
                    state = State.Attack;
                    break;
                }

                ChaseUpdate();
                break;

            case State.Attack:
                if (lostTarget)
                {
                    state = patrol ? State.Patrol : State.Idle;
                    break;
                }

                if (preset != null && preset.type == EnemyPreset2D.EnemyType.Kamikaze)
                {
                    AttackUpdate(dist);
                    break;
                }

                if (dist > engageRange + 0.35f)
                {
                    state = State.Chase;
                    break;
                }

                AttackUpdate(dist);
                break;
        }

        UpdateAnimator();
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

        attackRange = preset.attackRange;
        attackCooldown = preset.attackCooldown;
        damage = preset.damage;
        attackBoxSize = preset.attackBoxSize;
        attackBoxOffset = preset.attackBoxOffset;

        projectilePrefab = preset.projectilePrefab;
        shootCooldown = preset.shootCooldown;
        projectileSpeed = preset.projectileSpeed;
        shootRange = preset.shootRange;
        preferredDistance = preset.preferredDistance;
        minDistance = preset.minDistance;
    }

    // ---------------------------
    // ANIMATION
    // ---------------------------
    void UpdateAnimator()
    {
        if (animator == null) return;

        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.05f;

        // Pendant une attaque, on évite que Move reprenne trop vite.
        if (attackAnimTimer > 0f)
            isMoving = false;

        animator.SetBool(isMovingParam, isMoving);
    }

    void PlayAttackAnimation()
    {
        if (animator == null) return;

        attackAnimTimer = attackAnimLockTime;
        animator.ResetTrigger(attackTriggerParam);
        animator.SetTrigger(attackTriggerParam);
    }

    // ---------------------------
    // PATROL
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

        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(wallCheckOffset.x * dir, wallCheckOffset.y);
        var wallHit = Physics2D.Raycast(wallOrigin, Vector2.right * dir, wallCheckDistance, groundMask);

        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(ledgeCheckOffset.x * dir, ledgeCheckOffset.y);
        var groundHit = Physics2D.Raycast(ledgeOrigin, Vector2.down, ledgeCheckDistance, groundMask);

        bool willHitWall = wallHit.collider != null;
        bool noGroundAhead = groundHit.collider == null;

        bool enemyAhead = false;

        if (avoidOtherEnemies && enemyMask.value != 0)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(enemyCheckOffset.x * dir, enemyCheckOffset.y);

            float radius = 0.2f;
            Collider2D hit = Physics2D.OverlapCircle(origin, radius, enemyMask);

            if (hit != null)
            {
                if (!hit.transform.IsChildOf(transform) && !transform.IsChildOf(hit.transform))
                    enemyAhead = true;
            }
        }

        if (willHitWall || noGroundAhead || enemyAhead)
        {
            patrolDir *= -1;
            waitTimer = patrolWait;
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

        if (preset != null && preset.type == EnemyPreset2D.EnemyType.Shooter)
        {
            ShooterReposition(dx);
            return;
        }

        if (preset != null && preset.type == EnemyPreset2D.EnemyType.Kamikaze)
        {
            float dirK = Mathf.Sign(dx);

            if (dirK == 0f)
                dirK = GetFacingSign();

            MoveX(dirK, chaseSpeed);

            if (faceTarget)
                FaceDir(dirK);

            return;
        }

        if (Mathf.Abs(dx) <= stopDistance)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (faceTarget)
                FaceDir(Mathf.Sign(dx));

            return;
        }

        float dir = Mathf.Sign(dx);

        MoveX(dir, chaseSpeed);

        if (faceTarget)
            FaceDir(dir);
    }

    void ShooterReposition(float dx)
    {

        float abs = Mathf.Abs(dx);
        float dirToPlayer = Mathf.Sign(dx);

        if (dirToPlayer == 0f)
            dirToPlayer = GetFacingSign();

        if (abs < minDistance)
        {
            float dirAway = -dirToPlayer;

            MoveX(dirAway, chaseSpeed);

            if (faceTarget)
                FaceDir(dirToPlayer);

            return;
        }

        if (abs < preferredDistance)
        {
            float dirAway = -dirToPlayer;

            MoveX(dirAway, moveSpeed);

            if (faceTarget)
                FaceDir(dirToPlayer);

            return;
        }

        if (abs > shootRange)
        {
            MoveX(dirToPlayer, chaseSpeed);

            if (faceTarget)
                FaceDir(dirToPlayer);

            return;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (faceTarget)
            FaceDir(dirToPlayer);
    }

    bool IsBlockedInDirection(float dir)
    {
        if (dir == 0f)
            return false;

        float sign = Mathf.Sign(dir);

        Vector2 wallOrigin = (Vector2)transform.position + new Vector2(
            wallCheckOffset.x * sign,
            wallCheckOffset.y
        );

        RaycastHit2D wallHit = Physics2D.Raycast(
            wallOrigin,
            Vector2.right * sign,
            wallCheckDistance,
            groundMask
        );

        if (wallHit.collider != null)
            return true;

        Vector2 ledgeOrigin = (Vector2)transform.position + new Vector2(
            ledgeCheckOffset.x * sign,
            ledgeCheckOffset.y
        );

        RaycastHit2D groundHit = Physics2D.Raycast(
            ledgeOrigin,
            Vector2.down,
            ledgeCheckDistance,
            groundMask
        );

        if (groundHit.collider == null)
            return true;

        return false;
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

        if (attackTimer > 0f)
            return;

        attackTimer = attackCooldown;

        PlayRandomSound(meleeAttackSounds, meleeAttackVolume);

        // On lance seulement l'animation. Les dégâts seront appliqués par l'Animation Event.
        PlayAttackAnimation();
    }

    // Appelé par un Animation Event au bon moment de l'animation d'attaque
    public void AnimationDealDamage()
    {
        if (target == null)
            return;

        float facing = GetFacingSign();

        Vector2 center = (Vector2)transform.position + new Vector2(
            attackBoxOffset.x * facing,
            attackBoxOffset.y
        );

        Collider2D hit = Physics2D.OverlapBox(
            center,
            attackBoxSize,
            0f,
            damageMask
        );

        if (hit == null)
            return;

        IDamageable dmg = hit.GetComponentInParent<IDamageable>();

        if (dmg == null)
            return;

        int finalDamage = damage;

        if (allyPowerDrain != null)
            finalDamage = allyPowerDrain.GetBoostedDamage(damage);

        dmg.TakeDamage(finalDamage);
    }

    void KamikazeAttackUpdate()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        PlayAttackAnimation();

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

        state = State.Chase;
    }

    void ShooterAttackUpdate()
    {
        float dx = target.position.x - transform.position.x;
        ShooterReposition(dx);

        if (attackTimer > 0f) return;

        if (allyPowerDrain != null)
            attackTimer = allyPowerDrain.GetReducedCooldown(shootCooldown);
        else
            attackTimer = shootCooldown;
        PlayAttackAnimation();
        PlayRandomSound(shootAttackSounds, shootAttackVolume);

        if (projectilePrefab == null) return;
        if (target == null) return;

        float facing = GetFacingSign();

        Vector2 spawnPos = shootMuzzle != null
            ? (Vector2)shootMuzzle.position
            : (Vector2)transform.position + new Vector2(0.6f * facing, 0.2f);

        Vector2 targetPos = target.position;

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        var laser = go.GetComponent<LaserProjectile2D>();

        if (laser != null)
        {
            float distanceToPlayer = Vector2.Distance(spawnPos, targetPos);

            int finalDamage = damage;

            if (allyPowerDrain != null)
                finalDamage = allyPowerDrain.GetBoostedDamage(damage);

            laser.InitBeam(spawnPos, targetPos, distanceToPlayer);

            NodeAllyPowerDrain2D nodePower = GetComponent<NodeAllyPowerDrain2D>();

            if (nodePower != null)
                nodePower.PlayRandomAttackSound();

            laser.SetDamage(finalDamage);
            return;
        }

        var proj = go.GetComponent<Projectile2D>();

        if (proj != null)
        {
            Vector2 dir = (targetPos - spawnPos).normalized;
            proj.Init(dir);
            return;
        }

        var prb = go.GetComponent<Rigidbody2D>();

        if (prb != null)
        {
            Vector2 dir = (targetPos - spawnPos).normalized;
            prb.linearVelocity = dir * projectileSpeed;
        }
    }


    // LOS
    bool HasLineOfSight()
    {
        if (requireLineOfSight && obstacleMask.value == 0)
            return false;

        Vector2 from = (Vector2)transform.position + sightOriginOffset;
        Vector2 to = (Vector2)target.position + sightTargetOffset;

        Vector2 dir = (to - from).normalized;
        float dist = Vector2.Distance(from, to);

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


    // Helpers
    void PlayRandomSound(AudioClip[] clips, float volume)
    {
        if (audioSource == null)
            return;

        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, volume);
    }

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


    // Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, GetEngageRange());

        float facing = Application.isPlaying ? facingSign : 1f;

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