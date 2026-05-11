using UnityEngine;

public class BossRootHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private BossRoot bossRoot;

    private int currentHealth;

    public int CurrentHealth => currentHealth;
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

        if (currentHealth <= 0)
        {
            if (bossRoot != null)
                bossRoot.OnDeath();
        }
    }
}