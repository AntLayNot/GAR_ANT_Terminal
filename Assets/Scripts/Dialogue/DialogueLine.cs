using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Header("Speaker")]
    public string speakerName;
    public Sprite speakerPortrait;

    [Header("Text")]
    [TextArea(3, 8)]
    public string text;

    [Header("Options")]
    public float autoAdvanceDelay = 0f; // 0 = attendre le joueur
}