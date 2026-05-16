using UnityEngine;

public class CommandDoorObjective2D : MonoBehaviour
{
    [SerializeField] private PlayerCommandProgression2D playerProgression;

    [Header("Options")]
    [SerializeField] private bool countOnlyOnce = true;

    private bool alreadyCounted;

    private void Awake()
    {
        if (playerProgression == null)
            playerProgression = FindFirstObjectByType<PlayerCommandProgression2D>();
    }

    public void RegisterDoorToggle()
    {
        if (countOnlyOnce && alreadyCounted)
            return;

        alreadyCounted = true;

        if (playerProgression != null)
            playerProgression.RegisterDoorToggled();
    }
}