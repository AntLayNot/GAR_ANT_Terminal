using UnityEngine;

public class MobHealerFlower : MonoBehaviour
{
    [Header("Heal")]
    [SerializeField] private int healAmount = 1;
    [SerializeField] private float healInterval = 1f;
    [SerializeField] private float healRadius = 4f;

    [Header("Targets")]
    [SerializeField] private LayerMask mobLayer;
    [SerializeField] private bool canHealSelf = false;
    [SerializeField] private bool onlyHealDamagedMobs = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color healRadiusColor = Color.green;

    private float timer;

    private void Start()
    {
        timer = healInterval;
    }

    private void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            HealMobsInRadius();
            timer = healInterval;
        }
    }

    private void HealMobsInRadius()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            healRadius,
            mobLayer
        );

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            if (!canHealSelf && hit.gameObject == gameObject)
                continue;

            Health2D health = hit.GetComponent<Health2D>();

            if (health == null)
                health = hit.GetComponentInParent<Health2D>();

            if (health == null)
                continue;

            if (health.IsDead)
                continue;

            if (onlyHealDamagedMobs && health.currentHP >= health.maxHP)
                continue;

            health.Heal(healAmount);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = healRadiusColor;
        Gizmos.DrawWireSphere(transform.position, healRadius);
    }
}