using UnityEngine;

public class FallingTrap2D : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string targetTag = "Player";

    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Damage")]
    [SerializeField] private int touchDamage = 1;
    [SerializeField] private float damageCooldown = 0.5f;
    [SerializeField] private bool applyKnockback = true;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundCollisionArmingDelay = 0.1f;
    [SerializeField] private float minimumFallSpeedToBreak = 0.05f;

    [Header("Player Hit Conditions")]
    [SerializeField] private float playerHitArmingDelay = 0.05f;
    [SerializeField] private float minimumFallSpeedToDamagePlayer = 0.2f;

    private bool hasFallen = false;
    private bool consumed = false;
    private float lastDamageTime = -999f;
    private float fallStartTime = -999f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void TriggerFall()
    {
        if (hasFallen) return;

        hasFallen = true;
        fallStartTime = Time.time;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other, true);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other, false);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleGroundCollision(collision.collider);
        TryDamage(collision.collider, true);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleGroundCollision(collision.collider);
        TryDamage(collision.collider, false);
    }

    private void HandleGroundCollision(Collider2D other)
    {
        if (consumed) return;
        if (!hasFallen) return;

        if (((1 << other.gameObject.layer) & groundLayers) == 0)
            return;

        if (Time.time < fallStartTime + groundCollisionArmingDelay)
            return;

        if (rb != null && rb.linearVelocity.y > -minimumFallSpeedToBreak)
            return;

        consumed = true;
        Destroy(gameObject);
    }

    private bool CanHitPlayer()
    {
        if (!hasFallen)
            return false;

        if (Time.time < fallStartTime + playerHitArmingDelay)
            return false;

        if (rb != null && rb.linearVelocity.y > -minimumFallSpeedToDamagePlayer)
            return false;

        return true;
    }

    private void TryDamage(Collider2D other, bool destroyOnSuccessfulPlayerHit)
    {
        if (consumed)
            return;

        if (!other.CompareTag(targetTag))
            return;

        if (!CanHitPlayer())
            return;

        if (Time.time < lastDamageTime + damageCooldown)
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable == null)
            return;

        damageable.TakeDamage(touchDamage);
        lastDamageTime = Time.time;

        if (applyKnockback)
        {
            PlayerKnockback2D knockback = other.GetComponent<PlayerKnockback2D>();
            if (knockback == null)
                knockback = other.GetComponentInParent<PlayerKnockback2D>();

            if (knockback != null)
                knockback.ApplyKnockback(transform.position);
        }

        if (destroyOnSuccessfulPlayerHit)
        {
            consumed = true;
            Destroy(gameObject);
        }
    }
}