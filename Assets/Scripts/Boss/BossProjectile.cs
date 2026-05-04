using UnityEngine;

public class BossProjectile : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float lifeTime = 5f;

    [Header("Projectile HP")]
    [SerializeField] private int projectileHP = 1;

    [Header("Collision")]
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private bool destroyOnAnyValidHit = true;
    [SerializeField] private bool canBeDamagedByPlayerProjectiles = true;

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

    public void TakeDamage(int amount)
    {
        if (!canBeDamagedByPlayerProjectiles) return;
        if (amount <= 0) return;

        projectileHP -= amount;

        if (projectileHP <= 0)
            Destroy(gameObject);
    }

    private bool IsLayerIncluded(GameObject obj)
    {
        return ((1 << obj.layer) & hitLayers) != 0;
    }

    private void TryHit(Collider2D other)
    {
        if (!IsLayerIncluded(other.gameObject))
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(damage);

        if (destroyOnAnyValidHit)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.collider);
    }
}