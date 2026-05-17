using TMPro;
using UnityEngine;

public class SkillInput : MonoBehaviour
{
    public SkillBindingManager bindings;

    [Header("Block while typing")]
    public GameObject terminalRoot;
    public TMP_InputField terminalInput;

    [Header("Slots, Keys (4 skills)")]
    public KeyCode key1 = KeyCode.Alpha1;
    public KeyCode key2 = KeyCode.Alpha2;
    public KeyCode key3 = KeyCode.Alpha3;
    public KeyCode key4 = KeyCode.Alpha4;

    void Update()
    {
        if (bindings == null) return;

        bool terminalOpen = terminalRoot != null && terminalRoot.activeSelf;
        bool typing = terminalInput != null && terminalInput.isFocused;

        if (terminalOpen && typing)
            return;

        TryKey(key1, "1");
        TryKey(key2, "2");
        TryKey(key3, "3");
        TryKey(key4, "4");
    }

    void TryKey(KeyCode key, string slot)
    {
        if (!Input.GetKeyDown(key)) return;

        if (bindings.TryUse(slot, out var result) && !string.IsNullOrWhiteSpace(result))
            Debug.Log(result);
    }
}
