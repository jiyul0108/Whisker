using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 게임오버 → 화면 암전 → R 키로 처음부터 다시 시작
// 게임오버 화면(검은 화면 + 글자)은 실행할 때 코드가 직접 만든다
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임오버 연출")]
    [SerializeField] private float freezeTime = 0.6f;      // 잡힌 순간을 멈춰서 보여주는 시간
    [SerializeField] private float fadeTime = 1.0f;        // 화면이 까매지는 시간
    [SerializeField] private float autoRestartDelay = 0f;  // 0이면 R 키를 눌러야 재시작
    [SerializeField] private string caughtText = "CAUGHT";
    [SerializeField] private string restartText = "Press R to restart";

    public bool IsGameOver { get; private set; }

    private Image fadeImage;
    private Text titleText;
    private Text subText;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 존재에게 잡혔을 때 PresenceAI가 호출
    public void GameOver(string reason)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Debug.Log($"게임오버: {reason}");
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        // 1) 모든 움직임 정지 (잡힌 장면을 잠깐 보여줌)
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freezeTime);

        // 2) 화면 암전
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            SetFade(t / fadeTime);
            yield return null;
        }
        SetFade(1f);

        // 3) 글자 표시 후 재시작 대기
        titleText.enabled = true;
        subText.enabled = autoRestartDelay <= 0f;

        float wait = 0f;
        while (true)
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.rKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) break;
            wait += Time.unscaledDeltaTime;
            if (autoRestartDelay > 0f && wait >= autoRestartDelay) break;
            yield return null;
        }

        Restart();
    }

    // 처음부터 다시 시작 (체크포인트 없음 = 씬을 새로 불러옴)
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void SetFade(float a)
    {
        fadeImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(a));
    }

    // ---------- 게임오버 화면 만들기 ----------
    private void BuildUI()
    {
        var canvasGO = new GameObject("GameOverUI", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 화면 전체를 덮는 검은 판
        var fadeGO = new GameObject("Fade", typeof(Image));
        fadeGO.transform.SetParent(canvasGO.transform, false);
        fadeImage = fadeGO.GetComponent<Image>();
        fadeImage.raycastTarget = false;
        Stretch(fadeImage.rectTransform);
        SetFade(0f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText = CreateText(canvasGO.transform, font, caughtText, 120, new Color(0.75f, 0.05f, 0.05f), 60f);
        subText = CreateText(canvasGO.transform, font, restartText, 40, new Color(0.8f, 0.8f, 0.8f), -80f);
        titleText.enabled = false;
        subText.enabled = false;
    }

    private Text CreateText(Transform parent, Font font, string content, int size, Color color, float y)
    {
        var go = new GameObject("Text", typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        var rt = text.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1600, 200);
        rt.anchoredPosition = new Vector2(0, y);
        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
