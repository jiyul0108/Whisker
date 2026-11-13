using UnityEngine;

// 바닥에 놓인 열쇠. 고양이가 닿으면 줍는다
[RequireComponent(typeof(BoxCollider2D))]
public class KeyItem : MonoBehaviour
{
    [Header("열쇠 정보 (잠긴 문의 Key Id와 같게)")]
    [SerializeField] private string keyId = "A";
    [SerializeField] private string keyName = "녹슨 열쇠";

    [Header("위아래로 둥실둥실")]
    [SerializeField] private float bobHeight = 0.06f;
    [SerializeField] private float bobSpeed = 2.5f;

    [Header("반짝임 (바닥에 떨어진 열쇠가 잘 보이게)")]
    [SerializeField] private bool showSparkle = true;

    private Transform visual;
    private Vector3 visualBase;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.transform != transform)
        {
            visual = sr.transform;              // 그림이 자식(Visual)에 있으면 그림만 움직임
            visualBase = visual.localPosition;
        }
        if (showSparkle) Sparkle.Attach(transform, new Vector3(0.12f, 0.3f, 0f));
    }

    void Update()
    {
        if (visual == null) return;
        visual.localPosition = visualBase + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<CatController>() == null) return; // 고양이만 주울 수 있음

        KeyInventory.Add(keyId, keyName);
        if (HintUI.Instance != null) HintUI.Instance.ShowItemMessage(Loc.F("{0} 획득", Loc.T(keyName)));
        Destroy(gameObject);
    }
}
