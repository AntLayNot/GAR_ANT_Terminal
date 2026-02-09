using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class SkillBindingManager : MonoBehaviour
{
    [Serializable]
    public class Binding
    {
        public string slot;          // "1".."4"
        [TextArea] public string command;

        [Tooltip("Cooldown imposé par le slot (1..4). Ne pas éditer à la main.")]
        public float cooldown;       // affiché/lu par l'UI

        [NonSerialized] public float nextReadyTimeUnscaled;
    }

    [Header("Refs")]
    public CommandProcessor commandProcessor;

    [Header("Bindings")]
    public List<Binding> bindings = new();

    [Header("Events")]
    public UnityEvent onBindingsChanged;

    private readonly Dictionary<string, Binding> bySlot = new(StringComparer.OrdinalIgnoreCase);

    static bool IsValidSlot(string slot)
        => slot == "1" || slot == "2" || slot == "3" || slot == "4";

    float GetFixedCooldown(string slot)
    {
        return slot switch
        {
            "1" => 1.5f,
            "2" => 3.0f,
            "3" => 6.0f,
            "4" => 10.0f,
            _ => 3.0f
        };
    }

    void Awake()
    {
        RebuildCache();
    }

    public void RebuildCache()
    {
        bySlot.Clear();

        // 1) Nettoyage + normalisation slot
        var cleaned = bindings
            .Where(b => b != null && !string.IsNullOrWhiteSpace(b.slot) && IsValidSlot(b.slot.Trim()))
            .Select(b =>
            {
                b.slot = b.slot.Trim();
                b.cooldown = GetFixedCooldown(b.slot); // ✅ impose cooldown fixe
                return b;
            })
            .ToList();

        // 2) Supprime doublons : garde le dernier binding par slot
        // (utile si tu as eu des copies dans l'inspector)
        var unique = new List<Binding>();
        foreach (var b in cleaned)
        {
            // si déjà présent, remplace l'ancien
            int idx = unique.FindIndex(x => string.Equals(x.slot, b.slot, StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) unique[idx] = b;
            else unique.Add(b);
        }

        bindings = unique;

        foreach (var b in bindings)
            bySlot[b.slot] = b;
    }

    public IReadOnlyList<Binding> GetAll() => bindings;

    public float CooldownRemaining(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot)) return 0f;
        slot = slot.Trim();
        if (!bySlot.TryGetValue(slot, out var b) || b == null) return 0f;

        return Mathf.Max(0f, b.nextReadyTimeUnscaled - Time.unscaledTime);
    }

    public float CooldownDuration(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot)) return 0f;
        slot = slot.Trim();
        if (!bySlot.TryGetValue(slot, out var b) || b == null) return 0f;

        return Mathf.Max(0f, b.cooldown);
    }

    // ✅ cooldown non configurable
    public string Bind(string slot, string command)
    {
        if (string.IsNullOrWhiteSpace(slot)) return "bind: slot invalide.";
        if (string.IsNullOrWhiteSpace(command)) return "bind: commande invalide.";

        slot = slot.Trim();

        if (!IsValidSlot(slot))
            return "bind: slots autorisés = 1, 2, 3, 4.";

        command = command.Trim();

        if (!bySlot.TryGetValue(slot, out var b) || b == null)
        {
            b = new Binding { slot = slot };
            bindings.Add(b);
            bySlot[slot] = b;
        }

        b.command = command;
        b.cooldown = GetFixedCooldown(slot);   // ✅ impose cooldown fixe
        b.nextReadyTimeUnscaled = 0f;

        onBindingsChanged?.Invoke();
        return $"Bind OK: [{slot}] = \"{command}\" (cd {b.cooldown:0.##}s)";
    }

    public string Unbind(string slot)
    {
        if (string.IsNullOrWhiteSpace(slot)) return "unbind: slot invalide.";
        slot = slot.Trim();

        if (!bySlot.TryGetValue(slot, out var b) || b == null)
            return $"Aucun bind sur [{slot}].";

        bindings.Remove(b);
        bySlot.Remove(slot);

        onBindingsChanged?.Invoke();
        return $"Unbind OK: [{slot}]";
    }

    public string ListBinds()
    {
        if (bindings.Count == 0) return "Aucun bind.";

        var ordered = bindings
            .Where(b => b != null && !string.IsNullOrWhiteSpace(b.slot))
            .OrderBy(b => b.slot, StringComparer.OrdinalIgnoreCase)
            .ToList();

        string s = "Binds:\n";
        foreach (var b in ordered)
            s += $"- [{b.slot}] -> {b.command} (cd {b.cooldown:0.##}s)\n";
        return s.TrimEnd();
    }

    public bool TryUse(string slot, out string result)
    {
        result = null;

        if (commandProcessor == null)
        {
            result = "SkillBindingManager: CommandProcessor manquant.";
            return true;
        }

        if (string.IsNullOrWhiteSpace(slot)) return false;
        slot = slot.Trim();

        if (!bySlot.TryGetValue(slot, out var b) || b == null || string.IsNullOrWhiteSpace(b.command))
            return false;

        // ✅ cooldown basé sur Time.unscaledTime (pas affecté par timeScale)
        if (b.cooldown > 0f && Time.unscaledTime < b.nextReadyTimeUnscaled)
        {
            float remain = b.nextReadyTimeUnscaled - Time.unscaledTime;
            result = $"[{b.slot}] cooldown ({remain:0.0}s).";
            return true;
        }

        if (b.cooldown > 0f)
            b.nextReadyTimeUnscaled = Time.unscaledTime + b.cooldown;

        result = commandProcessor.ExecuteLine(b.command);
        return true;
    }

    public static bool TryParseFloat(string s, out float value)
    {
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            || float.TryParse(s, out value);
    }
}
