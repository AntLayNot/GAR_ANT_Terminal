using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Health2D))]
public class MiniBossController : MonoBehaviour
{
    [Header("Fight State")]
    [SerializeField] private bool fightStartsAutomatically = false;
    [SerializeField] private bool isFightActive = false;
    [SerializeField] private bool isDead = false;

    [Header("Auto Setup")]
    [SerializeField] private bool autoFindPlayerOnSpawn = true;
    [SerializeField] private bool startFightOnSpawn = true;
    [SerializeField] private string playerTag = "Player";

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private List<BossPillar> pillars = new List<BossPillar>();
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Health2D health;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D bossCollider;

    [Header("Facing / Visual")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private bool useVisualRootRotationInsteadOfFlip = true;
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private bool faceDashDirection = true;
    [SerializeField] private bool faceRetreatDirection = false;

    private float facingSign = 1f;

    [Header("Boss Indicators")]
    [SerializeField] private GameObject bossProtectedIndicator;
    [SerializeField] private GameObject bossVulnerableIndicator;
    [SerializeField] private Color protectedColor = Color.white;
    [SerializeField] private Color vulnerableColor = Color.red;
    [SerializeField] private bool colorBossWhenVulnerable = true;

    [Header("Projectile Attack")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileCooldown = 1.4f;
    [SerializeField] private int phase1ShotCount = 1;
    [SerializeField] private int phase2ShotCount = 2;
    [SerializeField] private int phase3ShotCount = 3;
    [SerializeField] private float delayBetweenShots = 0.12f;

    [Header("Dash Attack")]
    [SerializeField] private float dashSpeed = 16f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float dashCooldown = 2.6f;
    [SerializeField] private float dashTelegraphTime = 0.2f;

    private bool isDashing = false;

    [Header("Boss Contact Damage")]
    [SerializeField] private int touchDamage = 1;
    [SerializeField] private float touchDamageCooldown = 0.5f;

    [Header("Retreat After Melee Hit")]
    [SerializeField] private float retreatSpeed = 22f;
    [SerializeField] private float retreatDuration = .55f;
    [SerializeField] private float retreatCooldown = 0.75f;

    private bool isRetreating = false;
    private float lastRetreatTime = -999f;

    [Header("Timings")]
    [SerializeField] private float delayBetweenAttacks = 0.3f;

    [Header("Visual Feedback")]
    [SerializeField] private Color telegraphColor = Color.yellow;
    [SerializeField] private Color phase3Color = Color.magenta;
    [SerializeField] private GameObject deathEffect;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip[] fightStartSounds;
    [SerializeField] private AudioClip[] shootSounds;
    [SerializeField] private AudioClip[] dashChargeSounds;
    [SerializeField] private AudioClip[] dashStartSounds;
    [SerializeField] private AudioClip[] protectedHitSounds;
    [SerializeField] private AudioClip[] vulnerableSounds;
    [SerializeField] private AudioClip[] contactHitSounds;
    [SerializeField] private AudioClip[] deathSounds;

    [Header("Audio Volumes")]
    [SerializeField, Range(0f, 1f)] private float fightStartVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float dashChargeVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float dashStartVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float protectedHitVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float vulnerableVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float contactHitVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

    [Header("Audio Settings")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Optional Arena Rewards")]
    [SerializeField] private GameObject objectToEnableOnDeath;
    [SerializeField] private GameObject objectToDisableOnDeath;

    private int destroyedPillars = 0;
    private int currentPhase = 1;

    private bool canShoot = true;
    private bool canDash = true;
    private bool isVulnerable = false;
    private bool isTelegraphing = false;

    private BossPillar currentTargetPillar;

    private Coroutine fightCoroutine;

    private readonly Dictionary<GameObject, float> touchCooldowns = new Dictionary<GameObject, float>();

    public bool IsFightActive => isFightActive;
    public bool IsDead => isDead;
    public bool IsVulnerable => isVulnerable;
    public int DestroyedPillars => destroyedPillars;
    public int RemainingPillars => pillars.Count - destroyedPillars;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (health == null)
            health = GetComponent<Health2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (bossCollider == null)
            bossCollider = GetComponent<Collider2D>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (health != null)
        {
            health.onDeath.AddListener(Die);
            health.onDamageBlocked.AddListener(OnDamageBlocked);
        }

        for (int i = 0; i < pillars.Count; i++)
        {
            if (pillars[i] != null)
                pillars[i].SetBoss(this);
        }

        AutoFindPlayer();
        RefreshVulnerabilityState();

        FaceDir(1f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDeath.RemoveListener(Die);
            health.onDamageBlocked.RemoveListener(OnDamageBlocked);
        }
    }

    private void Start()
    {
        AutoFindPlayer();

        if (fightStartsAutomatically || startFightOnSpawn)
            StartFight();
    }

    private void Update()
    {
        if (!isFightActive || isDead)
            return;

        if (facePlayer && !isDashing && !isRetreating && player != null)
            FaceTarget();
    }

    private void AutoFindPlayer()
    {
        if (!autoFindPlayerOnSpawn)
            return;

        if (player != null)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
            Debug.Log("[MiniBoss] Joueur trouvé automatiquement : " + playerObject.name);
        }
        else
        {
            Debug.LogWarning("[MiniBoss] Aucun joueur trouvé avec le tag : " + playerTag);
        }
    }

    public void StartFight()
    {
        if (isFightActive || isDead) return;

        AutoFindPlayer();

        if (player == null)
        {
            Debug.LogWarning("[MiniBoss] Impossible de démarrer le combat : joueur introuvable.");
            return;
        }

        isFightActive = true;

        PlayRandomSound(fightStartSounds, fightStartVolume);

        FaceTarget();

        InitializePillarTargets();
        RefreshVulnerabilityState();

        if (fightCoroutine != null)
            StopCoroutine(fightCoroutine);

        fightCoroutine = StartCoroutine(FightLoop());

        Debug.Log("[MiniBoss] Combat démarré.");
    }

    private void InitializePillarTargets()
    {
        for (int i = 0; i < pillars.Count; i++)
        {
            if (pillars[i] != null && !pillars[i].IsDestroyed)
                pillars[i].SetAttackable(false);
        }

        SelectNextTargetPillar();
    }

    private void SelectNextTargetPillar()
    {
        currentTargetPillar = null;

        List<BossPillar> available = new List<BossPillar>();

        for (int i = 0; i < pillars.Count; i++)
        {
            if (pillars[i] == null) continue;
            if (pillars[i].IsDestroyed) continue;

            pillars[i].SetAttackable(false);
            available.Add(pillars[i]);
        }

        if (available.Count == 0)
            return;

        currentTargetPillar = available[Random.Range(0, available.Count)];
        currentTargetPillar.SetAttackable(true);

        Debug.Log("[MiniBoss] Nouveau pilier cible : " + currentTargetPillar.name);
    }

    private IEnumerator FightLoop()
    {
        while (isFightActive && !isDead)
        {
            UpdatePhase();

            if (player == null)
            {
                AutoFindPlayer();
                yield return null;
                continue;
            }

            if (isRetreating)
            {
                yield return null;
                continue;
            }

            FaceTarget();

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);

            if (distanceToPlayer < 3.5f && canDash)
            {
                yield return StartCoroutine(DashRoutine());
            }
            else
            {
                List<int> availableAttacks = new List<int>();

                if (canShoot)
                    availableAttacks.Add(0);

                if (canDash)
                    availableAttacks.Add(1);

                if (availableAttacks.Count == 0)
                {
                    yield return null;
                    continue;
                }

                int chosenAttack = availableAttacks[Random.Range(0, availableAttacks.Count)];

                if (chosenAttack == 0)
                    yield return StartCoroutine(ShootRoutine());
                else
                    yield return StartCoroutine(DashRoutine());
            }

            yield return new WaitForSeconds(delayBetweenAttacks);
        }
    }

