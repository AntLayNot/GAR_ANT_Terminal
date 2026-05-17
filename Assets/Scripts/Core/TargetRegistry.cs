using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TargetRegistry
{
    // Conserver l'ordre d'insertion pour que le "dernier gagne"
    private static readonly LinkedList<TargetObject> ordered = new();
    private static readonly Dictionary<TargetObject, LinkedListNode<TargetObject>> nodes = new();

    public static void Register(TargetObject t)
    {
        if (t == null) return;

        // Si déjà enregistré, le déplacer en fin (devient le "dernier")
        if (nodes.TryGetValue(t, out var node))
        {
            ordered.Remove(node);
            ordered.AddLast(node);
            return;
        }

        var n = ordered.AddLast(t);
        nodes[t] = n;
    }

    public static void Unregister(TargetObject t)
    {
        if (t == null) return;

        if (nodes.TryGetValue(t, out var node))
        {
            ordered.Remove(node);
            nodes.Remove(t);
        }
    }

    public static IEnumerable<string> GetAllNames()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var t in ordered)
        {
            if (t == null) continue;
            var name = t.Name;
            if (string.IsNullOrWhiteSpace(name)) continue;
            set.Add(name.Trim());
        }

        return set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Recherche par nom (insensible à la casse). Si plusieurs partagent le même nom,
    /// retourne le plus récemment enregistré.
    /// </summary>
    public static bool TryFindByName(string name, out TargetObject found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.Trim();

        // Itérer depuis la fin => le plus récemment enregistré en premier
        for (var node = ordered.Last; node != null; node = node.Previous)
        {
            var t = node.Value;
            if (t == null) continue;

            if (string.Equals(t.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase))
            {
                found = t;
                return true;
            }
        }

        return false;
    }

    public static void Touch(TargetObject t)
    {
        Register(t);
    }
}
