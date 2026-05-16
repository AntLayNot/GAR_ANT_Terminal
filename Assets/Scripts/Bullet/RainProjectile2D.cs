using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RainProjectile2D : MonoBehaviour
{
    [Header("Dégâts")]
    [SerializeField] private float damage = 10f;

    [Header("Mouvement")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private Vector2 direction = Vector2.down;

    [Header("Collision")]
    [SerializeField] private LayerMask damageLayers;
    [SerializeField] private bool destroyOnHit = true;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void Start()
    {
        ApplyVelocity();
    }

    public void Init(Vector2 newDirection, float newSpeed, float newDamage, float lifetime)
    {
        direction = newDirection.normalized;
        speed = newSpeed;
        damage = newDamage;

        ApplyVelocity();

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    private void ApplyVelocity()
    {
        if (rb != null)
            rb.linearVelocity = direction * speed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & damageLayers) == 0)
            return;

        IDamageable damageable = collision.GetComponent<IDamageable>();

        if (damageable != null)
        {
            damageable.TakeDamage(Mathf.RoundToInt(damage));
        }

        if (destroyOnHit)
            Destroy(gameObject);
    }
}