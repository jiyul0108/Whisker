using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 벽 타기 지점 (2번 방 시계 밑). 이 오브젝트는 시계 가운데에 둔다
// 의자(Chair)를 시계 밑으로 밀어 놓고, 고양이가 의자 위에 올라서 있을 때만 W / ↑ 로 벽을 탈 수 있다
// 다 올라가면 시계를 조사할 수 있고(Interactable의 Require Climbing), S / ↓ 로 내려온다
public class ClimbSpot : MonoBehaviour
{
    public static bool IsClimbing { get; private set; }   // 벽에 붙어 있는가 (올라가는 중 포함)
    public static bool IsMoving { get; private set; }     // 올라가는 중인가 (애니메이션용)

    [Header("의자 (밀 수 있는 발판)")]
    [SerializeField] private Collider2D chair;
    [SerializeField] private float columnHalfWidth = 0.7f;   // 의자 가운데가 시계에서 이 거리 안이어야 함

    [Header("올라가기")]
    [SerializeField] private float climbTime = 1.0f;
    [SerializeField] private float stopBelowCenter = 0.3f;    // 고양이 몸 가운데가 시계 가운데보다 이만큼 아래에서 멈춤

    [Header("문구")]
    [SerializeField] private string climbLabel = "벽 타기";
    [SerializeField] private string tooHighMessage = "너무 높다. 밟고 올라설 것이 필요하다";

    private CatController cat;
    private Rigidbody2D catRb;
    private Collider2D catCol;
    private float savedGravity;
    private bool hinting;
    private string hintText;

    void Awake()
    {
        IsClimbing = false;
        IsMoving = false;
    }

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        if (cat != null)
        {
            catRb = cat.GetComponent<Rigidbody2D>();
            catCol = cat.GetComponent<Collider2D>();
        }
        if (chair == null) Debug.LogWarning("ClimbSpot: 의자(Chair) 칸이 비어 있어요");
    }

    void OnDestroy()
    {
        IsClimbing = false;
        IsMoving = false;
    }

    void Update()
    {
        if (cat == null) return;
        if (GameInput.Blocked) { SetHint(null); return; }
        var kb = Keyboard.current;

        if (IsClimbing)
        {
            if (IsMoving) { SetHint(null); return; }
            SetHint("S / ↓  내려가기");
            if (kb != null && (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) Release();
            return;
        }

        bool underClock = Mathf.Abs(cat.transform.position.x - transform.position.x) <= columnHalfWidth;
        bool canClimb = underClock && OnChair() && ChairUnderClock();
        SetHint(canClimb ? $"W / ↑  {climbLabel}" : null);

        if (kb == null || !(kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)) return;
        if (canClimb) StartCoroutine(Climb());
        else if (underClock && cat.IsGrounded && HintUI.Instance != null) HintUI.Instance.ShowMessage(tooHighMessage, 2f);
    }

    // 고양이가 의자 위에 서 있는가
    private bool OnChair()
    {
        if (chair == null || catCol == null || !cat.IsGrounded) return false;
        Bounds c = chair.bounds;
        Bounds k = catCol.bounds;
        return k.min.y >= c.max.y - 0.15f && Mathf.Abs(k.center.x - c.center.x) <= c.extents.x + 0.1f;
    }

    private bool ChairUnderClock()
    {
        return chair != null && Mathf.Abs(chair.bounds.center.x - transform.position.x) <= columnHalfWidth;
    }

    private IEnumerator Climb()
    {
        IsClimbing = true;
        IsMoving = true;
        SetHint(null);

        savedGravity = catRb.gravityScale;
        catRb.gravityScale = 0f;
        catRb.linearVelocity = Vector2.zero;

        // 고양이 몸 가운데가 시계 바로 아래에 오도록
        Vector2 from = catRb.position;
        float bodyOffset = from.y - catCol.bounds.center.y;
        Vector2 to = new Vector2(transform.position.x, transform.position.y - stopBelowCenter + bodyOffset);

        float t = 0f;
        while (t < climbTime)
        {
            if (!IsClimbing) yield break;   // 도중에 게임오버 등
            t += Time.fixedDeltaTime;
            catRb.linearVelocity = Vector2.zero;
            catRb.MovePosition(Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / climbTime)));
            yield return new WaitForFixedUpdate();
        }
        catRb.position = to;
        IsMoving = false;
        GameLog.Info("고양이: 벽을 다 올라갔다");
    }

    private void Release()
    {
        IsClimbing = false;
        IsMoving = false;
        catRb.gravityScale = savedGravity;
        SetHint(null);
        GameLog.Info("고양이: 벽에서 내려왔다");
    }

    private void SetHint(string text)
    {
        bool on = !string.IsNullOrEmpty(text);
        if (on == hinting && text == hintText) return;
        hinting = on;
        hintText = text;
        if (HintUI.Instance != null) HintUI.Instance.SetHint(this, text);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position + Vector3.left * columnHalfWidth, transform.position + Vector3.right * columnHalfWidth);
    }
}
