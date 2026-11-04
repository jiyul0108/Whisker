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

    private BoxCollider2D area;

    void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;   // 범위 표시용, 부딪히지 않음
    }

    public bool Contains(float x)
    {
        if (area == null) area = GetComponent<BoxCollider2D>();
        return x >= area.bounds.min.x && x < area.bounds.max.x;
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.5f);
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
