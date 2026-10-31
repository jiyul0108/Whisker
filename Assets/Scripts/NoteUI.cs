using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 쪽지 화면: 읽기 안내, 쪽지 팝업, 모은 개수 표시
// 화면(Canvas)은 실행할 때 코드가 직접 만든다
public class NoteUI : MonoBehaviour
{
    public static NoteUI Instance { get; private set; }
    public static bool IsReading { get; private set; }   // 읽는 동안 고양이 조작을 막는 데 사용

    [Header("한글 폰트 (Assets/Fonts/Galmuri11)")]
    [SerializeField] private Font font;

    [Header("문구")]
    [SerializeField] private string hintText = "W / ↑  쪽지 읽기";
    [SerializeField] private string closeText = "W / ↑ / Esc  닫기";

    private Note nearby;
    private Note reading;
    private int readCount;
    private int totalCount;
    private float prevTimeScale = 1f;

    private Text hint;
    private Text counter;
    private GameObject panel;
    private Text panelTitle;
    private Text panelBody;

    void Awake()
    {
        Instance = this;
        IsReading = false;
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Debug.LogWarning("NoteUI: 한글 폰트가 비어 있어요. Font 칸에 Galmuri11을 넣어주세요.");
        }
        BuildUI();
    }

    void Start()
    {
        totalCount = FindObjectsByType<Note>(FindObjectsSortMode.None).Length;
        UpdateCounter();
    }

    void OnDestroy()
    {
        if (Instance == this) { Instance = null; IsReading = false; }
    }

    public void SetNearby(Note note)
    {
        nearby = note;
        hint.enabled = !IsReading;
    }

    public void ClearNearby(Note note)
    {
        if (nearby != note) return;
        nearby = null;
        hint.enabled = false;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        bool readKey = kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;

        if (IsReading)
        {
            if (readKey || kb.escapeKey.wasPressedThisFrame) Close();
        }
        else if (nearby != null && readKey)
        {
            Open(nearby);
        }
    }

    private void Open(Note note)
    {
        reading = note;
        IsReading = true;
        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;              // 읽는 동안 게임 정지

        panelTitle.text = note.Title;
        panelBody.text = note.Body;
        panel.SetActive(true);
        hint.enabled = false;

        if (!note.IsRead)
        {
            note.MarkRead();
            readCount++;
            UpdateCounter();
        }
    }

    private void Close()
    {
        panel.SetActive(false);
        Time.timeScale = prevTimeScale;
        IsReading = false;
        reading = null;
        hint.enabled = nearby != null;
    }

    private void UpdateCounter()
    {
        counter.text = $"쪽지 {readCount} / {totalCount}";
        counter.enabled = totalCount > 0;
    }

    // ---------- 화면 만들기 ----------
    private void BuildUI()
    {
        var canvasGO = new GameObject("NoteUI_Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;          // 게임오버 화면(100)보다 아래
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Transform root = canvasGO.transform;

        // 읽기 안내 (화면 아래 가운데)
        hint = CreateText(root, hintText, 36, Color.white, TextAnchor.MiddleCenter);
        Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(800, 60));
        hint.enabled = false;

        // 모은 개수 (오른쪽 위)
        counter = CreateText(root, "", 32, new Color(0.9f, 0.85f, 0.7f), TextAnchor.MiddleRight);
        Place(counter.rectTransform, new Vector2(1f, 1f), new Vector2(-40, -40), new Vector2(400, 50));
        counter.rectTransform.pivot = new Vector2(1f, 1f);

        // 쪽지 팝업: 화면을 어둡게 + 가운데 종이
        panel = new GameObject("NotePanel", typeof(Image));
        panel.transform.SetParent(root, false);
        var dim = panel.GetComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.75f);
        Stretch(dim.rectTransform);

        var paper = new GameObject("Paper", typeof(Image));
        paper.transform.SetParent(panel.transform, false);
        var paperImg = paper.GetComponent<Image>();
        paperImg.color = new Color(0.84f, 0.78f, 0.63f);
        Place(paperImg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 640));

        panelTitle = CreateText(paper.transform, "", 44, new Color(0.25f, 0.18f, 0.12f), TextAnchor.UpperCenter);
        Place(panelTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(800, 70));
        panelTitle.rectTransform.pivot = new Vector2(0.5f, 1f);

        panelBody = CreateText(paper.transform, "", 34, new Color(0.2f, 0.15f, 0.1f), TextAnchor.UpperLeft);
        panelBody.horizontalOverflow = HorizontalWrapMode.Wrap;
        panelBody.verticalOverflow = VerticalWrapMode.Overflow;
        panelBody.lineSpacing = 1.3f;
        Place(panelBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(760, 400));

        var close = CreateText(paper.transform, closeText, 26, new Color(0.4f, 0.32f, 0.22f), TextAnchor.LowerRight);
        Place(close.rectTransform, new Vector2(1f, 0f), new Vector2(-40, 30), new Vector2(500, 40));
        close.rectTransform.pivot = new Vector2(1f, 0f);

        panel.SetActive(false);
    }

    private Text CreateText(Transform parent, string content, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        return t;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
