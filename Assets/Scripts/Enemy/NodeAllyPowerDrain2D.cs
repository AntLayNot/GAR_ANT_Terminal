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

    [Header("Attack Audio")]
    [SerializeField] private AudioSource attackAudioSource;

    [SerializeField] private AudioClip[] attackClips = new AudioClip[4];

    [SerializeField, Range(0f, 1f)] private float attackVolume = 1f;

    [Tooltip("Évite de rejouer deux fois exactement le même son à la suite.")]
    [SerializeField] private bool avoidSameSoundTwice = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool drawGizmos = true;

    private readonly Collider2D[] results = new Collider2D[32];
    private readonly List<GameObject> detectedAllies = new List<GameObject>();

    private int currentAllyCount;
    private float currentPowerMultiplier = 1f;

    private int lastAttackSoundIndex = -1;

    public int CurrentAllyCount => currentAllyCount;
    public float CurrentPowerMultiplier => currentPowerMultiplier;

    private void Awake()
    {
        if (attackAudioSource == null)
            attackAudioSource = FindFirstObjectByType<AudioSource>();
    }

    private void Update()
    {
        RefreshPower();
    }

    private void RefreshPower()
    {
        detectedAllies.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            detectionRadius,
            allyLayer
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];

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

    public void PlayRandomAttackSound()
    {
        if (attackAudioSource == null)
            return;

        int validCount = GetValidAttackClipCount();

        if (validCount <= 0)
            return;

        int randomIndex = GetRandomAttackClipIndex();

        if (randomIndex < 0 || randomIndex >= attackClips.Length)
            return;

        AudioClip clip = attackClips[randomIndex];

        if (clip == null)
            return;

        lastAttackSoundIndex = randomIndex;
        attackAudioSource.PlayOneShot(clip, attackVolume);
    }

    private int GetValidAttackClipCount()
    {
        int count = 0;

        for (int i = 0; i < attackClips.Length; i++)
        {
            if (attackClips[i] != null)
                count++;
        }

        return count;
    }

    private int GetRandomAttackClipIndex()
    {
        List<int> validIndexes = new List<int>();

        for (int i = 0; i < attackClips.Length; i++)
        {
            if (attackClips[i] == null)
                continue;

            if (avoidSameSoundTwice && attackClips.Length > 1 && i == lastAttackSoundIndex)
                continue;

            validIndexes.Add(i);
        }

        if (validIndexes.Count == 0)
        {
            for (int i = 0; i < attackClips.Length; i++)
            {
                if (attackClips[i] != null)
                    validIndexes.Add(i);
            }
        }

        if (validIndexes.Count == 0)
            return -1;

        return validIndexes[Random.Range(0, validIndexes.Count)];
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.75f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}