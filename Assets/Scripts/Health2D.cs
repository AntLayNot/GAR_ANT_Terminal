using UnityEngine;
using UnityEngine.Events;

public class Health2D : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public int maxHP = 5;
    public int currentHP;

    [Header("Options")]
    [SerializeField] private bool destroyOnDeath = false;

    [Header("Damage Rules")]
    [SerializeField] private bool canTakeDamage = true;

    [Header("Events")]
    public UnityEvent<int, int> onHPChanged; // current, max
    public UnityEvent onDeath;
    public UnityEvent onDamageBlocked;

    private bool isDead = false;

    public bool IsDead => isDead;
    public bool CanTakeDamage => canTakeDamage;

    void Awake()
    {
        currentHP = maxHP;
        onHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return;

        if (!canTakeDamage)
        {
            onDamageBlocked?.Invoke();
            return;
        }

        currentHP = Mathf.Max(0, currentHP - amount);
        onHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (isDead) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        onHPChanged?.Invoke(currentHP, maxHP);
    }

    public void FullHeal()
    {
        if (isDead) return;

        currentHP = maxHP;
        onHPChanged?.Invoke(currentHP, maxHP);
    }

    public void SetHealth(int value)
    {
        currentHP = Mathf.Clamp(value, 0, maxHP);
        isDead = currentHP <= 0;
        onHPChanged?.Invoke(currentHP, maxHP);

        if (isDead)
            onDeath?.Invoke();
    }

    public void SetCanTakeDamage(bool value)
    {
        canTakeDamage = value;
    }

    public void Revive(int healthAmount)
    {
        isDead = false;
        currentHP = Mathf.Clamp(healthAmount, 1, maxHP);
        onHPChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        onDeath?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}

public interface IDamageable
{
    void TakeDamage(int amount);
}