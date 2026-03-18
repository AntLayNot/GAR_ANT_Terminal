using System.Collections;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("References")]
    public DialogueUI dialogueUI;

    [Header("Controls")]
    public KeyCode nextKey = KeyCode.Space;

    [Header("Typing Effect")]
    public bool useTypingEffect = true;
    public float typingSpeed = 0.03f;

    public bool IsDialoguePlaying { get; private set; }

    private DialogueSequence currentSequence;
    private int currentLineIndex;
    private Coroutine typingCoroutine;
    private bool isTyping;
    private bool waitingForInput;
    private bool skipTypingRequested;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        dialogueUI.ShowUI(false);
    }

    private void Update()
    {
        if (!IsDialoguePlaying) return;

        if (Input.GetKeyDown(nextKey))
        {
            if (isTyping)
            {
                skipTypingRequested = true;
            }
            else if (waitingForInput)
            {
                ShowNextLine();
            }
        }
    }

    public void PlayDialogue(DialogueSequence sequence)
    {
        if (sequence == null || sequence.lines == null || sequence.lines.Count == 0)
        {
            Debug.LogWarning("Dialogue sequence vide ou nulle.");
            return;
        }

        currentSequence = sequence;
        currentLineIndex = 0;
        IsDialoguePlaying = true;
        dialogueUI.ShowUI(true);
        ShowCurrentLine();
    }

    public void StopDialogue()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        IsDialoguePlaying = false;
        isTyping = false;
        waitingForInput = false;
        currentSequence = null;
        dialogueUI.ShowUI(false);
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentSequence == null || currentLineIndex >= currentSequence.lines.Count)
        {
            StopDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (currentSequence == null) return;

        DialogueLine line = currentSequence.lines[currentLineIndex];

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        waitingForInput = false;
        skipTypingRequested = false;
        dialogueUI.SetContinueIndicator(false);

        if (useTypingEffect)
        {
            typingCoroutine = StartCoroutine(TypeLine(line));
        }
        else
        {
            dialogueUI.SetLine(line.speakerName, line.text, line.speakerPortrait);
            HandleLineEnd(line);
        }
    }

    private IEnumerator TypeLine(DialogueLine line)
    {
        isTyping = true;
        dialogueUI.SetLine(line.speakerName, "", line.speakerPortrait);

        string fullText = line.text;
        string currentText = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipTypingRequested)
            {
                currentText = fullText;
                break;
            }

            currentText += fullText[i];
            dialogueUI.SetLine(line.speakerName, currentText, line.speakerPortrait);
            yield return new WaitForSeconds(typingSpeed);
        }

        dialogueUI.SetLine(line.speakerName, fullText, line.speakerPortrait);
        isTyping = false;

        HandleLineEnd(line);
    }

    private void HandleLineEnd(DialogueLine line)
    {
        if (line.autoAdvanceDelay > 0f)
        {
            StartCoroutine(AutoAdvance(line.autoAdvanceDelay));
        }
        else
        {
            waitingForInput = true;
            dialogueUI.SetContinueIndicator(true);
        }
    }

    private IEnumerator AutoAdvance(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowNextLine();
    }
}