using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
public class LaserProjectile2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Animation")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 16f;
    [SerializeField] private bool loop = true;

    [Header("Beam")]
    [SerializeField] private float beamLength = 5f;
    [SerializeField] private float beamThickness = 0.35f;
    [SerializeField] private bool fitVisualToBeamLength = true;

    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 0.35f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private bool destroyOnHit = false;

    private Rigidbody2D rb;
    private CapsuleCollider2D capsuleCollider;

    private float animTimer;
    private int frameIndex;
    private Vector2 direction = Vector2.right;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = true;

        capsuleCollider.isTrigger = true;
        capsuleCollider.direction = CapsuleDirection2D.Horizontal;
    }

    private void OnEnable()
    {
        animTimer = 0f;
        frameIndex = 0;

        if (frames != null && frames.Length > 0 && spriteRenderer != null)
            spriteRenderer.sprite = frames[0];

        ApplyBeamShape();

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        UpdateAnimation();

        // Sécurité : le laser ne doit jamais se déplacer comme une bullet.
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    public void InitBeam(Vector2 startPosition, Vector2 targetPosition, float lengthOverride = -1f)
    {
        transform.position = startPosition;

        direction = (targetPosition - startPosition).normalized;

        if (direction == Vector2.zero)
            direction = Vector2.right;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (lengthOverride > 0f)
            beamLength = lengthOverride;

        ApplyBeamShape();
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection.normalized;

        if (direction == Vector2.zero)
            direction = Vector2.right;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        ApplyBeamShape();
    }

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
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

            ApplyBeamShape();
        }
    }

    private void ApplyBeamShape()
    {
        if (capsuleCollider != null)
        {
            capsuleCollider.direction = CapsuleDirection2D.Horizontal;

            // Le collider commence au point de tir et part vers l'avant.
            capsuleCollider.offset = new Vector2(beamLength * 0.5f, 0f);
            capsuleCollider.size = new Vector2(beamLength, beamThickness);
        }

        if (spriteRenderer != null)
        {
            // Très important :
            // le sprite est enfant du laser, donc on peut le décaler sans déplacer le point de tir.
            spriteRenderer.transform.localPosition = new Vector3(beamLength * 0.5f, 0f, 0f);
            spriteRenderer.transform.localRotation = Quaternion.identity;

            if (fitVisualToBeamLength && spriteRenderer.sprite != null)
            {
                Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

                if (spriteSize.x > 0f && spriteSize.y > 0f)
                {
                    spriteRenderer.transform.localScale = new Vector3(
                        beamLength / spriteSize.x,
                        beamThickness / spriteSize.y,
                        1f
                    );
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
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

    private void OnDrawGizmosSelected()
    {
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col == null) return;

        Gizmos.color = Color.red;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(col.offset, col.size);

        Gizmos.matrix = oldMatrix;
    }
}