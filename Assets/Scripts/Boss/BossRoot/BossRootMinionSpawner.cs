using UnityEngine;

public class BossRootMinionSpawner : MonoBehaviour
{
    [System.Serializable]
    public class MinionEntry
    {
        public GameObject prefab;
        public int amount = 1;
    }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Phase 1")]
    [SerializeField] private MinionEntry[] phase1Minions;

    [Header("Phase 2")]
    [SerializeField] private MinionEntry[] phase2Minions;

    [Header("Phase 3")]
    [SerializeField] private MinionEntry[] phase3Minions;

    public void SpawnWave(BossRoot.BossPhase phase)
    {
        MinionEntry[] entries = GetEntries(phase);
        if (entries == null || entries.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        foreach (var entry in entries)
        {
            if (entry == null || entry.prefab == null) continue;

            for (int i = 0; i < entry.amount; i++)
            {
                Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                Instantiate(entry.prefab, point.position, Quaternion.identity);
            }
        }
    }

    private MinionEntry[] GetEntries(BossRoot.BossPhase phase)
    {
        switch (phase)
        {
            case BossRoot.BossPhase.Phase1: return phase1Minions;
            case BossRoot.BossPhase.Phase2: return phase2Minions;
            case BossRoot.BossPhase.Phase3: return phase3Minions;
        }

        return null;
    }
}