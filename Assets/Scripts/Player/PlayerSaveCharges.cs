using UnityEngine;
using UnityEngine.Events;

public class PlayerSaveCharges : MonoBehaviour
{
    [Header("Save Charges")]
    [SerializeField] private int startingCharges = 0;
    [SerializeField] private int maxCharges = 99;

    [Header("Events")]
    public UnityEvent<int> onChargesChanged;

    private int currentCharges;

    public int CurrentCharges => currentCharges;

    private void Awake()
    {
        currentCharges = Mathf.Clamp(startingCharges, 0, maxCharges);
        onChargesChanged?.Invoke(currentCharges);
    }

    public void AddCharges(int amount)
    {
        if (amount <= 0) return;

        currentCharges = Mathf.Clamp(currentCharges + amount, 0, maxCharges);
        onChargesChanged?.Invoke(currentCharges);

        Debug.Log("[SaveCharges] Charges actuelles : " + currentCharges);
    }

    public bool HasEnoughCharges(int amount)
    {
        return currentCharges >= amount;
    }

    public bool TrySpendCharges(int amount)
    {
        if (amount <= 0) return true;
        if (currentCharges < amount) return false;

        currentCharges -= amount;
        onChargesChanged?.Invoke(currentCharges);

        Debug.Log("[SaveCharges] Charge utilisée. Restant : " + currentCharges);
        return true;
    }

    public void SetCharges(int amount)
    {
        currentCharges = Mathf.Clamp(amount, 0, maxCharges);
        onChargesChanged?.Invoke(currentCharges);
    }
}