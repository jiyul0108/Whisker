using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 계단 입구: 고양이가 가까이에서 W / ↑ 를 누르면 화면이 어두워졌다가 연결된 계단(Linked)으로 이동한다
// 주인이 가까이(Owner Range) 있으면 함께 이동하고, 멀리 있으면 남는다
// Key Id를 넣으면 잠긴 계단: 입구가 문(Locked Door Visual)으로 막혀 있고,
// 맞는 열쇠로 열면 자물쇠가 떨어지고 문이 안쪽으로 열리는 동작 후 계속 열린 채로 남는다 (다시 누르면 이동)
// 존재(PresenceAI)도 이 계단으로 층을 오간다 (잠금 무시)
[RequireComponent(typeof(BoxCollider2D))]
public class Stairs : MonoBehaviour
{
    public static readonly List<Stairs> All = new List<Stairs>();
    public static bool IsTraveling { get; private set; }

    [Header("연결된 계단 (다른 층의 계단 입구)")]
    [SerializeField] private Stairs linked;
    [SerializeField] private string label = "계단 이용하기";   // 안내: "↑  지하로 내려가기"

    [Header("잠금 (Key Id를 비우면 열려 있음)")]
    [SerializeField] private string keyId = "";
    [SerializeField] private string keyName = "";
    [SerializeField] private bool consumeKey = true;
    [SerializeField] private GameObject lockedDoorVisual;   // 잠겨 있는 동안 보이는 문 그림 (자식, 기준점 발밑 가운데)
    [SerializeField] private Transform lockVisual;           // 문에 달린 쇠사슬·자물쇠 (문의 자식, 열 때 떨어짐)
    [SerializeField] private AudioClip unlockClip;           // 열쇠로 열 때 소리 (비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float unlockVolume = 0.9f;
    [SerializeField] private AudioClip travelClip;           // 계단으로 이동할 때 발소리 (비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float travelVolume = 0.8f;

    [Header("전자 잠금 (Required Flag를 넣으면 열쇠 대신 진행으로 열림. 예: POWER)")]
    [SerializeField] private string requiredFlag = "";
    [SerializeField] private string flagLockedMessage = "전자 잠금으로 잠겨 있다. 전기가 들어오지 않는다";

    [Header("문 열리는 동작")]
    [SerializeField] private float shakeTime = 0.25f;   // 덜컹거림
    [SerializeField] private float lockFallTime = 0.45f; // 자물쇠 떨어짐
    [SerializeField] private float swingTime = 0.6f;    // 문이 안쪽으로 열림

    [Header("범위·연출")]
    [SerializeField] private float useRange = 0.6f;     // 입구 가장자리에서 이 거리 안이면 이용 가능
    [SerializeField] private float ownerRange = 2.5f;   // 주인이 이 거리 안에 있으면 함께 이동
    [SerializeField] private float fadeTime = 0.35f;

    public Stairs Linked => linked;
    public float X => transform.position.x;
    public float FloorY => transform.position.y;        // 입구 오브젝트는 바닥 높이에 둔다

    private BoxCollider2D col;
    private CatController cat;
    private Rigidbody2D catRb;
    private OwnerController owner;
    private bool unlocked;
    private bool hinting;
    private bool opening;     // 문이 열리는 중

