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
    private Collider2D col;
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
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 자식 Visual의 그림
    }

    void Update()
    {
        var kb = Keyboard.current;
        moveInput = 0f;
        if (kb == null) return;
        if (HideController.IsHidden) { jumpPressed = false; return; } // 숨은 중에는 조작 안 됨
        if (ClimbSpot.IsClimbing) { jumpPressed = false; return; }    // 벽에 붙어 있는 중
        if (GameInput.Blocked) { jumpPressed = false; return; }       // 팝업·연출 중

        // 이동은 방향키(← →)만. W·A·S·D는 상호작용 전용 (W 조사, S 숨기)
        if (kb.leftArrowKey.isPressed) moveInput -= 1f;
        if (kb.rightArrowKey.isPressed) moveInput += 1f;

        if (kb.spaceKey.wasPressedThisFrame) jumpPressed = true;

        // 이동 방향에 따라 좌우 반전
        if (moveInput != 0f && sr != null) sr.flipX = moveInput < 0f;
    }

    void FixedUpdate()
    {
        isGrounded = CheckGround();
        if (ClimbSpot.IsClimbing) return;   // 벽을 타는 동안은 ClimbSpot이 움직임

        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
        jumpPressed = false;
    }

    // 바닥 확인: 발 가운데 원 + 발바닥 너비 전체
    // (의자 끝에 몸 절반만 걸쳐 서 있으면 가운데 원은 허공이라 공중으로 판단 → 착지 그림에서 멈추던 문제)
    private bool CheckGround()
    {
        if (Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer)) return true;
        if (col == null || rb.linearVelocity.y > 0.1f) return false;   // 뛰어오르는 중에는 아님
        Bounds b = col.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - 0.04f);
        Vector2 size = new Vector2(b.size.x * 0.85f, 0.08f);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f, groundLayer))
            if (hit != null && hit != col && !hit.isTrigger) return true;
        return false;
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}