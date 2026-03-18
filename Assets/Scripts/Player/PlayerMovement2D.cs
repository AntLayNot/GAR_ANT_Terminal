using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerPlatformerController2D : MonoBehaviour
{
    [Header("Refs")]
    public Rigidbody2D rb;
    public Transform groundCheck;
    public LayerMask groundLayer;

    [Header("Ground Check")]
    public Vector2 groundCheckSize = new Vector2(0.6f, 0.12f);

    [Header("Terminal (optional)")]
    public TerminalController terminal;

    [Header("Move")]
    public float moveSpeed = 8f;
    public float acceleration = 60f;
    public float deceleration = 80f;
    public float airControl = 0.6f; // 0..1

    [Header("Jump")]
    public float jumpForce = 14f;
    public float coyoteTime = 0.12f;      // tolérance après avoir quitté le sol
    public float jumpBuffer = 0.12f;      // tolérance si appuyé un peu avant de toucher le sol
    public float fallMultiplier = 2.0f;   // chute plus rapide
    public float lowJumpMultiplier = 2.0f; // petit saut si relaché trop tot

    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;

    private float coyoteTimer;
    private float jumpBufferTimer;

    [Header("Wall Check")]
    public Transform wallCheck;
    public Vector2 wallCheckSize = new Vector2(0.1f, 0.8f);
    public LayerMask wallLayer;


    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    bool IsTouchingWall()
    {
        if (wallCheck == null) return false;
        return Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);
    }


    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying)
        {
            StopMovementCompletely();
            return;
        }

        bool blocked = terminal != null &&
                       terminal.isActiveAndEnabled &&
                       terminal.input != null &&
                       terminal.input.isFocused;

        if (blocked)
        {
            StopMovementCompletely();
            return;
        }

        // A/D ou Q/D (Unity gère ZQSD via Horizontal)
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump")) // Space par défaut
        {
            jumpPressed = true;
            jumpBufferTimer = jumpBuffer;
        }

        jumpHeld = Input.GetButton("Jump");

        // timers
        jumpBufferTimer -= Time.deltaTime;
        coyoteTimer -= Time.deltaTime;

        if (IsGrounded())
            coyoteTimer = coyoteTime;
    }

    void FixedUpdate()
    {
        if ((DialogueManager.Instance != null && DialogueManager.Instance.IsDialoguePlaying) ||
        (terminal != null && terminal.isActiveAndEnabled && terminal.input != null && terminal.input.isFocused))
        {
            StopMovementCompletely();
            return;
        }

        // Horizontal movement (accel/decel)
        float targetSpeed = moveInput * moveSpeed;
        float speedDif = targetSpeed - rb.linearVelocity.x;

        bool grounded = IsGrounded();
        bool touchingWall = IsTouchingWall();

        // Si on est en l'air ET collé à un mur, on force la gravité
        if (!grounded && touchingWall && rb.linearVelocity.y > -20f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * Time.fixedDeltaTime;
        }

        float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

        // moins de controle en l'air
        float control = grounded ? 1f : airControl;

        float movement = speedDif * accelRate * control;
        rb.AddForce(new Vector2(movement, 0f), ForceMode2D.Force);

        // Jump (buffer + coyote)
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            // consomme le jump
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;

            // reset vitesse verticale pour des sauts constants
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // --- Better jump feel
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }

        // reset one-frame input
        jumpPressed = false;
    }

    private void StopMovementCompletely()
    {
        moveInput = 0f;
        jumpPressed = false;
        jumpHeld = false;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
