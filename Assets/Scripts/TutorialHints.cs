using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 1번 방(시작 방) 벽에 그림으로 보여주는 조작 안내. 흰 테두리만 있는 고양이·주인이 반복해서 움직인다
// 왼쪽부터 차례로: ① ← → 이동  ② Space 점프  ③ E 울기 → 주인이 걸어옴  ④ ↓ 숨기  ⑤ 열쇠에 닿으면 줍기 → Tab 가방
// 고양이가 1번 방에 있을 때만 보이고, 나가면 천천히 사라진다 (월드 공간 캔버스라 어두워도 보임)
// 그림은 Resources/Tutorial 폴더에서 스스로 불러온다 (칸 연결 필요 없음)
public class TutorialHints : MonoBehaviour
{
    [Header("위치")]
    [SerializeField] private string roomName = "1번 방";
    [SerializeField] private float heightAboveFloor = 1.75f;   // 그림 아래쪽이 바닥에서 이만큼 위 (그림 높이는 최대 약 2칸)
    [SerializeField] private float[] stationX = { -19.5f, -13.6f, -7.2f, -0.8f, 5.4f };   // 그림 다섯 개의 가운데 X (1번 방 -24 ~ 9)

    [Header("모양")]
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private float fadeSpeed = 2f;

    private const float Scale = 0.008f;   // 그림 1px = 0.008칸

    private CatController cat;
    private Room room;
    private Font font;
    private CanvasGroup group;
    private float floorY;
    private float time;
    private readonly List<System.Action<float>> ticks = new List<System.Action<float>>();

    // Resources/Tutorial 그림
    private Sprite catIdle, catMeow, catHide, jumpUp, jumpFall, box, key, ownerIdle;
    private Sprite[] catWalk, ownerWalk;

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        foreach (var r in Room.All) if (r != null && r.RoomName == roomName) room = r;
        if (room == null) { Debug.LogWarning($"TutorialHints: '{roomName}'을 찾지 못했어요"); return; }
        font = UIKit.DefaultFont;
        LoadSprites();

        // 바닥 높이 (방 가운데에서 아래로)
        Bounds b = room.GetComponent<Collider2D>().bounds;
        floorY = b.min.y + 1.5f;
        var hit = Physics2D.Raycast(new Vector2(b.center.x, b.center.y), Vector2.down, b.size.y, LayerMask.GetMask("Ground"));
        if (hit.collider != null) floorY = hit.point.y;

        var holder = new GameObject("Tutorial_Pictures", typeof(CanvasGroup));
        holder.transform.SetParent(transform, false);
        group = holder.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;

