using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public string sequenceId;
    public List<DialogueLine> lines = new List<DialogueLine>();
}