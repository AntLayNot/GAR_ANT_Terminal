using UnityEngine;

public class FallingTrap2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private string targetTag = "Player";

    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Damage")]
    [SerializeField] private int touchDamage = 1;
    [SerializeField] private bool applyKnockback = true;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private float groundCollisionArmingDelay = 0.1f;

    [Header("Player Hit Conditions")]
    [SerializeField] private float playerHitArmingDelay = 0.05f;
    [SerializeField] private float minimumFallSpeedToDamagePlayer = 0.2f;

    [Header("Destroy")]
    [SerializeField] private bool destroyWhenHitGround = true;
    [SerializeField] private bool destroyWhenHitPlayer = true;
    [SerializeField] private GameObject destroyFX;

    private bool hasFallen;
    private bool consumed;
    private float fallStartTime = -999f;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 1f;
            rb.freezeRotation = true;
        }
    }

    public void TriggerFall()
    {
        if (hasFallen || consumed)
            return;

        hasFallen = true;
        fallStartTime = Time.time;

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (consumed)
            return;

        Collider2D other = collision.collider;

        if (other.CompareTag(targetTag))
        {
            TryHitPlayer(other);
            return;
        }

        if (IsGround(other))
        {
            TryBreakOnGround();
        }
    }

    private void TryHitPlayer(Collider2D other)
    {
        if (!CanHitPlayer())
            return;

        IDamageable damageable = other.GetComponent<IDamageable>();

        if (damageable == null)
            damageable = other.GetComponentInParent<IDamageable>();

        if (damageable != null)
            damageable.TakeDamage(touchDamage);

        if (applyKnockback)
        {
            PlayerKnockback2D knockback = other.GetComponent<PlayerKnockback2D>();

            if (knockback == null)
                knockback = other.GetComponentInParent<PlayerKnockback2D>();

            if (knockback != null)
                knockback.ApplyKnockback(transform.position);
        }

        if (destroyWhenHitPlayer)
            ConsumeTrap();
    }

    private void TryBreakOnGround()
    {
        if (!hasFallen)
            return;

        if (Time.time < fallStartTime + groundCollisionArmingDelay)
            return;

        if (destroyWhenHitGround)
            ConsumeTrap();
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

    private bool IsGround(Collider2D other)
    {
        return ((1 << other.gameObject.layer) & groundLayers) != 0;
    }

    private void ConsumeTrap()
    {
        if (consumed)
            return;

        consumed = true;

        if (destroyFX != null)
            Instantiate(destroyFX, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}