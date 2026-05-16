using UnityEngine;

public class SpikeTrap2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 0.5f;

    [Tooltip("Layers qui peuvent prendre des dégâts : Player, Enemy, Boss, etc.")]
    [SerializeField] private LayerMask damageLayers;

    [Header("Knockback")]
    [SerializeField] private bool applyKnockbackOnPlayer = true;

    private float lastDamageTime = -999f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        if (!IsInDamageLayer(other.gameObject.layer))
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(damage);
        lastDamageTime = Time.time;

        TryApplyPlayerKnockback(other);
    }

    private void TryApplyPlayerKnockback(Collider2D other)
    {
        if (!applyKnockbackOnPlayer)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayerKnockback2D knockback = other.GetComponent<PlayerKnockback2D>();

        if (knockback == null)
            knockback = other.GetComponentInParent<PlayerKnockback2D>();

        if (knockback != null)
            knockback.ApplyKnockback(transform.position);
    }

    private bool IsInDamageLayer(int layer)
    {
        return (damageLayers.value & (1 << layer)) != 0;
    }
}