using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 14f;
    public float lifetime = 3f;

    [Header("Impact")]
    public LayerMask hitLayers;   // Ground, Enemy, Target...
    public bool destroyOnHit = true;

    [Header("Damage")]
    public int damage = 1;

    private int bonusDamage;

    private Rigidbody2D rb;
    private float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    /// <summary>
    /// Initialise la direction du projectile.
    /// Appelé au moment où le projectile est vraiment lancé.
    /// </summary>
    public void Init(Vector2 direction)
    {
        ApplyProgressionBonus();

        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        direction.Normalize();
        rb.linearVelocity = direction * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ApplyProgressionBonus()
    {
        bonusDamage = 0;

        PlayerCommandProgression2D progression = PlayerCommandProgression2D.Current;

        if (progression == null)
            progression = FindFirstObjectByType<PlayerCommandProgression2D>();

        if (progression != null)
            bonusDamage = progression.GetProjectileDamageBonus();
    }

    private int GetFinalDamage()
    {
        return Mathf.Max(0, damage + bonusDamage);
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (lifetime > 0f && timer >= lifetime)
            Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsInLayerMask(collision.gameObject.layer, hitLayers))
            return;

        IDamageable damageable = collision.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(GetFinalDamage());

        if (destroyOnHit)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsInLayerMask(other.gameObject.layer, hitLayers))
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(GetFinalDamage());

            if (destroyOnHit)
                Destroy(gameObject);
        }
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}