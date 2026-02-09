using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Global registry of active TargetObject instances.
/// Model 1 + A2: if multiple objects share the same Name, the most recently registered wins.
/// </summary>
public static class TargetRegistry
{
    // Keep insertion order for "last wins"
    private static readonly LinkedList<TargetObject> ordered = new();
    private static readonly Dictionary<TargetObject, LinkedListNode<TargetObject>> nodes = new();

    public static void Register(TargetObject t)
    {
        if (t == null) return;

        // If already registered, move to end (becomes "last")
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
    /// Find by Name (case-insensitive). If multiple share the same name, returns the most recently registered.
    /// </summary>
    public static bool TryFindByName(string name, out TargetObject found)
    {
        found = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        name = name.Trim();

        // Iterate from end => last registered first
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

    /// <summary>
    /// For renames: move this target to the end so it becomes the "last" for its new name.
    /// </summary>
    public static void Touch(TargetObject t)
    {
        Register(t);
    }
}
