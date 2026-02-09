using UnityEngine;

public class TargetObject : MonoBehaviour
{
    [SerializeField] private string targetName;

    public string Name => string.IsNullOrWhiteSpace(targetName) ? gameObject.name : targetName;

    void OnEnable()
    {
        TargetRegistry.Register(this);
    }

    void OnDisable()
    {
        TargetRegistry.Unregister(this);
    }

    // Important: on "touch" le registry pour que ce target devienne le dernier (A2: last wins).
    public void SetName(string newName)
    {
        targetName = newName;

        if (isActiveAndEnabled)
            TargetRegistry.Touch(this);
    }
}