    private void UpdatePhase()
    {
        int remaining = RemainingPillars;

        if (remaining <= 2)
        {
            currentPhase = 3;
            projectileCooldown = 0.9f;
            dashCooldown = 1.8f;
            delayBetweenAttacks = 0.15f;
        }
        else if (remaining <= 4)
        {
            currentPhase = 2;
            projectileCooldown = 1.1f;
            dashCooldown = 2.2f;
            delayBetweenAttacks = 0.22f;
        }
        else
        {
            currentPhase = 1;
            projectileCooldown = 1.4f;
            dashCooldown = 2.6f;
            delayBetweenAttacks = 0.3f;
        }

        RefreshBossColor();
    }

    private void RefreshBossColor()
    {
        if (spriteRenderer == null)
            return;

        if (isTelegraphing)
        {
            spriteRenderer.color = telegraphColor;
            return;
        }

        if (isVulnerable && colorBossWhenVulnerable)
        {
            spriteRenderer.color = vulnerableColor;
            return;
        }

        if (currentPhase == 3)
            spriteRenderer.color = phase3Color;
        else
            spriteRenderer.color = protectedColor;
    }

    private void RefreshVulnerabilityState()
    {
        bool wasVulnerable = isVulnerable;

        isVulnerable = destroyedPillars >= pillars.Count;

        if (health != null)
            health.SetCanTakeDamage(isVulnerable);

        if (bossProtectedIndicator != null)
            bossProtectedIndicator.SetActive(!isVulnerable);

        if (bossVulnerableIndicator != null)
            bossVulnerableIndicator.SetActive(isVulnerable);

        if (!wasVulnerable && isVulnerable)
            PlayRandomSound(vulnerableSounds, vulnerableVolume);

        RefreshBossColor();
    }

