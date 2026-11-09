using UnityEngine;

// 출구 열쇠를 줍고 수술실 문 쪽(오른쪽)으로 가는 순간의 추격 (씬에 하나)
// 존재가 천장에서 수술대 위로 떨어지고, 잠깐 울부짖은 뒤 출구까지 끝까지 쫓아온다
// 시작: Trigger Item을 가진 고양이가 같은 층에서 Trigger X보다 오른쪽으로 가면 (또는 Begin() 호출)
// 추격 중에는 숨을 수 없다 (HideController가 확인)
public class ChaseEvent : MonoBehaviour
{
    public static ChaseEvent Instance { get; private set; }
    public static bool Active { get; private set; }

    [Header("연결")]
    [SerializeField] private PresenceAI presence;
    [SerializeField] private CameraFollow cam;

    [Header("떨어지는 자리 (수술대)")]
    [SerializeField] private Vector2 tableTop = new Vector2(23.12f, 18.5f);   // 수술대 위 (존재 발밑)
    [SerializeField] private float dropHeight = 5f;                          // 이만큼 위(천장)에서 떨어짐
    [SerializeField] private float floorY = 17.5f;                           // 2층 바닥 높이
    [SerializeField] private float huntSpeed = 1.6f;                         // 주인 걷는 속도(2)보다 조금 느리게

    [Header("시작 조건 (Trigger Item을 비우면 자동 시작 안 함)")]
    [SerializeField] private string triggerItem = "C";                      // 출구 열쇠를 주운 뒤
    [SerializeField] private float triggerX = 29f;                           // 이보다 오른쪽(수술실 문 쪽)으로 가면

    [Header("소리 (비워두면 소리 없음)")]
    [SerializeField] private AudioClip dropClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("문구")]
    [SerializeField] private string dropMessage = "위에서 무언가 떨어졌다…!";
    [SerializeField] private string runMessage = "도망쳐! 주인과 함께 로비 정문으로!";

    private bool started;
    private CatController cat;

    void Awake()
    {
        Instance = this;
        Active = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Active = false;
    }

    void Start()
    {
        if (presence == null) presence = FindAnyObjectByType<PresenceAI>();
        if (cam == null) cam = FindAnyObjectByType<CameraFollow>();
        cat = FindAnyObjectByType<CatController>();
    }

    void Update()
    {
        if (started || cat == null || string.IsNullOrEmpty(triggerItem)) return;
        if (GameInput.Blocked || !KeyInventory.Has(triggerItem)) return;
        Vector2 p = cat.transform.position;
        if (Stairs.SameFloor(p.y, floorY) && p.x >= triggerX) Begin();
    }

    // 자동 시작 또는 다른 스크립트(예: Interactable, Action = ChaseStart)가 호출
    public void Begin()
    {
        if (started) return;
        started = true;
        Active = true;

        if (dropClip != null)
            AudioSource.PlayClipAtPoint(dropClip, cam != null ? cam.transform.position : Vector3.zero, volume);
        if (cam != null) cam.Shake(0.4f, 0.1f);
        CatVision.Reveal(tableTop + Vector2.up * 0.5f, 3.5f, 3f);   // 어두워도 떨어지는 모습이 보이게
        if (HintUI.Instance != null) HintUI.Instance.ShowMessage(dropMessage, 1.8f);
        Invoke(nameof(ShowRun), 1.8f);

        if (presence != null)
            presence.StartHunt(tableTop + Vector2.up * dropHeight, tableTop, floorY, huntSpeed);
        GameLog.Info("추격 이벤트 시작");
    }

    private void ShowRun()
    {
        if (HintUI.Instance != null && (GameManager.Instance == null || !GameManager.Instance.IsGameOver))
            HintUI.Instance.ShowMessage(runMessage, 2.5f);
    }
}
