using UnityEngine;

[RequireComponent(typeof(Health2D))]
public class EnemyKillProgressionReporter2D : MonoBehaviour
{
    [SerializeField] private PlayerCommandProgression2D playerProgression;

    private Health2D health;
    private bool alreadyReported;

    private void Awake()
    {
        health = GetComponent<Health2D>();

        if (playerProgression == null)
            playerProgression = FindFirstObjectByType<PlayerCommandProgression2D>();
    }

    private void OnEnable()
    {
        if (health != null)
            health.onDeath.AddListener(ReportKill);
    }

    private void OnDisable()
    {
        if (health != null)
            health.onDeath.RemoveListener(ReportKill);
    }

    private void ReportKill()
    {
        if (alreadyReported)
            return;

        alreadyReported = true;

        if (playerProgression != null)
            playerProgression.RegisterEnemyKill();
    }
}