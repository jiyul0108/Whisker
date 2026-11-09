using UnityEngine;

// 주인: 고양이가 울면(E) 고양이 자리까지 걸어오고, 걷는 중에 한 번 더 울면 멈춘다 (도착하면 멈춤 → 다시 부르려면 또 울어야 함)
// 벽에 막히거나 앞에 바닥이 없으면(낭떠러지) 스스로 멈춘다
[RequireComponent(typeof(Rigidbody2D))]
public class OwnerController : MonoBehaviour
{
    public enum State { Waiting, Moving, Hiding }

    [Header("이동")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float arriveDistance = 0.3f;  // 고양이 자리와 이만큼 가까워지면 도착
    [SerializeField] private float runSpeed = 3.6f;        // 존재가 쫓아올 때 고양이를 따라 뛰는 속도 (존재 추격 3보다 빠르게)
    [SerializeField, Range(0.1f, 1f)] private float minFearSpeed = 0.7f;  // 공포도가 높아도 이보다 느려지지 않음

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
    public bool IsWalking { get; private set; }                 // 지금 실제로 걷고 있는가 (애니메이션용)

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private float targetX;
    private Transform followTarget;   // 따라갈 고양이 (운 뒤 고양이가 움직여도 따라감)
    private OwnerFear fear;           // 공포도가 높을수록 느려짐 (없으면 평소 속도)
    private PresenceAI presence;      // 쫓기고 있는지 확인용

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 자식 Visual의 그림
        fear = GetComponent<OwnerFear>();
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
        presence = FindAnyObjectByType<PresenceAI>();
        ApplyColor();
    }

    // 고양이가 들리는 거리에서 울었을 때 CatMeow가 호출
    public void OnMeow(Transform cat)
    {
        switch (CurrentState)
        {
            case State.Waiting:
                if (Mathf.Abs(cat.position.x - rb.position.x) <= arriveDistance) return; // 이미 옆에 있음
                followTarget = cat;
                targetX = cat.position.x;
                SetState(State.Moving);
                break;
            case State.Moving:
                SetState(State.Waiting); // 한 번 더 울면 멈춤
                break;
        }
    }

    // 숨기 / 나오기 (HideController가 호출)
    public void Hide() { IsWalking = false; SetState(State.Hiding); }
    public void Unhide() { if (CurrentState == State.Hiding) SetState(State.Waiting); }

    // 계단으로 고양이와 함께 다른 층으로 (Stairs가 호출)
    public void TeleportTo(Vector2 pos)
    {
        rb.position = pos;
        transform.position = pos;
        rb.linearVelocity = Vector2.zero;
        IsWalking = false;
        SetState(State.Waiting);
    }

    void FixedUpdate()
    {
        if (CurrentState != State.Moving) { IsWalking = false; return; }

        Vector2 pos = rb.position;
        // 고양이가 다른 층으로 가버리면 멈춤 (계단을 혼자 탄 경우)
        if (followTarget != null && Mathf.Abs(followTarget.position.y - pos.y) > 5f)
        {
            SetState(State.Waiting);
            IsWalking = false;
            return;
        }
        if (followTarget != null) targetX = followTarget.position.x; // 고양이의 지금 위치를 계속 따라감
        float dir = Mathf.Sign(targetX - pos.x);
        if (sr != null) sr.flipX = dir < 0f;

        if (Blocked(dir))
        {
            GameLog.Info("주인: 앞이 막혀서 멈춤");
            SetState(State.Waiting);
            IsWalking = false;
            return;
        }
        IsWalking = true;

        // 쫓기는 중이면 고양이를 따라 뛰고(공포도 무시), 평소에는 공포도만큼 느려짐 (최소 Min Fear Speed)
        bool chased = presence != null && presence.isActiveAndEnabled && presence.IsHunting;
        float speed = chased ? runSpeed
                             : moveSpeed * Mathf.Max(minFearSpeed, fear != null ? fear.SpeedMultiplier : 1f);
        float newX = Mathf.MoveTowards(pos.x, targetX, speed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector2(newX, pos.y));

        if (Mathf.Abs(targetX - newX) <= arriveDistance) { SetState(State.Waiting); IsWalking = false; }
    }

    // 벽에 막혔거나 앞에 바닥이 없는가
    private bool Blocked(float dir)
    {
        Bounds b = col.bounds;
        float halfW = b.extents.x;

        // 벽·낮은 천장: 몸 크기(조금 작게)의 상자를 앞쪽으로 밀어서 부딪히는지 확인
        // 고양이 전용 물건(의자)은 벽으로 보지 않고 지나감
        Vector2 boxSize = new Vector2(b.size.x * 0.9f, b.size.y * 0.9f);
        foreach (var hit in Physics2D.BoxCastAll(b.center, boxSize, 0f, Vector2.right * dir, wallCheckDistance, groundLayer))
            if (hit.collider != null && hit.collider != col && !CatOnlyProp.Is(hit.collider)) return true;

        // 낭떠러지: 앞발 위치에서 아래로
        Vector2 foot = new Vector2(b.center.x + dir * (halfW + wallCheckDistance), b.min.y + 0.1f);
        return !Physics2D.Raycast(foot, Vector2.down, ledgeCheckDepth, groundLayer);
    }

    private void SetState(State s)
    {
        CurrentState = s;
        ApplyColor();
        GameLog.Info($"주인 상태: {s}");
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
