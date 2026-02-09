using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldCommandActions : MonoBehaviour
{
    [System.Serializable]
    public struct Spawnable
    {
        public string id;            // ex: "wall", "projectile"
        public GameObject prefab;
        public float lifetime;       // 0 = infini
        public bool spawnInFront;    // devant la target
        public float frontDistance;  // distance devant
        public Vector2 offset;       // offset world
    }

    [Header("Spawnables")]
    public List<Spawnable> spawnables = new();

    private Dictionary<string, Spawnable> spawnById;
    public TargetObject LastSpawned { get; private set; }


    void Awake()
    {
        BuildSpawnCache();
    }

    public void RebuildSpawnCache() => BuildSpawnCache();

    void BuildSpawnCache()
    {
        spawnById = new Dictionary<string, Spawnable>(System.StringComparer.OrdinalIgnoreCase);

        foreach (var s in spawnables)
        {
            if (string.IsNullOrWhiteSpace(s.id) || s.prefab == null) continue;
            spawnById[s.id.Trim()] = s;
        }
    }

    public IReadOnlyList<string> GetSpawnIds()
    {
        if (spawnables == null) return System.Array.Empty<string>();
        return spawnables
            .Where(s => !string.IsNullOrWhiteSpace(s.id))
            .Select(s => s.id.Trim())
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => s)
            .ToList();
    }


    // -----------------------------
    // Actions existantes
    // -----------------------------
    public void Ping(TargetObject t)
    {
        Debug.Log($"PING -> {t.Name}");
    }

    public void ToggleActive(TargetObject t)
    {
        t.gameObject.SetActive(!t.gameObject.activeSelf);
    }

    public void DestroyTarget(TargetObject t)
    {
        Destroy(t.gameObject);
    }

    public bool SpawnById(string id, TargetObject origin)
    {
        if (origin == null) return false;
        if (string.IsNullOrWhiteSpace(id)) return false;

        if (spawnById == null || spawnById.Count == 0)
            BuildSpawnCache();

        id = id.Trim();

        if (!spawnById.TryGetValue(id, out var s) || s.prefab == null)
            return false;

        Vector3 pos = origin.transform.position;

        // direction "devant" en sidescroller (droite/gauche)
        Vector3 dir = Vector3.right;

        // priorité: SpriteRenderer.flipX si dispo (child compris), sinon scale.x
        var sr = origin.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.flipX) dir = Vector3.left;
        else if (origin.transform.lossyScale.x < 0f) dir = Vector3.left;

        if (s.spawnInFront)
            pos += dir * Mathf.Max(0f, s.frontDistance);

        pos += (Vector3)s.offset;

        GameObject go = Instantiate(s.prefab, pos, Quaternion.identity);

        //1) Garantir un TargetObject sur l'objet spawné
        var t = go.GetComponent<TargetObject>();
        if (t == null) t = go.AddComponent<TargetObject>();

        //2) Donne un nom ciblable
        t.SetName(id); // ex: "wall", "projectile"

        //3) Mémoriser le dernier spawné (pour un selector "last" plus tard)
        LastSpawned = t;

        //4) Initialiser projectile si présent
        var proj = go.GetComponent<Projectile2D>();
        if (proj != null)
        {
            Vector2 dir2 = (dir.x < 0f) ? Vector2.left : Vector2.right;
            proj.Init(dir2);
        }

        if (s.lifetime > 0f)
            StartCoroutine(DestroyAfter(go, s.lifetime));

        //5) Si tu as un cache de targets local, rebuild ici (voir plus bas)
        // BuildCaches(); // optionnel si tu veux le refresh immédiat

        return true;
    }


    IEnumerator DestroyAfter(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) Destroy(go);
    }
}
