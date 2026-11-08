using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 조사할 수 있는 물건 (두꺼비집, 시계, 화분, 소화기함, 침대, 안내데스크 문서, 금고, 수술대)
// Use Gauge를 켜면 조건을 채운 뒤 게이지(CraftUI)가 다 찬 다음에 동작한다 (퓨즈 끼우기, 열쇠 만들기)
// 고양이가 범위(Box Collider 2D) 안에서 W / ↑ 를 누르면 조사한다
//  1) 필요한 진행(Required Flag)이 없으면 → Flag Missing Message
//  2) 필요한 아이템(Required Items)이 없으면 → Missing Message (+ Missing Sets Flag 진행 표시)
//  3) 조건을 채우면 → 특수 동작(문서·침대·금고·전원·추격·만들기) 또는 바로 보상(아이템·진행·메시지)
//  Reveal Object를 넣으면 끝났을 때 그 오브젝트가 나타나 위에서 떨어진다 (예: 수술대에서 만든 출구 열쇠)
[RequireComponent(typeof(BoxCollider2D))]
public class Interactable : MonoBehaviour
{
    public enum Action { None, ShowText, ShowBed, Keypad, PowerOn, ChaseStart, Craft }

    [Header("안내")]
    [SerializeField] private string label = "조사하기";
    [SerializeField] private bool requireClimbing = false;      // 켜면 벽을 타고 있을 때만 (높은 곳의 시계)

    [Header("조건: 진행")]
    [SerializeField] private string requiredFlag = "";
    [SerializeField] private string flagMissingMessage = "";

    [Header("조건: 아이템")]
    [SerializeField] private string[] requiredItems = new string[0];
    [SerializeField] private string missingMessage = "";
    [SerializeField] private bool listMissing = true;            // 모자란 아이템 이름을 덧붙여 보여줌
    [SerializeField] private string missingSetsFlag = "";        // 아이템이 없을 때 조사만 해도 남기는 진행 (예: FUSEBOX_CHECKED)
    [SerializeField] private bool consumeItems = true;

    [Header("결과")]
    [SerializeField] private string rewardItem = "";             // 얻는 아이템 Id (ItemDB)
    [SerializeField] private string setFlag = "";
    [SerializeField] private string successMessage = "";         // 비우면 "○○ 획득"
    [SerializeField] private bool oneShot = true;                // 한 번만 (끝난 뒤에는 Done Message)
    [SerializeField] private string doneMessage = "";

    [Header("특수 동작")]
    [SerializeField] private Action action = Action.None;
    [SerializeField] private string popupTitle = "";
    [TextArea(3, 10)]
    [SerializeField] private string popupBody = "";
    [SerializeField] private Color bedColor = Color.red;
    [SerializeField] private string bedNumber = "";
    [SerializeField] private Sprite bedImage;                    // 단서가 그려진 침대 그림 (넣으면 Bed Color·Number 대신 이 그림)
    [SerializeField] private string keypadCode = "0000";

    [Header("시간이 걸리는 작업 (켜면 게이지가 다 찬 뒤에 동작. Action = Craft는 항상 게이지)")]
    [SerializeField] private bool useGauge = false;
    [SerializeField] private float craftTime = 10f;
    [SerializeField] private string craftTitle = "작업하는 중…";

    [Header("끝났을 때 나타나는 오브젝트 (처음엔 꺼두기)")]
    [SerializeField] private GameObject revealObject;
    [SerializeField] private float revealDropHeight = 1.4f;   // 이만큼 위에서 떨어져 제자리에 놓임

    private BoxCollider2D area;
    private CatController cat;
    private Collider2D catCol;
    private bool done;
    private bool hinting;

