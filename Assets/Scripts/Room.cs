using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 방 하나의 범위. Box Collider 2D의 가로 범위가 곧 방의 왼쪽 끝 ~ 오른쪽 끝
// 고양이가 이 범위 안에 들어오면 카메라가 이 방 범위로 바뀐다 (CameraFollow가 확인)
// Is Dark를 켜면 어두운 방: 화면이 어둡게 덮이고, 주인의 공포도가 오른다
// Lit By Power를 켜면 전기가 들어올 때(PowerEvent) 불이 깜빡이다 켜진다
[RequireComponent(typeof(BoxCollider2D))]
public class Room : MonoBehaviour
{
    public static readonly List<Room> All = new List<Room>();

    [SerializeField] private string roomName = "1번 방";

    [Header("어두운 방")]
    [SerializeField] private bool isDark = false;
    [SerializeField] private bool litByPower = false;
    [SerializeField, Range(0f, 1f)] private float darkAlpha = 0.5f;

    public string RoomName => roomName;
    public float Left => area.bounds.min.x;
    public float Right => area.bounds.max.x;
    public float CenterY => Area.bounds.center.y;   // 층 구분용 (1층·지하·2층은 높이가 다름)
    public bool IsDarkNow => isDark && !lit;

    private BoxCollider2D Area => area != null ? area : (area = GetComponent<BoxCollider2D>());

    private BoxCollider2D area;
    private SpriteRenderer overlay;
    private bool lit;

    private static Sprite whiteSprite;

    void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;   // 범위 표시용, 부딪히지 않음
    }

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Start()
    {
        if (isDark) CreateOverlay();
    }

    public bool Contains(float x)
    {
        return x >= Area.bounds.min.x && x < Area.bounds.max.x;
    }

    // 가로와 세로를 모두 확인 (층이 여러 개일 때)
    public bool Contains(Vector2 p)
    {
        Bounds b = Area.bounds;
        return p.x >= b.min.x && p.x < b.max.x && p.y >= b.min.y && p.y < b.max.y;
    }

    public static Room Find(Vector2 p)
    {
        foreach (var r in All) if (r != null && r.Contains(p)) return r;
        return null;
    }

    // 전기가 들어옴: Lit By Power 방들이 깜빡이다 밝아진다
    public static void PowerOnAll()
    {
        foreach (var r in All)
            if (r != null && r.isDark && r.litByPower && !r.lit) r.StartCoroutine(r.LightUp());
    }

    private IEnumerator LightUp()
    {
        lit = true;
        if (overlay == null) yield break;
        float[] flicker = { 0.1f, 0.6f, 0.08f, 0.5f, 0.15f, 0.3f };
        for (int i = 0; i < flicker.Length; i++)
        {
            SetOverlay(i % 2 == 0 ? 0.05f : darkAlpha);
            yield return new WaitForSeconds(flicker[i] * 0.5f);
        }
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            SetOverlay(Mathf.Lerp(darkAlpha, 0f, t / 0.6f));
            yield return null;
        }
        overlay.gameObject.SetActive(false);
    }

    // 방 전체를 덮는 검은 반투명 판 (캐릭터까지 같이 어두워짐)
    private void CreateOverlay()
    {
        if (whiteSprite == null)
        {
            var tex = Texture2D.whiteTexture;
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        }
        var go = new GameObject("DarkOverlay");
        go.transform.SetParent(transform, false);
        Bounds b = Area.bounds;
        go.transform.position = new Vector3(b.center.x, b.center.y, 0f);
        go.transform.localScale = new Vector3(b.size.x / transform.lossyScale.x, (b.size.y + 4f) / transform.lossyScale.y, 1f);
        overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = whiteSprite;
        overlay.sortingOrder = 40;
        SetOverlay(darkAlpha);
    }

    private void SetOverlay(float a)
    {
        if (overlay != null) overlay.color = new Color(0f, 0f, 0f, a);
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
