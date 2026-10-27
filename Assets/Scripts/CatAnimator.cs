using UnityEngine;

// 고양이 상태(대기·걷기·점프·착지·울기)에 맞춰 스프라이트를 바꿔 끼우는 스크립트
[RequireComponent(typeof(CatController))]
public class CatAnimator : MonoBehaviour
{
    [Header("그림을 그리는 곳 (자식 Visual)")]
    [SerializeField] private SpriteRenderer sr;

    [Header("대기")]
    [SerializeField] private Sprite[] idleFrames;      // cat_meow_01
    [SerializeField] private float idleFps = 2f;

    [Header("걷기")]
    [SerializeField] private Sprite[] walkFrames;      // cat_walk_01~04
    [SerializeField] private float walkFps = 10f;

    [Header("점프")]
    [SerializeField] private Sprite jumpUp;            // cat_jump_02
    [SerializeField] private Sprite jumpFall;          // cat_jump_03
    [SerializeField] private Sprite land;              // cat_jump_04
    [SerializeField] private float landTime = 0.1f;

    [Header("울기")]
    [SerializeField] private Sprite meow;              // cat_meow_02
    [SerializeField] private float meowTime = 0.35f;

    private CatController cat;
    private float timer;          // 현재 동작이 시작된 뒤 지난 시간
    private string current = "";  // 현재 동작 이름
    private float landTimer;
    private float meowTimer;
    private bool wasGrounded = true;

    void Awake()
    {
        cat = GetComponent<CatController>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
    }

    // CatMeow가 호출
    public void PlayMeow()
    {
        meowTimer = meowTime;
    }

    void Update()
    {
        bool grounded = cat.IsGrounded;

        // 공중에 있다가 막 내려앉은 순간
        if (grounded && !wasGrounded) landTimer = landTime;
        wasGrounded = grounded;

        landTimer -= Time.deltaTime;
        meowTimer -= Time.deltaTime;

        // 우선순위: 공중 > 착지 > 울기 > 걷기 > 대기
        if (!grounded)
            Show(cat.VelocityY > 0f ? "jumpUp" : "jumpFall", cat.VelocityY > 0f ? jumpUp : jumpFall);
        else if (landTimer > 0f)
            Show("land", land);
        else if (meowTimer > 0f)
            Show("meow", meow);
        else if (cat.MoveInput != 0f)
            Play("walk", walkFrames, walkFps);
        else
            Play("idle", idleFrames, idleFps);
    }

    // 여러 장을 반복 재생
    private void Play(string name, Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;
        if (current != name) { current = name; timer = 0f; }
        timer += Time.deltaTime;
        int i = (int)(timer * fps) % frames.Length;
        sr.sprite = frames[i];
    }

    // 한 장만 표시
    private void Show(string name, Sprite sprite)
    {
        if (sprite == null) return;
        current = name;
        sr.sprite = sprite;
    }
}
