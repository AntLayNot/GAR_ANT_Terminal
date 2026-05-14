using System.Collections.Generic;
using UnityEngine;

public class NodeAllyPowerDrain2D : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask allyLayer;
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private bool includeSelf = false;

    [Header("Power Settings")]
    [SerializeField] private int maxAlliesCounted = 5;
    [SerializeField] private float powerPerAlly = 0.15f;

    [Header("Bonus Limits")]
    [SerializeField] private float maxPowerMultiplier = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool drawGizmos = true;

    private readonly Collider2D[] results = new Collider2D[32];
    private readonly List<GameObject> detectedAllies = new List<GameObject>();

    private int currentAllyCount;
    private float currentPowerMultiplier = 1f;

    public int CurrentAllyCount => currentAllyCount;
    public float CurrentPowerMultiplier => currentPowerMultiplier;

    private void Update()
    {
        RefreshPower();
    }

    private void RefreshPower()
    {
        detectedAllies.Clear();

        int hitCount = Physics2D.OverlapCircleNonAlloc(
            transform.position,
            detectionRadius,
            results,
            allyLayer
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = results[i];

            if (hit == null)
                continue;

            GameObject ally = hit.gameObject;

            if (!includeSelf && ally == gameObject)
                continue;

            if (!includeSelf && ally.transform.IsChildOf(transform))
                continue;

            if (detectedAllies.Contains(ally))
                continue;

            detectedAllies.Add(ally);
        }

        currentAllyCount = Mathf.Min(detectedAllies.Count, maxAlliesCounted);

        currentPowerMultiplier = 1f + currentAllyCount * powerPerAlly;
        currentPowerMultiplier = Mathf.Min(currentPowerMultiplier, maxPowerMultiplier);

        if (debugLogs)
        {
            Debug.Log(
                $"[NodeAllyPowerDrain2D] Alliés proches : {currentAllyCount} | Multiplicateur : x{currentPowerMultiplier}"
            );
        }
    }

    public int GetBoostedDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * currentPowerMultiplier);
    }

    public float GetBoostedFloat(float baseValue)
    {
        return baseValue * currentPowerMultiplier;
    }

    public float GetReducedCooldown(float baseCooldown)
    {
        return baseCooldown / currentPowerMultiplier;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}