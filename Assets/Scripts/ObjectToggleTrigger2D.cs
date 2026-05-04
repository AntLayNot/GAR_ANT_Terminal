using UnityEngine;

public class ObjectToggleTrigger2D : MonoBehaviour
{
    [Header("Trigger Filter")]
    [SerializeField] private bool onlyPlayerCanTrigger = true;
    [SerializeField] private string requiredTag = "Player";

    [Header("Objects To Enable")]
    [SerializeField] private GameObject[] objectsToEnable;

    [Header("Objects To Disable")]
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Options")]
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool disableTriggerObjectAfterUse = false;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerOnlyOnce && hasTriggered)
            return;

        if (onlyPlayerCanTrigger)
        {
            if (!other.CompareTag(requiredTag))
                return;
        }

        ActivateObjects();
        hasTriggered = true;

        if (disableTriggerObjectAfterUse)
            gameObject.SetActive(false);
    }

    private void ActivateObjects()
    {
        for (int i = 0; i < objectsToEnable.Length; i++)
        {
            if (objectsToEnable[i] != null)
                objectsToEnable[i].SetActive(true);
        }

        for (int i = 0; i < objectsToDisable.Length; i++)
        {
            if (objectsToDisable[i] != null)
                objectsToDisable[i].SetActive(false);
        }
    }
}