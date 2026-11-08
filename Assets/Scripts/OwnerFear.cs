using UnityEngine;
using UnityEngine.UI;

// 주인 공포도 (주인 오브젝트에 붙임). 왼쪽 위에 막대로 표시
// 주인은 존재를 못 보기 때문에, 고양이와 멀어지거나 어두운 방에 있으면 오른다
// 고양이 곁(밝은 방)에 있으면 내려가고, 고양이가 울어 주면 조금 더 내려간다
// 공포도가 높을수록 주인이 느려지고, 내려가면 원래 속도로 돌아온다
[RequireComponent(typeof(OwnerController))]
public class OwnerFear : MonoBehaviour
{
    [Header("오르는 조건")]
    [SerializeField] private float farDistance = 5f;     // 고양이와 이보다 멀면 (다른 층이면 항상) 오름
    [SerializeField] private float farRise = 6f;         // 초당 (0~100)
    [SerializeField] private float darkRise = 4f;        // 어두운 방에 있으면 초당

    [Header("내려가는 조건")]
    [SerializeField] private float calmFall = 8f;        // 고양이 곁 + 밝은 방이면 초당
    [SerializeField] private float meowCalm = 10f;       // 고양이가 울어 주면 한 번에

    [Header("느려지기")]
    [SerializeField, Range(0.1f, 1f)] private float slowestSpeed = 0.4f;  // 공포도 100일 때 속도 배율
    [SerializeField] private float slowStart = 30f;      // 공포도가 이보다 높아지면 느려지기 시작

    [Header("표시")]
    [SerializeField] private bool showBar = true;

    public float Fear { get; private set; }               // 0 ~ 100
    public float SpeedMultiplier { get; private set; } = 1f;

    private CatController cat;
    private OwnerController owner;
    private OwnerAnimator anim;
    private Image fill;
    private Text label;

    void Awake()
    {
        owner = GetComponent<OwnerController>();
        anim = GetComponent<OwnerAnimator>();
    }

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        if (showBar) BuildUI();
    }

    // 고양이가 울었을 때 CatMeow가 호출
    public void Calm()
    {
        Fear = Mathf.Max(0f, Fear - meowCalm);
    }

    void Update()
    {
        if (cat == null || Time.timeScale == 0f) return;

        Vector2 me = transform.position;
        Vector2 c = cat.transform.position;
        bool sameFloor = Stairs.SameFloor(me.y, c.y);
        bool far = !sameFloor || Mathf.Abs(c.x - me.x) > farDistance;
        Room room = Room.Find(me);
        bool dark = room != null && room.IsDarkNow;

        float delta = 0f;
        if (far) delta += farRise;
        if (dark) delta += darkRise;
        if (!far && !dark) delta -= calmFall;
        Fear = Mathf.Clamp(Fear + delta * Time.deltaTime, 0f, 100f);

        // 공포도 → 속도 (Slow Start까지는 평소 속도)
        float k = Mathf.InverseLerp(slowStart, 100f, Fear);
        SpeedMultiplier = Mathf.Lerp(1f, slowestSpeed, k);

        // 떨림도 같이 커짐 (0.5 ~ 3)
        if (anim != null) anim.SetFear(Mathf.Lerp(0.5f, 3f, Fear / 100f));

        UpdateUI();
    }

    // ---------- 왼쪽 위 막대 ----------
    private void BuildUI()
    {
        var canvas = UIKit.MakeCanvas(transform, "FearUI_Canvas", 55);
        Vector2 tl = new Vector2(0f, 1f);
        label = UIKit.Label(canvas.transform, "주인 공포도", 26, new Color(0.88f, 0.85f, 0.78f), tl,
                            new Vector2(30f, -24f), new Vector2(400f, 34f), TextAnchor.MiddleLeft);
        var back = UIKit.Box(canvas.transform, new Color(0.08f, 0.08f, 0.09f, 0.8f), tl, new Vector2(30f, -64f),
                             new Vector2(320f, 24f), "FearBack");
        fill = UIKit.Box(back.transform, Color.white, new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(314f, 18f), "FearFill");
        // 채우기 (왼쪽부터 늘어남)
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        var tex = Texture2D.whiteTexture;
        fill.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (fill == null) return;
        float f = Fear / 100f;
        fill.fillAmount = f;
        // 노랑 → 주황 → 빨강
        fill.color = Color.Lerp(new Color(0.85f, 0.75f, 0.3f), new Color(0.8f, 0.1f, 0.08f), f);
        label.text = Fear >= 70f ? "주인 공포도  (몸이 굳는다…)" : "주인 공포도";
    }
}
