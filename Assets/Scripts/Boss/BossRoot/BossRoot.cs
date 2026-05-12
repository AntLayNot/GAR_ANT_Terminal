using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossRoot : MonoBehaviour
{
    public enum State
    {
        Idle,
        Intro,
        Reposition,
        Attack,
        Dead
    }

    public enum BossPhase
    {
        Phase1,
        Phase2,
        Phase3
    }

    public enum BossPattern
    {
        None,
        TripleShot,
        RainShot,
        SummonWave,
        PulseZone,
        DashStrike
    }

    [Header("Facing")]
    public Transform visualRoot;
    public bool rotateVisualRootOnly = false;

    private float facingSign = 1f;

    [Header("Target")]
    public Transform target;
    public string targetTag = "Player";

    [Header("References")]
    public BossRootHealth health;
    public Rigidbody2D rb;

    [Header("Detection")]
    public float detectRange = 12f;
    public float loseRange = 18f;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float repositionSpeed = 3.2f;
    public float preferredDistance = 6f;
    public float minDistance = 3f;
    public bool faceTarget = true;

    [Header("Attack Timing")]
    public float introDuration = 1f;
    public float attackCooldown = 1.25f;
    public float repositionTolerance = 0.4f;

    [Header("Projectile Attack")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 8f;
    public int tripleShotBursts = 2;
    public float tripleShotBurstDelay = 0.35f;
    public float tripleShotSpreadA = 15f;
    public float tripleShotSpreadB = 25f;

    [Header("Rain Attack")]
    public int rainShotCountPhase2 = 6;
    public int rainShotCountPhase3 = 10;
    public Vector2 rainHorizontalRange = new Vector2(-4f, 4f);
    public Vector2 rainVerticalRange = new Vector2(4f, 6f);
    public float rainShotDelay = 0.12f;

    [Header("Summon")]
    public BossRootMinionSpawner minionSpawner;

    [Header("Pulse")]
    public float pulseRadius = 3f;
    public int pulseDamage = 1;
    public LayerMask damageMask;
    public GameObject pulseEffectPrefab;
    public float pulseChargeTime = 0.4f;
    public float pulseRecoveryTime = 0.2f;

    [Header("Dash")]
    public float dashSpeed = 12f;
    public float dashDuration = 0.35f;
    public float dashDamage = 1f;
    public Vector2 dashHitboxSize = new Vector2(1.4f, 1.2f);
    public Vector2 dashHitboxOffset = new Vector2(1f, 0f);
    public TrailRenderer dashTrail;
    public float dashChargeTime = 0.15f;

    [Header("Simple Visual FX")]
    public Color pulseFlashColor = new Color(0.65f, 0.95f, 1f, 1f);
    public Color hurtFlashColor = new Color(1f, 0.45f, 0.75f, 1f);
    public SpriteRenderer[] flashRenderers;

    [Header("Death Sequence")]
    [SerializeField] private bool useDeathSequence = true;
    [SerializeField] private float deathSlowTimeScale = 0.15f;
    [SerializeField] private float deathSlowDuration = 0.6f;
    [SerializeField] private float deathDestroyDelay = 0.35f;
    [SerializeField] private CinemachineCamera deathZoomCamera;
    [SerializeField] private int deathZoomPriority = 100;
    [SerializeField] private int normalCameraPriority = 10;
    [SerializeField] private GameObject deathEffectPrefab;

    private bool deathSequenceStarted;


    [Header("Debug")]
    public bool showGizmos = true;

    private State state = State.Idle;
    private BossPhase currentPhase = BossPhase.Phase1;

    private float attackTimer;
    private bool introPlayed;
    private bool isDead;
    private bool isPerformingPattern;

    private BossPattern lastPattern = BossPattern.None;
    private Coroutine currentRoutine;

    void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (health == null)
            health = GetComponent<BossRootHealth>();
    }

    void Start()
    {
        if (target == null)
        {
            var go = GameObject.FindGameObjectWithTag(targetTag);
            if (go != null)
                target = go.transform;
        }

        FaceDir(1f);

        if (dashTrail != null)
            dashTrail.emitting = false;
    }

    void Update()
    {
        if (isDead)
        {
            state = State.Dead;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (target == null)
            return;

        attackTimer -= Time.deltaTime;
        UpdatePhase();

        float dist = Vector2.Distance(transform.position, target.position);
        bool seesTarget = dist <= detectRange;
        bool lostTarget = dist >= loseRange;

        if (faceTarget && !isPerformingPattern)
        {
            float dx = target.position.x - transform.position.x;
            if (dx != 0f)
                FaceDir(dx);
        }

        switch (state)
        {
            case State.Idle:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                if (seesTarget)
                {
                    if (!introPlayed)
                    {
                        state = State.Intro;
                        StartStateRoutine(IntroRoutine());
                    }
                    else
                    {
                        state = State.Reposition;
                    }
                }
                break;

            case State.Intro:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;

            case State.Reposition:
                if (lostTarget)
                {
                    state = State.Idle;
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                    break;
                }

                RepositionUpdate();

                if (IsAtPreferredDistance() && attackTimer <= 0f && !isPerformingPattern)
                {
                    state = State.Attack;
                }
                break;

            case State.Attack:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

                if (lostTarget)
                {
                    state = State.Idle;
                    break;
                }

                if (attackTimer > 0f || isPerformingPattern)
                    break;

                BossPattern pattern = ChoosePattern();
                if (pattern == BossPattern.None)
                {
                    state = State.Reposition;
                    break;
                }

                StartStateRoutine(AttackRoutine(pattern));
                break;

            case State.Dead:
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                break;
        }
    }

    void UpdatePhase()
    {
        if (health == null) return;

        float ratio = health.CurrentHealthNormalized;

        if (ratio > 0.66f)
            currentPhase = BossPhase.Phase1;
        else if (ratio > 0.33f)
            currentPhase = BossPhase.Phase2;
        else
            currentPhase = BossPhase.Phase3;
    }

    void RepositionUpdate()
    {
        float dx = target.position.x - transform.position.x;
        float abs = Mathf.Abs(dx);
        float dirToTarget = Mathf.Sign(dx);

        if (dirToTarget == 0f)
            dirToTarget = GetFacingSign();

        if (abs < minDistance)
        {
            float dirAway = -dirToTarget;
            MoveX(dirAway, repositionSpeed);
            if (faceTarget) FaceDir(dirToTarget);
            return;
        }

        if (abs > preferredDistance + repositionTolerance)
        {
            MoveX(dirToTarget, repositionSpeed);
            if (faceTarget) FaceDir(dirToTarget);
            return;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        if (faceTarget) FaceDir(dirToTarget);
    }

    bool IsAtPreferredDistance()
    {
        if (target == null) return false;

        float abs = Mathf.Abs(target.position.x - transform.position.x);
        return abs >= minDistance && abs <= preferredDistance + repositionTolerance;
    }

    void StartStateRoutine(IEnumerator routine)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(routine);
    }

    IEnumerator IntroRoutine()
    {
        introPlayed = true;
        isPerformingPattern = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(introDuration);

        isPerformingPattern = false;
        attackTimer = 0.35f;
        state = State.Reposition;
        currentRoutine = null;
    }

    IEnumerator AttackRoutine(BossPattern pattern)
    {
        isPerformingPattern = true;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        switch (pattern)
        {
            case BossPattern.TripleShot:
                yield return StartCoroutine(TripleShotRoutine());
                break;

            case BossPattern.RainShot:
                yield return StartCoroutine(RainShotRoutine());
                break;

            case BossPattern.SummonWave:
                yield return StartCoroutine(SummonWaveRoutine());
                break;

            case BossPattern.PulseZone:
                yield return StartCoroutine(PulseZoneRoutine());
                break;

            case BossPattern.DashStrike:
                yield return StartCoroutine(DashStrikeRoutine());
                break;
        }

        lastPattern = pattern;
        attackTimer = attackCooldown;
        isPerformingPattern = false;
        state = State.Reposition;
        currentRoutine = null;
    }

    BossPattern ChoosePattern()
    {
        List<BossPattern> pool = new List<BossPattern>();

        switch (currentPhase)
        {
            case BossPhase.Phase1:
                pool.Add(BossPattern.TripleShot);
                pool.Add(BossPattern.PulseZone);
                break;

            case BossPhase.Phase2:
                pool.Add(BossPattern.TripleShot);
                pool.Add(BossPattern.RainShot);
                pool.Add(BossPattern.SummonWave);
                pool.Add(BossPattern.PulseZone);
                break;

            case BossPhase.Phase3:
                pool.Add(BossPattern.TripleShot);
                pool.Add(BossPattern.RainShot);
                pool.Add(BossPattern.SummonWave);
                pool.Add(BossPattern.PulseZone);
                pool.Add(BossPattern.DashStrike);
                break;
        }

        if (pool.Count == 0)
            return BossPattern.None;

        if (pool.Count > 1 && lastPattern != BossPattern.None)
            pool.Remove(lastPattern);

        int index = Random.Range(0, pool.Count);
        return pool[index];
    }

    private IEnumerator TripleShotRoutine()
    {
        if (target == null || projectilePrefab == null || firePoint == null)
            yield break;

        for (int burst = 0; burst < tripleShotBursts; burst++)
        {
            Vector2 aimDir = ((Vector2)target.position - (Vector2)firePoint.position).normalized;

            float spread = burst == 0 ? tripleShotSpreadA : tripleShotSpreadB;

            FireProjectile(aimDir);
            FireProjectile(Quaternion.Euler(0f, 0f, spread) * aimDir);
            FireProjectile(Quaternion.Euler(0f, 0f, -spread) * aimDir);

            if (burst < tripleShotBursts - 1)
                yield return new WaitForSeconds(tripleShotBurstDelay);
        }
    }

    IEnumerator RainShotRoutine()
    {
        if (target == null || projectilePrefab == null)
            yield break;

        int count = currentPhase == BossPhase.Phase3 ? rainShotCountPhase3 : rainShotCountPhase2;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(
                target.position.x + Random.Range(rainHorizontalRange.x, rainHorizontalRange.y),
                target.position.y + Random.Range(rainVerticalRange.x, rainVerticalRange.y),
                0f
            );

            GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            BossRootProjectile proj = go.GetComponent<BossRootProjectile>();
            if (proj == null)
                proj = go.GetComponentInChildren<BossRootProjectile>();

            if (proj != null)
            {
                proj.Init(Vector2.down, projectileSpeed);
            }
            else
            {
                Rigidbody2D prb = go.GetComponent<Rigidbody2D>();
                if (prb == null)
                    prb = go.GetComponentInChildren<Rigidbody2D>();

                if (prb != null)
                    prb.linearVelocity = Vector2.down * projectileSpeed;
            }

            yield return new WaitForSeconds(rainShotDelay);
        }
    }

    IEnumerator SummonWaveRoutine()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (minionSpawner != null)
            minionSpawner.SpawnWave(currentPhase);

        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator PulseZoneRoutine()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (pulseEffectPrefab != null)
            Instantiate(pulseEffectPrefab, transform.position, Quaternion.identity);

        StartCoroutine(FlashRoutine(pulseFlashColor, pulseChargeTime));

        yield return new WaitForSeconds(pulseChargeTime);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pulseRadius, damageMask);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;

            var dmg = hits[i].GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(pulseDamage);
            else
                hits[i].SendMessage("TakeDamage", pulseDamage, SendMessageOptions.DontRequireReceiver);
        }

        yield return new WaitForSeconds(pulseRecoveryTime);
    }

    IEnumerator DashStrikeRoutine()
    {
        if (target == null)
            yield break;

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        yield return new WaitForSeconds(dashChargeTime);

        float dx = target.position.x - transform.position.x;
        float dir = Mathf.Sign(dx);
        if (dir == 0f)
            dir = GetFacingSign();

        FaceDir(dir);

        if (dashTrail != null)
        {
            dashTrail.Clear();
            dashTrail.emitting = true;
        }

        float timer = dashDuration;
        HashSet<IDamageable> damaged = new HashSet<IDamageable>();

        while (timer > 0f)
        {
            MoveX(dir, dashSpeed);

            Vector2 center = (Vector2)transform.position + new Vector2(dashHitboxOffset.x * dir, dashHitboxOffset.y);
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, dashHitboxSize, 0f, damageMask);

            foreach (Collider2D hit in hits)
            {
                if (hit == null) continue;

                IDamageable dmg = hit.GetComponentInParent<IDamageable>();
                if (dmg != null)
                {
                    if (!damaged.Contains(dmg))
                    {
                        dmg.TakeDamage((int)dashDamage);
                        damaged.Add(dmg);
                    }
                }
                else
                {
                    hit.SendMessage("TakeDamage", (int)dashDamage, SendMessageOptions.DontRequireReceiver);
                }
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (dashTrail != null)
            dashTrail.emitting = false;
    }

    void FireProjectile(Vector2 direction)
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        GameObject go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        BossRootProjectile proj = go.GetComponent<BossRootProjectile>();
        if (proj == null)
            proj = go.GetComponentInChildren<BossRootProjectile>();

        if (proj != null)
        {
            proj.Init(direction.normalized, projectileSpeed);
            return;
        }

        Rigidbody2D prb = go.GetComponent<Rigidbody2D>();
        if (prb == null)
            prb = go.GetComponentInChildren<Rigidbody2D>();

        if (prb != null)
            prb.linearVelocity = direction.normalized * projectileSpeed;
    }

    public void OnDeath()
    {
        if (isDead || deathSequenceStarted) return;

        isDead = true;
        state = State.Dead;
        deathSequenceStarted = true;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        StopAllCoroutines();

        rb.linearVelocity = Vector2.zero;

        if (dashTrail != null)
            dashTrail.emitting = false;

        Debug.Log("ROOT / ∅ defeated.");

        if (useDeathSequence)
            StartCoroutine(DeathSequenceRoutine());
        else
            Destroy(gameObject);
    }

    private IEnumerator DeathSequenceRoutine()
    {
        float oldTimeScale = Time.timeScale;
        float oldFixedDeltaTime = Time.fixedDeltaTime;

        // Zoom sur le boss
        if (deathZoomCamera != null)
        {
            deathZoomCamera.Follow = transform;
            deathZoomCamera.LookAt = transform;
            deathZoomCamera.Priority = deathZoomPriority;
        }

        // Effet
        if (deathEffectPrefab != null)
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

        // Ralenti global
        Time.timeScale = deathSlowTimeScale;
        Time.fixedDeltaTime = oldFixedDeltaTime * deathSlowTimeScale;

        // Attente en temps réel, pour ne pas être affectée par le slowmo
        yield return new WaitForSecondsRealtime(deathSlowDuration);

        // Détruire le boss
        Destroy(gameObject, deathDestroyDelay);

        // Attendre encore un peu avant de remettre le temps normal
        yield return new WaitForSecondsRealtime(deathDestroyDelay);

        Time.timeScale = 1f;
        Time.fixedDeltaTime = oldFixedDeltaTime;

        if (deathZoomCamera != null)
            deathZoomCamera.Priority = normalCameraPriority;
    }

    public void OnDamaged()
    {
        if (!isDead)
            StartCoroutine(FlashRoutine(hurtFlashColor, 0.12f));
    }

    IEnumerator FlashRoutine(Color flashColor, float duration)
    {
        if (flashRenderers == null || flashRenderers.Length == 0)
            yield break;

        Color[] baseColors = new Color[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
                baseColors[i] = flashRenderers[i].color;
        }

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
                flashRenderers[i].color = flashColor;
        }

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < flashRenderers.Length; i++)
        {
            if (flashRenderers[i] != null)
                flashRenderers[i].color = baseColors[i];
        }
    }

    void MoveX(float dir, float speed)
    {
        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    void FaceDir(float dir)
    {
        if (dir == 0f) return;

        facingSign = dir < 0f ? -1f : 1f;

        if (rotateVisualRootOnly)
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
            transform.rotation = facingSign < 0f
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.Euler(0f, 0f, 0f);
        }
    }

    float GetFacingSign()
    {
        return facingSign;
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, preferredDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pulseRadius);

        float facing = Application.isPlaying ? facingSign : 1f;
        Vector2 dashCenter = (Vector2)transform.position + new Vector2(dashHitboxOffset.x * facing, dashHitboxOffset.y);

        Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.8f);
        Gizmos.DrawWireCube(dashCenter, dashHitboxSize);
    }
}