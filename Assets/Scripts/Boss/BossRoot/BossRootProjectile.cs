using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BossRootProjectile : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private int damage = 1;

    [Header("Collision")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField] private bool destroyOnAnyCollision = true;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private bool rotateToDirection = true;

    private Vector2 moveDirection = Vector2.right;
    private bool initialized;

    public void Init(Vector2 dir, float overrideSpeed = -1f)
    {
        moveDirection = dir.normalized;

        if (overrideSpeed > 0f)
            speed = overrideSpeed;


        if (rotateToDirection)
        {
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        initialized = true;
    }

    private void Awake()
    {
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += (Vector3)(moveDirection * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;

        bool inMask = hitMask.value == 0 || ((hitMask.value & (1 << other.gameObject.layer)) != 0);
        if (!inMask) return;

        bool hitSomething = false;

        var dmg = other.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
            hitSomething = true;
        }
        else if (other.CompareTag("Player"))
        {
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            hitSomething = true;
        }
        else if (destroyOnAnyCollision)
        {
            hitSomething = true;
        }

        if (hitSomething)
        {
            SpawnHitEffect();
            Destroy(gameObject);
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
    }
}