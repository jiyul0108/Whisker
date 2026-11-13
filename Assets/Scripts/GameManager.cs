using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 게임오버 / 탈출 성공 → 화면 암전 → R 키로 처음부터 다시 시작
// 게임오버 화면(검은 화면 + 글자)은 실행할 때 코드가 직접 만든다
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("게임오버 연출")]
    [SerializeField] private float freezeTime = 0.6f;      // 잡힌 순간을 멈춰서 보여주는 시간
    [SerializeField] private float fadeTime = 1.0f;        // 화면이 까매지는 시간
    [SerializeField] private float autoRestartDelay = 0f;  // 0이면 R 키를 눌러야 재시작
    [SerializeField] private string caughtTitle = "잡혔다…";
    [SerializeField] private string caughtHint = "R  처음부터 다시 시작";
    [SerializeField] private AudioClip caughtClip;          // 잡혔을 때 소리 (비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float caughtVolume = 1f;

    [Header("탈출 성공 연출")]
    [SerializeField] private string clearTitle = "탈출 성공";
    [SerializeField] private string clearHint = "주인과 함께 폐병원을 빠져나왔다\n\nR  다시 하기";
    [SerializeField] private AudioClip clearClip;           // 탈출했을 때 소리 (비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float clearVolume = 1f;

    [Header("글꼴 (비워두면 안내 문구와 같은 Galmuri)")]
    [SerializeField] private Font font;

    public bool IsGameOver { get; private set; }   // 게임이 끝났는가 (잡힘 또는 탈출)
    public bool IsCleared { get; private set; }    // 탈출에 성공했는가

    // 플레이 시간·잡힌 횟수: 잡혀서 처음부터 다시 해도 이어서 셈 (타이틀로 가거나 탈출 후 다시 하면 새로 셈)
    private static float runStart = -1f;
    private static int deaths;
    public static float PlayTime => runStart < 0f ? 0f : Time.time - runStart;
    public static int Deaths => deaths;

    // 새 판 시작 (TitleScreen이 게임을 시작할 때, 탈출 후 다시 할 때)
    public static void ResetRun()
    {
        runStart = -1f;
        deaths = 0;
    }

    private Image fadeImage;
    private Text titleText;
    private Text subText;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        if (runStart < 0f) runStart = Time.time;   // 처음 시작할 때만 (다시 시작하면 이어서)
        BuildUI();
    }

    void Start()
    {
        // 한글이 깨지지 않게 Galmuri (HintUI가 준비된 뒤에 적용)
        Font f = font != null ? font : UIKit.DefaultFont;
        titleText.font = f;
        subText.font = f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // 존재에게 잡혔을 때 PresenceAI가 호출
    // shown: 화면에 함께 보여줄 이유 (예: "주인이 잡혔다", "숨는 걸 들켰다"). 비우면 안 보여줌
    public void GameOver(string reason, string shown = null)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        deaths++;
        Sfx.Play(caughtClip, caughtVolume);
        GameLog.Info($"게임오버: {reason} (잡힌 횟수 {deaths})");
        titleText.text = Loc.T(caughtTitle);
        titleText.color = new Color(0.75f, 0.05f, 0.05f);
        subText.text = (string.IsNullOrEmpty(shown) ? "" : Loc.T(shown) + "\n\n")
                       + Loc.F("플레이 시간  {0}", FormatTime(PlayTime)) + "\n\n" + Loc.T(caughtHint);
        StartCoroutine(EndRoutine(freezeTime));
    }

    // 탈출문을 열었을 때 LockedDoor(탈출문)가 호출
    public void GameClear()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        IsCleared = true;
        Sfx.Play(clearClip, clearVolume);
        float t = PlayTime;
        GameLog.Info($"탈출 성공! 플레이 시간 {t:0}초, 잡힌 횟수 {deaths}");
        titleText.text = Loc.T(clearTitle);
        titleText.color = new Color(0.85f, 0.8f, 0.6f);
        subText.text = Loc.F("플레이 시간  {0}", FormatTime(t)) + "\n" + Loc.F("잡힌 횟수  {0}", deaths)
                       + "\n\n" + Loc.T(clearHint);
        StartCoroutine(EndRoutine(0.3f));
    }

    private IEnumerator EndRoutine(float freeze)
    {
        // 1) 모든 움직임 정지 (마지막 장면을 잠깐 보여줌)
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(freeze);

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

    private static string FormatTime(float t) => $"{(int)t / 60:00}:{(int)t % 60:00}";

    // 처음부터 다시 시작 (체크포인트 없음 = 씬을 새로 불러옴). 탈출한 뒤라면 시간도 새로
    public void Restart()
    {
        if (IsCleared) ResetRun();
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

        Font f = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");   // Start에서 Galmuri로 바뀜
        titleText = CreateText(canvasGO.transform, f, caughtTitle, 120, new Color(0.75f, 0.05f, 0.05f), 80f);
        subText = CreateText(canvasGO.transform, f, caughtHint, 40, new Color(0.8f, 0.8f, 0.8f), -110f);
        // 여러 줄(이유·플레이 시간·안내)이어도 제목과 겹치지 않게 위에서부터 아래로 씀
        subText.alignment = TextAnchor.UpperCenter;
        subText.rectTransform.pivot = new Vector2(0.5f, 1f);
        subText.rectTransform.anchoredPosition = new Vector2(0f, -10f);
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
        text.verticalOverflow = VerticalWrapMode.Overflow;   // 여러 줄이어도 잘리지 않게
        text.lineSpacing = 1.2f;
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
