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

    [Header("Damage (optional)")]
    public int damage = 1;

    Rigidbody2D rb;
    float timer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    /// <summary>
    /// Initialise la direction du projectile
    /// </summary>
    public void Init(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector2.right;

        direction.Normalize();
        rb.linearVelocity = direction * speed;

        // Orientation visuelle (facultatif)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (lifetime > 0f && timer >= lifetime)
            Destroy(gameObject);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Vérifie layer
        if (!IsInLayerMask(collision.gameObject.layer, hitLayers))
            return;

        // Damage si possible
        var damageable = collision.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);

        if (destroyOnHit)
            Destroy(gameObject);
    }

    static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