    void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
    }

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        if (cat != null) catCol = cat.GetComponent<Collider2D>();
    }

    void OnDisable() { SetHint(false); }

    void Update()
    {
        if (cat == null) return;
        if (GameInput.Blocked) { SetHint(false); return; }

        bool near = catCol != null && area.bounds.Intersects(catCol.bounds)
                    && !HideController.IsHidden
                    && requireClimbing == ClimbSpot.IsClimbing
                    && !(done && oneShot && string.IsNullOrEmpty(doneMessage) && !Repeatable);
        SetHint(near);
        if (!near) return;

        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame) Interact();
    }

    // 문서·침대는 몇 번이고 다시 볼 수 있음
    private bool Repeatable => action == Action.ShowText || action == Action.ShowBed;

    private void Interact()
    {
        if (done && oneShot && !Repeatable)
        {
            Message(doneMessage);
            return;
        }

        if (!string.IsNullOrEmpty(requiredFlag) && !GameFlags.Has(requiredFlag))
        {
            Message(flagMissingMessage);
            return;
        }

        var missing = new List<string>();
        foreach (var id in requiredItems)
            if (!string.IsNullOrEmpty(id) && !KeyInventory.Has(id)) missing.Add(ItemDB.Name(id));
        if (missing.Count > 0)
        {
            string text = missingMessage;
            if (listMissing) text = (string.IsNullOrEmpty(text) ? "" : text + "  ") + $"(필요: {string.Join(", ", missing)})";
            Message(text);
            GameFlags.Set(missingSetsFlag);
            return;
        }

        // 게이지가 다 차면 동작 (도중에 Esc로 그만두면 아무 일도 없음)
        if ((useGauge || action == Action.Craft) && CraftUI.Instance != null)
        {
            CraftUI.Instance.Run(craftTime, craftTitle, DoAction);
            return;
        }
        DoAction();
    }

    private void DoAction()
    {
        switch (action)
        {
            case Action.ShowText:
                if (PopupUI.Instance != null) PopupUI.Instance.ShowText(popupTitle, popupBody);
                Complete();
                return;
            case Action.ShowBed:
                if (PopupUI.Instance != null) PopupUI.Instance.ShowBed(bedColor, bedNumber, bedImage);
                Complete();
                return;
            case Action.Keypad:
                if (PopupUI.Instance != null) PopupUI.Instance.ShowKeypad(keypadCode, Complete);
                return;   // 맞는 번호를 넣었을 때 Complete
        }
        Complete();
    }

    private void Complete()
    {
        if (consumeItems)
            foreach (var id in requiredItems) KeyInventory.Remove(id);

        if (!string.IsNullOrEmpty(rewardItem)) KeyInventory.Add(rewardItem);
        GameFlags.Set(setFlag);

        string text = successMessage;
        if (string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(rewardItem)) text = $"{ItemDB.Name(rewardItem)} 획득";
        Message(text);

        if (oneShot && !Repeatable) done = true;
        SetHint(false);    // 안내 문구를 지웠다가 다음 프레임에 다시 계산

        if (revealObject != null) StartCoroutine(Reveal());

        if (action == Action.PowerOn && PowerEvent.Instance != null) PowerEvent.Instance.Begin();
        if (action == Action.ChaseStart && ChaseEvent.Instance != null) ChaseEvent.Instance.Begin();
    }

    // 숨겨 두었던 오브젝트가 위에서 톡 떨어짐 (제자리 = 놓인 위치)
    private IEnumerator Reveal()
    {
        Transform t = revealObject.transform;
        Vector3 to = t.position;
        Vector3 from = to + Vector3.up * revealDropHeight;
        t.position = from;
        revealObject.SetActive(true);

        float v = 0f, y = from.y;
        while (y > to.y)
        {
            v += 12f * Time.deltaTime;
            y -= v * Time.deltaTime;
            t.position = new Vector3(to.x, Mathf.Max(y, to.y), to.z);
            yield return null;
        }
        // 살짝 튀어 오름
        float b = 0f;
        while (b < 0.18f)
        {
            b += Time.deltaTime;
            t.position = to + Vector3.up * (Mathf.Sin(b / 0.18f * Mathf.PI) * 0.12f);
            yield return null;
        }
        t.position = to;
    }

    private static void Message(string text)
    {
        if (!string.IsNullOrEmpty(text) && HintUI.Instance != null) HintUI.Instance.ShowMessage(text, 2.2f);
    }

    private void SetHint(bool on)
    {
        if (on == hinting) return;
        hinting = on;
        if (HintUI.Instance != null) HintUI.Instance.SetHint(this, on ? $"W / ↑  {label}" : null);
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        Gizmos.color = new Color(1f, 0.5f, 0.9f, 0.7f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
