using UnityEngine;
using UnityEngine.UI;

// 언어를 바꾸면 글자도 바뀌는 Text (UIKit.Label이 자동으로 붙임)
// 코드가 나중에 글자를 직접 바꾼 Text는 건드리지 않음 (그 코드가 Loc.T로 넣음)
[RequireComponent(typeof(Text))]
public class LocText : MonoBehaviour
{
    private string key;       // 한국어 원문
    private string applied;   // 마지막으로 넣은 글자
    private Text text;

    public static void Attach(Text t, string ko)
    {
        var lt = t.gameObject.GetComponent<LocText>();
        if (lt == null) lt = t.gameObject.AddComponent<LocText>();
        lt.text = t;
        lt.key = ko;
        lt.applied = Loc.T(ko);
        t.text = lt.applied;
    }

    void OnEnable()
    {
        Loc.Changed += Refresh;
        Refresh();   // 꺼져 있는 동안 언어가 바뀌었을 수 있음
    }

    void OnDisable() => Loc.Changed -= Refresh;

    private void Refresh()
    {
        if (text == null || string.IsNullOrEmpty(key) || text.text != applied) return;
        applied = Loc.T(key);
        text.text = applied;
    }
}
