using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 오른쪽 위: 가진 아이템 그림을 한 줄로 표시
// Tab: 인벤토리 창 열기. ← → 로 설정창(PauseMenu)과 오감. 닫기는 Tab으로만 (Esc로는 안 닫힘)
// (웹 전체 화면에서는 Esc를 브라우저가 가져가므로 Tab만으로도 설정을 열고 닫을 수 있게)
// 인벤토리 창: 아이템 그림 · 이름 · 짧은 설명. 열려 있는 동안 게임이 멈춤
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [SerializeField] private float slotSize = 72f;

    [Header("아이템을 얻었을 때 소리 (비워두면 소리 없음)")]
    [SerializeField] private AudioClip pickupClip;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;
    private int shownCount = -1;   // 지난번에 보여준 아이템 수 (늘어나면 소리)

    private RectTransform bar;          // 오른쪽 위 줄
    private GameObject panel;           // Tab 창
    private RectTransform list;
    private Text emptyText;
    private Text titleText;   // "인벤토리 | 설정" (지금 쪽 = 인벤토리가 노랑)
    private int shownVersion = -1;
    private bool open;
    private int openFrame;

    private readonly List<GameObject> barSlots = new List<GameObject>();
    private readonly List<GameObject> rows = new List<GameObject>();

    private static readonly Color SlotColor = new Color(0.08f, 0.08f, 0.09f, 0.75f);
    private static readonly Color TextColor = new Color(0.88f, 0.85f, 0.78f);

    void Awake() { Instance = this; }
    void OnDestroy() { if (Instance == this) Instance = null; }
    void OnEnable() => Loc.Changed += RefreshTitle;
    void OnDisable() => Loc.Changed -= RefreshTitle;

    // 설정창에서 ← → 로 돌아올 때
    public void OpenFromSettings()
    {
        if (!open) SetOpen(true);
    }

    private void RefreshTitle()
    {
        if (titleText == null) return;
        titleText.text = $"<color=#FFD95A>{Loc.T("인벤토리")}</color>      <color=#7A7A7A>{Loc.T("설정")}</color>  <color=#FFD95A>▶</color>";
    }

    void Start()
    {
        Build();
        Refresh();
    }

    void Update()
    {
        if (shownVersion != KeyInventory.Version) Refresh();

        var kb = Keyboard.current;
        if (kb == null) return;

        if (open)
        {
            if (Time.frameCount == openFrame) return;
            if (kb.tabKey.wasPressedThisFrame) SetOpen(false);
            else if (kb.leftArrowKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame)
            {
                SetOpen(false);
                if (PauseMenu.Instance != null) PauseMenu.Instance.OpenFromInventory();   // 옆 쪽: 설정
            }
            return;
        }

        // 다른 창이 열려 있거나 연출 중이면 열지 않음
        if (kb.tabKey.wasPressedThisFrame && !GameInput.Blocked) SetOpen(true);
    }

    private void SetOpen(bool on)
    {
        open = on;
        openFrame = Time.frameCount;
        panel.SetActive(on);
        GameInput.SetPopup(on);
    }

    // ---------- 다시 그리기 ----------
    private void Refresh()
    {
        shownVersion = KeyInventory.Version;
        foreach (var g in barSlots) Destroy(g);
        foreach (var g in rows) Destroy(g);
        barSlots.Clear();
        rows.Clear();

        var ids = KeyInventory.Ids;
        if (shownCount >= 0 && ids.Count > shownCount) Sfx.Play(pickupClip, pickupVolume);
        shownCount = ids.Count;
        for (int i = 0; i < ids.Count; i++)
        {
            string id = ids[i];

            // 오른쪽 위 칸 (오른쪽부터 왼쪽으로 쌓임)
            var slot = UIKit.Box(bar, SlotColor, new Vector2(1f, 1f), new Vector2(-i * (slotSize + 10f), 0f),
                                 new Vector2(slotSize, slotSize), "Slot_" + id);
            AddIcon(slot.transform, id, slotSize - 12f);
            barSlots.Add(slot.gameObject);

            // Tab 창의 한 줄
            var row = UIKit.Box(list, new Color(1f, 1f, 1f, 0.04f), new Vector2(0.5f, 1f), new Vector2(0, -i * 130f),
                                new Vector2(1000, 118), "Row_" + id);
            var iconBox = UIKit.Box(row.transform, SlotColor, new Vector2(0f, 0.5f), new Vector2(14, 0), new Vector2(100, 100));
            AddIcon(iconBox.transform, id, 88f);
            UIKit.Label(row.transform, ItemDB.Name(id), 34, new Color(0.95f, 0.85f, 0.6f), new Vector2(0f, 1f),
                        new Vector2(140, -12), new Vector2(840, 46), TextAnchor.MiddleLeft);
            UIKit.Label(row.transform, ItemDB.Desc(id), 27, TextColor, new Vector2(0f, 1f),
                        new Vector2(140, -60), new Vector2(840, 50), TextAnchor.UpperLeft);
            rows.Add(row.gameObject);
        }
        emptyText.enabled = ids.Count == 0;
    }

    private void AddIcon(Transform parent, string id, float size)
    {
        var icon = UIKit.Box(parent, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), "Icon");
        Sprite s = ItemDB.Icon(id);
        if (s != null) { icon.sprite = s; icon.preserveAspect = true; }
        else icon.color = new Color(0.6f, 0.55f, 0.4f);   // 그림이 없으면 색 칸
    }

    // ---------- 화면 만들기 ----------
    private void Build()
    {
        var canvas = UIKit.MakeCanvas(transform, "InventoryUI_Canvas", 60);

        // 오른쪽 위 줄
        var barGO = new GameObject("Bar", typeof(RectTransform));
        barGO.transform.SetParent(canvas.transform, false);
        bar = barGO.GetComponent<RectTransform>();
        UIKit.Place(bar, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(slotSize, slotSize));
        UIKit.Label(canvas.transform, "Tab  인벤토리 · 설정", 22, new Color(0.7f, 0.7f, 0.7f), new Vector2(1f, 1f),
                    new Vector2(-30f, -30f - slotSize - 6f), new Vector2(300, 30), TextAnchor.UpperRight);

        // Tab 창
        var dim = UIKit.Box(canvas.transform, new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "Dim");
        UIKit.Stretch(dim.rectTransform);
        panel = dim.gameObject;
        var box = UIKit.Box(panel.transform, new Color(0.06f, 0.06f, 0.07f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero,
                            new Vector2(1100, 860), "Panel");
        titleText = UIKit.Label(box.transform, "", 46, TextColor, new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(900, 70));
        RefreshTitle();

        var listGO = new GameObject("List", typeof(RectTransform));
        listGO.transform.SetParent(box.transform, false);
        list = listGO.GetComponent<RectTransform>();
        UIKit.Place(list, new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(1000, 680));

        emptyText = UIKit.Label(box.transform, "가진 아이템이 없다", 32, new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0.5f),
                                Vector2.zero, new Vector2(600, 60));
        UIKit.Label(box.transform, "← →  설정    Tab  닫기", 26, new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0f),
                    new Vector2(0, 26), new Vector2(600, 40));
        panel.SetActive(false);
    }
}
