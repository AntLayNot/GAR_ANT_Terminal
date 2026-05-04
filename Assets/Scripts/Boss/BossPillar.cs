using System.Collections;
using UnityEngine;

public class BossPillar : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 10;

    [Header("References")]
    [SerializeField] private MiniBossController boss;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Collider2D hitCollider;

    [Header("Indicators")]
    [SerializeField] private GameObject attackableIndicator;
    [SerializeField] private GameObject lockedIndicator;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color attackableColor = Color.cyan;
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color damageFlashColor = Color.red;

    [Header("Feedback")]
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private GameObject destroyEffect;

    private int currentHealth;
    private bool isDestroyed;
    private bool isAttackable;
    private Coroutine flashCoroutine;

    public bool IsDestroyed => isDestroyed;
    public bool IsAttackable => isAttackable;
    public int CurrentHealth => currentHealth;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (hitCollider != null)
            hitCollider.isTrigger = true;

        RefreshVisualState();
    }

    public void SetBoss(MiniBossController newBoss)
    {
        boss = newBoss;
    }

    public void SetAttackable(bool value)
    {
        if (isDestroyed)
            return;

        isAttackable = value;
        RefreshVisualState();
    }

    public void TakeDamage(int amount)
    {
        if (isDestroyed) return;
        if (!isAttackable) return;
        if (amount <= 0) return;

        currentHealth -= amount;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        if (spriteRenderer != null)
            flashCoroutine = StartCoroutine(DamageFlashRoutine());

        if (currentHealth <= 0)
            DestroyPillar();
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = damageFlashColor;

        yield return new WaitForSeconds(flashDuration);

        RefreshVisualState();
    }

    private void DestroyPillar()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (destroyEffect != null)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);

        if (hitCollider != null)
            hitCollider.enabled = false;

        if (boss != null)
            boss.OnPillarDestroyed(this);

        gameObject.SetActive(false);
    }

    private void RefreshVisualState()
    {
        if (spriteRenderer != null)
        {
            if (isAttackable)
                spriteRenderer.color = attackableColor;
            else
                spriteRenderer.color = lockedColor;
        }

        if (attackableIndicator != null)
            attackableIndicator.SetActive(isAttackable);

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!isAttackable);
    }
}