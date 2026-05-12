using UnityEngine;

public class BossRootMinionSpawner : MonoBehaviour
{
    [System.Serializable]
    public class MinionEntry
    {
        public GameObject prefab;
        [Min(1)] public int amount = 1;
        public Vector2 randomOffsetMin = new Vector2(-0.3f, 0f);
        public Vector2 randomOffsetMax = new Vector2(0.3f, 0.2f);
    }

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Phase 1")]
    [SerializeField] private MinionEntry[] phase1Minions;

    [Header("Phase 2")]
    [SerializeField] private MinionEntry[] phase2Minions;

    [Header("Phase 3")]
    [SerializeField] private MinionEntry[] phase3Minions;

    [Header("Options")]
    [SerializeField] private bool avoidUsingSamePointTwiceInRow = true;

    private int lastSpawnPointIndex = -1;

    public void SpawnWave(BossRoot.BossPhase phase)
    {
        MinionEntry[] entries = GetEntries(phase);
        if (entries == null || entries.Length == 0) return;
        if (spawnPoints == null || spawnPoints.Length == 0) return;

        foreach (var entry in entries)
        {
            if (entry == null || entry.prefab == null) continue;

            for (int i = 0; i < entry.amount; i++)
            {
                Transform point = GetSpawnPoint();
                if (point == null) continue;

                Vector2 offset = new Vector2(
                    Random.Range(entry.randomOffsetMin.x, entry.randomOffsetMax.x),
                    Random.Range(entry.randomOffsetMin.y, entry.randomOffsetMax.y)
                );

                Vector3 spawnPos = point.position + (Vector3)offset;
                Instantiate(entry.prefab, spawnPos, Quaternion.identity);
            }
        }
    }

    private Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return null;

        if (spawnPoints.Length == 1)
            return spawnPoints[0];

        int index = Random.Range(0, spawnPoints.Length);

        if (avoidUsingSamePointTwiceInRow && spawnPoints.Length > 1)
        {
            int safety = 8;
            while (index == lastSpawnPointIndex && safety-- > 0)
                index = Random.Range(0, spawnPoints.Length);
        }

        lastSpawnPointIndex = index;
        return spawnPoints[index];
    }

    private MinionEntry[] GetEntries(BossRoot.BossPhase phase)
    {
        switch (phase)
        {
            case BossRoot.BossPhase.Phase1: return phase1Minions;
            case BossRoot.BossPhase.Phase2: return phase2Minions;
            case BossRoot.BossPhase.Phase3: return phase3Minions;
            default: return null;
        }
    }
}