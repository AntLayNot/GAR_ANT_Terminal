using UnityEngine;

public class SpikeTrap2D : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private string targetTag = "Player";

    [Header("Knockback")]
    [SerializeField] private bool applyKnockback = true;

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
        if (!other.CompareTag(targetTag))
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

        if (applyKnockback)
        {
            PlayerKnockback2D knockback = other.GetComponent<PlayerKnockback2D>();
            if (knockback == null)
                knockback = other.GetComponentInParent<PlayerKnockback2D>();

            if (knockback != null)
                knockback.ApplyKnockback(transform.position);
        }
    }
}