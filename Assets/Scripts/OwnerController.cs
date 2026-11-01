using UnityEngine;

// 주인: 고양이가 울면 운 자리까지 걸어오고, 걷는 중에 한 번 더 울면 멈춘다
// 벽에 막히거나 앞에 바닥이 없으면(낭떠러지) 스스로 멈춘다
[RequireComponent(typeof(Rigidbody2D))]
public class OwnerController : MonoBehaviour
{
    public enum State { Waiting, Moving, Hiding }

    [Header("이동")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveDistance = 0.3f;  // 고양이 자리와 이만큼 가까워지면 도착

    [Header("장애물 감지")]
    [SerializeField] private LayerMask groundLayer;        // Ground
    [SerializeField] private float wallCheckDistance = 0.1f;
    [SerializeField] private float ledgeCheckDepth = 0.4f; // 발 앞 아래로 이만큼 바닥이 없으면 멈춤

    [Header("상태별 색 (확인용, 끄면 원래 그림 색)")]
    [SerializeField] private bool useStateColors = false;
    [SerializeField] private Color waitingColor = new Color(0.45f, 0.65f, 1f);
    [SerializeField] private Color movingColor = new Color(0.45f, 1f, 0.55f);
    [SerializeField] private Color hidingColor = new Color(0.6f, 0.6f, 0.6f);

    public State CurrentState { get; private set; } = State.Waiting;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private float targetX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 자식 Visual의 그림
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
        ApplyColor();
    }

    // 고양이가 들리는 거리에서 울었을 때 CatMeow가 호출
    public void OnMeow(Vector2 catPos)
    {
        switch (CurrentState)
        {
            case State.Waiting:
                if (Mathf.Abs(catPos.x - rb.position.x) <= arriveDistance) return; // 이미 옆에 있음
                targetX = catPos.x;
                SetState(State.Moving);
                break;
            case State.Moving:
                SetState(State.Waiting); // 한 번 더 울면 멈춤
                break;
        }
    }

    // 숨기 / 나오기 (HideController가 호출)
    public void Hide() { SetState(State.Hiding); }
    public void Unhide() { if (CurrentState == State.Hiding) SetState(State.Waiting); }

    void FixedUpdate()
    {
        if (CurrentState != State.Moving) return;

        Vector2 pos = rb.position;
        float dir = Mathf.Sign(targetX - pos.x);
        if (sr != null) sr.flipX = dir < 0f;

        if (Blocked(dir))
        {
            Debug.Log("주인: 앞이 막혀서 멈춤");
            SetState(State.Waiting);
            return;
        }

        float newX = Mathf.MoveTowards(pos.x, targetX, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector2(newX, pos.y));

        if (Mathf.Abs(targetX - newX) <= arriveDistance) SetState(State.Waiting);
    }

    // 벽에 막혔거나 앞에 바닥이 없는가
    private bool Blocked(float dir)
    {
        Bounds b = col.bounds;
        float halfW = b.extents.x;

        // 벽·낮은 천장: 몸 크기(조금 작게)의 상자를 앞쪽으로 밀어서 부딪히는지 확인
        Vector2 boxSize = new Vector2(b.size.x * 0.9f, b.size.y * 0.9f);
        if (Physics2D.BoxCast(b.center, boxSize, 0f, Vector2.right * dir, wallCheckDistance, groundLayer)) return true;

        // 낭떠러지: 앞발 위치에서 아래로
        Vector2 foot = new Vector2(b.center.x + dir * (halfW + wallCheckDistance), b.min.y + 0.1f);
        return !Physics2D.Raycast(foot, Vector2.down, ledgeCheckDepth, groundLayer);
    }

    private void SetState(State s)
    {
        CurrentState = s;
        ApplyColor();
        Debug.Log($"주인 상태: {s}");
    }

    private void ApplyColor()
    {
        if (sr == null) return;
        if (!useStateColors) return;
        sr.color = CurrentState switch
        {
            State.Moving => movingColor,
            State.Hiding => hidingColor,
            _ => waitingColor,
        };
    }

    void OnDrawGizmosSelected()
    {
        if (CurrentState == State.Moving)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, new Vector3(targetX, transform.position.y, 0f));
        }
    }
}
