using UnityEngine;

// 밀 수 있는 물건(2번 방 의자)이 움직일 수 있는 가로 범위
// 범위 끝에 닿으면 더 밀리지 않는다 → 다른 방으로 넘어가지 않고,
// 벽에 딱 붙지 않아서 고양이가 뒤로 돌아가 반대쪽으로 다시 밀 수 있다
[RequireComponent(typeof(Rigidbody2D))]
public class PushLimit : MonoBehaviour
{
    [SerializeField] private float minX = 10.8f;   // 물건 가운데가 갈 수 있는 왼쪽 끝
    [SerializeField] private float maxX = 24.4f;   // 오른쪽 끝

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        Vector2 p = rb.position;
        Vector2 v = rb.linearVelocity;
        if (p.x < minX)
        {
            rb.position = new Vector2(minX, p.y);
            if (v.x < 0f) rb.linearVelocity = new Vector2(0f, v.y);
        }
        else if (p.x > maxX)
        {
            rb.position = new Vector2(maxX, p.y);
            if (v.x > 0f) rb.linearVelocity = new Vector2(0f, v.y);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        float y = transform.position.y;
        Gizmos.DrawLine(new Vector3(minX, y - 0.5f, 0f), new Vector3(minX, y + 1f, 0f));
        Gizmos.DrawLine(new Vector3(maxX, y - 0.5f, 0f), new Vector3(maxX, y + 1f, 0f));
    }
}
