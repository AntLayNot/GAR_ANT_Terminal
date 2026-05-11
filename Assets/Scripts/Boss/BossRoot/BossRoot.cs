using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRoot : MonoBehaviour
{
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

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private BossRootHealth health;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer sr;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 8f;

    [Header("Summon")]
    [SerializeField] private BossRootMinionSpawner minionSpawner;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float keepDistance = 6f;
    [SerializeField] private float retreatDistance = 3f;

    [Header("Pattern Timing")]
    [SerializeField] private float timeBetweenPatterns = 2f;
    [SerializeField] private float patternCooldown = 1.2f;

    [Header("Dash Strike")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.35f;

    [Header("Pulse Zone")]
    [SerializeField] private float pulseRadius = 3f;
    [SerializeField] private int pulseDamage = 1;
    [SerializeField] private LayerMask playerLayer;

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    private BossPhase currentPhase = BossPhase.Phase1;
    private bool isBusy;
    private bool isDead;

    private void Awake()
    {
        if (health == null) health = GetComponent<BossRootHealth>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        StartCoroutine(BossLoop());
    }

    private void Update()
    {
        if (isDead || player == null) return;

        UpdatePhase();
        UpdateFacing();
    }

    private void UpdatePhase()
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

    private void UpdateFacing()
    {
        if (player == null) return;

        if (player.position.x < transform.position.x)
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        else
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
    }

    private IEnumerator BossLoop()
    {
        yield return new WaitForSeconds(1f);

        while (!isDead)
        {
            if (!isBusy && player != null)
            {
                yield return StartCoroutine(HandlePositioning());

                BossPattern pattern = ChoosePattern();
                if (pattern != BossPattern.None)
                {
                    yield return StartCoroutine(ExecutePattern(pattern));
                    yield return new WaitForSeconds(patternCooldown);
                }
            }

            yield return new WaitForSeconds(timeBetweenPatterns);
        }
    }

    private IEnumerator HandlePositioning()
    {
        if (player == null) yield break;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > keepDistance)
        {
            while (distance > keepDistance && !isBusy && player != null)
            {
                Vector2 dir = (player.position - transform.position).normalized;
                rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

                distance = Vector2.Distance(transform.position, player.position);
                yield return null;
            }
        }
        else if (distance < retreatDistance)
        {
            float timer = 0.4f;

            while (timer > 0f && !isBusy && player != null)
            {
                Vector2 dir = (transform.position - player.position).normalized;
                rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);

                timer -= Time.deltaTime;
                yield return null;
            }
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private BossPattern ChoosePattern()
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

        if (pool.Count == 0) return BossPattern.None;

        int index = Random.Range(0, pool.Count);
        return pool[index];
    }

    private IEnumerator ExecutePattern(BossPattern pattern)
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

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

        isBusy = false;
    }

    private IEnumerator TripleShotRoutine()
    {
        if (player == null || projectilePrefab == null || firePoint == null)
            yield break;

        Vector2 baseDir = (player.position - firePoint.position).normalized;

        FireProjectile(baseDir);
        FireProjectile(Quaternion.Euler(0f, 0f, 15f) * baseDir);
        FireProjectile(Quaternion.Euler(0f, 0f, -15f) * baseDir);

        yield return new WaitForSeconds(0.4f);

        FireProjectile(baseDir);
        FireProjectile(Quaternion.Euler(0f, 0f, 25f) * baseDir);
        FireProjectile(Quaternion.Euler(0f, 0f, -25f) * baseDir);
    }

    private IEnumerator RainShotRoutine()
    {
        if (player == null || projectilePrefab == null)
            yield break;

        int count = currentPhase == BossPhase.Phase3 ? 10 : 6;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(
                player.position.x + Random.Range(-4f, 4f),
                player.position.y + Random.Range(4f, 6f),
                0f
            );

            GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            BossRootProjectile proj = go.GetComponent<BossRootProjectile>();

            if (proj != null)
                proj.Init(Vector2.down, projectileSpeed);

            yield return new WaitForSeconds(0.12f);
        }
    }

    private IEnumerator SummonWaveRoutine()
    {
        if (minionSpawner != null)
            minionSpawner.SpawnWave(currentPhase);

        yield return new WaitForSeconds(0.8f);
    }

    private IEnumerator PulseZoneRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        Collider2D hit = Physics2D.OverlapCircle(transform.position, pulseRadius, playerLayer);
        if (hit != null)
        {
            hit.SendMessage("TakeDamage", pulseDamage, SendMessageOptions.DontRequireReceiver);
        }

        yield return new WaitForSeconds(0.3f);
    }

    private IEnumerator DashStrikeRoutine()
    {
        if (player == null) yield break;

        Vector2 dir = (player.position - transform.position).normalized;

        float timer = dashDuration;
        while (timer > 0f)
        {
            rb.linearVelocity = new Vector2(dir.x * dashSpeed, rb.linearVelocity.y);
            timer -= Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void FireProjectile(Vector2 direction)
    {
        GameObject go = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        BossRootProjectile proj = go.GetComponent<BossRootProjectile>();

        if (proj != null)
            proj.Init(direction.normalized, projectileSpeed);
    }

    public void OnDeath()
    {
        isDead = true;
        StopAllCoroutines();
        rb.linearVelocity = Vector2.zero;

        // Ici tu peux :
        // - lancer animation de mort
        // - ouvrir la sortie
        // - déclencher la fin de combat
        Debug.Log("ROOT / ∅ defeated.");
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pulseRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, keepDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
}