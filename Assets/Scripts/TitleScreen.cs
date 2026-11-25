using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 타이틀 화면 (Title 씬에 하나). 버튼: 게임 시작 / 설정
// ↑↓로 고르고 Enter·Space로 누름. 마우스로 가리키고 클릭해도 됨
// 설정은 같은 씬의 PauseMenu(Settings Only)를 연다 (언어를 바꾸면 타이틀 글자도 바로 바뀜)
// 화면은 실행할 때 코드가 직접 만든다 (메뉴 Whisker > 타이틀 씬 만들기가 칸을 채움)
// 게임을 처음 켜면 타이틀 전에 심약자 주의 화면을 먼저 보여준다 (Enter·Space·클릭으로 넘김, 한 번 켤 때 한 번만)
public class TitleScreen : MonoBehaviour
{
    [Header("연결")]
    [SerializeField] private string gameScene = "Main";
    [SerializeField] private Sprite background;      // 어둡게 깔리는 배경 (1번 방)
    [SerializeField] private Sprite catSprite;       // 오른쪽 아래 고양이
    [SerializeField] private Font font;              // Galmuri11
    [SerializeField] private AudioClip ambience;     // 배경 소리 (반복)
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.5f;

    [Header("문구 (한국어 원문. 영어는 Loc 사전에서 찾음)")]
    [SerializeField] private string title = "Whisker";
    [SerializeField] private string subtitle = "고양이만 볼 수 있는 것";
    [SerializeField] private string story = "폐병원에 갇힌 주인은 그곳을 떠도는 '존재'를 보지 못한다.\n그것을 볼 수 있는 건 고양이뿐.";

    // 심약자 주의 화면 문구 (한국어 원문. 영어는 Loc 사전에서 찾음)
    private const string WarnTitle = "주의";
    private const string WarnBody = "이 게임에는 갑자기 나타나는 귀신, 비명 소리, 어두운 화면 등 공포 연출이 포함되어 있습니다.\n심장이 약하거나 공포 연출에 민감한 분은 플레이에 주의해 주세요.\n헤드폰을 쓴다면 소리를 조금 낮춰 주세요.";
    private const string WarnHelp = "Enter  계속";
    private static bool warned;     // 이번에 게임을 켠 뒤 주의 화면을 이미 봤는가 (타이틀로 돌아오면 다시 안 보여줌)

    private static readonly string[] ButtonNames = { "게임 시작", "설정" };
    private static readonly Color ButtonNormal = new Color(0.7f, 0.68f, 0.63f);
    private static readonly Color ButtonSelected = new Color(1f, 0.85f, 0.35f);

    private Text titleText, subtitleText, storyText, helpText;
    private Text[] buttons;
    private Image fade;
    private AudioSource source;
    private bool starting;
    private float flickerTimer;
    private int index;
    private bool settingsWasOpen;   // 설정을 닫은 그 Enter로 버튼이 또 눌리지 않게
    private bool warning;           // 심약자 주의 화면을 보여주는 중
    private GameObject warnRoot;
    private Text warnTitleText, warnBodyText, warnHelpText;

