using System.Collections;
using UnityEngine;

public class BossArenaController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Health2D playerHealth;
    [SerializeField] private Transform arenaSpawnPoint;
    [SerializeField] private GameObject arenaContentPrefab;

    [Header("Arena Doors / Walls")]
    [SerializeField] private GameObject[] objectsToEnableDuringFight;
    [SerializeField] private GameObject[] objectsToDisableDuringFight;

    [Header("Options")]
    [SerializeField] private bool spawnArenaOnStart = false;
    [SerializeField] private bool resetArenaOnPlayerDeath = true;
    [SerializeField] private bool respawnArenaImmediatelyAfterReset = false;

    [Header("Death Reset Delay")]
    [SerializeField] private float arenaResetDelayAfterPlayerDeath = 1.5f;

    private GameObject currentArenaInstance;
    private bool fightActive = false;
    private Coroutine resetRoutine;

    private void Awake()
    {
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<Health2D>();
                if (playerHealth == null)
                    playerHealth = player.GetComponentInParent<Health2D>();
            }
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
            playerHealth.onDeath.AddListener(OnPlayerDeath);
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.onDeath.RemoveListener(OnPlayerDeath);
    }

    private void Start()
    {
        if (spawnArenaOnStart)
            SpawnFreshArena();

        SetFightState(false);
    }

    public void StartFight()
    {
        if (fightActive)
            return;

        if (currentArenaInstance == null)
            SpawnFreshArena();

        SetFightState(true);
    }

    public void EndFight()
    {
        SetFightState(false);
    }

    public void ResetArena()
    {
        if (currentArenaInstance != null)
            Destroy(currentArenaInstance);

        currentArenaInstance = null;
        fightActive = false;

        SetFightState(false);
    }

    public void SpawnFreshArena()
    {
        if (arenaContentPrefab == null)
        {
            Debug.LogWarning("[BossArenaController] arenaContentPrefab manquant.");
            return;
        }

        if (currentArenaInstance != null)
            Destroy(currentArenaInstance);

        Vector3 spawnPos = arenaSpawnPoint != null ? arenaSpawnPoint.position : transform.position;
        Quaternion spawnRot = arenaSpawnPoint != null ? arenaSpawnPoint.rotation : Quaternion.identity;

        currentArenaInstance = Instantiate(arenaContentPrefab, spawnPos, spawnRot);
    }

    private void OnPlayerDeath()
    {
        if (!resetArenaOnPlayerDeath)
            return;

        if (resetRoutine != null)
            StopCoroutine(resetRoutine);

        resetRoutine = StartCoroutine(ResetArenaAfterDeathRoutine());
    }

    private IEnumerator ResetArenaAfterDeathRoutine()
    {
        yield return new WaitForSeconds(arenaResetDelayAfterPlayerDeath);

        ResetArena();

        if (respawnArenaImmediatelyAfterReset)
            SpawnFreshArena();

        resetRoutine = null;
    }

    private void SetFightState(bool active)
    {
        fightActive = active;

        for (int i = 0; i < objectsToEnableDuringFight.Length; i++)
        {
            if (objectsToEnableDuringFight[i] != null)
                objectsToEnableDuringFight[i].SetActive(active);
        }

        for (int i = 0; i < objectsToDisableDuringFight.Length; i++)
        {
            if (objectsToDisableDuringFight[i] != null)
                objectsToDisableDuringFight[i].SetActive(!active);
        }
    }

    public bool IsFightActive()
    {
        return fightActive;
    }
}