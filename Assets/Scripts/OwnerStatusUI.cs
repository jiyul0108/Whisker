using UnityEngine;
using UnityEngine.UI;

// 화면 가운데 위: 주인이 지금 따라오는 중인지 표시 (UI_Managers에 붙임)
//  ● 따라오는 중 (초록) / ○ 기다리는 중 (회색) / ■ 숨어 있음 (파랑)
public class OwnerStatusUI : MonoBehaviour
{
    private static readonly Color FollowColor = new Color(0.55f, 0.9f, 0.55f);
    private static readonly Color WaitColor = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color HideColor = new Color(0.55f, 0.7f, 0.95f);

    private OwnerController owner;
    private Text text;
    private OwnerController.State shown = (OwnerController.State)(-1);

    void Start()
    {
        owner = FindAnyObjectByType<OwnerController>();
        var canvas = UIKit.MakeCanvas(transform, "OwnerStatus_Canvas", 55);   // 공포도와 같은 높이
        text = UIKit.Label(canvas.transform, "", 28, WaitColor, new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(700, 40));
        if (owner == null) text.enabled = false;
    }

    void OnEnable() => Loc.Changed += ForceRefresh;
    void OnDisable() => Loc.Changed -= ForceRefresh;

    private void ForceRefresh() => shown = (OwnerController.State)(-1);

    void Update()
    {
        if (owner == null || text == null) return;
        bool over = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        text.enabled = !over;
        if (owner.CurrentState == shown) return;
        shown = owner.CurrentState;

        switch (shown)
        {
            case OwnerController.State.Moving:
                text.text = "●  " + Loc.T("주인이 따라오는 중");
                text.color = FollowColor;
                break;
            case OwnerController.State.Hiding:
                text.text = "■  " + Loc.T("주인이 숨어 있음");
                text.color = HideColor;
                break;
            default:
                text.text = "○  " + Loc.T("주인이 기다리는 중");
                text.color = WaitColor;
                break;
        }
    }
}
