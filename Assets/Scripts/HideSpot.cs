using UnityEngine;

// 숨을 곳 (박스, 책상, 캐비닛 등): 이 범위 안에 고양이와 주인이 함께 있으면 숨을 수 있다
[RequireComponent(typeof(BoxCollider2D))]
public class HideSpot : MonoBehaviour
{
    [SerializeField] private string spotName = "박스";

    [Header("숨을 때 소리 (비워두면 고양이의 기본 숨기 소리)")]
    [SerializeField] private AudioClip hideClip;           // 상자: 상자 속으로 들어가는 소리, 책상: 책상 밑으로 들어가는 소리

    [Header("들출 그림 (존재에게 들켰을 때 들썩임. 배경에 그려진 물건이면 비워둠)")]
    [SerializeField] private SpriteRenderer prop;

    public string SpotName => spotName;
    public AudioClip HideClip => hideClip;
    public SpriteRenderer Prop => prop;
    public Vector2 Center => area != null ? (Vector2)area.bounds.center : (Vector2)transform.position;

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
