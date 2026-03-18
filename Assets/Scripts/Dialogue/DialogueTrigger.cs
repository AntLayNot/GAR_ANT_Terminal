using UnityEngine;

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

    [Header("Debug")]
    public bool debugLogs = true;
    public bool drawGizmos = true;

    private bool hasTriggered = false;

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
        {
            if (debugLogs)
                Debug.Log("[DialogueTrigger] Mauvais mode de trigger");
            return;
        }

        if (!other.CompareTag(requiredTag))
        {
            if (debugLogs)
                Debug.Log($"[DialogueTrigger] Tag incorrect (attendu: {requiredTag})");
            return;
        }

        TriggerDialogue();
    }

    public void TriggerDialogue()
    {
        if (debugLogs)
            Debug.Log("[DialogueTrigger] Tentative de lancement du dialogue");

        if (hasTriggered && triggerOnlyOnce)
        {
            if (debugLogs)
                Debug.Log("[DialogueTrigger] Déjà déclenché (bloqué)");
            return;
        }

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

        DialogueManager.Instance.PlayDialogue(sequence);
    }

    // 🔍 Debug visuel dans la scène
    private void OnDrawGizmos()
    {
        if (!drawGizmos) return;

        Gizmos.color = hasTriggered ? Color.red : Color.green;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider2D box)
            {
                Gizmos.DrawWireCube(box.offset, box.size);
            }
            else if (col is CircleCollider2D circle)
            {
                Gizmos.DrawWireSphere(circle.offset, circle.radius);
            }
        }
    }
}