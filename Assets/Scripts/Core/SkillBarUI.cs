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
        [Header("Slot")]
        public string slot;          // "1".."4"
        public TMP_Text slotLabel;
        public TMP_Text commandLabel;

        [Header("Icon")]
        public Image skillIcon;
        public Sprite normalSprite;
        public Sprite usedSprite;

        [Header("Cooldown")]
        public Image cooldownFill;   // Image type Filled

        [HideInInspector] public bool wasOnCooldown;
    }

    [Header("Bindings")]
    public SkillBindingManager bindings;

    [Header("Slots")]
    public List<SlotUI> slots = new();

    [Header("Cooldown Visual")]
    [SerializeField] private bool forceVerticalCooldown = true;

    [Tooltip("Si true, le cooldown se vide du haut vers le bas.")]
    [SerializeField] private bool cooldownFromTopToBottom = true;

    private void OnEnable()
    {
        if (bindings != null)
            bindings.onBindingsChanged.AddListener(Refresh);

        SetupCooldownImages();
        Refresh();
    }

    private void OnDisable()
    {
        if (bindings != null)
            bindings.onBindingsChanged.RemoveListener(Refresh);
    }

    private void Update()
    {
        if (bindings == null) return;

        foreach (var s in slots)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.slot))
                continue;

            float remain = bindings.CooldownRemaining(s.slot);
            float dur = Mathf.Max(0.0001f, bindings.CooldownDuration(s.slot));

            bool isOnCooldown = remain > 0f;
            float cooldownRatio = isOnCooldown ? Mathf.Clamp01(remain / dur) : 0f;

            UpdateCooldownFill(s, cooldownRatio);
            UpdateSkillSprite(s, isOnCooldown);

            s.wasOnCooldown = isOnCooldown;
        }
    }

    public void Refresh()
    {
        if (bindings == null) return;

        foreach (var s in slots)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.slot))
                continue;

            string command = GetCommandForSlot(s.slot);

            if (s.slotLabel != null)
            {
                bool hasCommand = !string.IsNullOrWhiteSpace(command);
                s.slotLabel.gameObject.SetActive(!hasCommand);
                s.slotLabel.text = s.slot;
            }

            if (s.commandLabel != null)
                s.commandLabel.text = command ?? "-";

            float remain = bindings.CooldownRemaining(s.slot);
            bool isOnCooldown = remain > 0f;

            UpdateSkillSprite(s, isOnCooldown);
        }
    }

    private void SetupCooldownImages()
    {
        if (!forceVerticalCooldown)
            return;

        foreach (var s in slots)
        {
            if (s == null || s.cooldownFill == null)
                continue;

            s.cooldownFill.type = Image.Type.Filled;
            s.cooldownFill.fillMethod = Image.FillMethod.Vertical;

            // 1 = Top, 0 = Bottom pour Image.FillMethod.Vertical
            s.cooldownFill.fillOrigin = cooldownFromTopToBottom ? 0 : 1;
        }
    }

    private void UpdateCooldownFill(SlotUI slotUI, float cooldownRatio)
    {
        if (slotUI.cooldownFill == null)
            return;

        slotUI.cooldownFill.fillAmount = cooldownRatio;
    }

    private void UpdateSkillSprite(SlotUI slotUI, bool isOnCooldown)
    {
        if (slotUI.skillIcon == null)
            return;

        if (isOnCooldown)
        {
            if (slotUI.usedSprite != null)
                slotUI.skillIcon.sprite = slotUI.usedSprite;
        }
        else
        {
            if (slotUI.normalSprite != null)
                slotUI.skillIcon.sprite = slotUI.normalSprite;
        }
    }

    private string GetCommandForSlot(string slot)
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