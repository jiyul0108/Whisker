using UnityEngine;
using UnityEngine.InputSystem;

// S / ↓ 키로 숨기·나오기 (고양이 오브젝트에 붙임)
// 고양이가 숨을 곳(HideSpot) 안에 있으면 숨을 수 있다
// 그때 주인도 같은 숨을 곳 안에 있으면 함께 숨는다
public class HideController : MonoBehaviour
{
    public static bool IsHidden { get; private set; }      // 고양이가 숨었는가

    [SerializeField] private OwnerController owner;
    [SerializeField] private float hiddenAlpha = 0.6f;     // 숨은 동안 반투명

    private CatController cat;
    private Collider2D catCol;
    private Collider2D ownerCol;
    private SpriteRenderer catSr;
    private SpriteRenderer ownerSr;
    private HideSpot[] spots;
    private HideSpot current;
    private bool ownerHidden;                               // 주인도 같이 숨었는가

    void Awake()
    {
        IsHidden = false;
        cat = GetComponent<CatController>();
        catCol = GetComponent<Collider2D>();
        catSr = GetComponentInChildren<SpriteRenderer>();
        if (owner == null) owner = FindAnyObjectByType<OwnerController>();
        if (owner != null)
        {
            ownerCol = owner.GetComponent<Collider2D>();
            ownerSr = owner.GetComponentInChildren<SpriteRenderer>();
        }
    }

    void Start()
    {
        spots = FindObjectsByType<HideSpot>(FindObjectsSortMode.None);
    }

    void OnDestroy()
    {
        IsHidden = false;
    }

    void Update()
    {
        if (GameInput.Blocked || ClimbSpot.IsClimbing) { ClearHint(); return; }

        HideSpot spot = IsHidden ? current : FindSpot();

        // 추격 중에는 숨을 수 없음
        if (ChaseEvent.Active && !IsHidden)
        {
            if (HintUI.Instance != null) HintUI.Instance.SetHint(spot != null ? "숨을 곳이 없다!" : null);
            var k = Keyboard.current;
            if (spot != null && k != null && (k.sKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame)
                && HintUI.Instance != null)
                HintUI.Instance.ShowMessage("숨을 곳이 없다! 도망쳐야 한다", 1.5f);
            return;
        }

        UpdateHint(spot);

        var kb = Keyboard.current;
        if (kb == null) return;
        if (!(kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame)) return;

        if (IsHidden) Unhide();
        else if (spot != null) Hide(spot);
        else LogWhyNot();
    }

    // 고양이가 들어가 있는 숨을 곳 찾기
    private HideSpot FindSpot()
    {
        if (spots == null || cat == null || !cat.IsGrounded) return null;
        foreach (var s in spots)
            if (s != null && s.Covers(catCol)) return s;
        return null;
    }

    private bool OwnerInside(HideSpot spot)
    {
        return spot != null && owner != null && spot.Covers(ownerCol);
    }

    private void Hide(HideSpot spot)
    {
        current = spot;
        IsHidden = true;
        SetAlpha(catSr, hiddenAlpha);

        ownerHidden = OwnerInside(spot);
        if (ownerHidden)
        {
            owner.Hide();
            SetAlpha(ownerSr, hiddenAlpha);
            GameLog.Info($"주인과 함께 {spot.SpotName}에 숨었다");
        }
        else
        {
            GameLog.Info($"고양이 혼자 {spot.SpotName}에 숨었다 (주인은 밖에 있음)");
        }
    }

    private void Unhide()
    {
        IsHidden = false;
        current = null;
        SetAlpha(catSr, 1f);
        if (ownerHidden)
        {
            owner.Unhide();
            SetAlpha(ownerSr, 1f);
            ownerHidden = false;
        }
        GameLog.Info("숨은 곳에서 나왔다");
    }

    private static void SetAlpha(SpriteRenderer sr, float a)
    {
        if (sr == null) return;
        Color c = sr.color; c.a = a; sr.color = c;
    }

    private void ClearHint()
    {
        if (HintUI.Instance != null && !IsHidden) HintUI.Instance.SetHint(null);
    }

    private void UpdateHint(HideSpot spot)
    {
        if (HintUI.Instance == null) return;
        if (IsHidden) HintUI.Instance.SetHint("S / ↓  나오기");
        else if (spot == null) HintUI.Instance.SetHint(null);
        else if (OwnerInside(spot)) HintUI.Instance.SetHint($"S / ↓  주인과 {spot.SpotName}에 숨기");
        else HintUI.Instance.SetHint($"S / ↓  {spot.SpotName}에 숨기");
    }

    // 숨기가 안 될 때 이유를 Console에 출력
    private void LogWhyNot()
    {
        if (spots == null || spots.Length == 0) { GameLog.Info("숨기 실패: 씬에 Hide Spot이 하나도 없어요"); return; }
        if (!cat.IsGrounded) { GameLog.Info("숨기 실패: 고양이가 바닥에 있어야 해요"); return; }
        GameLog.Info("숨기 실패: 고양이가 숨을 곳(Hide Spot) 범위 밖에 있어요");
    }
}
