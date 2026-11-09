using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 화면 가운데에 뜨는 창 (열려 있는 동안 게임이 멈춤)
//  - 문서: 제목 + 글 (안내데스크 간호 일지)
//  - 침대: 1인칭 확대 그림 + 이불 끝의 작은 단서 (색 띠 + 숫자)
//  - 금고: 숫자 4자리 입력
// 화면은 실행할 때 코드가 직접 만든다
public class PopupUI : MonoBehaviour
{
    public static PopupUI Instance { get; private set; }

    [Header("침대 확대 그림 (침대마다 그림이 없을 때 쓰는 기본 그림. 비워두면 임시 색 판)")]
    [SerializeField] private Sprite bedCloseup;
    [SerializeField] private Vector2 clueAnchor = new Vector2(0.80f, 0.16f);   // 그림 안에서 단서 위치 (0~1)

    [Header("금고 소리 (비워두면 소리 없음)")]
    [SerializeField] private AudioClip safeOpenClip;   // 맞는 번호
    [SerializeField] private AudioClip safeFailClip;   // 틀린 번호
    [SerializeField, Range(0f, 1f)] private float safeVolume = 0.9f;
    [SerializeField] private AudioClip paperClip;      // 문서를 볼 때
    [SerializeField, Range(0f, 1f)] private float paperVolume = 0.8f;

    private enum Mode { None, Text, Bed, Keypad }
    private Mode mode = Mode.None;
    private int openFrame;

    private GameObject root;
    private GameObject textPanel, bedPanel, keypadPanel;
    private Text textTitle, textBody;
    private Image bedImage, clueBand;
    private Text clueNumber;
    private Text keypadMessage;
    private readonly Text[] digitTexts = new Text[4];
    private readonly Image[] digitBoxes = new Image[4];

    private readonly int[] digits = new int[4];
    private int cursor;
    private string code;
    private Action onCorrect;

    private static readonly Color PanelColor = new Color(0.06f, 0.06f, 0.07f, 0.96f);
    private static readonly Color TextColor = new Color(0.88f, 0.85f, 0.78f);
    private static readonly Color DimColor = new Color(0.6f, 0.6f, 0.6f);

    void Awake() { Instance = this; }
    void Start() { Build(); }
    void OnDestroy() { if (Instance == this) Instance = null; }

    public bool IsOpen => mode != Mode.None;

    // ---------- 열기 ----------
    public void ShowText(string title, string body)
    {
        textTitle.text = Loc.T(title);
        textBody.text = Loc.T(body);
        Sfx.Play(paperClip, paperVolume);
        Open(Mode.Text);
    }

    // image: 단서(색 꼬리표 + 숫자)가 그려진 침대 그림. 있으면 그 그림을 보여주고, 없으면 기본 그림 위에 코드로 단서를 그린다
    public void ShowBed(Color bandColor, string number, Sprite image = null)
    {
        bool baked = image != null;
        Sprite s = baked ? image : bedCloseup;
        bedImage.sprite = s;
        bedImage.color = s != null ? Color.white : new Color(0.32f, 0.34f, 0.33f);
        bedImage.preserveAspect = s != null;

        clueBand.gameObject.SetActive(!baked);
        clueNumber.gameObject.SetActive(!baked);
        clueBand.color = bandColor;
        clueNumber.text = number;
        Open(Mode.Bed);
    }

    public void ShowKeypad(string correctCode, Action correct)
    {
        code = correctCode;
        onCorrect = correct;
        for (int i = 0; i < 4; i++) digits[i] = 0;
        cursor = 0;
        keypadMessage.text = "";
        RefreshKeypad();
        Open(Mode.Keypad);
    }

    private void Open(Mode m)
    {
        mode = m;
        openFrame = Time.frameCount;
        root.SetActive(true);
        textPanel.SetActive(m == Mode.Text);
        bedPanel.SetActive(m == Mode.Bed);
        keypadPanel.SetActive(m == Mode.Keypad);
        GameInput.SetPopup(true);
    }

    private void Close()
    {
        mode = Mode.None;
        root.SetActive(false);
        GameInput.SetPopup(false);
    }

    // ---------- 입력 ----------
    void Update()
    {
        if (mode == Mode.None || Time.frameCount == openFrame) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.escapeKey.wasPressedThisFrame) { Close(); return; }

        if (mode == Mode.Text || mode == Mode.Bed)
        {
            if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame
                || kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                Close();
            return;
        }

