using System.Collections;
using UnityEngine;

public class TrapShooter2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject projectilePrefab;

    [Header("Shooting")]
    [SerializeField] private float shootInterval = 2f;
    [SerializeField] private bool startShootingOnStart = true;

    [Header("Direction")]
    [SerializeField] private bool useFirePointRightDirection = true;
    [SerializeField] private Vector2 fixedDirection = Vector2.right;

    [Header("Audio")]
    [SerializeField] private AudioSource shootAudioSource;
    [SerializeField] private AudioClip shootClip;
    [SerializeField, Range(0f, 1f)] private float shootVolume = 1f;

    [Header("Gizmos")]
    [SerializeField] private bool drawRangeGizmo = true;
    [SerializeField] private float gizmoLineLength = 2f;

    private Coroutine shootRoutine;

    private void Awake()
    {
        if (shootAudioSource == null)
            shootAudioSource = FindFirstObjectByType<AudioSource>();
    }

    private void Start()
    {
        if (startShootingOnStart)
            StartShooting();
    }

    public void StartShooting()
    {
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);

        shootRoutine = StartCoroutine(ShootRoutine());
    }

    public void StopShooting()
    {
        if (shootRoutine != null)
            StopCoroutine(shootRoutine);

        shootRoutine = null;
    }

    private IEnumerator ShootRoutine()
    {
        while (true)
        {
            Shoot();
            yield return new WaitForSeconds(shootInterval);
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        PlayShootSound();

        GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);

        TrapProjectile2D trapProjectile = projectile.GetComponent<TrapProjectile2D>();

        if (trapProjectile != null)
        {
            Vector2 direction = useFirePointRightDirection
                ? (Vector2)firePoint.right
                : fixedDirection.normalized;

            trapProjectile.SetDirection(direction);
        }
    }

    private void PlayShootSound()
    {
        if (shootAudioSource == null)
            return;

        AudioClip clipToPlay = shootClip;

        if (clipToPlay == null)
            clipToPlay = shootAudioSource.clip;

        if (clipToPlay == null)
            return;

        shootAudioSource.PlayOneShot(clipToPlay, shootVolume);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawRangeGizmo || firePoint == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(firePoint.position, 0.08f);

        Vector2 direction = useFirePointRightDirection
            ? (Vector2)firePoint.right
            : fixedDirection.normalized;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(firePoint.position, firePoint.position + (Vector3)(direction * gizmoLineLength));
    }
}