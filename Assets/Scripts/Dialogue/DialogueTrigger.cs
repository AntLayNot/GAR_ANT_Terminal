using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class DialogueTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        OnStart,
        OnTriggerEnter2D,
        Manual
    }

    [Header("Trigger Settings")]
    public TriggerType triggerType = TriggerType.Manual;
    public DialogueSequence sequence;
    public bool triggerOnlyOnce = true;
    public string requiredTag = "Player";

    [Header("Dialogue Events")]
    public UnityEvent onDialogueStarted;
    public UnityEvent onDialogueEnded;

    [Header("Dialogue Slow Motion")]
    public bool slowMotionDuringDialogue = true;

    [Range(0.01f, 1f)]
    public float dialogueTimeScale = 0.2f;

    public float fixedDeltaBase = 0.02f;

    [Tooltip("Si true, remet automatiquement le temps normal quand le dialogue se termine.")]
    public bool restoreTimeAfterDialogue = true;

    [Header("Debug")]
    public bool debugLogs = true;
    public bool drawGizmos = true;

    private bool hasTriggered = false;
    private Coroutine dialogueRoutine;

    private void Start()
    {
        if (debugLogs)
            Debug.Log($"[DialogueTrigger] Start sur {gameObject.name} | Mode: {triggerType}");

        if (triggerType == TriggerType.OnStart)
        {
            if (debugLogs)
                Debug.Log("[DialogueTrigger] Déclenchement automatique (OnStart)");

            TriggerDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugLogs)
            Debug.Log($"[DialogueTrigger] Collision détectée avec: {other.name} (tag: {other.tag})");

        if (triggerType != TriggerType.OnTriggerEnter2D)
            return;

        if (!other.CompareTag(requiredTag))
            return;

        TriggerDialogue();
    }

    public void TriggerDialogue()
    {
        if (debugLogs)
            Debug.Log("[DialogueTrigger] Tentative de lancement du dialogue");

        if (hasTriggered && triggerOnlyOnce)
            return;

        if (sequence == null)
        {
            Debug.LogError("[DialogueTrigger] Sequence NULL !");
            return;
        }

        if (DialogueManager.Instance == null)
        {
            Debug.LogError("[DialogueTrigger] DialogueManager manquant !");
            return;
        }

        if (DialogueManager.Instance.IsDialoguePlaying)
        {
            if (debugLogs)
                Debug.Log("[DialogueTrigger] Dialogue déjà en cours");
            return;
        }

        hasTriggered = true;

        if (debugLogs)
            Debug.Log($"[DialogueTrigger] Lancement du dialogue: {sequence.name}");

        if (dialogueRoutine != null)
            StopCoroutine(dialogueRoutine);

        dialogueRoutine = StartCoroutine(DialogueRoutine());
    }

    private IEnumerator DialogueRoutine()
    {
        onDialogueStarted?.Invoke();

        DialogueManager.Instance.PlayDialogue(sequence);

        if (slowMotionDuringDialogue)
            StartDialogueSlowMotion();

        while (DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying)
        {
            yield return null;
        }

        if (restoreTimeAfterDialogue)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedDeltaBase;
        }

        onDialogueEnded?.Invoke();

        dialogueRoutine = null;
    }

    private void StartDialogueSlowMotion()
    {
        Time.timeScale = dialogueTimeScale;
        Time.fixedDeltaTime = fixedDeltaBase * dialogueTimeScale;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;

        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }

        if (restoreTimeAfterDialogue)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedDeltaBase;
        }

        if (debugLogs)
            Debug.Log("[DialogueTrigger] Trigger reset.");
    }

    private void OnDisable()
    {
        if (dialogueRoutine != null)
        {
            StopCoroutine(dialogueRoutine);
            dialogueRoutine = null;
        }

        if (restoreTimeAfterDialogue)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = fixedDeltaBase;
        }
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = hasTriggered ? Color.red : Color.green;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider2D box)
                Gizmos.DrawWireCube(box.offset, box.size);
            else if (col is CircleCollider2D circle)
                Gizmos.DrawWireSphere(circle.offset, circle.radius);
        }
    }
}