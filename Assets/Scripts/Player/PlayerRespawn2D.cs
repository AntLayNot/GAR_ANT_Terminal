using System.Collections;
using UnityEngine;

public class PlayerRespawn2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health2D health;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Collider2D[] collidersToDisableOnDeath;
    [SerializeField] private Behaviour[] behavioursToDisableOnDeath;
    [SerializeField] private SpriteRenderer[] renderersToHideOnDeath;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 1f;
    [SerializeField] private int healthOnRespawn = 5;
    [SerializeField] private bool fullHealOnRespawn = true;

    [Header("Checkpoint")]
    [SerializeField] private Transform initialSpawnPoint;

    private Vector3 currentCheckpointPosition;
    private bool hasCheckpoint = false;
    private bool isRespawning = false;

    private void Reset()
    {
        health = GetComponent<Health2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Awake()
    {
        if (health == null)
            health = GetComponent<Health2D>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (initialSpawnPoint != null)
        {
            currentCheckpointPosition = initialSpawnPoint.position;
            hasCheckpoint = true;
        }
        else
        {
            currentCheckpointPosition = transform.position;
            hasCheckpoint = true;
        }
    }

    private void OnEnable()
    {
        if (health != null)
            health.onDeath.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        if (health != null)
            health.onDeath.RemoveListener(HandleDeath);
    }

    public void SetCheckpoint(Vector3 newCheckpointPosition)
    {
        currentCheckpointPosition = newCheckpointPosition;
        hasCheckpoint = true;

        Debug.Log("[Respawn] Nouveau checkpoint : " + currentCheckpointPosition);
    }

    private void HandleDeath()
    {
        if (isRespawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        SetPlayerActiveState(false);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        yield return new WaitForSeconds(respawnDelay);

        if (hasCheckpoint)
            transform.position = currentCheckpointPosition;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (health != null)
        {
            if (fullHealOnRespawn)
                health.Revive(health.maxHP);
            else
                health.Revive(healthOnRespawn);
        }

        SetPlayerActiveState(true);

        isRespawning = false;
    }

    private void SetPlayerActiveState(bool active)
    {
        if (collidersToDisableOnDeath != null)
        {
            for (int i = 0; i < collidersToDisableOnDeath.Length; i++)
            {
                if (collidersToDisableOnDeath[i] != null)
                    collidersToDisableOnDeath[i].enabled = active;
            }
        }

        if (behavioursToDisableOnDeath != null)
        {
            for (int i = 0; i < behavioursToDisableOnDeath.Length; i++)
            {
                if (behavioursToDisableOnDeath[i] != null)
                    behavioursToDisableOnDeath[i].enabled = active;
            }
        }

        if (renderersToHideOnDeath != null)
        {
            for (int i = 0; i < renderersToHideOnDeath.Length; i++)
            {
                if (renderersToHideOnDeath[i] != null)
                    renderersToHideOnDeath[i].enabled = active;
            }
        }
    }
}