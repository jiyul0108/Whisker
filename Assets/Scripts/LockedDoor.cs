using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// 잠긴 문: 고양이와 주인은 못 지나가고, 존재는 그냥 통과한다
// 고양이가 가까이에서 W / ↑ 를 누르면, 맞는 열쇠가 있을 때 열린다
// Key Id를 비워두면 열쇠 없이 열리는 문
[RequireComponent(typeof(BoxCollider2D))]
public class LockedDoor : MonoBehaviour
{
    [Header("열쇠 (열쇠의 Key Id와 같게)")]
    [SerializeField] private string keyId = "A";
    [SerializeField] private string keyName = "녹슨 열쇠";
    [SerializeField] private bool consumeKey = true;          // 열면 열쇠가 사라짐

    [Header("범위")]
    [SerializeField] private float openRange = 0.8f;         // 문 가장자리에서 이 거리 안이면 열 수 있음
    [SerializeField] private float blockHeight = 6f;         // 막는 높이 (점프로 넘지 못하게 높게)

    [Header("문구")]
    [SerializeField] private string lockedMessage = "잠겨 있다";
    [SerializeField] private string openedMessage = "문이 열렸다";

    [Header("탈출문 (켜면 열었을 때 탈출 성공)")]
    [SerializeField] private bool isExit = false;
    [SerializeField] private float ownerRange = 3f;          // 주인이 이 거리 안에 있어야 탈출 가능
    [SerializeField] private string needOwnerMessage = "주인과 함께 나가야 한다";

    [Header("열렸을 때 추가로 할 일")]
    [SerializeField] private UnityEvent onOpened;

    public bool IsOpen { get; private set; }

    private BoxCollider2D col;
    private SpriteRenderer sr;
    private CatController cat;
    private OwnerController owner;
    private bool hinting;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = false;
        sr = GetComponentInChildren<SpriteRenderer>();

        // 주인은 Ground 레이어를 벽으로 보고 멈춘다
        int ground = LayerMask.NameToLayer("Ground");
        if (ground >= 0) gameObject.layer = ground;

        // 충돌 범위: 문 그림 폭 × 막는 높이 (발밑 기준)
        float w = (sr != null && sr.sprite != null) ? sr.sprite.bounds.size.x : col.size.x;
        col.size = new Vector2(w, blockHeight);
        col.offset = new Vector2(0f, blockHeight * 0.5f);
    }

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        owner = FindAnyObjectByType<OwnerController>();
    }

    void Update()
    {
        if (IsOpen || cat == null) return;
        if (GameInput.Blocked) { SetHint(false); return; }

        bool near = CatNear() && !HideController.IsHidden && !ClimbSpot.IsClimbing;
        SetHint(near);
        if (!near) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) TryOpen();
    }

    private bool CatNear()
    {
        Vector2 c = cat.transform.position;
        float halfW = col.bounds.extents.x;
        float dx = Mathf.Abs(c.x - col.bounds.center.x) - halfW;
        float dy = Mathf.Abs(c.y - transform.position.y);
        return dx <= openRange && dy < 2.5f;
    }

    private void TryOpen()
    {
        bool free = string.IsNullOrEmpty(keyId);
        if (free || KeyInventory.Has(keyId))
        {
            // 탈출문은 주인이 가까이 있을 때만 (열쇠는 그대로 둠)
            if (isExit && !OwnerNear())
            {
                if (HintUI.Instance != null) HintUI.Instance.ShowMessage(needOwnerMessage);
                return;
            }
            if (!free && consumeKey) KeyInventory.Remove(keyId);
            Open();
        }
        else if (HintUI.Instance != null)
        {
            HintUI.Instance.ShowMessage($"{lockedMessage}  ({keyName} 필요)");
        }
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        col.enabled = false;
        if (sr != null) sr.enabled = false;
        SetHint(false);
        if (HintUI.Instance != null) HintUI.Instance.ShowMessage(openedMessage);
        GameLog.Info($"{name}: 문이 열렸다");
        onOpened?.Invoke();
        if (isExit && GameManager.Instance != null) GameManager.Instance.GameClear();
    }

    private bool OwnerNear()
    {
        if (owner == null) return true; // 주인이 없는 테스트 씬
        return Mathf.Abs(owner.transform.position.x - transform.position.x) <= ownerRange;
    }

    private void SetHint(bool on)
    {
        if (on == hinting) return;
        hinting = on;
        string text = isExit ? "W / ↑  탈출하기" : "W / ↑  문 열기";
        if (HintUI.Instance != null) HintUI.Instance.SetHint(this, on ? text : null);
    }

    void OnDisable()
    {
        SetHint(false);
    }

    void OnDrawGizmosSelected()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0.2f);
        Vector3 center = c.bounds.center;
        Gizmos.DrawWireCube(center, new Vector3(c.bounds.size.x + openRange * 2f, 2.5f, 0f));
    }
}
