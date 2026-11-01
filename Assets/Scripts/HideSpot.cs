using UnityEngine;

// 숨을 곳 (박스, 책상, 캐비닛 등): 이 범위 안에 고양이와 주인이 함께 있으면 숨을 수 있다
[RequireComponent(typeof(BoxCollider2D))]
public class HideSpot : MonoBehaviour
{
    [SerializeField] private string spotName = "박스";

    public string SpotName => spotName;

    private BoxCollider2D area;

    void Awake()
    {
        area = GetComponent<BoxCollider2D>();
        area.isTrigger = true;
    }

    // 이 콜라이더가 숨을 곳 범위와 겹치는가
    public bool Covers(Collider2D other)
    {
        return other != null && area.bounds.Intersects(other.bounds);
    }

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        if (box == null) return;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(box.bounds.center, box.bounds.size);
    }
}
