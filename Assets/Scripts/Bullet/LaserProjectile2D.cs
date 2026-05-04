using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class LaserProjectile2D : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 16f;
    [SerializeField] private bool loop = true;

    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private bool useDirectionMovement = false;
    [SerializeField] private Vector2 direction = Vector2.right;
    [SerializeField] private bool rotateToDirection = false;
    [SerializeField] private bool flipXWithDirection = true;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 3f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private bool destroyOnHit = true;

    private Rigidbody2D rb;
    private float animTimer;
    private int frameIndex;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        animTimer = 0f;
        frameIndex = 0;

        if (frames != null && frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[0];

        Destroy(gameObject, lifeTime);
    }

    private void Start()
    {
        if (rb != null && useDirectionMovement)
            rb.linearVelocity = direction.normalized * speed;

        UpdateVisualDirection();
    }

    private void Update()
    {
        UpdateAnimation();

        if (useDirectionMovement && rb == null)
            transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;

        if (rb != null && useDirectionMovement)
            rb.linearVelocity = direction * speed;

        UpdateVisualDirection();
    }

    private void UpdateAnimation()
    {
        if (frames == null || frames.Length == 0 || spriteRenderer == null)
            return;

        animTimer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, framesPerSecond);

        while (animTimer >= frameDuration)
        {
            animTimer -= frameDuration;
            frameIndex++;

            if (loop)
                frameIndex %= frames.Length;
            else
                frameIndex = Mathf.Min(frameIndex, frames.Length - 1);

            spriteRenderer.sprite = frames[frameIndex];
        }
    }

    private void UpdateVisualDirection()
    {
        if (spriteRenderer == null)
            return;

        if (rotateToDirection)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
        else if (flipXWithDirection)
        {
            if (direction.x != 0f)
                spriteRenderer.flipX = direction.x < 0f;
        }
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

        if (destroyOnHit)
            Destroy(gameObject);
    }
}