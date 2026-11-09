using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// 존재: 맵을 알아서 돌아다니다가, 고양이와 눈이 마주치면 쫓아온다
// Ambush Mode(기본 켜짐): 평소엔 사라져 있다가 잠시 뒤 고양이 근처(같은 층, 앞이나 뒤)에 튀어나와
//   고양이가 있던 자리로 천천히 기어간다. 눈이 마주치면 쫓아오고, 못 찾거나 놓치면 다시 사라진다
// 순찰 지점(Patrol Points)을 넣으면 그 지점들만 오가고,
// 비워두면 씬의 방(Room)들 중 하나를 골라 알아서 돌아다닌다 (안전한 방 제외)
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PresenceAI : MonoBehaviour
{
    // Sweeping = 전원 이벤트에서 지하를 훑고 지나감, Dropping·Roaring·Hunting = 추격 이벤트 (천장에서 떨어짐 → 울부짖음 → 끝까지 쫓아옴)
    // Investigating = 쫓다가 숨어버린 자리(마지막으로 본 곳)까지 가서 찾아봄 → 못 찾으면 사라짐
    // Vanished = 사라져 있음 (Ambush Mode, 보이지 않고 잡지도 않음), Emerging = 그 자리에서 천천히 모습을 드러내는 중
    // Glimpse = 깜짝 등장 (고양이 앞에 뒷모습으로 가만히 서 있다가, 다가가면 웃음소리와 함께 사라짐. 쫓아오지도 잡지도 않음)
    public enum State { Lurking, Patrolling, Noticing, Chasing, Pouncing, Searching, Returning, Caught, Sweeping, Dropping, Roaring, Hunting, Vanished, Emerging, Investigating, Glimpse }

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
    [SerializeField] private bool alwaysVisible = true;  // 켜면 고양이가 안 봐도 사라지지 않고 항상 보임

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
    [SerializeField] private float searchTime = 2.5f;  // 숨어버리면 이 시간 동안 두리번거리다 포기
    // 쫓을 때: 가까우면 Chase Speed, 멀어지면 빨라져서 화면 밖으로 놓치지 않음 (가까운 쪽 = 고양이·주인 중 가까운 쪽)
    [SerializeField] private float chaseKeepDistance = 5f;
    [SerializeField] private float chaseCatchUp = 0.6f;    // 멀어진 칸당 이만큼 빨라짐
    [SerializeField] private float chaseMaxSpeed = 5.2f;   // 고양이(5)와 비슷 → 계속 달아나도 화면 끝에 붙어 따라옴

    [Header("돌아다니기")]
    [SerializeField] private bool autoRoam = true;       // 순찰 지점이 비어 있으면 방들을 알아서 돌아다님
    [SerializeField] private string[] safeRoomNames = { "1번 방" }; // 존재가 절대 들어가지 않는 방 (Room의 Room Name)
    // 갑자기 나타나지는 않지만, 쫓아서 들어오거나 숨은 곳을 찾아 들어올 수는 있는 방
    [SerializeField] private string[] noAppearRoomNames = { "2번 방" };
    // 연출만 있는 방 (지하): 갑자기 나타나거나 쫓아오거나 찾아오지 않음. 깜짝 등장·전원 이벤트만
    [SerializeField] private string[] eventOnlyRoomNames = { "6번 방", "7번 방" };
    [SerializeField] private float roamMargin = 1f;      // 방 끝에서 이만큼 떨어진 곳까지만 목적지로 고름
    [SerializeField, Range(0f, 1f)] private float stayInRoomChance = 0.4f; // 같은 방 안에서 다른 곳으로 갈 확률
    [SerializeField] private float patrolSpeed = 1.2f;  // 천천히 기어 다님
    [SerializeField] private float patrolWait = 2f;     // 목적지에 도착하면 이만큼 멈춰 있음

    [Header("숨는 걸 들킴 (존재가 보는 앞에서 숨으면 숨을 곳까지 쫓아와 잡음)")]
    [SerializeField] private float seeHideRange = 7f;    // 이 거리 안에서, 존재가 바라보는 쪽에서 숨으면 들킴
    [SerializeField] private string exposedMessage = "들켰다…!";

    [Header("숨었다가 튀어나오기 (켜면 맵을 계속 돌아다니지 않음)")]
    [SerializeField] private bool ambushMode = true;
    [SerializeField] private float firstAppearDelay = 40f;                    // 게임 시작 후 처음 나타나기까지
    [SerializeField] private Vector2 appearGap = new Vector2(20f, 35f);       // 사라진 뒤 다시 나타나기까지 (최소~최대 초)
    [SerializeField] private Vector2 appearDistance = new Vector2(6f, 9f);    // 고양이에게서 이만큼 떨어진 곳에 나타남
    [SerializeField, Range(0f, 1f)] private float appearBehindChance = 0.6f;  // 고양이 등 뒤에 나타날 확률
    [SerializeField] private float emergeTime = 2.5f;                         // 나타날 때 천천히 드러나는 시간 (그동안 움직이지도 잡지도 않음, 심장 소리로 먼저 알 수 있음)
    [SerializeField] private float stalkTime = 15f;                           // 나타난 뒤 못 찾으면 이 시간 뒤 사라짐
    [SerializeField] private AudioClip appearClip;                            // 나타날 때 소리 (비워두면 없음, 심장 소리로 알 수 있음)
    [SerializeField, Range(0f, 1f)] private float appearVolume = 0.6f;

    [Header("깜짝 등장 (고양이 앞에 잠깐 보였다 사라짐, 쫓아오지 않음)")]
    // 깜짝 등장은 정해진 자리(GlimpseTrigger)에서 나옴. 아래 확률을 올리면 무작위로도 나옴
    [SerializeField, Range(0f, 1f)] private float glimpseChance = 0f;           // 나타날 때 깜짝 등장으로 바뀔 확률 (두 번 연속은 없음)
    [SerializeField] private Vector2 glimpseDistance = new Vector2(5.5f, 6.5f); // 고양이 앞쪽 이 거리에 뒷모습으로 서 있음
    [SerializeField] private float glimpseVanishDistance = 3f;                  // 고양이가 이만큼 다가오면 웃으며 사라짐
    [SerializeField] private float glimpseMaxTime = 10f;                        // 다가오지 않아도 이 시간 뒤 사라짐
    [SerializeField] private Vector2 glimpseGap = new Vector2(10f, 18f);         // 깜짝 등장 뒤 진짜로 나타나기까지
    [SerializeField] private AudioClip glimpseClip;                              // 사라질 때 웃음소리 (비워두면 없음)
    [SerializeField, Range(0f, 1f)] private float glimpseVolume = 0.8f;

    [Header("숨은 자리 찾아오기 (숨으면 어디든 숨은 곳 바로 앞까지 와서 노려보다 사라짐)")]
    [SerializeField] private float hideStopDistance = 0.9f;   // 숨은 곳 이만큼 앞에서 멈춤
    [SerializeField] private float vanishFadeSpeed = 1.2f;    // 사라질 때 흐려지는 속도 (낮을수록 천천히)

    [Header("어둠 속에서 희미하게 보이기 (깜짝 등장·숨은 곳 찾아올 때)")]
    [SerializeField] private Color glowColor = new Color(0.7f, 0.78f, 0.95f);
    [SerializeField] private float glowRadius = 2f;
    [SerializeField] private float glowIntensity = 0.55f;

    [Header("순찰 지점 (넣으면 이 지점들만 오감, 비워두면 알아서 돌아다님)")]
    [SerializeField] private Transform[] patrolPoints;  // 순서대로 오가는 지점들 (여러 방에 걸쳐도 됨)
    [SerializeField] private bool pingPong = true;      // 끝에 가면 되돌아옴 (끄면 처음으로 반복)

    [Header("추격 이벤트 (ChaseEvent)")]
    [SerializeField] private float dropTime = 0.6f;    // 천장에서 떨어지는 시간
    [SerializeField] private float roarTime = 2f;      // 떨어진 뒤 움직이기 전까지 (도망칠 시간)

    [Header("소리 (비워두면 소리 없음)")]
    [SerializeField] private AudioClip noticeClip;     // 고양이를 발견했을 때, 추격 이벤트에서 울부짖을 때
    [SerializeField, Range(0f, 1f)] private float noticeVolume = 0.9f;
    [SerializeField] private AudioClip crawlClip;      // 기어다니는 소리 (반복). 같은 층에서 가까울수록 크게, 왼쪽·오른쪽 방향도 들림
    [SerializeField, Range(0f, 1f)] private float crawlVolume = 0.7f;
    [SerializeField] private float crawlHearDistance = 12f;
    [SerializeField] private AudioClip stairsClip;     // 계단을 오르내리는 소리 (고양이 계단 소리와 같은 파일)
    [SerializeField, Range(0f, 1f)] private float stairsVolume = 0.7f;

    [Header("마지막 추격 (Hunting): 주인·고양이 뒤를 아슬아슬한 거리로 따라옴")]
    [SerializeField] private float huntKeepDistance = 3.5f;   // 이 거리를 유지하려고 함 (멈추면 따라잡혀 잡힘)
    [SerializeField] private float huntPaceSpeed = 3.4f;      // 유지 거리일 때 속도 (주인이 뛰는 속도 3.6보다 조금 느리게)
    [SerializeField] private float huntMinSpeed = 1.4f;
    [SerializeField] private float huntMaxSpeed = 6f;
    [SerializeField] private float huntCatchUp = 1.5f;        // 멀어지면 이만큼씩 빨라짐 (칸당)

    [Header("계단")]
    [SerializeField] private float stairsTime = 1.2f;  // 계단을 오르내리는 동안 (안 보이고 잡지도 않음)

    [Header("충돌 범위 (발밑 기준)")]
    [SerializeField] private Vector2 standSize = new Vector2(0.9f, 1.8f); // 서 있을 때 (대기·발견)
    [SerializeField] private Vector2 crawlSize = new Vector2(1.8f, 1.0f); // 기어갈 때 (추격·덮치기·복귀)

    public State CurrentState { get; private set; } = State.Lurking;
    public bool IsPresent => CurrentState != State.Vanished;   // 맵에 나와 있는가 (심장 소리용)
    // 쫓아오는 중인가 (주인이 이때는 뛰어서 따라옴)
    public bool IsHunting => CurrentState == State.Noticing || CurrentState == State.Chasing || CurrentState == State.Pouncing
                          || CurrentState == State.Roaring || CurrentState == State.Hunting;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Collider2D catCol;
    private Collider2D ownerCol;
    private Vector2 home;
    private float stateTimer;
    private float loseTimer;
    private float alpha;
    private int pounceDir;
    private int patrolIndex;
    private int patrolStep = 1;

    private readonly List<Room> roamRooms = new List<Room>();
    private float roamX;
    private Room roamRoom;          // 목적지 방 (다른 층이면 계단을 타고 감)
    private Stairs routeStairs;     // 지금 향하는 계단 (없으면 null)
    private int glimpseDir;         // 깜짝 등장: 바라보는 쪽 (고양이 반대쪽 = 뒷모습)
    private bool glimpseLeaving;    // 웃으며 사라지는 중
    private float glimpseLeaveTimer;
    private Light2D glow;
    private bool lastWasGlimpse;    // 깜짝 등장이 두 번 연속 나오지 않게
    private float glimpseReturnWait; // 깜짝 등장 뒤 다시 나타나기까지

    // 이벤트용
    private bool paused;
    private bool relentless;        // 추격 이벤트: 포기하지 않고 층을 넘어 쫓아옴
    private float[] sweepXs;
    private int sweepIndex;
    private float sweepSpeed;
    private System.Action onSweepDone;
    private Vector2 dropFrom, dropLand;
    private float huntFloorY;
    private float huntSpeed;
    private float vanishWait;       // 사라진 뒤 이 시간이 지나면 나타남
    private float appearedAt;       // 마지막으로 나타난 시각
    private bool exposed;           // 보는 앞에서 숨어서 들킴 (숨어도 잡힘)
    private bool wasHidden;
    private Vector2 lastKnown;
    private AudioSource crawlSource;
    private float climbTimer;       // 계단 이용 중 남은 시간 (0보다 크면 이용 중)
    private Vector2 climbTo;

    private bool HasPatrol => patrolPoints != null && patrolPoints.Length > 0;
    private bool HasRoute => HasPatrol || (autoRoam && roamRooms.Count > 0);

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        if (crawlClip != null)
        {
            crawlSource = gameObject.AddComponent<AudioSource>();
            crawlSource.clip = crawlClip;
            crawlSource.loop = true;
            crawlSource.playOnAwake = false;
            crawlSource.spatialBlend = 0f;   // 크기·방향은 코드로 (UpdateCrawlSound)
            crawlSource.volume = 0f;
        }
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
        if (wallLayer.value == 0) wallLayer = LayerMask.GetMask("Ground");
        home = rb.position;
        alpha = alwaysVisible ? 1f : 0f;
        SetAlpha(alpha);
    }

    void Start()
    {
        // 돌아다닐 방 목록 (안전한 방 제외)
        foreach (var room in FindObjectsByType<Room>(FindObjectsSortMode.None))
            if (System.Array.IndexOf(safeRoomNames, room.RoomName) < 0 && System.Array.IndexOf(noAppearRoomNames, room.RoomName) < 0)
                roamRooms.Add(room);

        glow = CatVision.AttachGlow(transform, new Vector3(0f, 1f, 0f), glowColor, glowRadius);

        if (ambushMode) { alpha = 0f; SetAlpha(0f); Vanish(firstAppearDelay); }   // 처음엔 숨어 있음
        else if (HasRoute) { PickNextTarget(true); SetState(State.Patrolling); }
        GameLog.Info($"존재: 돌아다닐 방 {roamRooms.Count}개, 순찰 지점 {(HasPatrol ? patrolPoints.Length : 0)}개");
    }

    void Update()
    {
        if (cat == null || paused) { UpdateCrawlSound(false); return; }
        stateTimer += Time.deltaTime;

        // 계단을 오르내리는 중: 안 보이고, 잡지도 않고, 끝나면 연결된 계단 앞에 나타남
        if (climbTimer > 0f)
        {
            climbTimer -= Time.deltaTime;
            alpha = Mathf.MoveTowards(alpha, 0f, fadeSpeed * 2f * Time.deltaTime);
            SetAlpha(alpha);
            UpdateCrawlSound(false);
            if (climbTimer <= 0f) MoveTo(climbTo);
            return;
        }

        // 1) 보이는지 판단해서 투명도 조절
        bool hunting = CurrentState == State.Noticing || CurrentState == State.Chasing
                    || CurrentState == State.Pouncing || CurrentState == State.Searching
                    || CurrentState == State.Caught;
        bool rawHidden = HideController.IsHidden;
        // 존재가 보는 앞에서 숨으면 들킴 → 숨을 곳까지 쫓아옴
        if (rawHidden && !wasHidden && SawHiding())
        {
            exposed = true;
            if (HintUI.Instance != null) HintUI.Instance.ShowMessage(exposedMessage, 1.5f);
            if (CurrentState != State.Chasing && CurrentState != State.Pouncing && CurrentState != State.Hunting)
                SetState(State.Noticing);
            GameLog.Info("존재: 숨는 걸 봤다");
        }
        if (!rawHidden) exposed = false;
        wasHidden = rawHidden;
        bool hidden = rawHidden && !exposed;   // 들키면 숨지 않은 것과 같음
        if (!rawHidden) lastKnown = cat.transform.position;   // 숨기 전 마지막 위치 (숨은 곳)
        float target = CurrentState == State.Vanished ? 0f
                     : CurrentState == State.Emerging ? Mathf.Clamp01(stateTimer / emergeTime)
                     : CurrentState == State.Glimpse ? (glimpseLeaving ? 0f : 1f)
                     : alwaysVisible || CatCanSee() || (stayVisibleWhenChasing && hunting) ? 1f : 0f;
        alpha = Mathf.MoveTowards(alpha, target, (CurrentState == State.Vanished ? vanishFadeSpeed : fadeSpeed) * Time.deltaTime);
        SetAlpha(alpha);

        // 숨었으면 어디든(다른 층·안전한 방이어도) 숨은 자리 바로 앞까지 찾아와서 노려보다가 사라짐
        // (숨어 있으면 잡히지 않음)
        if (ambushMode && !relentless && hidden && !InEventOnlyRoom(lastKnown)
            && (CurrentState == State.Patrolling || CurrentState == State.Lurking || CurrentState == State.Returning))
            SetState(State.Investigating);
        // 지하(연출만 있는 방)에 숨었거나 그쪽 소리를 들었으면 찾아가지 않고 포기
        if (!relentless && (CurrentState == State.Investigating || CurrentState == State.Searching) && InEventOnlyRoom(lastKnown))
        {
            GiveUp();
            return;
        }

        // 무언가를 만드는 동안(퓨즈 끼우기·열쇠 붙이기)에는 새로 나타나거나 다가오지 않고 사라짐
        // 단, 이미 쫓고 있던 중(발견·추격·덮치기·숨은 곳 찾기)이면 그대로 쫓아옴
        if (CraftUI.IsCrafting && ambushMode && !relentless
            && (CurrentState == State.Patrolling || CurrentState == State.Lurking || CurrentState == State.Emerging
                || CurrentState == State.Returning || CurrentState == State.Glimpse))
        {
            Vanish(RandomGap());
            return;
        }

        // 어둠 속에서도 희미하게 보이게 (깜짝 등장, 숨은 곳을 찾아올 때)
        if (glow != null)
        {
            bool glowOn = CurrentState == State.Glimpse
                       || (rawHidden && (CurrentState == State.Investigating || CurrentState == State.Searching));
            glow.intensity = glowOn ? glowIntensity * alpha : 0f;
        }

        // 2) 상태별 판단
        Vector2 toCat = (Vector2)cat.transform.position - rb.position;
        float dist = toCat.magnitude;

        // 나타난 뒤 오래 못 찾으면 사라짐
        if (ambushMode && !relentless && (CurrentState == State.Patrolling || CurrentState == State.Lurking)
            && Time.time - appearedAt > stalkTime)
        {
            Vanish(RandomGap());
            return;
        }

        switch (CurrentState)
        {
            case State.Vanished:
                if (stateTimer >= vanishWait) TryAppear();
                break;

            case State.Glimpse:
                // 깜짝 등장: 뒷모습으로 가만히 서 있음 → 고양이가 다가오면 웃음소리와 함께 사라짐 (발견·잡기 없음)
                Face(glimpseDir);
                Play(idleFrames, idleFps);
                if (!glimpseLeaving)
                {
                    bool near = Stairs.SameFloor(cat.transform.position.y, rb.position.y) && Mathf.Abs(toCat.x) <= glimpseVanishDistance;
                    if (near || stateTimer >= glimpseMaxTime)
                    {
                        glimpseLeaving = true;
                        glimpseLeaveTimer = 0f;
                        Sfx.Play(glimpseClip, glimpseVolume);
                    }
                }
                else if ((glimpseLeaveTimer += Time.deltaTime) >= 0.5f)
                {
                    lastWasGlimpse = true;
                    Vanish(glimpseReturnWait);
                }
                break;

            case State.Emerging:
                // 제자리에서 천천히 드러남 → 다 드러나면 고양이가 있던 자리로 기어감
                alpha = Mathf.Clamp01(stateTimer / emergeTime);
                SetAlpha(alpha);
                Play(idleFrames, idleFps);
                if (stateTimer >= emergeTime) SetState(State.Patrolling);
                break;

            case State.Lurking:
                Play(idleFrames, idleFps);
                if (CanNotice(hidden, dist)) SetState(State.Noticing);
                else if (stateTimer >= patrolWait)
                {
                    if (ambushMode && !relentless) Vanish(RandomGap());   // 고양이가 있던 자리에 왔는데 없음 → 사라짐
                    else if (HasRoute) { PickNextTarget(false); SetState(State.Patrolling); }
                }
                break;

            case State.Patrolling:
                // 천천히 기어서 다음 목적지로 (다른 층이면 먼저 계단으로)
                float goX = TargetX();
                Face(goX - rb.position.x);
                Play(crawlFrames, crawlFps * 0.5f);
                if (CanNotice(hidden, dist)) SetState(State.Noticing);
                else if (Mathf.Abs(goX - rb.position.x) < 0.05f)
                {
                    if (routeStairs != null) TakeStairs(routeStairs);
                    else SetState(State.Lurking);
                }
                break;

            case State.Noticing:
                if (hidden) { SetState(State.Investigating); break; }
                Face(toCat.x);
                PlayOnce(noticeFrames, noticeTime);
                if (stateTimer >= noticeTime) SetState(State.Chasing);
                break;

            case State.Chasing:
            {
                if (hidden) { SetState(State.Investigating); break; }   // 숨어버림 → 마지막으로 본 자리까지 가서 찾아봄
                // 고양이가 다른 층으로 가면 계단으로 따라감
                float gx = HuntTargetX();
                Face(gx - rb.position.x);
                Play(crawlFrames, crawlFps);
                if (routeStairs == null && Mathf.Abs(toCat.x) <= pounceRange && Mathf.Abs(toCat.y) <= pounceHeight)
                {
                    pounceDir = toCat.x >= 0 ? 1 : -1;
                    SetState(State.Pouncing);
                }
                else if (routeStairs != null && Mathf.Abs(gx - rb.position.x) < 0.05f) { TakeStairs(routeStairs); break; }
                // 고양이가 지하(연출만 있는 방)로 도망치면 따라가지 않고 포기
                if (InEventOnlyRoom(cat.transform.position)) { GiveUp(); break; }
                // 같은 층에서 멀어지면 일정 시간 뒤 포기 (다른 층이면 계단으로 쫓아가는 중이라 세지 않음)
                bool sameFloor = Stairs.SameFloor(cat.transform.position.y, rb.position.y);
                loseTimer = sameFloor && dist > loseRange ? loseTimer + Time.deltaTime : 0f;
                if (loseTimer >= loseTime) GiveUp();
                break;
            }

            case State.Pouncing:
                if (pounceSprite != null) sr.sprite = pounceSprite;
                if (stateTimer >= pounceTime) SetState(relentless ? State.Hunting : hidden ? State.Investigating : State.Chasing);
                break;

            case State.Sweeping:
            {
                // 정해진 지점들을 차례로 훑고 지나감 (숨지 않은 쪽은 닿으면 잡힘)
                float gx = sweepXs[sweepIndex];
                Face(gx - rb.position.x);
                Play(crawlFrames, crawlFps * 0.7f);
                if (Mathf.Abs(gx - rb.position.x) < 0.05f && ++sweepIndex >= sweepXs.Length) FinishSweep();
                break;
            }

            case State.Dropping:
            {
                float k = Mathf.Clamp01(stateTimer / dropTime);
                MoveTo(Vector2.Lerp(dropFrom, dropLand, k * k));
                if (pounceSprite != null) sr.sprite = pounceSprite;
                if (k >= 1f) SetState(State.Roaring);
                break;
            }

            case State.Roaring:
                Face(toCat.x);
                PlayOnce(noticeFrames, roarTime);
                if (stateTimer >= roarTime)
                {
                    MoveTo(new Vector2(rb.position.x, huntFloorY));   // 수술대에서 바닥으로 내려옴
                    SetState(State.Hunting);
                }
                break;

            case State.Hunting:
            {
                float gx = HuntTargetX();
                Face(gx - rb.position.x);
                Play(crawlFrames, crawlFps);
                if (routeStairs == null && Mathf.Abs(toCat.x) <= pounceRange && Mathf.Abs(toCat.y) <= pounceHeight)
                {
                    pounceDir = toCat.x >= 0 ? 1 : -1;
                    SetState(State.Pouncing);
                }
                else if (routeStairs != null && Mathf.Abs(gx - rb.position.x) < 0.05f) TakeStairs(routeStairs);
                break;
            }

            case State.Investigating:
            {
                // 마지막으로 본 자리까지 (다른 층이면 계단으로) → 도착하면 두리번거리다 포기
                float gx = InvestigateTargetX();
                Face(gx - rb.position.x);
                Play(crawlFrames, crawlFps * 0.8f);
                if (CanNotice(hidden, dist)) SetState(State.Noticing);
                else if (routeStairs != null && Mathf.Abs(gx - rb.position.x) < 0.05f) TakeStairs(routeStairs);
                else if (routeStairs == null && Mathf.Abs(gx - rb.position.x) < 0.1f) SetState(State.Searching);
                else if (stateTimer > 40f) GiveUp();   // 너무 오래 걸리면 포기
                break;
            }

            case State.Searching:
                // 고양이를 놓침: 숨었으면 숨은 곳을 노려보고, 아니면 제자리에서 좌우를 두리번거림
                Play(idleFrames, idleFps);
                if (rawHidden) Face(lastKnown.x - rb.position.x);
                else Face(Mathf.Sin(stateTimer * 2.5f));
                if (CanNotice(hidden, dist)) SetState(State.Noticing);
                else if (stateTimer >= searchTime) GiveUp();
                break;

            case State.Returning:
                Face(home.x - rb.position.x);
                Play(crawlFrames, crawlFps * 0.6f);
                if (CanNotice(hidden, dist)) SetState(State.Noticing);
                else if (Mathf.Abs(home.x - rb.position.x) < 0.05f) SetState(State.Lurking);
                break;
        }

        UpdateCrawlSound(true);

        // 3) 닿았는지 확인
        if (CurrentState != State.Caught && CurrentState != State.Returning && CurrentState != State.Vanished && CurrentState != State.Emerging
            && CurrentState != State.Dropping && CurrentState != State.Roaring && CurrentState != State.Glimpse) CheckCatch();
    }

    void FixedUpdate()
    {
        if (paused || cat == null || climbTimer > 0f) return;
        float speed = 0f;
        float targetX = rb.position.x;

        switch (CurrentState)
        {
            case State.Chasing:   targetX = HuntTargetX(); speed = ChasePace(); break;
            case State.Pouncing:  speed = pounceSpeed; targetX = rb.position.x + pounceDir; break;
            case State.Returning: speed = returnSpeed; targetX = home.x; break;
            case State.Patrolling: speed = patrolSpeed; targetX = TargetX(); break;
            case State.Sweeping:
                speed = sweepSpeed;
                targetX = sweepXs[Mathf.Min(sweepIndex, sweepXs.Length - 1)];
                break;
            case State.Hunting:
                targetX = HuntTargetX();
                speed = HuntPace() * (stateTimer < 1f ? 0.5f : 1f);   // 처음 1초는 천천히 (덮친 뒤에도)
                break;
            case State.Investigating: speed = chaseSpeed * 0.8f; targetX = InvestigateTargetX(); break;
        }
        if (speed <= 0f) return;

        float newX = Mathf.MoveTowards(rb.position.x, targetX, speed * Time.fixedDeltaTime);

        // 안전한 방(1·2번 방)에는 절대 들어가지 않음: 문턱에서 멈추고, 쫓던 중이면 놓침
        float dir = Mathf.Sign(newX - rb.position.x);
        // 단, 숨은 곳을 찾아가는 중(고양이가 숨어 있음)에는 숨은 곳 앞까지 들어감
        bool visitingHide = CurrentState == State.Investigating && HideController.IsHidden;
        if (dir != 0f && !visitingHide && InSafeRoom(new Vector2(newX + dir * 0.8f, rb.position.y)))
        {
            switch (CurrentState)
            {
                case State.Chasing: case State.Pouncing: case State.Investigating:
                    SetState(State.Searching);   // 문턱에서 두리번거리다 포기
                    break;
                case State.Patrolling:
                    PickNextTarget(true);        // 다른 곳으로
                    break;
            }
            return;                              // 추격 이벤트(Hunting) 중에도 문턱에서 기다림
        }
        rb.MovePosition(new Vector2(newX, rb.position.y));
    }

    private bool InEventOnlyRoom(Vector2 p)
    {
        Room r = Room.Find(p + Vector2.up * 0.5f);
        return r != null && System.Array.IndexOf(eventOnlyRoomNames, r.RoomName) >= 0;
    }

    private bool InSafeRoom(Vector2 p)
    {
        Room r = Room.Find(p + Vector2.up * 0.5f);
        return r != null && System.Array.IndexOf(safeRoomNames, r.RoomName) >= 0;
    }

    // 고양이를 발견할 수 있는가: 숨지 않았고, 고양이 시야에 있고, 가깝고, 같은 방 (다른 방에서는 못 봄 → 그냥 지나감)
    private bool CanNotice(bool hidden, float dist)
    {
        return !hidden && dist <= noticeRange && SameRoomAsCat() && CatCanSee() && !InSafeRoom(rb.position);
    }

    private bool SameRoomAsCat()
    {
        Room rc = Room.Find(cat.transform.position);
        Room rp = Room.Find(rb.position + Vector2.up * 0.5f);
        return rc != null && rc == rp;
    }

    // 마지막으로 본 자리 (다른 층이면 그 층으로 가는 계단)
    private float InvestigateTargetX()
    {
        routeStairs = null;
        if (Stairs.SameFloor(lastKnown.y, rb.position.y))
        {
            // 바로 위가 아니라 눈앞(조금 앞)에서 멈춤
            float side = rb.position.x >= lastKnown.x ? 1f : -1f;
            return lastKnown.x + side * hideStopDistance;
        }
        routeStairs = Stairs.FindRoute(rb.position, lastKnown.y);
        return routeStairs != null ? routeStairs.X : rb.position.x;
    }

    // 쫓는 속도: 가까운 쪽(고양이·주인)이 Chase Keep Distance보다 멀면 빨라짐
    // 가까이 있을 때는 Chase Speed(3)라 뛰는 주인(3.6)은 따라잡지 못함, 대신 화면 밖으로 놓치지도 않음
    private float ChasePace()
    {
        if (routeStairs != null) return chaseMaxSpeed;   // 다른 층 → 계단까지 서두름
        float d = Mathf.Abs(cat.transform.position.x - rb.position.x);
        if (owner != null && Stairs.SameFloor(owner.transform.position.y, rb.position.y))
            d = Mathf.Min(d, Mathf.Abs(owner.transform.position.x - rb.position.x));
        return Mathf.Clamp(chaseSpeed + (d - chaseKeepDistance) * chaseCatchUp, chaseSpeed, chaseMaxSpeed);
    }

    // 마지막 추격 속도: 가장 가까운 쪽(고양이·주인)과 Hunt Keep Distance를 유지하려고 함
    private float HuntPace()
    {
        if (routeStairs != null) return huntMaxSpeed;   // 다른 층 → 계단까지 서둘러
        float d = Mathf.Abs(cat.transform.position.x - rb.position.x);
        if (owner != null && Stairs.SameFloor(owner.transform.position.y, rb.position.y))
            d = Mathf.Min(d, Mathf.Abs(owner.transform.position.x - rb.position.x));
        return Mathf.Clamp(huntPaceSpeed + (d - huntKeepDistance) * huntCatchUp, huntMinSpeed, huntMaxSpeed);
    }

    // 소리를 듣고 찾아옴 (주인의 비명 등). 쫓는 중·이벤트 중에는 무시
    public void HearNoise(Vector2 pos)
    {
        if (relentless || paused || InEventOnlyRoom(pos)) return;   // 지하에서 난 소리는 무시 (연출만)
        switch (CurrentState)
        {
            case State.Noticing: case State.Chasing: case State.Pouncing: case State.Caught:
            case State.Sweeping: case State.Dropping: case State.Roaring: case State.Hunting: case State.Glimpse:
                return;
        }
        if (CurrentState == State.Vanished || CurrentState == State.Emerging)
        {
            // 소리 난 곳 근처(같은 층)에서 나타남
            if (FindAppearSpot(pos, out Vector2 spot))
            {
                MoveTo(spot);
                alpha = 0f;
                SetAlpha(0f);
            }
        }
        appearedAt = Time.time;          // 찾아오는 동안은 사라지지 않게
        lastKnown = pos;
        SetState(State.Investigating);   // 소리 난 자리까지 와서 찾아봄
        GameLog.Info($"존재: 소리를 들었다 (x {pos.x:0.0})");
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

    // ---------- 이벤트용 (PowerEvent · ChaseEvent가 호출) ----------

    // 연출 중 멈춤
    public void SetPaused(bool p) { paused = p; }

    // 지하 훑기: start에 나타나 xs 지점들을 차례로 지나간 뒤 계단으로 올라감
    public void StartSweep(Vector2 start, float[] xs, float speed, System.Action done)
    {
        relentless = false;
        MoveTo(start);
        PlayStairsSound(start, start, true);   // 계단을 내려오는 소리 (이벤트라 항상 들림)
        sweepXs = xs;
        sweepIndex = 0;
        sweepSpeed = speed;
        onSweepDone = done;
        alpha = 1f;
        SetAlpha(1f);
        SetState(State.Sweeping);
        GameLog.Info("존재: 지하를 훑기 시작");
    }

    private void FinishSweep()
    {
        var cb = onSweepDone;
        onSweepDone = null;
        // 바로 옆의 계단으로 올라감
        Stairs up = Stairs.FindRoute(rb.position, rb.position.y + 20f);
        if (up != null && Mathf.Abs(up.X - rb.position.x) < 1.5f) TakeStairs(up);
        if (ambushMode) Vanish(RandomGap());
        else if (HasRoute) { PickNextTarget(true); SetState(State.Patrolling); }
        else SetState(State.Lurking);
        cb?.Invoke();
    }

    // 추격: from에서 land로 떨어진 뒤 울부짖고, 바닥(floorY)으로 내려와 끝까지 쫓아옴
    public void StartHunt(Vector2 from, Vector2 land, float floorY, float speed)
    {
        relentless = true;
        dropFrom = from;
        dropLand = land;
        huntFloorY = floorY;
        huntSpeed = speed;
        MoveTo(from);
        alpha = 1f;
        SetAlpha(1f);
        SetState(State.Dropping);
        GameLog.Info("존재: 추격 시작");
    }

    // 추격 목표: 같은 층이면 고양이, 다른 층이면 고양이 층으로 가는 계단
    private float HuntTargetX()
    {
        routeStairs = null;
        Vector2 c = cat.transform.position;
        if (Stairs.SameFloor(c.y, rb.position.y)) return c.x;
        routeStairs = Stairs.FindRoute(rb.position, c.y);
        return routeStairs != null ? routeStairs.X : rb.position.x;
    }

    private void MoveTo(Vector2 p)
    {
        climbTimer = 0f;
        rb.position = p;
        transform.position = p;
    }

    // 고양이를 놓쳤을 때: 다시 돌아다니기 (경로가 없으면 원래 자리로)
    private void GiveUp()
    {
        if (ambushMode && !relentless) { Vanish(RandomGap()); return; }
        if (HasRoute) { PickNextTarget(true); SetState(State.Patrolling); }
        else SetState(State.Returning);
    }

    // 지금 향하는 목적지 X (목적지 방이 다른 층이면 이 층의 계단 X)
    private float TargetX()
    {
        routeStairs = null;
        if (HasPatrol)
        {
            var p = patrolPoints[patrolIndex];
            return p != null ? p.position.x : rb.position.x;
        }
        if (roamRoom != null && !Stairs.SameFloor(roamRoom.CenterY - 2f, rb.position.y))
        {
            routeStairs = Stairs.FindRoute(rb.position, roamRoom.CenterY - 2f);
            if (routeStairs != null) return routeStairs.X;
        }
        return roamX;
    }

    // 계단을 타고 다른 층으로 (존재는 잠금과 상관없이 통과)
    private void TakeStairs(Stairs s)
    {
        if (s.Linked == null) { routeStairs = null; return; }
        climbTo = new Vector2(s.Linked.X, s.Linked.FloorY);
        climbTimer = stairsTime;
        routeStairs = null;
        PlayStairsSound(rb.position, climbTo);
        GameLog.Info($"존재: 계단으로 이동 ({s.name} → {s.Linked.name})");
    }

    // 계단 소리: 고양이가 출발 쪽이나 도착 쪽 층에 있으면 가까울수록 크게
    private void PlayStairsSound(Vector2 from, Vector2 to, bool always = false)
    {
        if (stairsClip == null || cat == null) return;
        if (always) { Sfx.Play(stairsClip, stairsVolume); return; }
        Vector2 c = cat.transform.position;
        float d = float.MaxValue;
        if (Stairs.SameFloor(c.y, from.y)) d = Mathf.Min(d, Mathf.Abs(c.x - from.x));
        if (Stairs.SameFloor(c.y, to.y)) d = Mathf.Min(d, Mathf.Abs(c.x - to.x));
        if (d > crawlHearDistance * 1.5f) return;
        float k = 1f - d / (crawlHearDistance * 1.5f);
        Sfx.Play(stairsClip, stairsVolume * Mathf.Lerp(0.25f, 1f, k));
    }

    // 기어다니는 소리: 같은 층에서 움직이는 동안만, 가까울수록 크게, 고양이 기준 왼쪽·오른쪽으로 들림
    private void UpdateCrawlSound(bool active)
    {
        if (crawlSource == null) return;
        bool moving = active && climbTimer <= 0f && alpha > 0.3f
                      && (CurrentState == State.Patrolling || CurrentState == State.Chasing || CurrentState == State.Hunting
                          || CurrentState == State.Sweeping || CurrentState == State.Returning || CurrentState == State.Investigating)
                      && (GameManager.Instance == null || !GameManager.Instance.IsGameOver);
        float target = 0f;
        if (moving && Stairs.SameFloor(cat.transform.position.y, rb.position.y))
        {
            float dx = rb.position.x - cat.transform.position.x;
            float k = 1f - Mathf.Clamp01(Mathf.Abs(dx) / crawlHearDistance);
            target = crawlVolume * k * k;
            crawlSource.panStereo = Mathf.Clamp(dx / 8f, -1f, 1f) * 0.7f;
            bool fast = CurrentState == State.Chasing || CurrentState == State.Hunting;
            crawlSource.pitch = fast ? 1.25f : 1f;
        }
        // 마지막 추격 중에는 어디서든 발소리가 계속 들림
        if (CurrentState == State.Hunting && climbTimer <= 0f && moving) target = Mathf.Max(target, crawlVolume * 0.45f);
        crawlSource.volume = Mathf.MoveTowards(crawlSource.volume, target, 3f * Time.unscaledDeltaTime);
        if (crawlSource.volume > 0.001f && !crawlSource.isPlaying) crawlSource.Play();
        else if (crawlSource.volume <= 0.001f && crawlSource.isPlaying) crawlSource.Pause();
    }

    // 다음 목적지 고르기. fromHere = true면 순찰 지점 중 가장 가까운 곳부터
    private void PickNextTarget(bool fromHere)
    {
        if (HasPatrol)
        {
            if (fromHere) patrolIndex = NearestPatrolIndex();
            else NextPatrolPoint();
            return;
        }
        if (roamRooms.Count == 0) return;

        // 같은 방의 다른 곳, 아니면 다른 방으로
        Room current = roamRooms.Find(r => r != null && r.Contains(rb.position));
        Room next = current;
        if (current == null || roamRooms.Count == 1 || Random.value > stayInRoomChance)
        {
            for (int tries = 0; tries < 10; tries++)
            {
                next = roamRooms[Random.Range(0, roamRooms.Count)];
                if (next != current) break;
            }
        }
        float left = next.Left + roamMargin;
        float right = next.Right - roamMargin;
        roamX = left < right ? Random.Range(left, right) : (next.Left + next.Right) * 0.5f;
        roamRoom = next;
        GameLog.Info($"존재: {next.RoomName}(x {roamX:0.0})으로 이동");
    }

    private int NearestPatrolIndex()
    {
        int best = 0; float bestD = float.MaxValue;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            float d = Mathf.Abs(patrolPoints[i].position.x - rb.position.x);
            if (d < bestD) { bestD = d; best = i; }
        }
        return best;
    }

    private void NextPatrolPoint()
    {
        if (patrolPoints.Length < 2) return;
        if (pingPong)
        {
            if (patrolIndex + patrolStep >= patrolPoints.Length || patrolIndex + patrolStep < 0) patrolStep = -patrolStep;
            patrolIndex += patrolStep;
        }
        else patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    // ---------- 숨었다가 튀어나오기 (Ambush Mode) ----------

    private float RandomGap() => Random.Range(appearGap.x, appearGap.y);

    // 고양이가 숨는 순간을 존재가 봤는가 (나와 있고, 가깝고, 같은 층, 바라보는 쪽, 벽에 안 가림)
    private bool SawHiding()
    {
        if (!IsPresent || CurrentState == State.Caught || CurrentState == State.Dropping || alpha < 0.5f) return false;
        if (!SameRoomAsCat()) return false;   // 다른 방에서는 못 봄
        Vector2 me = col.bounds.center;
        Vector2 c = cat.transform.position;
        Vector2 d = c - me;
        if (Mathf.Abs(d.x) > seeHideRange || !Stairs.SameFloor(c.y, rb.position.y)) return false;
        int facing = (spriteFacesRight ? !sr.flipX : sr.flipX) ? 1 : -1;
        if (Mathf.Abs(d.x) > 1.5f && Mathf.Sign(d.x) != facing) return false;
        return Physics2D.Linecast(me, c + Vector2.up * 0.3f, wallLayer).collider == null;
    }

    private void Vanish(float wait)
    {
        exposed = false;
        if (onSweepDone != null) { var cb = onSweepDone; onSweepDone = null; cb(); }   // 지하 훑기 도중이었으면 이벤트를 끝냄
        routeStairs = null;
        vanishWait = wait;
        SetState(State.Vanished);
        GameLog.Info($"존재: 사라짐 ({wait:0}초 뒤 다시 나타남)");
    }

    // 고양이 근처에 나타남. 지금 나타날 수 없으면 3초 뒤 다시 시도
    private void TryAppear()
    {
        bool busy = GameInput.Blocked || Stairs.IsTraveling || PowerEvent.Active || ChaseEvent.Active || CraftUI.IsCrafting;
        Room catRoom = Room.Find(cat.transform.position);
        bool inSafe = catRoom != null && (System.Array.IndexOf(safeRoomNames, catRoom.RoomName) >= 0
                                          || System.Array.IndexOf(noAppearRoomNames, catRoom.RoomName) >= 0);   // 2번 방에서도 갑자기 안 나타남
        bool eventOnly = InEventOnlyRoom(cat.transform.position);   // 지하에서는 갑자기 나타나지 않음
        if (busy || inSafe || eventOnly)
        {
            vanishWait = stateTimer + 3f;
            return;
        }

        // 가끔은 깜짝 등장만 (고양이 앞에 잠깐 보이고 사라짐 → 조금 뒤 진짜로 나타남)
        if (!lastWasGlimpse && Random.value < glimpseChance
            && FindSpot(cat.transform.position, cat.Facing, glimpseDistance, out Vector2 gSpot))
        {
            StartGlimpse(gSpot);
            return;
        }
        lastWasGlimpse = false;

        if (!FindAppearSpot(out Vector2 spot))
        {
            vanishWait = stateTimer + 3f;
            return;
        }

        MoveTo(spot);
        appearedAt = Time.time;
        roamRoom = null;                    // 목적지 = 고양이가 지금 있는 자리 (같은 층)
        roamX = cat.transform.position.x;
        Face(roamX - spot.x);
        Sfx.Play(appearClip, appearVolume);
        alpha = 0f;
        SetAlpha(0f);
        SetState(State.Emerging);
        GameLog.Info($"존재: x {spot.x:0.0}에 나타남");
    }

    // 정해진 자리(GlimpseTrigger)에서 부름. 지금 보여줄 수 있으면 깜짝 등장하고 true
    public bool PlayGlimpse() => PlayGlimpse(float.NaN);

    // fixedX를 넣으면 그 자리(고양이와 같은 층 바닥)에 나타남 (예: 쪽지 바로 위)
    public bool PlayGlimpse(float fixedX)
    {
        // 사라져 있거나 그냥 돌아다니는 중이면 되고, 쫓는 중·이벤트 중이면 안 됨
        bool ambient = CurrentState == State.Vanished || CurrentState == State.Patrolling || CurrentState == State.Lurking
                    || CurrentState == State.Emerging || CurrentState == State.Returning;
        if (paused || relentless || !ambient) return false;
        if (GameInput.Blocked || Stairs.IsTraveling || PowerEvent.Active || ChaseEvent.Active || CraftUI.IsCrafting) return false;
        Vector2 spot;
        if (!float.IsNaN(fixedX))
        {
            Vector2 c = cat.transform.position;
            RaycastHit2D floor = Physics2D.Raycast(new Vector2(fixedX, c.y + 1.5f), Vector2.down, 6f, wallLayer);
            if (floor.collider == null) return false;
            spot = new Vector2(fixedX, floor.point.y);
        }
        else if (!FindSpot(cat.transform.position, cat.Facing, glimpseDistance, out spot, true)) return false;
        StartGlimpse(spot);
        return true;
    }

    private void StartGlimpse(Vector2 spot)
    {
        // 원래 나타나기로 한 시간이 더 남아 있으면 그대로 지킴 (깜짝 등장 때문에 일찍 오지 않게)
        float remaining = CurrentState == State.Vanished ? vanishWait - stateTimer : 0f;
        glimpseReturnWait = Mathf.Max(remaining, Random.Range(glimpseGap.x, glimpseGap.y));
        MoveTo(spot);
        glimpseDir = spot.x >= cat.transform.position.x ? 1 : -1;   // 고양이 반대쪽을 바라봄 (뒷모습)
        glimpseLeaving = false;
        routeStairs = null;
        Face(glimpseDir);
        alpha = 1f;                         // 갑자기 확 보임 (소리 없이)
        SetAlpha(1f);
        SetState(State.Glimpse);
        GameLog.Info($"존재: x {spot.x:0.0}에 깜짝 등장");
    }

    // 고양이와 같은 층에서 appearDistance만큼 떨어진 바닥 (벽 너머·안전한 방 제외)
    private bool FindAppearSpot(out Vector2 spot) => FindAppearSpot(cat.transform.position, out spot);

    private bool FindAppearSpot(Vector2 c, out Vector2 spot) => FindSpot(c, 0, appearDistance, out spot);

    // side: 1 = 오른쪽, -1 = 왼쪽, 0 = 무작위 (등 뒤일 확률 = Appear Behind Chance)
    // allowEventOnly: 지하(연출만 있는 방)에도 나타나도 되는가 (깜짝 등장만 true)
    private bool FindSpot(Vector2 c, int forcedSide, Vector2 distance, out Vector2 spot, bool allowEventOnly = false)
    {
        int behind = -cat.Facing;
        for (int tries = 0; tries < 8; tries++)
        {
            int side = forcedSide != 0 ? forcedSide : (Random.value < appearBehindChance ? behind : -behind);
            float x = c.x + side * Random.Range(distance.x, distance.y);

            RaycastHit2D floor = Physics2D.Raycast(new Vector2(x, c.y + 1.5f), Vector2.down, 6f, wallLayer);
            if (floor.collider == null) continue;
            Vector2 p = new Vector2(x, floor.point.y);

            Room r = Room.Find(p + Vector2.up * 0.5f);
            if (r == null || System.Array.IndexOf(safeRoomNames, r.RoomName) >= 0) continue;
            if (System.Array.IndexOf(noAppearRoomNames, r.RoomName) >= 0) continue;   // 2번 방에는 나타나지 않음
            if (!allowEventOnly && System.Array.IndexOf(eventOnlyRoomNames, r.RoomName) >= 0) continue;

            // 고양이와 그 자리 사이에 벽이 있으면 안 됨 (맵 끝 벽 너머 등)
            if (Physics2D.Linecast(new Vector2(c.x, p.y + 1f), new Vector2(x, p.y + 1f), wallLayer).collider != null) continue;

            spot = p;
            return true;
        }
        spot = Vector2.zero;
        return false;
    }

    private void CheckCatch()
    {
        // 숨은 쪽은 잡히지 않음 (고양이 혼자 숨으면 밖에 있는 주인은 잡힐 수 있음)
        bool catHidden = HideController.IsHidden && !exposed;
        bool ownerHidden = owner != null && owner.CurrentState == OwnerController.State.Hiding && !exposed;

        if (!catHidden && catCol != null && col.bounds.Intersects(catCol.bounds))
            Caught("고양이");
        else if (!ownerHidden && ownerCol != null && col.bounds.Intersects(ownerCol.bounds))
            Caught("주인");
    }

    private void Caught(string who)
    {
        SetState(State.Caught);
        if (pounceSprite != null) sr.sprite = pounceSprite;
        alpha = 1f;          // 숨어 있다가 잡아도 모습이 보이게
        SetAlpha(1f);
        GameLog.Info($"존재에게 {who}이(가) 잡혔다!");

        if (GameManager.Instance != null) GameManager.Instance.GameOver($"{who}이(가) 잡힘");
        else Debug.LogWarning("GameManager가 씬에 없어서 게임오버를 할 수 없어요");
    }

    private void SetState(State s)
    {
        if (s != CurrentState && (s == State.Noticing || s == State.Roaring)) Sfx.Play(noticeClip, noticeVolume);
        CurrentState = s;
        stateTimer = 0f;
        loseTimer = 0f;
        ApplySize();
        GameLog.Info($"존재 상태: {s}");
    }

    // 서 있을 때와 기어갈 때 충돌 상자 크기를 바꿈
    private void ApplySize()
    {
        bool standing = CurrentState == State.Lurking || CurrentState == State.Noticing
                     || CurrentState == State.Searching || CurrentState == State.Roaring
                     || CurrentState == State.Dropping || CurrentState == State.Glimpse;
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

        // 순찰 경로 (순찰 지점을 넣은 경우)
        if (patrolPoints == null || patrolPoints.Length == 0) return;
        Gizmos.color = new Color(0.8f, 0.2f, 1f);
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
            if (i > 0 && patrolPoints[i - 1] != null) Gizmos.DrawLine(patrolPoints[i - 1].position, patrolPoints[i].position);
        }
    }
}
