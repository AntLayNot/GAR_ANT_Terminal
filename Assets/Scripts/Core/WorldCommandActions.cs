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

    public GameObject pingTextPrefab;


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
        if (t == null) return;

        Debug.Log($"PING -> {t.Name}");

        // 🔶 OUTLINE
        var outline = t.GetComponent<TargetOutline2D>();
        if (outline != null)
        {
            outline.isGlow = true;
            StartCoroutine(DisableOutline(outline, 2f));
        }

        // 🔷 TEXTE
        if (pingTextPrefab != null)
        {
            Vector3 offset = Vector3.up * 1.5f;
            GameObject go = Instantiate(pingTextPrefab, t.transform.position + offset, Quaternion.identity);

            var txt = go.GetComponent<TMPro.TextMeshPro>();
            if (txt != null)
                txt.text = t.Name;

            StartCoroutine(PingTextRoutine(go, t));
        }
    }

    IEnumerator PingTextRoutine(GameObject go, TargetObject target)
    {
        float duration = 2f;
        float t = 0f;

        while (t < duration && go != null && target != null)
        {
            go.transform.position = target.transform.position + Vector3.up * 1.5f;

            // Optionnel : toujours face caméra
            if (Camera.main != null)
                go.transform.forward = Camera.main.transform.forward;

            t += Time.deltaTime;
            yield return null;
        }

        if (go != null)
            Destroy(go);
    }

    IEnumerator DisableOutline(TargetOutline2D outline, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (outline != null) outline.isGlow = false;
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

        // direction "devant" (droite/gauche)
        Vector3 dir = Vector3.right;

        // On récupère la direction réelle du joueur si possible
        var playerMovement = origin.GetComponentInParent<PlayerPlatformerController2D>();

        if (playerMovement != null)
        {
            dir = playerMovement.GetFacingSign() < 0f ? Vector3.left : Vector3.right;
        }
        else
        {
            // fallback pour les objets non-joueurs
            if (origin.transform.lossyScale.x < 0f)
                dir = Vector3.left;
        }

        if (s.spawnInFront)
            pos += dir * Mathf.Max(0f, s.frontDistance);

        pos += (Vector3)s.offset;

        // -------------------------------------------------
        // CAS SPECIAL : projectile
        // -------------------------------------------------
        bool isProjectilePrefab =
            s.prefab.GetComponent<Projectile2D>() != null ||
            s.prefab.GetComponentInChildren<Projectile2D>() != null;

        if (isProjectilePrefab)
        {
            PlayerCommandAnimator playerAnimator = origin.GetComponentInParent<PlayerCommandAnimator>();

            if (playerAnimator == null)
                playerAnimator = origin.GetComponentInChildren<PlayerCommandAnimator>();

            if (playerAnimator != null)
            {
                Vector2 dir2 = dir.x < 0f ? Vector2.left : Vector2.right;

                // On ne spawn PAS maintenant.
                // On stocke la demande dans le player, puis l'animation fera le spawn.
                playerAnimator.RequestProjectileSpawn(s.prefab, pos, dir2, id, s.lifetime);

                return true;
            }

            Debug.LogWarning("[WorldCommandActions] Projectile détecté, mais aucun PlayerCommandAnimator trouvé depuis : " + origin.name);
        }

        // -------------------------------------------------
        // CAS NORMAL : objet classique
        // -------------------------------------------------
        GameObject go = Instantiate(s.prefab, pos, Quaternion.identity);

        //1) Garantir un TargetObject sur l'objet spawné
        var t = go.GetComponent<TargetObject>();
        if (t == null) t = go.AddComponent<TargetObject>();

        //2) Donne un nom ciblable
        t.SetName(id);

        //3) Mémoriser le dernier spawné
        LastSpawned = t;

        if (s.lifetime > 0f)
            StartCoroutine(DestroyAfter(go, s.lifetime));

        return true;
    }

    IEnumerator DestroyAfter(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) Destroy(go);
    }
}