        BuildMove(X(0));
        BuildJump(X(1));
        BuildMeow(X(2));
        BuildHide(X(3));
        BuildPickup(X(4));
    }

    private float X(int i) => i < stationX.Length ? stationX[i] : room.transform.position.x;

    void LateUpdate()
    {
        if (group == null || cat == null) return;
        bool show = Room.Find(cat.transform.position) == room && !GameInput.Blocked;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, fadeSpeed * Time.unscaledDeltaTime);
        if (group.alpha <= 0f) return;
        time += Time.deltaTime;
        foreach (var t in ticks) t(time);
    }

    private void LoadSprites()
    {
        Sprite L(string n)
        {
            var s = Resources.Load<Sprite>("Tutorial/" + n);
            if (s == null) Debug.LogWarning("TutorialHints: 그림을 찾지 못했어요 Resources/Tutorial/" + n);
            return s;
        }
        catIdle = L("tut_cat_idle"); catMeow = L("tut_cat_meow"); catHide = L("tut_cat_hide");
        jumpUp = L("tut_cat_jump_up"); jumpFall = L("tut_cat_jump_fall");
        box = L("tut_box"); key = L("tut_key"); ownerIdle = L("tut_owner_idle");
        catWalk = new[] { L("tut_cat_walk_01"), L("tut_cat_walk_02"), L("tut_cat_walk_03"), L("tut_cat_walk_04") };
        ownerWalk = new[] { L("tut_owner_walk_01"), L("tut_owner_walk_02"), L("tut_owner_walk_03"), L("tut_owner_walk_04") };
    }

    // =========================================================
    // ① ← → 이동: 고양이가 오른쪽으로 걷고(→) 왼쪽으로 돌아옴(A)
    private void BuildMove(float cx)
    {
        const float W = 440f, H = 230f, loop = 4f;
        var rt = Panel("Move", cx, W, H);
        var c = Pic(rt, catWalk[0], 60f, 0f);
        var a = Key(rt, "←", W * 0.5f - 70f, 170f, 56f);
        var d = Key(rt, "→", W * 0.5f + 14f, 170f, 56f);
        float x0 = 20f, x1 = W - 20f - Size(catWalk[0]).x;
        ticks.Add(t =>
        {
            t %= loop;
            bool right = t < 1.6f, left = t >= 2f && t < 3.6f;
            float k = right ? t / 1.6f : t < 2f ? 1f : left ? 1f - (t - 2f) / 1.6f : 0f;
            Place(c, Mathf.Lerp(x0, x1, k), 0f);
            Flip(c, !(left || (t >= 3.6f)));            // 왼쪽으로 갈 때·돌아온 뒤에는 왼쪽을 봄
            c.sprite = (right || left) ? Frame(catWalk, t, 9f) : catIdle;
            Lit(a, left);
            Lit(d, right);
        });
    }

    // ② Space 점프
    private void BuildJump(float cx)
    {
        const float W = 360f, H = 250f, loop = 2.6f;
        var rt = Panel("Jump", cx, W, H);
        var sp = Key(rt, "Space", 0f, 40f, 140f);           // 고양이 옆 (점프할 때 겹치지 않게)
        float x = 170f + (190f - Size(catIdle).x) * 0.5f;
        var c = Pic(rt, catIdle, x, 0f);
        ticks.Add(t =>
        {
            t %= loop;
            const float jump = 0.7f, air = 0.8f;
            float k = Mathf.InverseLerp(jump, jump + air, t);
            bool inAir = k > 0f && k < 1f;
            float y = inAir ? Mathf.Sin(k * Mathf.PI) * 90f : 0f;
            c.sprite = !inAir ? catIdle : k < 0.5f ? jumpUp : jumpFall;
            Vector2 sz = Size(c.sprite);
            c.rectTransform.sizeDelta = sz;                   // 점프 그림은 크기가 다름
            Place(c, 170f + (190f - sz.x) * 0.5f, y);
            Lit(sp, t >= jump - 0.1f && t < jump + 0.15f);
        });
    }

    // ③ E 울기 → 주인이 고양이 곁까지 걸어옴
    private void BuildMeow(float cx)
    {
        const float W = 560f, H = 300f, loop = 4.2f;
        var rt = Panel("Meow", cx, W, H);
        Vector2 cs = Size(catIdle);
        float catX = W - 30f - cs.x;
        var c = Pic(rt, catIdle, catX, 0f);
        Flip(c, false);                                          // 주인 쪽(왼쪽)을 봄
        var o = Pic(rt, ownerIdle, 30f, 0f);
        var e = Key(rt, "E", catX + cs.x * 0.5f - 30f, cs.y + 40f, 60f);
        var waves = new Text[3];
        for (int i = 0; i < 3; i++) waves[i] = Txt(rt, "(", 40 + i * 10, catX - 40f - i * 34f, cs.y * 0.45f - i * 5f, 40f, 80f);
        float startX = 30f, endX = catX - Size(ownerWalk[0]).x - 15f;
        ticks.Add(t =>
        {
            t %= loop;
            const float press = 0.6f, walkStart = 0.95f, walkEnd = 2.7f, resetStart = 3.7f;
            Lit(e, t >= press && t < press + 0.25f);
            c.sprite = t >= press && t < press + 0.9f ? catMeow : catIdle;
            for (int i = 0; i < 3; i++)
            {
                float k = Mathf.InverseLerp(press + i * 0.12f, press + 0.9f + i * 0.12f, t);
                SetAlpha(waves[i], k > 0f && k < 1f ? Mathf.Sin(k * Mathf.PI) : 0f);
            }
            float w = Mathf.InverseLerp(walkStart, walkEnd, t);
            bool walking = t > walkStart && t < walkEnd;
            o.sprite = walking ? Frame(ownerWalk, t, 8f) : ownerIdle;
            Place(o, t >= resetStart ? endX : Mathf.Lerp(startX, endX, w), 0f);
            float fade = t < 0.3f ? t / 0.3f : t < resetStart ? 1f : 1f - Mathf.InverseLerp(resetStart, loop, t);
            SetAlpha(o, fade);
        });
    }

    // ④ ↓ 숨기: 상자까지 걸어가 ↓ → 상자 속에서 눈만 → 다시 ↓로 나옴
    private void BuildHide(float cx)
    {
        const float W = 420f, H = 260f, loop = 5f;
        var rt = Panel("Hide", cx, W, H);
        Vector2 bs = Size(box);
        float boxX = W - 30f - bs.x;
        var c = Pic(rt, catIdle, 20f, 0f);
        Pic(rt, box, boxX, 0f);
        var eyes = Pic(rt, catHide, boxX + (bs.x - Size(catHide).x) * 0.5f, 6f);
        var s = Key(rt, "↓", boxX + bs.x * 0.5f - 28f, bs.y + 40f, 56f);
        float x0 = 20f, x1 = boxX + (bs.x - Size(catWalk[0]).x) * 0.5f;
        ticks.Add(t =>
        {
            t %= loop;
            const float hideAt = 1.7f, outAt = 3.6f;
            bool walking = t < 1.5f;
            bool hidden = t >= hideAt && t < outAt;
            Place(c, walking ? Mathf.Lerp(x0, x1, t / 1.5f) : x1, 0f);
            c.sprite = walking ? Frame(catWalk, t, 9f) : catIdle;
            SetAlpha(c, hidden ? 0f : t > 4.5f ? 1f - (t - 4.5f) / 0.5f : t < 0.2f ? t / 0.2f : 1f);
            // 숨은 동안: 상자 속 고양이(테두리 흐리게) + 눈 깜빡
            float blink = (t > 2.7f && t < 2.85f) ? 0f : 1f;
            SetAlpha(eyes, hidden ? 0.6f * blink : 0f);
            Lit(s, (t >= hideAt - 0.15f && t < hideAt + 0.1f) || (t >= outAt - 0.15f && t < outAt + 0.1f));
        });
    }

    // ⑤ 열쇠에 닿으면 줍음(키 없음) → Tab으로 가방을 열면 들어있음
    private void BuildPickup(float cx)
    {
        const float W = 560f, H = 250f, loop = 5.2f;
        var rt = Panel("Pickup", cx, W, H);
        Vector2 ks = Size(key);
        float keyX = 200f;
        var c = Pic(rt, catIdle, 10f, 0f);
        var k = Pic(rt, key, keyX, 6f);

        // 가방 (칸 3개) — 오른쪽, Tab 키는 그 위
        var bag = Rect(rt, "Bag", W - 200f, 110f, 190f, 76f);
        var bagGroup = bag.gameObject.AddComponent<CanvasGroup>();
        for (int i = 0; i < 3; i++) Border(Rect(bag, "Slot", 8f + i * 60f, 10f, 56f, 56f), 3f);
        var inBag = Pic(bag, key, 0f, 0f);
        inBag.rectTransform.sizeDelta = new Vector2(46f, 24f);        // 칸에 맞게 작게
        Place(inBag, 8f + 5f, 10f + 16f);
        var tab = Key(rt, "Tab", W - 150f, 192f, 90f);

        // 고양이가 열쇠 위까지 걸어가면(닿으면) 열쇠가 떠오르며 사라짐
        float x0 = 10f, x1 = keyX + (ks.x - Size(catWalk[0]).x) * 0.5f;
        ticks.Add(t =>
        {
            t %= loop;
            const float pickAt = 1.3f, tabAt = 2.5f;
            bool walking = t < pickAt;
            Place(c, walking ? Mathf.Lerp(x0, x1, t / pickAt) : x1, 0f);
            c.sprite = walking ? Frame(catWalk, t, 9f) : catIdle;
            SetAlpha(c, t > 4.7f ? 1f - (t - 4.7f) / 0.5f : t < 0.2f ? t / 0.2f : 1f);

            float rise = Mathf.InverseLerp(pickAt, pickAt + 0.5f, t);   // 열쇠가 떠오르며 사라짐
            Place(k, keyX, 6f + rise * 60f);
            SetAlpha(k, t < pickAt ? 1f : 1f - rise);

            Lit(tab, t >= tabAt - 0.1f && t < tabAt + 0.15f);
            bool open = t >= tabAt && t < tabAt + 1.8f;
            bagGroup.alpha = Mathf.MoveTowards(bagGroup.alpha, open ? 1f : 0f, Time.deltaTime * 6f);
            SetAlpha(inBag, 1f);
        });
    }

    // =========================================================
    // 만들기 도우미 (패널 좌표: 왼쪽 아래 0,0 / px)

    private RectTransform Panel(string name, float cx, float w, float h)
    {
        var go = new GameObject("Tutorial_" + name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(group.transform, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 45;                                    // 어두운 방 덮개(40) 위
        go.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        rt.localScale = Vector3.one * Scale;
        rt.position = new Vector3(cx, floorY + heightAboveFloor + h * Scale * 0.5f, 0f);
        return rt;
    }

    private static RectTransform Rect(RectTransform parent, string name, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x + w * 0.5f, y);
        return rt;
    }

    private static Vector2 Size(Sprite s) => s != null ? s.rect.size : new Vector2(100f, 100f);

    private Image Pic(RectTransform parent, Sprite s, float x, float y)
    {
        Vector2 size = Size(s);
        var img = Rect(parent, "Pic", x, y, size.x, size.y).gameObject.AddComponent<Image>();
        img.sprite = s;
        img.enabled = s != null;      // 그림이 없으면 흰 네모가 보이지 않게
        img.color = lineColor;
        img.raycastTarget = false;
        return img;
    }

    private static void Place(Image img, float x, float y)
    {
        var rt = img.rectTransform;
        rt.anchoredPosition = new Vector2(x + rt.sizeDelta.x * 0.5f, y);
    }

    // 오른쪽을 보면 true (원래 그림은 오른쪽을 봄)
    private static void Flip(Image img, bool faceRight)
    {
        img.rectTransform.localScale = new Vector3(faceRight ? 1f : -1f, 1f, 1f);
    }

    private static Sprite Frame(Sprite[] frames, float t, float fps) => frames[(int)(t * fps) % frames.Length];

    private void SetAlpha(Graphic g, float a)
    {
        Color c = lineColor;
        if (g is Image img && img.sprite == catHide) c = Color.white;   // 숨은 고양이는 눈 색을 살림
        g.color = new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01(a));
    }

    // 테두리만 있는 키 (눌리면 흰색으로 차고 글자가 어두워짐)
    private class KeyCap { public Image fill; public Text text; }

    private KeyCap Key(RectTransform parent, string label, float x, float y, float w)
    {
        const float h = 56f;
        var box = Rect(parent, "Key_" + label, x, y, w, h);
        var fill = box.gameObject.AddComponent<Image>();
        fill.color = Color.clear;
        fill.raycastTarget = false;
        Border(box, 4f);
        var text = Txt(box, label, label.Length > 1 ? 26 : 32, 0f, 0f, w, h);
        return new KeyCap { fill = fill, text = text };
    }

    private void Lit(KeyCap k, bool on)
    {
        k.fill.color = new Color(1f, 1f, 1f, on ? 0.85f : 0f);
        k.text.color = on ? new Color(0.1f, 0.1f, 0.1f) : lineColor;
        k.fill.rectTransform.localScale = Vector3.one * (on ? 0.92f : 1f);
    }

    private void Border(RectTransform box, float th)
    {
        Line(box, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -th), Vector2.zero);
        Line(box, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, th));
        Line(box, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(th, 0));
        Line(box, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-th, 0), Vector2.zero);
    }

    private void Line(RectTransform box, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(box, false);
        var img = go.GetComponent<Image>();
        img.color = lineColor;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
    }

    private Text Txt(RectTransform parent, string s, int size, float x, float y, float w, float h)
    {
        var t = Rect(parent, "Text", x, y, w, h).gameObject.AddComponent<Text>();
        t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = lineColor;
        t.alignment = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
}
