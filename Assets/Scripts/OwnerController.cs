using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class OwnerController : MonoBehaviour
{
    public enum State { Waiting, Moving, Arrived }

    [Header("이동")]
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;

    [Header("상태별 색 (프로토타입 확인용, 끄면 원래 그림 색)")]
    [SerializeField] private bool useStateColors = false;
    [SerializeField] private Color waitingColor = new Color(0.45f, 0.65f, 1f);
    [SerializeField] private Color movingColor = new Color(0.45f, 1f, 0.55f);
    [SerializeField] private Color arrivedColor = new Color(1f, 0.85f, 0.3f);

    public State CurrentState { get; private set; } = State.Waiting;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private int nextIndex;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>(); // 자식 Visual의 그림
        ApplyColor();
    }

    // 고양이가 근처에서 울었을 때 호출됨
    public void OnMeow()
    {
        switch (CurrentState)
        {
            case State.Waiting:
                if (nextIndex < waypoints.Length) SetState(State.Moving);
                break;
            case State.Moving:
                SetState(State.Waiting);
                break;
        }
    }

    void FixedUpdate()
    {
        if (CurrentState != State.Moving) return;

        Vector2 pos = rb.position;
        float targetX = waypoints[nextIndex].position.x;
        float newX = Mathf.MoveTowards(pos.x, targetX, moveSpeed * Time.fixedDeltaTime);

        if (sr != null && newX != pos.x) sr.flipX = targetX < pos.x;
        rb.MovePosition(new Vector2(newX, pos.y));

        if (Mathf.Approximately(newX, targetX))
        {
            nextIndex++;
            SetState(nextIndex >= waypoints.Length ? State.Arrived : State.Waiting);
        }
    }

    private void SetState(State newState)
    {
        CurrentState = newState;
        ApplyColor();
        Debug.Log($"주인 상태: {newState}");
    }

    private void ApplyColor()
    {
        if (sr == null) return;
        if (!useStateColors)
        {
            sr.color = Color.white;
            return;
        }
        sr.color = CurrentState switch
        {
            State.Moving => movingColor,
            State.Arrived => arrivedColor,
            _ => waitingColor,
        };
    }

    void OnDrawGizmos()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, 0.25f);
            if (i > 0 && waypoints[i - 1] != null)
                Gizmos.DrawLine(waypoints[i - 1].position, waypoints[i].position);
        }
    }
}