    void Start()
    {
        Time.timeScale = 1f;
        if (font != null) UIKit.FontOverride = font;
        Build();
        ApplyLanguage();
        if (ambience != null)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = ambience;
            source.loop = true;
            source.volume = 0f;
            source.Play();
        }
        if (!warned) StartCoroutine(WarningThenTitle());
        else StartCoroutine(FadeFrom(1f, 0f, 1.2f));
    }

    // 심약자 주의 화면 → 키를 누르면 사라지고 타이틀이 밝아짐
    private IEnumerator WarningThenTitle()
    {
        warning = true;
        warned = true;
        BuildWarning();
        SetWarnAlpha(0f);
        float t = 0f;
        while (t < 0.8f) { t += Time.deltaTime; SetWarnAlpha(t / 0.8f); yield return null; }
        SetWarnAlpha(1f);
        yield return new WaitForSeconds(0.6f);   // 바로 넘기지 않도록 잠깐 기다림

        while (true)
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if ((kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                || (mouse != null && mouse.leftButton.wasPressedThisFrame)) break;
            warnHelpText.color = new Color(0.6f, 0.6f, 0.6f, 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 2f)));
            yield return null;
        }

        t = 0f;
        while (t < 0.6f) { t += Time.deltaTime; SetWarnAlpha(1f - t / 0.6f); yield return null; }
        Destroy(warnRoot);
        warning = false;
        yield return FadeFrom(1f, 0f, 1.2f);
    }

    private void BuildWarning()
    {
        warnRoot = new GameObject("WarningCanvas", typeof(Canvas), typeof(CanvasScaler));
        warnRoot.transform.SetParent(transform, false);
        var canvas = warnRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;   // 타이틀·설정 위
        var scaler = warnRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = warnRoot.transform;

        Stretch(Img(root, "Black", Color.black));
        warnTitleText = Txt(root, Loc.T(WarnTitle), 72, new Vector2(0.5f, 0.5f), new Vector2(0, 190), new Vector2(1600, 100));
        warnTitleText.color = new Color(0.8f, 0.2f, 0.18f);
        warnBodyText = Txt(root, Loc.T(WarnBody), 34, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(1700, 260));
        warnBodyText.color = new Color(0.85f, 0.83f, 0.78f);
        warnBodyText.lineSpacing = 1.6f;
        warnHelpText = Txt(root, Loc.T(WarnHelp), 28, new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(1000, 40));
        warnHelpText.color = new Color(0.6f, 0.6f, 0.6f);
    }

    private void SetWarnAlpha(float a)
    {
        if (warnTitleText == null) return;
        SetA(warnTitleText, a);
        SetA(warnBodyText, a);
        SetA(warnHelpText, a);
    }

    private static void SetA(Text t, float a) { var c = t.color; c.a = a; t.color = c; }

    void OnEnable() => Loc.Changed += ApplyLanguage;
    void OnDisable() => Loc.Changed -= ApplyLanguage;

    void Update()
    {
        if (warning) return;   // 주의 화면을 보는 동안에는 배경 소리·메뉴 멈춤
        if (source != null && !starting) source.volume = Mathf.MoveTowards(source.volume, ambienceVolume, 0.3f * Time.deltaTime);

        // 제목은 가끔 형광등처럼 깜빡
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f)
        {
            bool off = Random.value < 0.25f;
            titleText.color = new Color(0.85f, 0.82f, 0.75f, off ? 0.35f : 1f);
            flickerTimer = off ? Random.Range(0.04f, 0.12f) : Random.Range(0.8f, 3f);
        }

        bool settingsOpen = PauseMenu.IsOpen;
        helpText.enabled = !settingsOpen;
        foreach (var b in buttons) b.enabled = !settingsOpen;
        bool justClosed = settingsWasOpen && !settingsOpen;
        settingsWasOpen = settingsOpen;
        if (starting || settingsOpen || justClosed) return;

        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) index = (index + buttons.Length - 1) % buttons.Length;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) index = (index + 1) % buttons.Length;
        }

        bool click = false;
        if (mouse != null)
        {
            Vector2 mp = mouse.position.ReadValue();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(buttons[i].rectTransform, mp, null)) continue;
                if (mouse.delta.ReadValue().sqrMagnitude > 0f) index = i;   // 가리키면 선택
                if (mouse.leftButton.wasPressedThisFrame) { index = i; click = true; }
            }
        }
        RefreshButtons();

        bool press = click || (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame));
        if (!press) return;
        if (index == 0) StartCoroutine(StartGame());
        else if (PauseMenu.Instance != null) PauseMenu.Instance.OpenSettings(null);
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            bool sel = i == index;
            buttons[i].text = sel ? $"▶  {Loc.T(ButtonNames[i])}  ◀" : Loc.T(ButtonNames[i]);
            float pulse = sel ? 0.8f + 0.2f * Mathf.Abs(Mathf.Sin(Time.time * 2.5f)) : 1f;
            Color c = sel ? ButtonSelected : ButtonNormal;
            buttons[i].color = new Color(c.r, c.g, c.b, pulse);
        }
    }

    private void ApplyLanguage()
    {
        if (warnTitleText != null)
        {
            warnTitleText.text = Loc.T(WarnTitle);
            warnBodyText.text = Loc.T(WarnBody);
            warnHelpText.text = Loc.T(WarnHelp);
        }
        if (titleText == null) return;
        titleText.text = title;
        subtitleText.text = Loc.T(subtitle);
        storyText.text = Loc.T(story);
        helpText.text = Loc.T("↑ ↓ 고르기    Enter 선택");
        RefreshButtons();
    }

    private IEnumerator StartGame()
    {
        starting = true;
        float t = 0f, v0 = source != null ? source.volume : 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            fade.color = new Color(0f, 0f, 0f, t);
            if (source != null) source.volume = v0 * (1f - t);
            yield return null;
        }
        GameManager.ResetRun();   // 새 판: 플레이 시간·잡힌 횟수를 처음부터
        SceneManager.LoadScene(gameScene);
    }

    private IEnumerator FadeFrom(float from, float to, float time)
    {
        float t = 0f;
        while (t < time)
        {
            t += Time.deltaTime;
            fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        fade.color = new Color(0f, 0f, 0f, to);
    }

    // ---------- 화면 만들기 ----------
    private void Build()
    {
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var go = new GameObject("TitleCanvas", typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var root = go.transform;

        // 배경: 검정 위에 어둡게 깐 1번 방
        Stretch(Img(root, "Black", Color.black));
        if (background != null)
        {
            var bg = Img(root, "Background", new Color(0.32f, 0.32f, 0.36f));
            bg.sprite = background;
            bg.preserveAspect = true;
            Stretch(bg);
        }
        // 위아래 어둡게 (비네트 대신 띠)
        var top = Img(root, "ShadeTop", new Color(0f, 0f, 0f, 0.55f));
        Place(top.rectTransform, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(1920, 330));
        var bottom = Img(root, "ShadeBottom", new Color(0f, 0f, 0f, 0.6f));
        Place(bottom.rectTransform, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(1920, 300));

        if (catSprite != null)
        {
            var c = Img(root, "Cat", new Color(0.55f, 0.55f, 0.6f));
            c.sprite = catSprite;
            c.preserveAspect = true;
            Place(c.rectTransform, new Vector2(1f, 0f), new Vector2(-180f, 200f), new Vector2(212, 196));
        }

        titleText = Txt(root, title, 150, new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1600, 190));
        subtitleText = Txt(root, "", 52, new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(1600, 80));
        subtitleText.color = new Color(0.75f, 0.2f, 0.18f);
        storyText = Txt(root, "", 30, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1600, 120));
        storyText.color = new Color(0.75f, 0.73f, 0.68f);

        buttons = new Text[ButtonNames.Length];
        for (int i = 0; i < buttons.Length; i++)
            buttons[i] = Txt(root, "", 44, new Vector2(0.5f, 0.5f), new Vector2(0, -150 - i * 80), new Vector2(560, 70));

        helpText = Txt(root, "", 24, new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(1000, 40));
        helpText.color = new Color(0.55f, 0.55f, 0.55f);

        fade = Img(root, "Fade", Color.black);
        Stretch(fade);
    }

    private static Image Img(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private Text Txt(Transform parent, string s, int size, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = new Color(0.9f, 0.88f, 0.82f);
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.3f;
        Place(t.rectTransform, anchor, pos, box);
        return t;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(Image img)
    {
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
