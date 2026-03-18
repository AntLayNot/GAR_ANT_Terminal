using UnityEngine;

public class StoryEventExample : MonoBehaviour
{
    public DialogueSequence itemFoundDialogue;

    public void OnImportantItemCollected()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.PlayDialogue(itemFoundDialogue);
        }
    }
}