using UnityEngine;

// 고양이가 이 범위(Box Collider 2D)에 처음 들어오면 노란 메시지를 한 번 보여준다
// 예) 지하에 처음 내려왔을 때 "오른쪽 안쪽에서 기계 소리가 들린다… →" (오른쪽에 창고가 있다는 안내)
[RequireComponent(typeof(BoxCollider2D))]
public class EnterMessage : MonoBehaviour
{
    [SerializeField] private string message = "";
    [SerializeField] private float seconds = 3f;
    [SerializeField] private float delay = 0.6f;    // 들어오고 이만큼 뒤에 (계단 화면 전환이 끝난 뒤)

    private BoxCollider2D area;
    private CatController cat;
    private float insideTime = -1f;
    private bool shown;

    void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
    }

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
    }

    void Update()
    {
        if (shown || cat == null) return;
        bool inside = area.bounds.Contains(new Vector3(cat.transform.position.x, cat.transform.position.y + 0.3f, area.bounds.center.z));
        if (!inside || GameInput.Blocked || Stairs.IsTraveling) { insideTime = -1f; return; }
        if (insideTime < 0f) insideTime = 0f;
        insideTime += Time.deltaTime;
        if (insideTime < delay) return;

        shown = true;
        if (HintUI.Instance != null && !string.IsNullOrEmpty(message)) HintUI.Instance.ShowItemMessage(message, seconds);
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<BoxCollider2D>();
        if (c == null) return;
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.5f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size);
    }
}
