using UnityEngine;

// 방 하나의 범위. Box Collider 2D의 가로 범위가 곧 방의 왼쪽 끝 ~ 오른쪽 끝
// 고양이가 이 범위 안에 들어오면 카메라가 이 방 범위로 바뀐다 (CameraFollow가 확인)
[RequireComponent(typeof(BoxCollider2D))]
public class Room : MonoBehaviour
{
    [SerializeField] private string roomName = "1번 방";

    public string RoomName => roomName;
    public float Left => area.bounds.min.x;
    public float Right => area.bounds.max.x;
    public float CenterY => Area.bounds.center.y;   // 층 구분용 (1층·지하·2층은 높이가 다름)

    private BoxCollider2D Area => area != null ? area : (area = GetComponent<BoxCollider2D>());

    private BoxCollider2D area;

    void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;   // 범위 표시용, 부딪히지 않음
    }

    public bool Contains(float x)
    {
        return x >= Area.bounds.min.x && x < Area.bounds.max.x;
    }

    // 가로와 세로를 모두 확인 (층이 여러 개일 때)
    public bool Contains(Vector2 p)
    {
        Bounds b = Area.bounds;
        return p.x >= b.min.x && p.x < b.max.x && p.y >= b.min.y && p.y < b.max.y;
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
