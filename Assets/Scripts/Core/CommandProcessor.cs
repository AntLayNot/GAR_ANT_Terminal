using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

public class CommandProcessor : MonoBehaviour
{
    public List<MaStruct> maList;

    [Header("Refs")]
    public PlayerTargeting2D targeting;          // Main Camera (avec PlayerTargeting2D)
    public WorldCommandActions worldActions;     // Objet qui contient WorldCommandActions

    private string targetString, actionString;

    private Dictionary<string, TargetObject> targetsByName;
    private Dictionary<string, UnityEvent<TargetObject>> actionsByKeyword;

    private static readonly string[] TargetSelectors = { "self", "selected", "nearest", "view" };

    [Header("Skills")]
    public SkillBindingManager skillBindings;

    void Start()
    {
        BuildCaches();
    }

    public void RebuildCaches() => BuildCaches();

    void BuildCaches()
    {
        // Targets
        targetsByName = new Dictionary<string, TargetObject>(StringComparer.OrdinalIgnoreCase);
        var allTargets = FindObjectsByType<TargetObject>(FindObjectsSortMode.None);

        foreach (var t in allTargets)
        {
            if (t == null || string.IsNullOrWhiteSpace(t.Name)) continue;
            targetsByName[t.Name.Trim()] = t;
        }

        // Actions
        actionsByKeyword = new Dictionary<string, UnityEvent<TargetObject>>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in maList)
        {
            if (string.IsNullOrWhiteSpace(a.keyWord) || a.action == null) continue;
            actionsByKeyword[a.keyWord.Trim()] = a.action;
        }
    }

    // ------------------------------------------------------------
    // Données utilisées par TerminalUI (autocomplete + help)
    // ------------------------------------------------------------
    public IReadOnlyList<string> GetCommandKeywords()
    {
        if (actionsByKeyword == null) BuildCaches();

        return actionsByKeyword.Keys
            .Concat(new[] { "help", "clear", "spawn", "bind", "unbind", "bindlist" })
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();
    }


    public IReadOnlyList<string> GetTargetNames()
    {
        return TargetRegistry.GetAllNames()
            .Concat(TargetSelectors)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetSpawnIds()
    {
        return worldActions != null ? worldActions.GetSpawnIds() : Array.Empty<string>();
    }



    // ------------------------------------------------------------
    //  Exécute une ligne et RENVOIE une réponse texte
    // ------------------------------------------------------------
    public string ExecuteLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        if (actionsByKeyword == null || targetsByName == null)
            BuildCaches();

        line = line.Trim();

        // Match: mots OU "texte entre guillemets"
        var matches = Regex.Matches(line, "\"([^\"]*)\"|(\\S+)");
        if (matches.Count == 0)
            return null;

        string GetToken(int i)
        {
            var m = matches[i];
            return m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value;
        }

        actionString = GetToken(0);

        // Commande globale : help
        if (string.Equals(actionString, "help", StringComparison.OrdinalIgnoreCase))
            return BuildHelpText();

        // Commande globale : clear
        if (string.Equals(actionString, "clear", StringComparison.OrdinalIgnoreCase))
            return "__CLEAR__";

        // Commande globale : bind
        // Usage: bind <slot> "<commande>"
        if (string.Equals(actionString, "bind", StringComparison.OrdinalIgnoreCase))
        {
            if (skillBindings == null)
                return "ERREUR: SkillBindingManager non assigné (drag dans CommandProcessor.skillBindings).";

            if (matches.Count < 3)
                return "Usage: bind <slot> \"<commande>\"  (ex: bind 1 \"spawn wall self\")";

            string slot = GetToken(1);

            // On récupère tous les tokens après le slot
            var tokens = new List<string>();
            for (int i = 2; i < matches.Count; i++)
                tokens.Add(GetToken(i));

            // Si le dernier token ressemble à un float, on refuse (cooldown interdit)
            if (tokens.Count >= 2 && SkillBindingManager.TryParseFloat(tokens[^1], out _))
                return "Le cooldown n'est pas configurable. Usage: bind <slot> \"<commande>\"";

            string cmd = string.Join(" ", tokens).Trim();
            if (string.IsNullOrWhiteSpace(cmd))
                return "bind: commande invalide.";

            // Validation: on refuse les commandes qui nécessitent une target mais n'en ont pas
            if (RequiresTarget(cmd) && !HasTargetToken(cmd))
                return "Bind refusé: cette commande nécessite une target (ex: self/selected/nearest/view ou un nom).";

            // cooldown géré côté SkillBindingManager (fixe par slot)
            return skillBindings.Bind(slot, cmd);
        }



        // Commande globale : unbind
        // Usage: unbind <slot>
        if (string.Equals(actionString, "unbind", StringComparison.OrdinalIgnoreCase))
        {
            if (skillBindings == null)
                return "ERREUR: SkillBindingManager non assigné (drag dans CommandProcessor.skillBindings).";

            if (matches.Count < 2)
                return "Usage: unbind <slot>  (ex: unbind 1)";

            string slot = GetToken(1);
            return skillBindings.Unbind(slot);
        }

        // Commande globale : bindlist
        if (string.Equals(actionString, "bindlist", StringComparison.OrdinalIgnoreCase))
        {
            if (skillBindings == null)
                return "ERREUR: SkillBindingManager non assigné (drag dans CommandProcessor.skillBindings).";

            return skillBindings.ListBinds();
        }


        // Commande spéciale : spawn <id> <target>
        if (string.Equals(actionString, "spawn", StringComparison.OrdinalIgnoreCase))
        {
            if (matches.Count < 3)
                return "Usage: spawn <id> <target>  (ex: spawn wall player | spawn projectile selected)";

            string spawnId = GetToken(1);
            targetString = GetToken(2);

            return CallSpawnAndReturn(spawnId);
        }

        // Ici : action + target requis
        if (matches.Count < 2)
            return "Format attendu: <action> <target>  (ex: toggle lamp) | tape 'help'";

        targetString = GetToken(1);

        return CallActionAndReturn();
    }

    public void SubmitLine(string line) => _ = ExecuteLine(line);

    // ------------------------------------------------------------
    // RANGE CHECK
    // ------------------------------------------------------------
    bool IsInRange(TargetObject target)
    {
        if (target == null) return false;
        if (targeting == null) return true;
        if (targeting.maxDistance <= 0f) return true;

        Transform originTf = targeting.origin;
        if (originTf == null) return true;

        float max = targeting.maxDistance;

        // Distance collider à collider si possible
        var originCol = originTf.GetComponentInChildren<Collider2D>();
        var targetCol = target.GetComponentInChildren<Collider2D>();

        if (originCol != null && targetCol != null)
        {
            // ColliderDistance2D.distance = 0 si ça touche / overlap
            var d = originCol.Distance(targetCol).distance;
            return d <= max;
        }

        // fallback pivot
        float dp = Vector2.Distance(originTf.position, target.transform.position);
        return dp <= max;
    }

    bool IsSelf(TargetObject t)
    {
        if (t == null || targeting == null || targeting.origin == null)
            return false;

        // Le TargetObject sur le player (origin)
        var self = targeting.origin.GetComponentInChildren<TargetObject>();
        if (self == null) return false;

        return t == self;
    }




    // ------------------------------------------------------------
    // RESOLVE TARGET TOKEN (name / selected / nearest / view)
    // ------------------------------------------------------------
    bool TryResolveSingleTarget(string token, out TargetObject target, out string error)
    {
        target = null;
        error = null;

        if (string.IsNullOrWhiteSpace(token))
        {
            error = "Aucune target fournie.";
            return false;
        }

        // self -> le TargetObject du joueur (origin)
        if (token.Equals("self", StringComparison.OrdinalIgnoreCase))
        {
            if (targeting == null || targeting.origin == null)
            {
                error = "ERREUR: origin non assigné dans PlayerTargeting2D.";
                return false;
            }

            // on récupère le TargetObject sur le player
            target = targeting.origin.GetComponentInChildren<TargetObject>();
            if (target == null)
            {
                error = "ERREUR: aucun TargetObject trouvé sur le joueur (origin).";
                return false;
            }

            return true;
        }


        // selected
        if (token.Equals("selected", StringComparison.OrdinalIgnoreCase))
        {
            if (targeting == null)
            {
                error = "ERREUR: targeting non assigné (glisse Main Camera dans CommandProcessor.targeting).";
                return false;
            }

            target = targeting.CurrentTarget;

            if (IsSelf(target))
            {
                error = "Refusé: 'nearest/selected' ne peut pas cibler self.";
                target = null;
                return false;
            }

            if (target == null)
            {
                error = "Aucune target sélectionnée (approche-toi d'un objet visible).";
                return false;
            }

            if (!IsInRange(target))
            {
                error = $"Hors portée: '{target.Name}'";
                target = null;
                return false;
            }

            return true;
        }

        // nearest
        if (token.Equals("nearest", StringComparison.OrdinalIgnoreCase))
        {
            if (targeting == null)
            {
                error = "ERREUR: targeting non assigné (glisse Main Camera dans CommandProcessor.targeting).";
                return false;
            }

            var list = targeting.VisibleTargets;
            if (list == null || list.Count == 0)
            {
                error = "Aucune target visible/à portée pour 'nearest'.";
                return false;
            }

            // La plus proche du joueur (origin) si dispo, sinon centre caméra.
            Vector3 refPos = targeting.origin != null
                ? targeting.origin.position
                : (targeting.cam != null
                    ? targeting.cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0))
                    : Vector3.zero);

            float best = float.PositiveInfinity;
            TargetObject bestT = null;

            foreach (var t in list)
            {
                if (t == null) continue;
                float d = Vector2.Distance(refPos, t.transform.position);
                if (d < best)
                {
                    best = d;
                    bestT = t;
                }
            }

            if (bestT == null)
            {
                error = "Aucune target valide pour 'nearest'.";
                return false;
            }

            target = bestT;
            return true;
        }

        // view = pas un single target
        if (token.Equals("view", StringComparison.OrdinalIgnoreCase))
        {
            error = "view ne peut pas être résolu en cible unique.";
            return false;
        }

        // name
        if (!TargetRegistry.TryFindByName(token.Trim(), out target) || target == null)
        {
            error = $"Target inconnue: '{token}'.";
            return false;
        }


        if (!IsInRange(target))
        {
            error = $"Hors portée: '{target.Name}'";
            target = null;
            return false;
        }

        return true;
    }

    bool TryResolveViewTargets(out List<TargetObject> targets, out string error)
    {
        targets = null;
        error = null;

        if (targeting == null)
        {
            error = "ERREUR: targeting non assigné (glisse Main Camera dans CommandProcessor.targeting).";
            return false;
        }

        var list = targeting.VisibleTargets;
        if (list == null || list.Count == 0)
        {
            error = "Aucune target visible/à portée dans la caméra.";
            return false;
        }

        // sécurité : filtre range au cas où
        targets = list.Where(t => t != null && IsInRange(t)).Distinct().ToList();
        if (targets.Count == 0)
        {
            error = "Aucune target à portée dans 'view'.";
            return false;
        }

        return true;
    }

    // ------------------------------------------------------------
    // EXÉCUTION ACTIONS (UnityEvents)
    // ------------------------------------------------------------
    string CallActionAndReturn()
    {
        if (string.IsNullOrWhiteSpace(actionString))
            return "Aucune action fournie.";

        if (!actionsByKeyword.TryGetValue(actionString.Trim(), out var evt) || evt == null)
            return $"Action inconnue: '{actionString}'. Tape 'help'.";

        if (string.IsNullOrWhiteSpace(targetString))
            return "Aucune target fournie.";

        // view (multi)
        if (targetString.Equals("view", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryResolveViewTargets(out var viewTargets, out var err))
                return err;
            int ok = 0;
            int skippedSelf = 0;
            int skippedRange = 0;

            foreach (var t in viewTargets)
            {
                if (t == null) continue;

                if (IsSelf(t))
                {
                    skippedSelf++;
                    continue;
                }

                if (!IsInRange(t))
                {
                    skippedRange++;
                    continue;
                }

                try { evt.Invoke(t); ok++; }
                catch { /* continue */ }
            }

            return $"OK: {actionString} view ({ok}/{viewTargets.Count})"
                 + (skippedSelf > 0 ? $" | self ignoré: {skippedSelf}" : "")
                 + (skippedRange > 0 ? $" | hors portée ignorés: {skippedRange}" : "");

        }

        // single target
        if (!TryResolveSingleTarget(targetString, out var target, out var error))
            return error;

        if (IsSelf(target))
            return "Refusé: tu ne peux pas cibler 'self' avec cette commande.";


        try
        {
            evt.Invoke(target);
            return $"OK: {actionString} {targetString} -> {target.Name}";
        }
        catch (Exception e)
        {
            return $"ERREUR: {e.GetType().Name} - {e.Message}";
        }
    }

    bool RequiresTarget(string line)
    {
        // actions qui ont BESOIN d'une target
        var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        string action = parts[0].ToLowerInvariant();

        // spawn: on veut au moins spawn <id> <target>
        if (action == "spawn") return true;

        // commandes "globales" qui n'ont pas de target
        if (action == "help" || action == "clear" || action == "bind" || action == "unbind" || action == "bindlist")
            return false;

        return true;
    }

    bool HasTargetToken(string line)
    {
        var parts = line.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        string action = parts[0].ToLowerInvariant();

        // spawn <id> <target>
        if (action == "spawn")
            return parts.Length >= 3;

        // action <target>
        return parts.Length >= 2;
    }


    // ------------------------------------------------------------
    // SPAWN
    // ------------------------------------------------------------
    string CallSpawnAndReturn(string spawnId)
    {
        if (worldActions == null)
            return "ERREUR: WorldCommandActions non assigné (drag l'objet WorldCommandActions dans CommandProcessor.worldActions).";

        if (string.IsNullOrWhiteSpace(spawnId))
            return "spawn: id vide";

        if (string.IsNullOrWhiteSpace(targetString))
            return "spawn: target vide";

        // view : spawn sur toutes les targets visibles/à portée
        if (targetString.Equals("view", StringComparison.OrdinalIgnoreCase))
        {
            if (!TryResolveViewTargets(out var viewTargets, out var err))
                return err;

            int ok = 0;
            foreach (var t in viewTargets)
            {
                if (t == null) continue;
                if (!IsInRange(t)) continue;

                bool spawned = worldActions.SpawnById(spawnId, t);
                if (spawned) ok++;
            }

            return $"OK: spawn {spawnId} view ({ok}/{viewTargets.Count})";
        }

        // single target (name/selected/nearest)
        if (!TryResolveSingleTarget(targetString, out var target, out var error))
            return error;

        bool success = worldActions.SpawnById(spawnId, target);
        return success
            ? $"OK: spawn {spawnId} -> {target.Name}"
            : $"Spawn inconnu: '{spawnId}' (ajoute-le dans WorldCommandActions.spawnables)";
    }

    // ------------------------------------------------------------
    // HELP
    // ------------------------------------------------------------
    string BuildHelpText()
    {
        var cmds = GetCommandKeywords();
        var tgs = GetTargetNames();

        string cmdLine = cmds.Count > 0 ? string.Join(", ", cmds) : "(aucune)";
        string tgLine = tgs.Count > 0 ? string.Join(", ", tgs) : "(aucune)";

        return
            $"Commandes: {cmdLine}\n" +
            $"Targets: {tgLine}\n" +
            $"Selectors: selected | nearest | view\n" +
            $"\n" +
            $"Ex: toggle selected | destroy nearest | ping view\n" +
            $"Ex: spawn wall player | spawn projectile selected | spawn turret nearest\n" +
            $"Ex: toggle lamp | destroy \"big lamp\" | help | clear" +
            $"\n" +
            $"Skills: bind | unbind | bindlist\n" +
            $"\n" +
            $"Ex: bind 1 \"toggle nearest\" 1.0\n" +
            $"Ex: bind q \"spawn projectile view\" 0.2\n" +
            $"Ex: unbind 1 | bindlist\n";

    }
}

[System.Serializable]
public struct MaStruct
{
    public string keyWord;
    public UnityEvent<TargetObject> action;
}
