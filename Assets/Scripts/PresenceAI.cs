using UnityEngine;

// 존재: 고양이 시야 안에서만 보이고, 눈이 마주치면 쫓아온다
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PresenceAI : MonoBehaviour
{
    public enum State { Lurking, Noticing, Chasing, Pouncing, Returning, Caught }

    [Header("대상")]
    [SerializeField] private CatController cat;
    [SerializeField] private OwnerController owner;

    [Header("그림 (자식 Visual)")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite[] idleFrames;      // presence_idle_01~03
    [SerializeField] private float idleFps = 3f;
    [SerializeField] private Sprite[] noticeFrames;    // presence_notice_01~03
    [SerializeField] private Sprite[] crawlFrames;     // presence_crawl_01~04
    [SerializeField] private float crawlFps = 8f;
    [SerializeField] private Sprite pounceSprite;      // presence_pounce_01
    [SerializeField] private bool spriteFacesRight = true;

    [Header("고양이 시야")]
    [SerializeField] private float sightRadius = 6f;   // 이 거리 안 + 고양이가 바라보는 쪽이면 보임
    [SerializeField] private float nearRadius = 1.5f;  // 이 거리 안이면 등 뒤라도 보임
    [SerializeField] private bool needFacing = true;   // 고양이가 바라보는 방향만 볼 수 있음
    [SerializeField] private LayerMask wallLayer;      // 시야를 가리는 벽 (Ground)
    [SerializeField] private float fadeSpeed = 4f;     // 나타나고 사라지는 속도
    [SerializeField] private bool stayVisibleWhenChasing = true; // 한번 눈이 마주치면 쫓아오는 동안 계속 보임

    [Header("행동")]
    [SerializeField] private float noticeRange = 5f;   // 눈이 마주쳤다고 판단하는 거리
    [SerializeField] private float noticeTime = 1.2f;  // 발견 연출 시간
    [SerializeField] private float chaseSpeed = 3f;    // 고양이 이동속도 5보다 느리게
    [SerializeField] private float pounceRange = 1.8f;
    [SerializeField] private float pounceTime = 0.35f;
    [SerializeField] private float pounceSpeed = 9f;
    [SerializeField] private float pounceHeight = 1.2f; // 고양이가 이보다 높이 있으면 못 덮침
    [SerializeField] private float loseRange = 10f;
    [SerializeField] private float loseTime = 3f;      // 이 시간 동안 멀리 있으면 포기
    [SerializeField] private float returnSpeed = 1.5f;

    [Header("충돌 범위 (발밑 기준)")]
    [SerializeField] private Vector2 standSize = new Vector2(0.9f, 1.8f); // 서 있을 때 (대기·발견)
    [SerializeField] private Vector2 crawlSize = new Vector2(1.8f, 1.0f); // 기어갈 때 (추격·덮치기·복귀)

    public State CurrentState { get; private set; } = State.Lurking;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Collider2D catCol;
    private Collider2D ownerCol;
    private Vector2 home;
    private float stateTimer;
    private float loseTimer;
    private float alpha;
    private int pounceDir;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        ApplySize();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        // 칸이 비어 있으면 씬에서 자동으로 찾음
        if (cat == null) cat = FindAnyObjectByType<CatController>();
        if (owner == null) owner = FindAnyObjectByType<OwnerController>();
        if (cat == null) Debug.LogWarning("PresenceAI: 고양이(CatController)를 찾지 못했어요");
        if (cat != null) catCol = cat.GetComponent<Collider2D>();
        if (owner != null) ownerCol = owner.GetComponent<Collider2D>();
        home = rb.position;
        SetAlpha(0f);
    }

    void Update()
    {
        if (cat == null) return;
        stateTimer += Time.deltaTime;

        // 1) 보이는지 판단해서 투명도 조절
        bool hunting = CurrentState == State.Noticing || CurrentState == State.Chasing
                    || CurrentState == State.Pouncing || CurrentState == State.Caught;
        float target = CatCanSee() || (stayVisibleWhenChasing && hunting) ? 1f : 0f;
        alpha = Mathf.MoveTowards(alpha, target, fadeSpeed * Time.deltaTime);
        SetAlpha(alpha);

        // 2) 상태별 판단
        Vector2 toCat = (Vector2)cat.transform.position - rb.position;
        float dist = toCat.magnitude;

        switch (CurrentState)
        {
            case State.Lurking:
                Play(idleFrames, idleFps);
                if (CatCanSee() && dist <= noticeRange) SetState(State.Noticing);
                break;

            case State.Noticing:
                Face(toCat.x);
                PlayOnce(noticeFrames, noticeTime);
                if (stateTimer >= noticeTime) SetState(State.Chasing);
                break;

            case State.Chasing:
                Face(toCat.x);
                Play(crawlFrames, crawlFps);
                if (Mathf.Abs(toCat.x) <= pounceRange && Mathf.Abs(toCat.y) <= pounceHeight)
                {
                    pounceDir = toCat.x >= 0 ? 1 : -1;
                    SetState(State.Pouncing);
                }
                // 멀어지면 일정 시간 뒤 포기
                loseTimer = dist > loseRange ? loseTimer + Time.deltaTime : 0f;
                if (loseTimer >= loseTime) SetState(State.Returning);
                break;

            case State.Pouncing:
                if (pounceSprite != null) sr.sprite = pounceSprite;
                if (stateTimer >= pounceTime) SetState(State.Chasing);
                break;

            case State.Returning:
                Face(home.x - rb.position.x);
                Play(crawlFrames, crawlFps * 0.6f);
                if (CatCanSee() && dist <= noticeRange) SetState(State.Noticing);
                else if (Mathf.Abs(home.x - rb.position.x) < 0.05f) SetState(State.Lurking);
                break;
        }

        // 3) 닿았는지 확인
        if (CurrentState != State.Caught && CurrentState != State.Returning) CheckCatch();
    }

    void FixedUpdate()
    {
        float speed = 0f;
        float targetX = rb.position.x;

        switch (CurrentState)
        {
            case State.Chasing:   speed = chaseSpeed;  targetX = cat.transform.position.x; break;
            case State.Pouncing:  speed = pounceSpeed; targetX = rb.position.x + pounceDir; break;
            case State.Returning: speed = returnSpeed; targetX = home.x; break;
        }
        if (speed <= 0f) return;

        float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);
        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    // 고양이가 존재를 볼 수 있는가
    public bool CatCanSee()
    {
        if (cat == null) return false;
        Vector2 eye = (Vector2)cat.transform.position + Vector2.up * 0.2f;
        Vector2 me = col.bounds.center;
        Vector2 toMe = me - eye;
        float d = toMe.magnitude;

        if (d > sightRadius) return false;
        if (needFacing && d > nearRadius && Mathf.Sign(toMe.x) != cat.Facing) return false;

        // 벽에 가려지면 안 보임
        RaycastHit2D hit = Physics2D.Linecast(eye, me, wallLayer);
        return hit.collider == null;
    }

    private void CheckCatch()
    {
        if (catCol != null && col.bounds.Intersects(catCol.bounds))
            Caught("고양이");
        else if (ownerCol != null && col.bounds.Intersects(ownerCol.bounds))
            Caught("주인");
    }

    private void Caught(string who)
    {
        SetState(State.Caught);
        if (pounceSprite != null) sr.sprite = pounceSprite;
        alpha = 1f;          // 숨어 있다가 잡아도 모습이 보이게
        SetAlpha(1f);
        Debug.Log($"존재에게 {who}이(가) 잡혔다!");

        if (GameManager.Instance != null) GameManager.Instance.GameOver($"{who}이(가) 잡힘");
        else Debug.LogWarning("GameManager가 씬에 없어서 게임오버를 할 수 없어요");
    }

    private void SetState(State s)
    {
        CurrentState = s;
        stateTimer = 0f;
        loseTimer = 0f;
        ApplySize();
        Debug.Log($"존재 상태: {s}");
    }

    // 서 있을 때와 기어갈 때 충돌 상자 크기를 바꿈
    private void ApplySize()
    {
        bool standing = CurrentState == State.Lurking || CurrentState == State.Noticing;
        Vector2 size = standing ? standSize : crawlSize;
        col.size = size;
        col.offset = new Vector2(0f, size.y / 2f);
    }

    private void Face(float dirX)
    {
        if (Mathf.Abs(dirX) < 0.01f) return;
        bool right = dirX > 0f;
        sr.flipX = spriteFacesRight ? !right : right;
    }

    private void Play(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;
        int i = (int)(stateTimer * fps) % frames.Length;
        sr.sprite = frames[i];
    }

    // 정해진 시간 동안 한 번만 재생 (마지막 장에서 멈춤)
    private void PlayOnce(Sprite[] frames, float duration)
    {
        if (frames == null || frames.Length == 0) return;
        int i = Mathf.Min((int)(stateTimer / duration * frames.Length), frames.Length - 1);
        sr.sprite = frames[i];
    }

    private void SetAlpha(float a)
    {
        if (sr == null) return;
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, noticeRange);
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, pounceRange);
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseRange);
    }
}
