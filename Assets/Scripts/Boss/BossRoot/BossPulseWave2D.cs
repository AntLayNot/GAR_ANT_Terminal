using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BossPulseWave2D : MonoBehaviour
{
    [Header("Wave")]
    [SerializeField] private float startRadius = 0.2f;
    [SerializeField] private float endRadius = 4f;
    [SerializeField] private float duration = 0.6f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask damageMask;
    [SerializeField] private bool damageOnlyOnce = true;

    [Header("Hit Precision")]
    [Tooltip("Épaisseur de la vague. Plus petit = il faut vraiment toucher l'anneau.")]
    [SerializeField] private float waveThickness = 0.35f;

    [Header("Fade")]
    [SerializeField] private bool fadeOut = true;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Destroy")]
    [Tooltip("Objet à détruire à la fin. Si vide, détruit automatiquement la racine du prefab.")]
    [SerializeField] private GameObject rootToDestroy;

    [SerializeField] private bool destroyRootObject = true;
    [SerializeField] private float destroyDelayAfterWave = 0f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private SpriteRenderer spriteRenderer;
    private Color baseColor;

    private float timer;
    private float currentRadius;
    private bool finished;

    private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer.color;

        if (rootToDestroy == null)
        {
            rootToDestroy = destroyRootObject
                ? transform.root.gameObject
                : gameObject;
        }
    }

    private void OnEnable()
    {
        timer = 0f;
        finished = false;
        damagedTargets.Clear();

        currentRadius = startRadius;
        ApplyVisual(0f);
    }

    public void Init(
        float newStartRadius,
        float newEndRadius,
        float newDuration,
        int newDamage,
        LayerMask newDamageMask
    )
    {
        startRadius = Mathf.Max(0.01f, newStartRadius);
        endRadius = Mathf.Max(startRadius, newEndRadius);
        duration = Mathf.Max(0.01f, newDuration);

        damage = newDamage;
        damageMask = newDamageMask;

        timer = 0f;
        finished = false;
        damagedTargets.Clear();

        currentRadius = startRadius;
        ApplyVisual(0f);
    }

    private void Update()
    {
        if (finished)
            return;

        if (PauseMenuController.IsPausedGlobal)
            return;

        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);

        ApplyVisual(t);
        CheckDamage();

        if (t >= 1f)
            FinishWave();
    }

    private void ApplyVisual(float t)
    {
        float curveT = scaleCurve.Evaluate(t);

        currentRadius = Mathf.Lerp(startRadius, endRadius, curveT);

        float diameter = currentRadius * 2f;
        transform.localScale = new Vector3(diameter, diameter, 1f);

        if (fadeOut && spriteRenderer != null)
        {
            Color c = baseColor;
            c.a = alphaCurve.Evaluate(t);
            spriteRenderer.color = c;
        }
    }

    private void CheckDamage()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            currentRadius + waveThickness * 0.5f,
            damageMask
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

            if (hit == null)
                continue;

            float distance = Vector2.Distance(transform.position, hit.bounds.center);

            bool isInsideWaveRing =
                distance >= currentRadius - waveThickness * 0.5f &&
                distance <= currentRadius + waveThickness * 0.5f;

            if (!isInsideWaveRing)
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable == null)
                continue;

            if (damageOnlyOnce && damagedTargets.Contains(damageable))
                continue;

            damageable.TakeDamage(damage);

            if (damageOnlyOnce)
                damagedTargets.Add(damageable);
        }
    }

    private void FinishWave()
    {
        if (finished)
            return;

        finished = true;

        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }

        GameObject objectToDestroy = rootToDestroy != null ? rootToDestroy : gameObject;

        Destroy(objectToDestroy, destroyDelayAfterWave);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, currentRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, currentRadius + waveThickness * 0.5f);
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, currentRadius - waveThickness * 0.5f));
    }
}