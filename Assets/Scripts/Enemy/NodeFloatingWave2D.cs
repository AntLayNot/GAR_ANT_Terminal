using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NodeFloatingWave2D : MonoBehaviour
{
    [Header("Float Movement")]
    [SerializeField] private bool enableFloating = true;

    [Tooltip("Hauteur de l'ondulation verticale.")]
    [SerializeField] private float amplitude = 0.75f;

    [Tooltip("Vitesse de l'ondulation.")]
    [SerializeField] private float frequency = 1.5f;

    [Tooltip("Force avec laquelle Node rejoint la hauteur de l'onde.")]
    [SerializeField] private float verticalFollowStrength = 8f;

    [Tooltip("Si true, Node commence en bas puis monte.")]
    [SerializeField] private bool startFromBottom = true;
    [SerializeField] private bool randomizePhase = false;

    [Header("Physics")]
    [SerializeField] private Rigidbody2D rb;

    [SerializeField] private bool forceGravityToZero = true;

    [Tooltip("Évite que Node tourne sur lui-même.")]
    [SerializeField] private bool freezeRotation = true;

    [Header("Pause")]
    [SerializeField] private bool respectPause = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    private float baseY;
    private float timer;
    private float phaseOffset;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            baseY = rb.position.y;

            if (forceGravityToZero)
                rb.gravityScale = 0f;

            if (freezeRotation)
                rb.freezeRotation = true;
        }

        if (startFromBottom)
            phaseOffset = -Mathf.PI * 0.5f;

        if (randomizePhase)
            phaseOffset += Random.Range(0f, Mathf.PI * 2f);
    }

    private void FixedUpdate()
    {
        if (!enableFloating)
            return;

        if (respectPause && PauseMenuController.IsPausedGlobal)
            return;

        if (rb == null)
            return;

        timer += Time.fixedDeltaTime * frequency;

        float wave = Mathf.Sin(timer + phaseOffset);
        float targetY = baseY + wave * amplitude;

        float yDifference = targetY - rb.position.y;
        float verticalVelocity = yDifference * verticalFollowStrength;

        // On garde la vitesse X actuelle pour laisser EnemyBrain2D gérer le patrol/chase.
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            verticalVelocity
        );
    }

    public void SetBaseYToCurrentPosition()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb != null)
            baseY = rb.position.y;
    }

    public void EnableFloating()
    {
        enableFloating = true;
        SetBaseYToCurrentPosition();
    }

    public void DisableFloating()
    {
        enableFloating = false;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                0f
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.cyan;

        Vector3 center = Application.isPlaying
            ? new Vector3(transform.position.x, baseY, transform.position.z)
            : transform.position;

        Vector3 top = center + Vector3.up * amplitude;
        Vector3 bottom = center + Vector3.down * amplitude;

        Gizmos.DrawLine(bottom, top);
        Gizmos.DrawWireSphere(top, 0.08f);
        Gizmos.DrawWireSphere(bottom, 0.08f);
    }
}