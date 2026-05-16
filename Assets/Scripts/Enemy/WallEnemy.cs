using UnityEngine;

[RequireComponent(typeof(Health2D))]
[RequireComponent(typeof(Collider2D))]
public class WallEnemy : MonoBehaviour
{
    [Header("Vie du mur")]
    [SerializeField] private int wallMaxHP = 100;

    [Header("Physique")]
    [SerializeField] private bool forceStaticRigidbody = true;

    [Header("État détruit")]
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private Color brokenColor = Color.gray;
    [SerializeField] private bool disableColliderOnDeath = true;

    [Header("Feedback de mort")]
    [SerializeField] private GameObject deathFX;

    [Header("Feedback visuel quand touché")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitFlashDuration = 0.08f;

    private Health2D health;
    private Rigidbody2D rb;
    private Collider2D wallCollider;

    private Color baseColor;
    private bool isBroken;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        health = GetComponent<Health2D>();
        rb = GetComponent<Rigidbody2D>();
        wallCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        SetupHealth();
        SetupPhysics();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.onHPChanged.AddListener(OnHPChanged);
            health.onDeath.AddListener(OnDeath);
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onHPChanged.RemoveListener(OnHPChanged);
            health.onDeath.RemoveListener(OnDeath);
        }
    }

    private void SetupHealth()
    {
        if (health == null)
            return;

        health.maxHP = wallMaxHP;
        health.currentHP = wallMaxHP;

        health.onHPChanged?.Invoke(health.currentHP, health.maxHP);
    }

    private void SetupPhysics()
    {
        if (!forceStaticRigidbody)
            return;

        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Static;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    private void OnHPChanged(int currentHP, int maxHP)
    {
        if (isBroken)
            return;

        if (currentHP <= 0)
            return;

        if (spriteRenderer != null && gameObject.activeInHierarchy)
        {
            if (flashCoroutine != null)
                StopCoroutine(flashCoroutine);

            flashCoroutine = StartCoroutine(HitFlash());
        }
    }

    private System.Collections.IEnumerator HitFlash()
    {
        spriteRenderer.color = hitColor;

        yield return new WaitForSeconds(hitFlashDuration);

        if (!isBroken && spriteRenderer != null)
            spriteRenderer.color = baseColor;

        flashCoroutine = null;
    }

    private void OnDeath()
    {
        if (isBroken)
            return;

        isBroken = true;

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        if (deathFX != null)
            Instantiate(deathFX, transform.position, Quaternion.identity);

        if (spriteRenderer != null)
        {
            if (brokenSprite != null)
                spriteRenderer.sprite = brokenSprite;

            spriteRenderer.color = brokenColor;
        }

        if (disableColliderOnDeath && wallCollider != null)
            wallCollider.enabled = false;
    }
}