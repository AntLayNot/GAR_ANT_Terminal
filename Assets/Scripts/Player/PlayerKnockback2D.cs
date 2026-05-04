using System.Collections;
using UnityEngine;

public class PlayerKnockback2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 14f;
    [SerializeField] private float knockbackDuration = 0.18f;

    private Coroutine knockbackCoroutine;
    private bool isKnockedBack = false;

    public bool IsKnockedBack => isKnockedBack;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    public void ApplyKnockback(Vector2 sourcePosition)
    {
        if (rb == null) return;

        Vector2 direction = ((Vector2)transform.position - sourcePosition).normalized;

        if (knockbackCoroutine != null)
            StopCoroutine(knockbackCoroutine);

        knockbackCoroutine = StartCoroutine(KnockbackRoutine(direction));
    }

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;

        rb.linearVelocity = Vector2.zero;
        rb.linearVelocity = direction * knockbackForce;

        yield return new WaitForSeconds(knockbackDuration);

        rb.linearVelocity = Vector2.zero;
        isKnockedBack = false;
    }
}