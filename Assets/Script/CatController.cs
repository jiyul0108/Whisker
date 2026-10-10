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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        moveInput = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveInput -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveInput += 1f;

        if (kb.spaceKey.wasPressedThisFrame) jumpPressed = true;

        // 이동 방향에 따라 좌우 반전 (나중에 고양이 스프라이트용)
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