using UnityEngine;

public class BossRootHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private BossRoot bossRoot;

    [SerializeField] private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public float CurrentHealthNormalized => maxHealth <= 0 ? 0f : (float)currentHealth / maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (bossRoot == null)
            bossRoot = GetComponent<BossRoot>();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"ROOT prend {amount} dégâts. HP = {currentHealth}/{maxHealth}");

        if (bossRoot != null)
            bossRoot.OnDamaged();

        if (currentHealth <= 0)
        {
            if (bossRoot != null)
                bossRoot.OnDeath();
        }
    }
}