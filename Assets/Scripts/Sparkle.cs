using UnityEngine;
using UnityEngine.Rendering.Universal;

// 아이템이 숨겨진 곳에서 반짝이는 작은 별 (오브젝트에 직접 붙이지 않음, Sparkle.Attach로 만든다)
// 별 그림은 코드가 직접 그리고, 작은 빛(Light2D)도 함께 깜빡여서 어두운 방에서도 보인다
// Interactable(아이템을 주는 조사 지점)과 KeyItem(바닥의 열쇠)이 만든다
public class Sparkle : MonoBehaviour
{
    private static Sprite starSprite;

    private SpriteRenderer sr;
    private Light2D glow;
    private float phase;
    private float period;
    private Vector3 basePos;

    // parent의 localPos 위치에 반짝임을 붙인다
    public static Sparkle Attach(Transform parent, Vector3 localPos)
    {
        var go = new GameObject("Sparkle");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        return go.AddComponent<Sparkle>();
    }

    void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = StarSprite();
        sr.sortingOrder = 45;                      // 어두운 방 덮개(40) 위
        sr.color = new Color(1f, 0.95f, 0.7f, 0f);
        glow = CatVision.AttachGlow(transform, Vector3.zero, new Color(1f, 0.9f, 0.6f), 0.8f);
        if (glow != null) glow.gameObject.SetActive(true);
        phase = Random.value * 10f;                // 여러 개가 동시에 깜빡이지 않게
        period = Random.Range(1.6f, 2.2f);
        basePos = transform.localPosition;
    }

    void Update()
    {
        // 한 주기 중 앞부분에 "반짝" 하고 커졌다가 사라짐
        float t = ((Time.time + phase) % period) / period;
        float k = t < 0.35f ? Mathf.Sin(t / 0.35f * Mathf.PI) : 0f;
        float s = Mathf.Lerp(0.5f, 1.25f, k);
        transform.localScale = new Vector3(s, s, 1f);
        transform.localRotation = Quaternion.Euler(0f, 0f, k * 25f);
        transform.localPosition = basePos + Vector3.up * (Mathf.Sin((Time.time + phase) * 2f) * 0.04f);

        Color c = sr.color;
        c.a = Mathf.Max(0.25f, k);                 // 평소에도 희미하게 보임
        sr.color = c;
        if (glow != null) glow.intensity = 0.15f + 0.55f * k;
    }

    // 4갈래 별 (11×11 픽셀)
    private static Sprite StarSprite()
    {
        if (starSprite != null) return starSprite;
        const int N = 11, C = 5;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                int dx = Mathf.Abs(x - C), dy = Mathf.Abs(y - C);
                float a = 0f;
                if (dx == 0 && dy <= 5) a = dy <= 1 ? 1f : 1f - (dy - 1) * 0.2f;   // 세로 줄기
                if (dy == 0 && dx <= 5) a = dx <= 1 ? 1f : 1f - (dx - 1) * 0.2f;   // 가로 줄기
                if (dx == 1 && dy == 1) a = 0.6f;                                   // 가운데 둥글게
                tex.SetPixel(x, y, a > 0f ? new Color(1f, 1f, 1f, a) : clear);
            }
        tex.Apply();
        starSprite = Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), 50f);   // 0.22칸 크기
        return starSprite;
    }
}
