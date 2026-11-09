using System.Collections;
using UnityEngine;

// 1번 방 → 2번 방 출입구: 고양이와 주인이 둘 다 2번 방으로 넘어가면 문이 쾅 닫히고 다시 돌아갈 수 없다
// (주인이 1번 방에 남아 있으면 닫히지 않음 → 주인을 두고 갇히는 일 없음)
// 문 그림(자식 SpriteRenderer)은 처음엔 숨겨 두고, 닫힐 때 경첩을 중심으로 돌며 나타난다
[RequireComponent(typeof(BoxCollider2D))]
public class AutoCloseDoor : MonoBehaviour
{
    [SerializeField] private float passX = 9.9f;          // 이 X보다 오른쪽으로 둘 다 넘어가면 닫힘
    [SerializeField] private AudioClip closeClip;         // 문 닫히는 소리 (비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float volume = 0.9f;
    [SerializeField] private float swingTime = 1.65f;     // 끼익 소리가 끝나고 쾅 하는 시점에 맞춤

    [Header("주인 없이는 못 지나감 (고양이만 막는 보이지 않는 벽)")]
    [SerializeField] private bool needOwner = true;
    [SerializeField] private float ownerNearDistance = 3.5f;   // 주인이 고양이와 이 거리 안이면 지나갈 수 있음
    [SerializeField] private float gateOffsetX = -0.4f;         // 문보다 조금 왼쪽(1번 방 쪽)에서 멈춤
    [SerializeField] private string needOwnerHint = "주인을 데리고 가야 한다";

    public bool IsClosed { get; private set; }

    private BoxCollider2D col;
    private SpriteRenderer sr;
    private CatController cat;
    private OwnerController owner;
    private BoxCollider2D gate;
    private bool hinting;

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.enabled = false;                               // 열려 있는 동안은 지나갈 수 있음
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        int ground = LayerMask.NameToLayer("Ground");      // 닫히면 주인도 벽으로 봄
        if (ground >= 0) gameObject.layer = ground;

        // 고양이만 막는 벽: Default 레이어라 주인(Ground만 벽으로 봄)은 그냥 지나감
        if (needOwner)
        {
            var g = new GameObject("OwnerGate");
            g.layer = 0;
            g.transform.SetParent(transform, false);
            g.transform.localPosition = new Vector3(gateOffsetX, 0f, 0f);
            gate = g.AddComponent<BoxCollider2D>();
            gate.size = new Vector2(0.3f, 6f);
            gate.offset = new Vector2(0f, 3f);
        }
    }

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        owner = FindAnyObjectByType<OwnerController>();
    }

    void Update()
    {
        if (IsClosed || cat == null) { SetGateHint(false); return; }
        float y = transform.position.y;

        // 주인이 고양이 곁에 없으면 못 지나감
        if (gate != null)
        {
            bool ownerNear = owner == null
                || (Stairs.SameFloor(owner.transform.position.y, cat.transform.position.y)
                    && Mathf.Abs(owner.transform.position.x - cat.transform.position.x) <= ownerNearDistance);
            gate.enabled = !ownerNear;
            float gx = gate.transform.position.x;
            bool atGate = !ownerNear && cat.transform.position.x < gx && gx - cat.transform.position.x < 1.2f
                          && Stairs.SameFloor(cat.transform.position.y, y);
            SetGateHint(atGate);
        }

        bool catPassed = cat.transform.position.x > passX && Stairs.SameFloor(cat.transform.position.y, y);
        bool ownerPassed = owner == null || (owner.transform.position.x > passX && Stairs.SameFloor(owner.transform.position.y, y));
        if (catPassed && ownerPassed) StartCoroutine(Close());
    }

    private void SetGateHint(bool on)
    {
        if (on == hinting) return;
        hinting = on;
        if (HintUI.Instance != null) HintUI.Instance.SetHint(this, on ? needOwnerHint : null);
    }

    private IEnumerator Close()
    {
        IsClosed = true;
        col.enabled = true;
        if (gate != null) gate.enabled = false;
        SetGateHint(false);
        Sfx.Play(closeClip, volume);
        GameLog.Info("1번 방 문이 닫혔다");
        var cam = FindAnyObjectByType<CameraFollow>();
        if (sr == null)
        {
            yield return new WaitForSeconds(swingTime);
            if (cam != null) cam.Shake(0.25f, 0.06f);
            yield break;
        }

        // 문이 경첩(왼쪽)을 중심으로 돌아 닫힘: 가로 폭이 0에서 1로
        Transform door = sr.transform;
        Vector3 basePos = door.localPosition;
        float halfW = sr.sprite != null ? sr.sprite.bounds.extents.x : 0.23f;
        sr.enabled = true;
        float t = 0f;
        while (t < swingTime)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / swingTime);
            float k = u * u;                                // 천천히 움직이다 끝에서 빨라져 쾅 닫힘
            float s = Mathf.Lerp(0.08f, 1f, k);
            door.localScale = new Vector3(s, 1f, 1f);
            door.localPosition = basePos + Vector3.left * (halfW * (1f - s));
            float shade = Mathf.Lerp(0.4f, 1f, k);
            sr.color = new Color(shade, shade, shade, 1f);
            yield return null;
        }
        door.localScale = Vector3.one;
        door.localPosition = basePos;
        sr.color = Color.white;
        if (cam != null) cam.Shake(0.25f, 0.06f);         // 다 닫힌 순간 쾅
    }
}
