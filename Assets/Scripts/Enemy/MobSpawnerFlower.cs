using System.Collections.Generic;
using UnityEngine;

public class SpawnerFlower2D : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject[] mobPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Activation")]
    [SerializeField] private Transform player;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float activationRadius = 6f;
    [SerializeField] private bool usePlayerLayerDetection = true;

    [Header("Timing")]
    [SerializeField] private float spawnInterval = 4f;
    [SerializeField] private bool spawnImmediatelyOnActivation = true;

    [Header("Limit")]
    [SerializeField] private int maxAliveMobs = 4;

    [Header("Random")]
    [SerializeField] private bool randomMob = true;
    [SerializeField] private bool randomSpawnPoint = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] spawnSounds;
    [SerializeField] private float spawnSoundVolume = 1f;
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color activationRadiusColor = Color.yellow;
    [SerializeField] private Color spawnPointColor = Color.cyan;

    private readonly List<GameObject> aliveMobs = new List<GameObject>();

    private float timer;
    private bool isPlayerInRange;
    private bool hasActivatedOnce;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        timer = spawnInterval;
    }

    private void Update()
    {
        CleanDeadMobs();
        CheckPlayerInRange();

        if (!isPlayerInRange)
            return;

        if (aliveMobs.Count >= maxAliveMobs)
            return;

        if (spawnImmediatelyOnActivation && !hasActivatedOnce)
        {
            TrySpawnMob();
            hasActivatedOnce = true;
            timer = spawnInterval;
            return;
        }

        hasActivatedOnce = true;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            TrySpawnMob();
            timer = spawnInterval;
        }
    }

    private void CheckPlayerInRange()
    {
        if (usePlayerLayerDetection)
        {
            Collider2D playerHit = Physics2D.OverlapCircle(
                transform.position,
                activationRadius,
                playerLayer
            );

            isPlayerInRange = playerHit != null;
            return;
        }

        if (player == null)
        {
            isPlayerInRange = false;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        isPlayerInRange = distance <= activationRadius;
    }

    private void TrySpawnMob()
    {
        if (mobPrefabs == null || mobPrefabs.Length == 0)
        {
            Debug.LogWarning($"{name} : aucun mob prefab assigné.");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning($"{name} : aucun spawn point assigné.");
            return;
        }

        if (aliveMobs.Count >= maxAliveMobs)
            return;

        GameObject prefab = GetMobPrefab();
        Transform spawnPoint = GetSpawnPoint();

        if (prefab == null || spawnPoint == null)
            return;

        GameObject newMob = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        aliveMobs.Add(newMob);

        PlaySpawnSound();
    }

    private GameObject GetMobPrefab()
    {
        if (!randomMob)
            return mobPrefabs[0];

        int index = Random.Range(0, mobPrefabs.Length);
        return mobPrefabs[index];
    }

    private Transform GetSpawnPoint()
    {
        if (!randomSpawnPoint)
            return spawnPoints[0];

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    private void PlaySpawnSound()
    {
        if (audioSource == null)
            return;

        if (spawnSounds == null || spawnSounds.Length == 0)
            return;

        AudioClip clip = spawnSounds[Random.Range(0, spawnSounds.Length)];

        if (clip == null)
            return;

        audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip, spawnSoundVolume);
    }

    private void CleanDeadMobs()
    {
        for (int i = aliveMobs.Count - 1; i >= 0; i--)
        {
            if (aliveMobs[i] == null)
            {
                aliveMobs.RemoveAt(i);
                continue;
            }

            Health2D health = aliveMobs[i].GetComponent<Health2D>();

            if (health == null)
                health = aliveMobs[i].GetComponentInChildren<Health2D>();

            if (health != null && health.IsDead)
            {
                aliveMobs.RemoveAt(i);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = activationRadiusColor;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        if (spawnPoints == null)
            return;

        Gizmos.color = spawnPointColor;

        foreach (Transform point in spawnPoints)
        {
            if (point == null)
                continue;

            Gizmos.DrawWireSphere(point.position, 0.25f);
            Gizmos.DrawLine(transform.position, point.position);
        }
    }
}