    private static Image fade;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        unlocked = string.IsNullOrEmpty(keyId) && string.IsNullOrEmpty(requiredFlag);
        IsTraveling = false;
        if (lockedDoorVisual != null) lockedDoorVisual.SetActive(!unlocked);
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); SetHint(false); }

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        if (cat != null) catRb = cat.GetComponent<Rigidbody2D>();
        owner = FindAnyObjectByType<OwnerController>();
        if (linked == null) Debug.LogWarning($"{name}: 연결된 계단(Linked)이 비어 있어요");
    }

    void Update()
    {
        if (cat == null || IsTraveling || opening) return;
        if (GameInput.Blocked) { SetHint(false); return; }

        bool near = CatNear() && !HideController.IsHidden && !ClimbSpot.IsClimbing;
        SetHint(near);
        if (!near) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) TryUse();
    }

    private bool CatNear()
    {
        Vector2 c = cat.transform.position;
        float dx = Mathf.Abs(c.x - col.bounds.center.x) - col.bounds.extents.x;
        float dy = Mathf.Abs(c.y - FloorY);
        return dx <= useRange && dy < 2.5f;
    }

    private void TryUse()
    {
        // 전원 이벤트 중: 존재가 계단으로 내려오는 중이라 도망칠 수 없음
        if (PowerEvent.BlockStairs)
        {
            if (HintUI.Instance != null) HintUI.Instance.ShowItemMessage("계단에서 무언가 내려오고 있다… 숨어야 한다!", 2f);
            return;
        }

        if (!unlocked && !string.IsNullOrEmpty(requiredFlag))
        {
            if (GameFlags.Has(requiredFlag)) StartCoroutine(RemoteUnlock());
            else if (HintUI.Instance != null) HintUI.Instance.ShowItemMessage(flagLockedMessage, 2.2f);   // 노란 메시지로 보이게
            return;
        }

        if (!unlocked)
        {
            if (!KeyInventory.Has(keyId))
            {
                if (HintUI.Instance != null) HintUI.Instance.ShowMessage(Loc.T("잠겨 있다") + "  " + Loc.F("({0} 필요)", Loc.T(keyName)));
                return;
            }
            if (consumeKey) KeyInventory.Remove(keyId);
            unlocked = true;
            SetHint(false);
            Sfx.Play(unlockClip, unlockVolume);
            if (lockedDoorVisual != null) StartCoroutine(OpenDoor());
            if (HintUI.Instance != null) HintUI.Instance.ShowMessage("문이 열렸다");
            return;            // 문만 열고, 이동은 한 번 더 눌렀을 때
        }
        if (linked == null) return;
        StartCoroutine(Travel());
    }

    private IEnumerator Travel()
    {
        IsTraveling = true;
        SetHint(false);
        Sfx.Play(travelClip, travelVolume);

        // 주인이 가까이 있고 숨어 있지 않으면 함께 간다
        bool withOwner = owner != null
            && owner.CurrentState != OwnerController.State.Hiding
            && Mathf.Abs(owner.transform.position.x - X) <= ownerRange
            && Mathf.Abs(owner.transform.position.y - FloorY) < 2.5f;

        Time.timeScale = 0f;
        yield return Fade(0f, 1f);

        // 고양이: 같은 높이 차이를 유지한 채 연결된 계단 앞으로
        Vector2 cp = cat.transform.position;
        Vector2 catTo = new Vector2(linked.X, linked.FloorY + (cp.y - FloorY));
        if (catRb != null) { catRb.position = catTo; catRb.linearVelocity = Vector2.zero; }
        cat.transform.position = catTo;

        if (withOwner)
        {
            Vector2 op = owner.transform.position;
            float side = Mathf.Clamp(op.x - X, -1f, 1f);
            owner.TeleportTo(new Vector2(linked.X + side, linked.FloorY + (op.y - FloorY)));
        }
        GameLog.Info($"계단 이동: {name} → {linked.name}" + (withOwner ? " (주인과 함께)" : " (고양이 혼자)"));

        yield return null;               // 카메라가 새 방으로 바뀔 시간
        yield return Fade(1f, 0f);
        Time.timeScale = 1f;
        IsTraveling = false;

        if (!withOwner && HintUI.Instance != null && owner != null)
            HintUI.Instance.ShowMessage("주인은 따라오지 못했다");
    }

    // 멀리서 잠금 해제 (PowerEvent가 카메라로 보여주며 호출). 문 열리는 동작이 끝날 때까지 기다릴 수 있음
    public IEnumerator RemoteUnlock()
    {
        if (unlocked) yield break;
        unlocked = true;
        SetHint(false);
        if (lockedDoorVisual != null && lockedDoorVisual.activeSelf) yield return OpenDoor();
        GameLog.Info($"{name}: 잠금 해제");
    }

    // 문 열리는 동작: 덜컹거림 → 쇠사슬·자물쇠가 떨어짐 → 문이 왼쪽 경첩을 중심으로 안쪽으로 열림
    private IEnumerator OpenDoor()
    {
        opening = true;
        Transform door = lockedDoorVisual.transform;
        var dsr = lockedDoorVisual.GetComponent<SpriteRenderer>();
        Vector3 basePos = door.localPosition;
        Color baseCol = dsr != null ? dsr.color : Color.white;
        float halfW = (dsr != null && dsr.sprite != null) ? dsr.sprite.bounds.extents.x : 0.84f;

        // 1) 덜컹거림
        float t = 0f;
        while (t < shakeTime)
        {
            t += Time.deltaTime;
            door.localPosition = basePos + Vector3.right * (Mathf.Sin(t * 90f) * 0.025f);
            yield return null;
        }
        door.localPosition = basePos;

        // 2) 쇠사슬·자물쇠가 떨어지며 사라짐
        if (lockVisual != null)
        {
            var lsr = lockVisual.GetComponent<SpriteRenderer>();
            float vy = 0f;
            t = 0f;
            while (t < lockFallTime)
            {
                t += Time.deltaTime;
                vy -= 12f * Time.deltaTime;
                lockVisual.localPosition += Vector3.up * (vy * Time.deltaTime);
                if (lsr != null) { Color c = lsr.color; c.a = 1f - t / lockFallTime; lsr.color = c; }
                yield return null;
            }
            lockVisual.gameObject.SetActive(false);
        }

        // 3) 문이 안쪽으로 돌며 열림 (가로로 좁아지고 어두워짐, 왼쪽 끝은 고정)
        t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / swingTime);
            float s = Mathf.Cos(k * 85f * Mathf.Deg2Rad);
            door.localScale = new Vector3(s, 1f, 1f);
            door.localPosition = basePos + Vector3.left * (halfW * (1f - s));
            if (dsr != null)
            {
                float shade = Mathf.Lerp(1f, 0.4f, k);
                dsr.color = new Color(baseCol.r * shade, baseCol.g * shade, baseCol.b * shade, baseCol.a);
            }
            yield return null;
        }
        lockedDoorVisual.SetActive(false);
        opening = false;
        hinting = false;   // 안내 문구를 "계단 이용"으로 다시 표시
    }

    private IEnumerator Fade(float from, float to)
    {
        Image img = GetFade();
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / fadeTime));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, to);
    }

    // 화면을 덮는 검은 판 (처음 쓸 때 만든다)
    private static Image GetFade()
    {
        if (fade != null) return fade;
        var canvasGO = new GameObject("StairsFade", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;        // 안내 문구(50) 위, 게임오버 화면(100) 아래
        var go = new GameObject("Fade", typeof(Image));
        go.transform.SetParent(canvasGO.transform, false);
        fade = go.GetComponent<Image>();
        fade.raycastTarget = false;
        fade.color = new Color(0f, 0f, 0f, 0f);
        var rt = fade.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return fade;
    }

    // 안내 문구: 열려 있으면 주인이 함께 갈 수 있는지도 표시 (숨기처럼 "주인과 …")
    private string hintText;
    private void SetHint(bool on)
    {
        string text = null;
        if (on)
        {
            // 전자 잠금(전기가 아직 안 들어옴)은 조사하기 대신 "굳게 잠겨 있다"로 바로 알려줌
            if (!unlocked) text = string.IsNullOrEmpty(requiredFlag) || GameFlags.Has(requiredFlag) ? "문 열기" : "굳게 잠겨 있다";
            else text = OwnerComes() ? Loc.F("주인과 {0}", Loc.T(label)) : $"{label}  (주인은 못 따라옴)";
            text = $"↑  {text}";
        }
        if (on == hinting && text == hintText) return;
        hinting = on;
        hintText = text;
        if (HintUI.Instance != null) HintUI.Instance.SetHint(this, text);
    }

    // 지금 계단을 타면 주인이 함께 가는가 (가까이 있고 숨어 있지 않음)
    private bool OwnerComes()
    {
        return owner != null
            && owner.CurrentState != OwnerController.State.Hiding
            && Mathf.Abs(owner.transform.position.x - X) <= ownerRange
            && Mathf.Abs(owner.transform.position.y - FloorY) < 2.5f;
    }

    // ---------- 존재(PresenceAI)용 길 찾기 ----------

    // 같은 층인가 (층끼리는 높이가 20 차이)
    public static bool SameFloor(float y1, float y2) => Mathf.Abs(y1 - y2) < 5f;

    // from 위치에서 targetY 층으로 가려면 이 층의 어느 계단을 타야 하나 (없으면 null)
    public static Stairs FindRoute(Vector2 from, float targetY)
    {
        Stairs best = null;
        float bestScore = float.MaxValue;
        foreach (var s in All)
        {
            if (s == null || s.linked == null || !SameFloor(s.FloorY, from.y)) continue;
            float score = Mathf.Abs(s.linked.FloorY - targetY) * 100f + Mathf.Abs(s.X - from.x);
            if (score < bestScore) { bestScore = score; best = s; }
        }
        return best;
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.8f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
        if (linked != null) Gizmos.DrawLine(transform.position, linked.transform.position);
    }
}
