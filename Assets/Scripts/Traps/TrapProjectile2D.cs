using UnityEngine;

public class TrapProjectile2D : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 5f;

    [Header("Collision")]
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private bool destroyOnHit = true;

    [Header("Knockback")]
    [SerializeField] private bool applyKnockbackToPlayer = true;

    private Vector2 direction;

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.collider);
    }

    private void TryHit(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(damage);

        if (applyKnockbackToPlayer && other.CompareTag("Player"))
        {
            PlayerKnockback2D knockback = other.GetComponent<PlayerKnockback2D>();
            if (knockback == null)
                knockback = other.GetComponentInParent<PlayerKnockback2D>();

            if (knockback != null)
                knockback.ApplyKnockback(transform.position);
        }

        if (destroyOnHit)
            Destroy(gameObject);
    }
}