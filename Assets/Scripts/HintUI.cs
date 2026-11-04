using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 아래 안내 문구 (숨기 안내, "잠겨 있다" 같은 짧은 메시지)
// 화면(Canvas)은 실행할 때 코드가 직접 만든다
public class HintUI : MonoBehaviour
{
    public static HintUI Instance { get; private set; }

    [Header("한글 폰트 (Assets/Fonts/Galmuri11)")]
    [SerializeField] private Font font;

    private Text hint;      // 계속 떠 있는 안내 (예: S / ↓ 숨기)
    private Text message;   // 잠깐 떴다 사라지는 메시지 (예: 잠겨 있다)
    private Coroutine messageRoutine;

    void Awake()
    {
        Instance = this;
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Debug.LogWarning("HintUI: 한글 폰트가 비어 있어요. Font 칸에 Galmuri11을 넣어주세요.");
        }
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 계속 떠 있는 안내. null이면 숨김
    public void SetHint(string text)
    {
        hint.enabled = !string.IsNullOrEmpty(text);
        if (hint.enabled) hint.text = text;
    }

    // 잠깐 보여주고 사라지는 메시지
    public void ShowMessage(string text, float seconds = 1.5f)
    {
        if (messageRoutine != null) StopCoroutine(messageRoutine);
        messageRoutine = StartCoroutine(MessageRoutine(text, seconds));
    }

    private IEnumerator MessageRoutine(string text, float seconds)
    {
        message.text = text;
        message.enabled = true;
        yield return new WaitForSecondsRealtime(seconds);
        message.enabled = false;
    }

    // ---------- 화면 만들기 ----------
    private void BuildUI()
    {
        var canvasGO = new GameObject("HintUI_Canvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;          // 게임오버 화면(100)보다 아래
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 화면 아래 가운데 (바닥판 아래 검은 공간)
        hint = CreateText(canvasGO.transform, 36, Color.white, new Vector2(0, 90));
        message = CreateText(canvasGO.transform, 36, new Color(0.9f, 0.8f, 0.6f), new Vector2(0, 150));
        hint.enabled = false;
        message.enabled = false;
    }

    private Text CreateText(Transform parent, int size, Color color, Vector2 pos)
    {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(1200, 60);
        return t;
    }
}
