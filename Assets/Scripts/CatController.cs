using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CatController : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("점프")]
    [SerializeField] private float jumpForce = 12f;

    [Header("바닥 체크")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float moveInput;
    private bool jumpPressed;
    private bool isGrounded;

    // 애니메이션 스크립트가 읽는 값
    public bool IsGrounded => isGrounded;
    public float MoveInput => moveInput;
    public float VelocityY => rb.linearVelocity.y;
    public int Facing => (sr != null && sr.flipX) ? -1 : 1; // 1 = 오른쪽, -1 = 왼쪽

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 자식 Visual의 그림
    }

    void Update()
    {
        var kb = Keyboard.current;
        moveInput = 0f;
        if (kb == null) return;
        if (NoteUI.IsReading) { jumpPressed = false; return; } // 쪽지 읽는 동안 조작 안 됨

        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput += 1f;

        if (kb.spaceKey.wasPressedThisFrame) jumpPressed = true;

        // 이동 방향에 따라 좌우 반전
        if (moveInput != 0f && sr != null) sr.flipX = moveInput < 0f;
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        jumpPressed = false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}