        // 금고
        if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) cursor = (cursor + 3) % 4;
        if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) cursor = (cursor + 1) % 4;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) digits[cursor] = (digits[cursor] + 1) % 10;
        if (kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame) digits[cursor] = (digits[cursor] + 9) % 10;

        // 숫자 키로 바로 입력
        for (int n = 0; n <= 9; n++)
        {
            if (DigitPressed(kb, n))
            {
                digits[cursor] = n;
                cursor = Mathf.Min(cursor + 1, 3);
                break;
            }
        }

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Confirm();
        RefreshKeypad();
    }

    private static bool DigitPressed(Keyboard kb, int n)
    {
        Key top = n == 0 ? Key.Digit0 : Key.Digit1 + (n - 1);   // 숫자줄은 1~9, 0 순서
        Key pad = Key.Numpad0 + n;
        return kb[top].wasPressedThisFrame || kb[pad].wasPressedThisFrame;
    }

    private void Confirm()
    {
        string typed = $"{digits[0]}{digits[1]}{digits[2]}{digits[3]}";
        if (typed == code)
        {
            var cb = onCorrect;
            onCorrect = null;
            Sfx.Play(safeOpenClip, safeVolume);
            Close();
            cb?.Invoke();
        }
        else
        {
            keypadMessage.text = Loc.T("딸깍… 열리지 않는다");
            Sfx.Play(safeFailClip, safeVolume);
            GameLog.Info($"금고: {typed} (틀림)");
        }
    }

    private void RefreshKeypad()
    {
        for (int i = 0; i < 4; i++)
        {
            digitTexts[i].text = digits[i].ToString();
            digitBoxes[i].color = i == cursor ? new Color(0.55f, 0.45f, 0.2f) : new Color(0.18f, 0.18f, 0.2f);
        }
    }

    // ---------- 화면 만들기 ----------
    private void Build()
    {
        var canvas = UIKit.MakeCanvas(transform, "PopupUI_Canvas", 80);   // 안내 문구(50) 위, 계단 암전(90) 아래
        root = canvas.gameObject;

        // 뒤 배경을 어둡게
        var dim = UIKit.Box(root.transform, new Color(0f, 0f, 0f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, "Dim");
        UIKit.Stretch(dim.rectTransform);

        Vector2 c = new Vector2(0.5f, 0.5f);

        // 문서
        textPanel = UIKit.Box(root.transform, PanelColor, c, Vector2.zero, new Vector2(1100, 720), "TextPanel").gameObject;
        textTitle = UIKit.Label(textPanel.transform, "", 46, TextColor, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(1000, 70));
        textBody = UIKit.Label(textPanel.transform, "", 34, TextColor, new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(940, 460),
                               TextAnchor.UpperLeft);
        UIKit.Label(textPanel.transform, "↑ / Esc  닫기", 26, DimColor, new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(600, 40));

        // 침대 확대
        bedPanel = UIKit.Box(root.transform, PanelColor, c, Vector2.zero, new Vector2(1240, 760), "BedPanel").gameObject;
        bedImage = UIKit.Box(bedPanel.transform, new Color(0.32f, 0.34f, 0.33f), new Vector2(0.5f, 1f), new Vector2(0, -30),
                             new Vector2(1180, 640), "BedImage");
        if (bedCloseup != null) { bedImage.sprite = bedCloseup; bedImage.color = Color.white; bedImage.preserveAspect = true; }
        clueBand = UIKit.Box(bedImage.transform, Color.red, clueAnchor, Vector2.zero, new Vector2(150, 22), "ClueBand");
        clueBand.rectTransform.localRotation = Quaternion.Euler(0, 0, -4f);
        clueNumber = UIKit.Label(bedImage.transform, "", 30, new Color(0.15f, 0.12f, 0.1f, 0.9f), clueAnchor, new Vector2(0, -34),
                                 new Vector2(80, 40), TextAnchor.MiddleCenter, "ClueNumber");
        UIKit.Label(bedPanel.transform, "↑ / Esc  닫기", 26, DimColor, new Vector2(0.5f, 0f), new Vector2(0, 26), new Vector2(600, 40));

        // 금고
        keypadPanel = UIKit.Box(root.transform, PanelColor, c, Vector2.zero, new Vector2(820, 560), "KeypadPanel").gameObject;
        UIKit.Label(keypadPanel.transform, "금고", 46, TextColor, new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(600, 70));
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 150f;
            digitBoxes[i] = UIKit.Box(keypadPanel.transform, Color.gray, c, new Vector2(x, 20), new Vector2(120, 150), "Digit" + i);
            digitTexts[i] = UIKit.Label(digitBoxes[i].transform, "0", 90, TextColor, c, Vector2.zero, new Vector2(120, 150));
        }
        keypadMessage = UIKit.Label(keypadPanel.transform, "", 30, new Color(0.9f, 0.45f, 0.4f), c, new Vector2(0, -110), new Vector2(700, 50));
        UIKit.Label(keypadPanel.transform, "← → 자리 이동   ↑ ↓ 숫자 바꾸기   숫자 키 입력   Enter 확인   Esc 닫기", 24, DimColor,
                    new Vector2(0.5f, 0f), new Vector2(0, 30), new Vector2(780, 40));

        root.SetActive(false);
    }
}
