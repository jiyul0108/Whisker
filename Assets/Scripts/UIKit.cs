using UnityEngine;
using UnityEngine.UI;

// 코드로 화면(UI)을 만들 때 쓰는 도우미 (오브젝트에 붙이지 않음)
public static class UIKit
{
    // 한글 폰트: HintUI의 Font 칸(Galmuri11)을 같이 씀
    public static Font DefaultFont
    {
        get
        {
            if (HintUI.Instance != null && HintUI.Instance.Font != null) return HintUI.Instance.Font;
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }

    public static Canvas MakeCanvas(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // anchor: 화면 기준점 (0,0 = 왼쪽 아래, 1,1 = 오른쪽 위), pivot도 같은 값으로 맞춤
    public static Image Box(Transform parent, Color color, Vector2 anchor, Vector2 pos, Vector2 size, string name = "Box")
    {
        var go = new GameObject(name, typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        Place(img.rectTransform, anchor, pos, size);
        return img;
    }

    public static Text Label(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 boxSize,
                             TextAnchor align = TextAnchor.MiddleCenter, string name = "Text")
    {
        var go = new GameObject(name, typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = DefaultFont;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.lineSpacing = 1.2f;
        Place(t.rectTransform, anchor, pos, boxSize);
        return t;
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
