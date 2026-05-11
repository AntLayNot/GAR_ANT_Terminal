using UnityEngine;

public class BossRootProjectile : MonoBehaviour
{
    [SerializeField] private float lifeTime = 4f;
    [SerializeField] private int damage = 1;

    private Vector2 moveDirection;
    private float moveSpeed;

    public void Init(Vector2 dir, float speed)
    {
        moveDirection = dir.normalized;
        moveSpeed = speed;

        float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position += (Vector3)(moveDirection * moveSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            other.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}