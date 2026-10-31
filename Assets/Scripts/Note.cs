using UnityEngine;

// 쪽지: 고양이가 가까이 가면 "읽기" 안내가 뜨고, W / ↑ 키로 읽는다
[RequireComponent(typeof(BoxCollider2D))]
public class Note : MonoBehaviour
{
    [Header("내용")]
    [SerializeField] private string title = "임시 쪽지";
    [TextArea(4, 10)]
    [SerializeField] private string body = "여기에 쪽지 내용을 적어요.\n스토리가 정해지면 바꿀 예정.";

    [Header("보이는 모습")]
    [SerializeField] private float bobHeight = 0.05f;    // 위아래로 살짝 떠다니는 높이 (안 읽었을 때만)
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private Color readColor = new Color(0.6f, 0.6f, 0.6f, 1f); // 읽은 뒤 색

    public string Title => title;
    public string Body => body;
    public bool IsRead { get; private set; }

    private SpriteRenderer sr;
    private Vector3 basePos;

    void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        sr = GetComponentInChildren<SpriteRenderer>();
        basePos = transform.localPosition;
    }

    void Update()
    {
        if (IsRead) return;
        float y = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = basePos + new Vector3(0f, y, 0f);
    }

    // NoteUI가 읽는 순간 호출
    public void MarkRead()
    {
        if (IsRead) return;
        IsRead = true;
        transform.localPosition = basePos;
        if (sr != null) sr.color = readColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<CatController>() != null && NoteUI.Instance != null)
            NoteUI.Instance.SetNearby(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<CatController>() != null && NoteUI.Instance != null)
            NoteUI.Instance.ClearNearby(this);
    }
}
