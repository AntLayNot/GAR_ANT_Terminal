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

    public void SetName(string newName)
    {
        targetName = newName;

        if (isActiveAndEnabled)
            TargetRegistry.Touch(this);
    }
}
