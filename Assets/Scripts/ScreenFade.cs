using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 화면 암전 (연출용, 오브젝트에 붙이지 않음). 게임이 멈춰 있어도 동작
public static class ScreenFade
{
    private static Image fade;

    public static IEnumerator Fade(float from, float to, float time)
    {
        Image img = Get();
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t / time));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, to);
    }

    // 하얗게 번쩍 (전기가 들어오는 순간)
    public static IEnumerator Flash(float time)
    {
        Image img = Get();
        float t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            img.color = new Color(1f, 1f, 0.9f, Mathf.Lerp(0.7f, 0f, t / time));
            yield return null;
        }
        img.color = new Color(0f, 0f, 0f, 0f);
    }

    private static Image Get()
    {
        if (fade != null) return fade;
        var canvas = UIKit.MakeCanvas(null, "ScreenFade", 92);   // 계단 암전(90) 위, 게임오버(100) 아래
        var go = new GameObject("Fade", typeof(Image));
        go.transform.SetParent(canvas.transform, false);
        fade = go.GetComponent<Image>();
        fade.raycastTarget = false;
        fade.color = new Color(0f, 0f, 0f, 0f);
        UIKit.Stretch(fade.rectTransform);
        return fade;
    }
}
