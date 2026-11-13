using UnityEngine;
using UnityEngine.InputSystem;

// S / ↓ 키로 숨기·나오기 (고양이 오브젝트에 붙임)
// 고양이가 숨을 곳(HideSpot) 안에 있으면 숨을 수 있다
// 그때 주인도 같은 숨을 곳 안에 있으면 함께 숨는다
public class HideController : MonoBehaviour
{
    public static bool IsHidden { get; private set; }      // 고양이가 숨었는가
    public static HideSpot Current { get; private set; }   // 지금 숨어 있는 곳 (존재가 들출 때 씀)
    public static HideController Instance { get; private set; }

    [SerializeField] private OwnerController owner;
    [SerializeField] private float hiddenAlpha = 0.6f;     // 숨은 동안 반투명
    [SerializeField] private AudioClip hideClip;           // 숨기·나오기 기본 소리 (아래 둘이 비어 있을 때)
    [SerializeField] private AudioClip boxClip;            // 상자·박스에 숨을 때 (숨을 곳에 소리가 따로 없으면 이름으로 고름)
    [SerializeField] private AudioClip deskClip;           // 책상·수술대 등 나머지에 숨을 때
    [SerializeField, Range(0f, 1f)] private float hideVolume = 0.8f;

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
        Current = null;
        Instance = this;
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
        Current = null;
        if (Instance == this) Instance = null;
    }

    // 존재에게 들켜 끌려 나옴: 숨은 그림(반투명)을 원래대로 (게임오버 직전 연출용, 소리 없음)
    public void Expose()
    {
        SetAlpha(catSr, 1f);
        if (ownerHidden) SetAlpha(ownerSr, 1f);
    }

    void Update()
    {
        if (GameInput.Blocked || ClimbSpot.IsClimbing) { ClearHint(); return; }

        HideSpot spot = IsHidden ? current : FindSpot();

        // 추격 중에는 숨을 수 없음
        if (ChaseEvent.Active && !IsHidden)
        {
            if (HintUI.Instance != null) HintUI.Instance.SetHint(null);   // 숨기 안내를 띄우지 않음 (도망쳐야 함)
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
        Current = spot;
        IsHidden = true;
        SetAlpha(catSr, hiddenAlpha);
        Sfx.Play(ClipFor(spot), hideVolume);   // 숨을 곳마다 다른 소리 (상자·책상)

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
        AudioClip outClip = ClipFor(current);
        current = null;
        Current = null;
        SetAlpha(catSr, 1f);
        Sfx.Play(outClip, hideVolume * 0.7f);   // 나올 때는 조금 작게
        if (ownerHidden)
        {
            owner.Unhide();
            SetAlpha(ownerSr, 1f);
            ownerHidden = false;
        }
        GameLog.Info("숨은 곳에서 나왔다");
    }

    // 숨을 곳의 소리: 숨을 곳에 직접 넣은 소리 → 이름에 상자·박스가 있으면 상자 소리, 아니면 책상 소리 → 기본 소리
    private AudioClip ClipFor(HideSpot spot)
    {
        if (spot == null) return hideClip;
        if (spot.HideClip != null) return spot.HideClip;
        string n = spot.SpotName ?? "";
        AudioClip byName = n.Contains("상자") || n.Contains("박스") ? boxClip : deskClip;
        return byName != null ? byName : hideClip;
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
        if (IsHidden) HintUI.Instance.SetHint("↓  나오기");
        else if (spot == null) HintUI.Instance.SetHint(null);
        else if (OwnerInside(spot)) HintUI.Instance.SetHint(Loc.F("↓  주인과 {0}에 숨기", Loc.T(spot.SpotName)));
        else HintUI.Instance.SetHint(Loc.F("↓  {0}에 숨기", Loc.T(spot.SpotName)));
    }

    // 숨기가 안 될 때 이유를 Console에 출력
    private void LogWhyNot()
    {
        if (spots == null || spots.Length == 0) { GameLog.Info("숨기 실패: 씬에 Hide Spot이 하나도 없어요"); return; }
        if (!cat.IsGrounded) { GameLog.Info("숨기 실패: 고양이가 바닥에 있어야 해요"); return; }
        GameLog.Info("숨기 실패: 고양이가 숨을 곳(Hide Spot) 범위 밖에 있어요");
    }
}
