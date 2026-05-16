using UnityEngine;
using UnityEngine.Events;

public class PlayerCommandProgression2D : MonoBehaviour
{
    public static PlayerCommandProgression2D Current { get; private set; }

    [Header("Progression")]
    [SerializeField] private int enemyKills;
    [SerializeField] private int doorsToggled;

    [Header("Projectile")]
    [SerializeField] private int projectileCount = 1;
    [SerializeField] private int maxProjectileCount = 2;

    [SerializeField] private int projectileDamageBonus = 0;
    [SerializeField] private int maxProjectileDamageBonus = 2;

    [Header("Vie")]
    [SerializeField] private Health2D playerHealth;
    [SerializeField] private int maxHealthBonusGiven = 0;
    [SerializeField] private int maxHealthBonusLimit = 2;

    [Header("Commandes spéciales")]
    [SerializeField] private bool rainUnlocked = false;

    private bool unlockedDoubleProjectile;
    private bool unlockedDamageBonus1;
    private bool unlockedDamageBonus2;
    private bool unlockedHealthBonus1;
    private bool unlockedRain;

    [Header("Events")]
    public UnityEvent onProgressionChanged;

    private void Awake()
    {
        Current = this;

        if (playerHealth == null)
            playerHealth = GetComponent<Health2D>();
    }

    public void RegisterEnemyKill()
    {
        enemyKills++;

        Debug.Log("[Progression] Ennemi tué : " + enemyKills);

        CheckUnlocks();
        onProgressionChanged?.Invoke();
    }

    public void RegisterDoorToggled()
    {
        doorsToggled++;

        Debug.Log("[Progression] Door toggled : " + doorsToggled);

        CheckUnlocks();
        onProgressionChanged?.Invoke();
    }

    private void CheckUnlocks()
    {
        if (!unlockedDoubleProjectile && enemyKills >= 3)
        {
            unlockedDoubleProjectile = true;
            projectileCount = Mathf.Min(2, maxProjectileCount);

            Debug.Log("[Progression] Débloqué : projectile x2");
        }

        if (!unlockedDamageBonus1 && enemyKills >= 6)
        {
            unlockedDamageBonus1 = true;
            AddProjectileDamageBonus(1);

            Debug.Log("[Progression] Débloqué : +1 dégât projectile");
        }

        if (!unlockedHealthBonus1 && doorsToggled >= 3)
        {
            unlockedHealthBonus1 = true;
            AddMaxHealthBonus(10);

            Debug.Log("[Progression] Débloqué : +10 vie max grâce aux Doors Toggle");
        }

        if (!unlockedRain && enemyKills >= 8)
        {
            unlockedRain = true;
            rainUnlocked = true;

            Debug.Log("[Progression] Débloqué : commande rain");
        }

        if (!unlockedDamageBonus2 && enemyKills >= 13)
        {
            unlockedDamageBonus2 = true;
            AddProjectileDamageBonus(1);

            Debug.Log("[Progression] Débloqué : +1 dégât projectile supplémentaire");
        }
    }

    private void AddProjectileDamageBonus(int amount)
    {
        projectileDamageBonus = Mathf.Clamp(
            projectileDamageBonus + amount,
            0,
            maxProjectileDamageBonus
        );
    }

    private void AddMaxHealthBonus(int amount)
    {
        if (playerHealth == null)
            return;

        int allowedAmount = Mathf.Min(
            amount,
            maxHealthBonusLimit - maxHealthBonusGiven
        );

        if (allowedAmount <= 0)
            return;

        maxHealthBonusGiven += allowedAmount;

        playerHealth.maxHP += allowedAmount;
        playerHealth.currentHP += allowedAmount;

        playerHealth.currentHP = Mathf.Clamp(
            playerHealth.currentHP,
            0,
            playerHealth.maxHP
        );

        playerHealth.onHPChanged?.Invoke(
            playerHealth.currentHP,
            playerHealth.maxHP
        );
    }

    public int GetProjectileCount()
    {
        return Mathf.Max(1, projectileCount);
    }

    public int GetProjectileDamageBonus()
    {
        return Mathf.Clamp(projectileDamageBonus, 0, maxProjectileDamageBonus);
    }

    public bool IsCommandUnlocked(string commandId)
    {
        if (string.IsNullOrWhiteSpace(commandId))
            return false;

        commandId = commandId.Trim().ToLower();

        if (commandId == "rain")
            return rainUnlocked;

        return true;
    }

    public int GetEnemyKills()
    {
        return enemyKills;
    }

    public int GetDoorsToggled()
    {
        return doorsToggled;
    }

    public bool IsRainUnlocked()
    {
        return rainUnlocked;
    }

    public int GetMaxHealthBonusGiven()
    {
        return maxHealthBonusGiven;
    }
}