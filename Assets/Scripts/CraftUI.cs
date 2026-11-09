using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 무언가를 만드는 동안 화면 아래에 뜨는 게이지 (수술대에서 출구 열쇠 만들기)
// 만드는 동안 고양이는 움직일 수 없고(시간은 흐름 → 존재는 계속 움직임), Esc로 그만둘 수 있다
// 다 차면 onDone 호출. 그만두거나 잡히면 아무 일도 없음 (재료는 그대로)
// 화면은 실행할 때 코드가 직접 만든다
public class CraftUI : MonoBehaviour
{
    public static CraftUI Instance { get; private set; }
    public static bool IsCrafting { get; private set; }

    [Header("만드는 동안 소리 (비워두면 소리 없음, 반복 재생)")]
    [SerializeField] private AudioClip loopClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;

    [Header("문구")]
    [SerializeField] private string cancelMessage = "작업을 멈췄다";

    private const float BarWidth = 640f;

    private GameObject root;
    private Text titleText, percentText;
    private Image fill;
    private AudioSource source;

    void Awake()
    {
        Instance = this;
        IsCrafting = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        IsCrafting = false;
    }

    void Start() { Build(); }

    public void Run(float seconds, string title, Action onDone)
    {
        if (IsCrafting) return;
        StartCoroutine(Routine(seconds, title, onDone));
    }

    private IEnumerator Routine(float seconds, string title, Action onDone)
    {
        IsCrafting = true;
        GameInput.SetCutscene(true);
        titleText.text = Loc.T(title);
        SetProgress(0f);
        root.SetActive(true);
        if (loopClip != null) { source.clip = loopClip; source.volume = volume; source.loop = true; source.Play(); }
        GameLog.Info($"제작 시작: {title}");

        float t = 0f;
        bool finished = false;
        yield return null;   // 조사 키를 누른 프레임은 건너뜀
        while (true)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsGameOver) break;   // 잡힘

            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (HintUI.Instance != null) HintUI.Instance.ShowMessage(cancelMessage, 1.5f);
                GameLog.Info("제작 취소");
                break;
            }

            t += Time.deltaTime;
            SetProgress(t / seconds);
            if (t >= seconds) { finished = true; break; }
            yield return null;
        }

        if (source.isPlaying) source.Stop();
        root.SetActive(false);
        GameInput.SetCutscene(false);
        IsCrafting = false;
        if (finished) onDone?.Invoke();
    }

    private void SetProgress(float p)
    {
        p = Mathf.Clamp01(p);
        fill.rectTransform.sizeDelta = new Vector2(BarWidth * p, fill.rectTransform.sizeDelta.y);
        percentText.text = $"{Mathf.FloorToInt(p * 100f)}%";
    }

    // ---------- 화면 만들기 ----------
    private void Build()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;

        var canvas = UIKit.MakeCanvas(transform, "CraftUI_Canvas", 58);   // 공포도(55) 위, 인벤토리(60) 아래
        root = canvas.gameObject;

        Vector2 bottom = new Vector2(0.5f, 0f);
        var panel = UIKit.Box(root.transform, new Color(0.05f, 0.05f, 0.06f, 0.85f), bottom, new Vector2(0, 150), new Vector2(980, 150), "CraftPanel");   // 영어 문구도 한 줄에 들어가게 넓게
        Vector2 c = new Vector2(0.5f, 0.5f);

        titleText = UIKit.Label(panel.transform, "", 32, new Color(0.88f, 0.85f, 0.78f), new Vector2(0.5f, 1f), new Vector2(0, -14), new Vector2(940, 44));
        titleText.horizontalOverflow = HorizontalWrapMode.Overflow;   // 길어도 두 줄로 잘리지 않게

        var bg = UIKit.Box(panel.transform, new Color(0.18f, 0.18f, 0.2f), c, new Vector2(0, -6), new Vector2(BarWidth + 8, 36), "BarBG");
        fill = UIKit.Box(bg.transform, new Color(0.85f, 0.55f, 0.2f), new Vector2(0f, 0.5f), new Vector2(4, 0), new Vector2(0, 28), "BarFill");
        percentText = UIKit.Label(bg.transform, "0%", 24, Color.white, c, Vector2.zero, new Vector2(200, 36));

        UIKit.Label(panel.transform, "Esc  그만두기", 22, new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0, 12), new Vector2(400, 30));

        root.SetActive(false);
    }
}
