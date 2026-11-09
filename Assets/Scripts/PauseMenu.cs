using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 일시정지·설정 메뉴
//  게임(Main): Esc(또는 P)로 열림. 계속하기 / 설정들 / 처음부터 다시 / 타이틀로
//  타이틀(Settings Only 켬): TitleScreen의 "설정" 버튼으로만 열림. 설정들 / 뒤로
// ↑↓ 고르기, ←→ 값 바꾸기, Enter 선택, Esc 닫기
// 설정(소리 크기·프레임 제한·FPS 표시·언어)은 PlayerPrefs에 저장되어 다음에 켜도 유지된다 (웹에서도)
// 팝업·조합 게이지·연출·계단 이동·게임오버 중에는 열리지 않음 (그때 Esc는 원래 기능)
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [SerializeField] private bool settingsOnly = false;   // 타이틀 화면용 (Esc로 열리지 않고, 게임을 멈추지 않음)
    [SerializeField] private string titleScene = "Title";
    [SerializeField] private Font font;                   // 비워두면 HintUI의 폰트 (타이틀에는 HintUI가 없으니 꼭 넣기)
    [SerializeField] private AudioClip previewClip;       // 소리 크기를 바꿀 때 들려줄 소리 (비워두면 소리 없음)

    private const string KeyVolume = "opt_volume", KeyFps = "opt_fps", KeyShowFps = "opt_showfps";
    // 모니터 주사율보다 높게 골라도 모니터 최대치까지만 나옴 (웹은 브라우저가 모니터에 맞춤)
    private static readonly int[] FpsValues = { 30, 60, 120, 144, -1 };
    private static readonly string[] FpsNames = { "30", "60", "120", "144", "모니터 맞춤" };

    private enum Item { Resume, Volume, Fps, ShowFps, Fullscreen, Language, Restart, Title, Back }
    private static readonly Item[] GameItems = { Item.Resume, Item.Volume, Item.Fps, Item.ShowFps, Item.Fullscreen, Item.Language, Item.Restart, Item.Title };
    private static readonly Item[] TitleItems = { Item.Volume, Item.Fps, Item.ShowFps, Item.Fullscreen, Item.Language, Item.Back };

    private static readonly Color Normal = new Color(0.75f, 0.73f, 0.68f);
    private static readonly Color Selected = new Color(1f, 0.85f, 0.35f);

    private Item[] items;
    private int index;
    private bool busyLastFrame;
    private Action onClose;
    private GameObject panel;
    private Text header, help;
    private Text[] nameTexts, valueTexts;
    private Text fpsText;
    private AudioSource preview;
    private float fpsTimer;
    private int fpsFrames;
    private bool wasFullscreen;
    private int openedFrame = -1;   // 열린 프레임에는 입력을 받지 않음 (열 때 누른 키로 바로 닫히지 않게)
    private const int HeaderIndex = -1;   // 맨 위 "인벤토리 | 설정" 줄을 고른 상태 (← 으로 인벤토리)

    // ---------- 저장된 설정 ----------
    public static int Volume => Mathf.Clamp(PlayerPrefs.GetInt(KeyVolume, 8), 0, 10);   // 0~10 (10 = 100%)
    public static int FpsIndex => Mathf.Clamp(PlayerPrefs.GetInt(KeyFps, 1), 0, FpsValues.Length - 1);
    public static bool ShowFps => PlayerPrefs.GetInt(KeyShowFps, 0) == 1;

    // 게임을 켜면 (타이틀부터) 저장된 설정을 바로 적용
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void ApplySaved()
    {
        AudioListener.volume = Volume / 10f;
        int fps = FpsValues[FpsIndex];
        QualitySettings.vSyncCount = fps < 0 ? 1 : 0;   // 모니터 맞춤 = 수직 동기화
        Application.targetFrameRate = fps;
    }

    void Awake()
    {
        Instance = this;
        items = settingsOnly ? TitleItems : GameItems;
    }

    void Start()
    {
        ApplySaved();
        if (font != null) UIKit.FontOverride = font;
        Build();
        panel.SetActive(false);
        preview = gameObject.AddComponent<AudioSource>();
        preview.playOnAwake = false;
        preview.spatialBlend = 0f;
        preview.ignoreListenerPause = true;   // 멈춘 동안에도 들림
    }

    void OnEnable() => Loc.Changed += OnLanguageChanged;
    void OnDisable() => Loc.Changed -= OnLanguageChanged;

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (IsOpen) { AudioListener.pause = false; IsOpen = false; }
    }

    void Update()
    {
        UpdateFps();
        var kb = Keyboard.current;
        if (kb == null) return;
        bool esc = kb.escapeKey.wasPressedThisFrame;   // Esc로 열기 (P로는 열리지 않음)

        // 웹 전체 화면에서 Esc는 브라우저가 먼저 가져가서(전체 화면만 풀림) 게임에는 안 들어옴
        // → 전체 화면이 풀리는 순간을 Esc로 보고 설정창을 열어 줌 (게임도 멈춤)
        bool fullscreenExited = false;
#if UNITY_WEBGL && !UNITY_EDITOR
        fullscreenExited = wasFullscreen && !Screen.fullScreen;
        wasFullscreen = Screen.fullScreen;
#endif

        if (IsOpen) { HandleMenu(kb, esc); return; }
        if (settingsOnly) return;   // 타이틀에서는 버튼으로만 열림

        // 다른 곳에서 Esc를 쓰는 중(팝업 닫기·조합 그만두기)이면 열지 않음 (직전 프레임까지 확인)
        bool busy = GameInput.Blocked || CraftUI.IsCrafting || (PopupUI.Instance != null && PopupUI.Instance.IsOpen);
        if ((esc || fullscreenExited) && !busy && !busyLastFrame) Open();
        busyLastFrame = busy;
    }

    // 인벤토리 창에서 ← → 를 누르면 부름 (인벤토리 → 설정). 맨 위 줄이 골라져 있어서 ← 한 번이면 돌아감
    public void OpenFromInventory()
    {
        if (IsOpen || settingsOnly) return;
        Open();
        index = HeaderIndex;
        Refresh();
    }

    // 타이틀 화면의 "설정" 버튼이 부름
    public void OpenSettings(Action closed)
    {
        onClose = closed;
        Open();
    }

    private void HandleMenu(Keyboard kb, bool esc)
    {
        if (Time.frameCount == openedFrame) return;
        // 닫기는 Tab으로만 (웹 전체 화면에서 Esc는 브라우저가 가져가서 헷갈리므로 Esc·P는 열기만)
        if (kb.tabKey.wasPressedThisFrame) { Close(); return; }

        // ↑↓: 게임 중에는 맨 위 줄(인벤토리 | 설정)까지 올라감
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)
        {
            if (index == HeaderIndex) { }
            else if (index == 0 && !settingsOnly) index = HeaderIndex;
            else index = (index + items.Length - 1) % items.Length;
        }
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)
            index = index == HeaderIndex ? 0 : (index + 1) % items.Length;

        int dx = 0;
        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dx = -1;
        if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dx = 1;
        if (dx != 0)
        {
            if (index == HeaderIndex)
            {
                // 맨 위 줄에서 ← → = 인벤토리로
                Close();
                if (InventoryUI.Instance != null) InventoryUI.Instance.OpenFromSettings();
                return;
            }
            Change(items[index], dx);
        }

        if (index != HeaderIndex && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
        {
            Select(items[index]);
            if (!IsOpen) return;
        }
        Refresh();
    }

    private void Open()
    {
        IsOpen = true;
        openedFrame = Time.frameCount;
        index = 0;
        if (!settingsOnly)
        {
            GameInput.SetPopup(true);     // 시간 멈춤 + 조작 막기
            AudioListener.pause = true;   // 배경 소리·존재 소리도 멈춤
        }
        panel.SetActive(true);
        Refresh();
    }

    private void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
        if (!settingsOnly)
        {
            AudioListener.pause = false;
            GameInput.SetPopup(false);
        }
        busyLastFrame = true;         // 같은 Esc로 다시 열리지 않게
        var cb = onClose;
        onClose = null;
        cb?.Invoke();
    }

    private void Change(Item item, int dx)
    {
        switch (item)
        {
            case Item.Volume:
                PlayerPrefs.SetInt(KeyVolume, Mathf.Clamp(Volume + dx, 0, 10));
                ApplySaved();
                if (previewClip != null && preview != null) preview.PlayOneShot(previewClip, 1f);
                break;
            case Item.Fps:
                PlayerPrefs.SetInt(KeyFps, (FpsIndex + dx + FpsValues.Length) % FpsValues.Length);
                ApplySaved();
                break;
            case Item.ShowFps:
                PlayerPrefs.SetInt(KeyShowFps, ShowFps ? 0 : 1);
                break;
            case Item.Fullscreen:
                Screen.fullScreen = !Screen.fullScreen;   // 에디터 Game 창에서는 동작하지 않음 (빌드·웹에서만)
                break;
            case Item.Language:
                Loc.SetKorean(!Loc.Korean);              // 글자가 바로 바뀜 (OnLanguageChanged)
                return;
            default:
                return;
        }
        PlayerPrefs.Save();
    }

    private void Select(Item item)
    {
        switch (item)
        {
            case Item.Resume:
            case Item.Back:
                Close();
                break;
            case Item.Restart:
                Close();
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
            case Item.Title:
                Close();
                if (Application.CanStreamedLevelBeLoaded(titleScene)) SceneManager.LoadScene(titleScene);
                else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                break;
            default:
                Change(item, 1);      // 설정 칸에서 Enter = 다음 값
                break;
        }
    }

    private static string Name(Item item)
    {
        switch (item)
        {
            case Item.Resume: return "계속하기";
            case Item.Volume: return "소리 크기";
            case Item.Fps: return "프레임 제한";
            case Item.ShowFps: return "FPS 표시";
            case Item.Fullscreen: return "전체 화면";
            case Item.Language: return "언어";
            case Item.Restart: return "처음부터 다시";
            case Item.Title: return "타이틀로";
            default: return "뒤로";
        }
    }

    private static string OnOff(bool on) => $"<  {Loc.T(on ? "켜기" : "끄기")}  >";

    private string Value(Item item)
    {
        switch (item)
        {
            case Item.Volume: return $"<  {Volume * 10}%  >";
            case Item.Fps: return $"<  {Loc.T(FpsNames[FpsIndex])}  >";
            case Item.ShowFps: return OnOff(ShowFps);
            case Item.Fullscreen:
#if UNITY_EDITOR
                return Loc.T("빌드에서만");
#else
                return OnOff(Screen.fullScreen);
#endif
            case Item.Language: return Loc.Korean ? "<  한국어  >" : "<  English  >";
            default: return "";
        }
    }

    // 맨 위 줄: 게임 중에는 "인벤토리 | 설정" (지금 쪽 = 설정이 노랑). 골라져 있으면 ◀ 표시
    private void RefreshHeader()
    {
        if (header == null) return;
        if (settingsOnly) { header.text = Loc.T("설정"); return; }
        bool sel = index == HeaderIndex;
        header.text = (sel ? "<color=#FFD95A>◀</color>  " : "")
                    + $"<color=#7A7A7A>{Loc.T("인벤토리")}</color>      <color=#FFD95A>{Loc.T("설정")}</color>";
    }

    private void OnLanguageChanged()
    {
        if (header == null) return;
        RefreshHeader();
        help.text = Loc.T("↑ ↓ 고르기    ← → 바꾸기    Enter 선택    Tab 닫기");
        if (IsOpen) Refresh();
    }

    private void Refresh()
    {
        RefreshHeader();
        for (int i = 0; i < items.Length; i++)
        {
            bool sel = i == index;
            Color c = sel ? Selected : Normal;
            // 언어 칸 이름만 반대 언어로 (지금 언어를 못 읽는 사람도 찾을 수 있게): 영어일 때 "언어", 한국어일 때 "Language"
            string label = items[i] == Item.Language ? (Loc.Korean ? "Language" : "언어") : Loc.T(Name(items[i]));
            nameTexts[i].text = (sel ? "▶  " : "    ") + label;
            nameTexts[i].color = c;
            valueTexts[i].text = Value(items[i]);
            valueTexts[i].color = c;
        }
    }

    private void UpdateFps()
    {
        if (fpsText == null) return;
        fpsText.enabled = ShowFps;
        if (!ShowFps) return;
        fpsFrames++;
        fpsTimer += Time.unscaledDeltaTime;
        if (fpsTimer >= 0.5f)
        {
            fpsText.text = $"{fpsFrames / fpsTimer:0} FPS";
            fpsFrames = 0;
            fpsTimer = 0f;
        }
    }

    // ---------- 화면 만들기 ----------
    private void Build()
    {
        var canvas = UIKit.MakeCanvas(transform, "PauseMenu_Canvas", 95);   // 계단 암전(90) 위, 게임오버 화면(100) 아래

        fpsText = UIKit.Label(canvas.transform, "", 24, new Color(0.7f, 0.9f, 0.7f), new Vector2(0f, 1f), new Vector2(20, -16),
                              new Vector2(200, 36), TextAnchor.UpperLeft, "FPS");


        panel = new GameObject("PausePanel", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        UIKit.Stretch(panel.GetComponent<RectTransform>());

        var dim = UIKit.Box(panel.transform, new Color(0f, 0f, 0f, 0.7f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "Dim");
        UIKit.Stretch(dim.rectTransform);

        float rowH = 58f;
        float boxH = 260f + items.Length * rowH;
        var box = UIKit.Box(panel.transform, new Color(0.06f, 0.06f, 0.07f, 0.96f), new Vector2(0.5f, 0.5f), Vector2.zero,
                            new Vector2(780, boxH), "Box");

        header = UIKit.Label(box.transform, "", 44, new Color(0.88f, 0.85f, 0.78f), new Vector2(0.5f, 1f), new Vector2(0, -40),
                             new Vector2(740, 60));
        header.horizontalOverflow = HorizontalWrapMode.Overflow;   // "◀ 인벤토리  설정"이 두 줄로 넘어가지 않게

        nameTexts = new Text[items.Length];
        valueTexts = new Text[items.Length];
        float y = -140f;
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == Item.Restart || items[i] == Item.Back) y -= 24f;   // 설정과 나가기 사이 간격
            nameTexts[i] = UIKit.Label(box.transform, "", 32, Normal, new Vector2(0.5f, 1f), new Vector2(-110, y),
                                       new Vector2(420, 50), TextAnchor.MiddleLeft, "Name_" + items[i]);
            valueTexts[i] = UIKit.Label(box.transform, "", 32, Normal, new Vector2(0.5f, 1f), new Vector2(170, y),
                                        new Vector2(320, 50), TextAnchor.MiddleRight, "Value_" + items[i]);
            y -= rowH;
        }

        help = UIKit.Label(box.transform, "", 22, new Color(0.6f, 0.6f, 0.6f), new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(740, 36));
        OnLanguageChanged();
    }
}