    private void OnDamageBlocked()
    {
        PlayRandomSound(protectedHitSounds, protectedHitVolume);
        Debug.Log("[MiniBoss] Boss protégé : détruire les piliers.");
    }

    private IEnumerator ShootRoutine()
    {
        canShoot = false;

        if (player == null)
            AutoFindPlayer();

        if (player == null || projectilePrefab == null || firePoint == null)
        {
            yield return new WaitForSeconds(projectileCooldown);
            canShoot = true;
            yield break;
        }

        FaceTarget();

        int shotCount = GetShotCountForCurrentPhase();

        for (int i = 0; i < shotCount; i++)
        {
            if (player == null)
                break;

            FaceTarget();

            PlayRandomSound(shootSounds, shootVolume);

            Vector2 dir = (player.position - firePoint.position).normalized;

            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

            BossProjectile bossProjectile = projectile.GetComponent<BossProjectile>();
            if (bossProjectile != null)
                bossProjectile.SetDirection(dir);

            yield return new WaitForSeconds(delayBetweenShots);
        }

        yield return new WaitForSeconds(projectileCooldown);
        canShoot = true;
    }

    private IEnumerator DashRoutine()
    {
        canDash = false;

        if (isRetreating)
        {
            yield return null;
            canDash = true;
            yield break;
        }

        if (player == null)
            AutoFindPlayer();

        if (player == null || rb == null)
        {
            yield return new WaitForSeconds(dashCooldown);
            canDash = true;
            yield break;
        }

        Vector2 dashDirection = (player.position - transform.position).normalized;

        if (faceDashDirection)
            FaceDir(dashDirection.x);
        else if (facePlayer)
            FaceTarget();

        PlayRandomSound(dashChargeSounds, dashChargeVolume);

        isTelegraphing = true;
        RefreshBossColor();

        yield return new WaitForSeconds(dashTelegraphTime);

        isTelegraphing = false;
        RefreshBossColor();

        PlayRandomSound(dashStartSounds, dashStartVolume);

        float timer = 0f;
        isDashing = true;

        while (timer < dashDuration && isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            timer += Time.deltaTime;
            yield return null;
        }

        isDashing = false;
        rb.linearVelocity = Vector2.zero;

        if (facePlayer)
            FaceTarget();

        UpdatePhase();

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void StopCurrentMovement()
    {
        isDashing = false;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private int GetShotCountForCurrentPhase()
    {
        switch (currentPhase)
        {
            case 1: return phase1ShotCount;
            case 2: return phase2ShotCount;
            case 3: return phase3ShotCount;
            default: return 1;
        }
    }

    public void OnPillarDestroyed(BossPillar pillar)
    {
        destroyedPillars++;

        Debug.Log("[MiniBoss] Pilier détruit : " + destroyedPillars + " / " + pillars.Count);

        if (destroyedPillars >= pillars.Count)
        {
            currentTargetPillar = null;
            RefreshVulnerabilityState();
            Debug.Log("[MiniBoss] Tous les piliers sont détruits : le boss est vulnérable.");
            return;
        }

        SelectNextTargetPillar();
        RefreshVulnerabilityState();
    }

    private bool DealTouchDamage(Collider2D other)
    {
        if (!isFightActive || isDead) return false;
        if (!other.CompareTag("Player")) return false;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null) return false;

        GameObject targetObject = other.gameObject;

        if (touchCooldowns.TryGetValue(targetObject, out float lastHitTime))
        {
            if (Time.time < lastHitTime + touchDamageCooldown)
                return false;
        }

        damageable.TakeDamage(touchDamage);
        touchCooldowns[targetObject] = Time.time;

        PlayRandomSound(contactHitSounds, contactHitVolume);

        PlayerKnockback2D knockback = other.GetComponent<PlayerKnockback2D>();
        if (knockback == null)
            knockback = other.GetComponentInParent<PlayerKnockback2D>();

        if (knockback != null)
            knockback.ApplyKnockback(transform.position);

        Debug.Log("[MiniBoss] Dégâts de contact infligés au joueur.");

        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (DealTouchDamage(collision.collider))
            TryStartRetreat();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (DealTouchDamage(collision.collider))
            TryStartRetreat();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (DealTouchDamage(other))
            TryStartRetreat();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (DealTouchDamage(other))
            TryStartRetreat();
    }

    private IEnumerator RetreatAfterHitRoutine()
    {
        if (rb == null || player == null)
            yield break;

        isRetreating = true;
        lastRetreatTime = Time.time;

        rb.linearVelocity = Vector2.zero;

        Vector2 retreatDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;

        if (faceRetreatDirection)
            FaceDir(retreatDirection.x);
        else if (facePlayer)
            FaceTarget();

        float retreatDistance = retreatSpeed * retreatDuration;
        float traveled = 0f;

        while (traveled < retreatDistance)
        {
            float step = retreatSpeed * Time.deltaTime;
            rb.linearVelocity = retreatDirection * retreatSpeed;
            traveled += step;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isRetreating = false;

        if (facePlayer)
            FaceTarget();
    }

    private void TryStartRetreat()
    {
        if (isDead || !isFightActive) return;
        if (rb == null || player == null) return;
        if (isRetreating) return;
        if (Time.time < lastRetreatTime + retreatCooldown) return;

        StopCurrentMovement();
        StartCoroutine(RetreatAfterHitRoutine());
    }

    private void FaceTarget()
    {
        if (player == null)
            return;

        float dx = player.position.x - transform.position.x;
        FaceDir(dx);
    }

    private void FaceDir(float dir)
    {
        if (dir == 0f)
            return;

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
            if (spriteRenderer != null)
                spriteRenderer.flipX = facingSign < 0f;
        }
    }

    public float GetFacingSign()
    {
        return facingSign;
    }

    private void PlayRandomSound(AudioClip[] clips, float volume)
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

    private void PlayDetachedDeathSound()
    {
        if (deathSounds == null || deathSounds.Length == 0)
            return;

        AudioClip clip = deathSounds[Random.Range(0, deathSounds.Length)];

        if (clip == null)
            return;

        GameObject audioObject = new GameObject("MiniBoss_DeathSound");
        audioObject.transform.position = transform.position;

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = deathVolume;
        source.pitch = Random.Range(pitchRange.x, pitchRange.y);
        source.spatialBlend = audioSource != null ? audioSource.spatialBlend : 0f;
        source.outputAudioMixerGroup = audioSource != null ? audioSource.outputAudioMixerGroup : null;

        source.Play();

        Destroy(audioObject, clip.length + 0.2f);
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        isFightActive = false;

        if (fightCoroutine != null)
            StopCoroutine(fightCoroutine);

        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (bossCollider != null)
            bossCollider.enabled = false;

        PlayDetachedDeathSound();

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (objectToEnableOnDeath != null)
            objectToEnableOnDeath.SetActive(true);

        if (objectToDisableOnDeath != null)
            objectToDisableOnDeath.SetActive(false);

        gameObject.SetActive(false);
    }
}