using UnityEngine;
using UnityEngine.Events;

public class Health2D : MonoBehaviour, IDamageable
{
    public int maxHP = 5;
    public int currentHP;

    [Header("Events")]
    public UnityEvent<int, int> onHPChanged; // current, max
    public UnityEvent onDeath;

    void Awake()
    {
        currentHP = maxHP;
        onHPChanged?.Invoke(currentHP, maxHP);
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0) return;
        if (currentHP <= 0) return;

        currentHP = Mathf.Max(0, currentHP - amount);
        onHPChanged?.Invoke(currentHP, maxHP);

        if (currentHP == 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (currentHP <= 0) return;

        currentHP = Mathf.Min(maxHP, currentHP + amount);
        onHPChanged?.Invoke(currentHP, maxHP);
    }

    void Die()
    {
        onDeath?.Invoke();
        // simple: destroy
        Destroy(gameObject);
    }
}

public interface IDamageable
{
    void TakeDamage(int amount);
}
