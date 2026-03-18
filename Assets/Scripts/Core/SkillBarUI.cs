using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillBarUI : MonoBehaviour
{
    [Serializable]
    public class SlotUI
    {
        public string slot;          // "1".."4"
        public TMP_Text slotLabel;
        public TMP_Text commandLabel;
        public Image cooldownFill;   // Image type Filled
    }

    public SkillBindingManager bindings;
    public List<SlotUI> slots = new();

    void OnEnable()
    {
        if (bindings != null)
            bindings.onBindingsChanged.AddListener(Refresh);

        Refresh();
    }

    void OnDisable()
    {
        if (bindings != null)
            bindings.onBindingsChanged.RemoveListener(Refresh);
    }

    void Update()
    {
        if (bindings == null) return;

        foreach (var s in slots)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.slot)) continue;
            if (s.cooldownFill == null) continue;

            float remain = bindings.CooldownRemaining(s.slot);
            float dur = Mathf.Max(0.0001f, bindings.CooldownDuration(s.slot));

            s.cooldownFill.fillAmount = remain > 0f ? Mathf.Clamp01(remain / dur) : 0f;
        }
    }

    public void Refresh()
    {
        if (bindings == null) return;

        foreach (var s in slots)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.slot)) continue;

            if (s.slotLabel != null)
                s.slotLabel.text = s.slot;

            if (s.commandLabel != null)
                s.commandLabel.text = GetCommandForSlot(s.slot) ?? "-";
        }
    }

    string GetCommandForSlot(string slot)
    {
        foreach (var b in bindings.GetAll())
        {
            if (b == null) continue;
            if (string.Equals(b.slot, slot, StringComparison.OrdinalIgnoreCase))
                return b.command;
        }
        return null;
    }
}
