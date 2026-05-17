using System.Collections;
using UnityEngine;

public class CommandProjectileRain2D : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int projectileCount = 12;
    [SerializeField] private float damage = 10f;

    [Header("Zone de pluie")]
    [SerializeField] private float width = 6f;
    [SerializeField] private float spawnHeight = 5f;

    [Header("Timing")]
    [SerializeField] private float duration = 2f;
    [SerializeField] private float projectileLifetime = 4f;

    [Header("Mouvement")]
    [SerializeField] private float fallSpeed = 8f;
    [SerializeField] private bool randomHorizontalAngle = true;
    [SerializeField] private float horizontalAngleStrength = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource rainAudioSource;

    [Tooltip("Son joué une seule fois au début de la pluie.")]
    [SerializeField] private AudioClip rainStartClip;

    [Tooltip("Son optionnel joué à chaque projectile spawn.")]
    [SerializeField] private AudioClip projectileSpawnClip;

    [SerializeField, Range(0f, 1f)] private float rainStartVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float projectileSpawnVolume = 0.5f;

    [Header("Fin")]
    [SerializeField] private bool destroyAfterRain = true;

    private void Awake()
    {
        if (rainAudioSource == null)
            rainAudioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        StartCoroutine(RainRoutine());
    }

    private IEnumerator RainRoutine()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[CommandProjectileRain2D] Aucun projectilePrefab assigné.");
            yield break;
        }

        PlaySound(rainStartClip, rainStartVolume);

        float interval = duration / Mathf.Max(1, projectileCount);

        for (int i = 0; i < projectileCount; i++)
        {
            SpawnProjectile();
            yield return new WaitForSeconds(interval);
        }

        if (destroyAfterRain)
        {
            float delay = 0f;

            if (rainAudioSource != null && rainAudioSource.isPlaying && rainAudioSource.clip != null)
                delay = rainAudioSource.clip.length;

            Destroy(gameObject, delay);
        }
    }

    private void SpawnProjectile()
    {
        float randomX = Random.Range(-width * 0.5f, width * 0.5f);

        Vector3 spawnPosition = transform.position + new Vector3(
            randomX,
            spawnHeight,
            0f
        );

        PlaySound(projectileSpawnClip, projectileSpawnVolume);

        GameObject projectile = Instantiate(
            projectilePrefab,
            spawnPosition,
            Quaternion.identity
        );

        RainProjectile2D rainProjectile = projectile.GetComponent<RainProjectile2D>();

        if (rainProjectile != null)
        {
            Vector2 direction = Vector2.down;

            if (randomHorizontalAngle)
            {
                direction.x = Random.Range(-horizontalAngleStrength, horizontalAngleStrength);
                direction.Normalize();
            }

            rainProjectile.Init(direction, fallSpeed, damage, projectileLifetime);
        }
        else
        {
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

            if (rb != null)
                rb.linearVelocity = Vector2.down * fallSpeed;

            Destroy(projectile, projectileLifetime);
        }
    }

    private void PlaySound(AudioClip clip, float volume)
    {
        if (rainAudioSource == null)
            return;

        AudioClip clipToPlay = clip;

        if (clipToPlay == null)
            return;

        rainAudioSource.PlayOneShot(clipToPlay, volume);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Vector3 center = transform.position + Vector3.up * spawnHeight;
        Vector3 size = new Vector3(width, 0.2f, 0f);

        Gizmos.DrawWireCube(center, size);
    }
}