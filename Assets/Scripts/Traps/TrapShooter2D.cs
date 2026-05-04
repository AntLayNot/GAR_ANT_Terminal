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

    [Header("Gizmos")]
    [SerializeField] private bool drawRangeGizmo = true;
    [SerializeField] private float gizmoLineLength = 2f;

    private Coroutine shootRoutine;

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