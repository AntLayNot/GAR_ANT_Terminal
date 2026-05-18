
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

    public void Ping(TargetObject t)
    {
        if (t == null) return;

        Debug.Log($"PING -> {t.Name}");

        // OUTLINE
        var outline = t.GetComponent<TargetOutline2D>();
        if (outline != null)
        {
            outline.isGlow = true;
            StartCoroutine(DisableOutline(outline, 2f));
        }

        // TEXTE
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

            // Toujours face caméra
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
        if (t == null)
            return;

        CommandDoorObjective2D doorObjective = t.GetComponent<CommandDoorObjective2D>();

        if (doorObjective == null)
            doorObjective = t.GetComponentInParent<CommandDoorObjective2D>();

        if (doorObjective == null)
            doorObjective = t.GetComponentInChildren<CommandDoorObjective2D>();

        t.gameObject.SetActive(!t.gameObject.activeSelf);

        if (doorObjective != null)
            doorObjective.RegisterDoorToggle();
    }

    public void DestroyTarget(TargetObject t)
    {
        Destroy(t.gameObject);
    }

    public void Menu()
    {
        PauseMenuController pauseMenu = PauseMenuController.Instance;

        if (pauseMenu == null)
            pauseMenu = FindFirstObjectByType<PauseMenuController>();

        if (pauseMenu == null)
        {
            Debug.LogWarning("[WorldCommandActions] Aucun PauseMenuController trouvé dans la scène.");
            return;
        }

        pauseMenu.ToggleMenu();
    }

    public bool SpawnById(string id, TargetObject origin)
    {
        if (origin == null) return false;
        if (string.IsNullOrWhiteSpace(id)) return false;

        if (spawnById == null || spawnById.Count == 0)
            BuildSpawnCache();

        id = id.Trim();

        PlayerCommandProgression2D progression = origin.GetComponentInParent<PlayerCommandProgression2D>();

        if (progression == null)
            progression = PlayerCommandProgression2D.Current;

        if (progression != null && !progression.IsCommandUnlocked(id))
        {
            Debug.Log("[WorldCommandActions] Command locked : " + id);
            return false;
        }

        if (!spawnById.TryGetValue(id, out var s) || s.prefab == null)
            return false;

        Vector3 pos = origin.transform.position;

        // direction "devant" droite/gauche
        Vector3 dir = Vector3.right;

        PlayerPlatformerController2D playerMovement = origin.GetComponentInParent<PlayerPlatformerController2D>();

        if (playerMovement == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                playerMovement = playerObject.GetComponentInParent<PlayerPlatformerController2D>();

            if (playerMovement == null && playerObject != null)
                playerMovement = playerObject.GetComponentInChildren<PlayerPlatformerController2D>();
        }

        if (playerMovement != null)
        {
            dir = playerMovement.GetFacingSign() < 0f ? Vector3.left : Vector3.right;
        }
        else
        {
            if (origin.transform.lossyScale.x < 0f)
                dir = Vector3.left;
        }

        if (s.spawnInFront)
            pos += dir * Mathf.Max(0f, s.frontDistance);

        pos += (Vector3)s.offset;

        // CAS SPECIAL : projectile
        bool isProjectilePrefab =
            s.prefab.GetComponent<Projectile2D>() != null ||
            s.prefab.GetComponentInChildren<Projectile2D>() != null;

        if (isProjectilePrefab)
        {
            PlayerCommandAnimator playerAnimator = FindPlayerCommandAnimator(origin);

            if (playerAnimator == null)
            {
                Debug.LogWarning(
                    "[WorldCommandActions] Projectile détecté, mais aucun PlayerCommandAnimator trouvé depuis : "
                    + origin.name
                    + " ni depuis le Player."
                );

                return false;
            }

            Vector2 dir2 = dir.x < 0f ? Vector2.left : Vector2.right;

            int projectileCount = 1;

            if (progression != null)
                projectileCount = progression.GetProjectileCount();

            projectileCount = Mathf.Max(1, projectileCount);

            Debug.Log("[WorldCommandActions] Spawn projectile count : " + projectileCount);

            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 spawnPos = pos;

                if (projectileCount > 1)
                {
                    float spacing = 0.25f;
                    float yOffset = (i - (projectileCount - 1) * 0.5f) * spacing;

                    spawnPos += Vector3.up * yOffset;
                }

                playerAnimator.RequestProjectileSpawn(
                    s.prefab,
                    spawnPos,
                    dir2,
                    id,
                    s.lifetime
                );
            }

            return true;
        }

        // CAS NORMAL : objet classique
        GameObject go = Instantiate(s.prefab, pos, Quaternion.identity);

        var t = go.GetComponent<TargetObject>();
        if (t == null)
            t = go.AddComponent<TargetObject>();

        t.SetName(id);

        LastSpawned = t;

        if (s.lifetime > 0f)
            StartCoroutine(DestroyAfter(go, s.lifetime));

        return true;
    }

    private PlayerCommandAnimator FindPlayerCommandAnimator(TargetObject origin)
    {
        PlayerCommandAnimator playerAnimator = null;

        if (origin != null)
        {
            playerAnimator = origin.GetComponentInParent<PlayerCommandAnimator>();

            if (playerAnimator == null)
                playerAnimator = origin.GetComponentInChildren<PlayerCommandAnimator>();
        }

        if (playerAnimator != null)
            return playerAnimator;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return null;

        playerAnimator = playerObject.GetComponent<PlayerCommandAnimator>();

        if (playerAnimator == null)
            playerAnimator = playerObject.GetComponentInParent<PlayerCommandAnimator>();

        if (playerAnimator == null)
            playerAnimator = playerObject.GetComponentInChildren<PlayerCommandAnimator>();

        return playerAnimator;
    }

    IEnumerator DestroyAfter(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null) Destroy(go);
    }
}
