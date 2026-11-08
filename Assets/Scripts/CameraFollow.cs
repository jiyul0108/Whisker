using UnityEngine;

// 카메라가 고양이를 부드럽게 따라감
// 씬에 Room이 있으면: 고양이가 있는 방의 범위 밖은 보이지 않게 막고, 방이 바뀌면 바로 새 방으로 넘어감
// 씬에 Room이 없으면: 아래 Room Left / Right 범위를 사용
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("높이 고정 (켜면 위아래로 안 움직임)")]
    [SerializeField] private bool lockY = true;
    [SerializeField] private float fixedY = -0.6f;          // Room이 없을 때 높이
    [SerializeField] private float roomOffsetY = -0.1f;     // Room이 있으면: 방 가운데 높이 + 이 값 (층마다 높이가 다름)

    [Header("방 가로 범위 (Room이 없을 때만 사용)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float roomLeft = -8f;
    [SerializeField] private float roomRight = 9f;

    public Room CurrentRoom { get; private set; }

    private Camera cam;
    private Vector3 velocity;
    private Room[] rooms;

    // 연출용: 고양이 대신 보여줄 자리 (PowerEvent가 2층 문 열리는 장면을 보여줄 때)
    private bool hasFocus;
    private Vector2 focus;
    private bool focusChanged;

    // 화면 흔들림
    private float shakeTimer;
    private float shakeAmount;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        if (target != null) UpdateRoom(true, target.position);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 follow = hasFocus ? (Vector3)focus : target.position;
        bool changed = UpdateRoom(false, follow) || focusChanged;
        focusChanged = false;

        Vector3 goal = follow + offset;
        if (lockY) goal.y = CurrentRoom != null ? CurrentRoom.CenterY + roomOffsetY : fixedY;

        if (useBounds)
        {
            // 화면 절반 너비만큼 안쪽으로 막아서 배경 밖이 보이지 않게
            float halfW = cam.orthographicSize * cam.aspect;
            float min = roomLeft + halfW;
            float max = roomRight - halfW;
            goal.x = min > max ? (roomLeft + roomRight) / 2f : Mathf.Clamp(goal.x, min, max);
        }

        if (changed)
        {
            // 방이 바뀌면 미끄러지지 않고 바로 새 방 화면으로
            transform.position = goal;
            velocity = Vector3.zero;
        }
        else
        {
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            transform.position += (Vector3)(Random.insideUnitCircle * shakeAmount);
        }
    }

    // 연출: 이 자리를 보여줌 (방이 다르면 바로 넘어감)
    public void ShowPoint(Vector2 p)
    {
        hasFocus = true;
        focus = p;
        focusChanged = true;
    }

    // 연출 끝: 다시 고양이를 따라감
    public void ClearShowPoint()
    {
        hasFocus = false;
        focusChanged = true;
        CurrentRoom = null;
    }

    public void Shake(float time, float amount)
    {
        shakeTimer = time;
        shakeAmount = amount;
    }

    // 고양이가 있는 방을 찾아 범위를 바꿈. 방이 바뀌었으면 true
    private bool UpdateRoom(bool force, Vector2 p)
    {
        if (rooms == null || rooms.Length == 0 || target == null) return false;
        if (!force && CurrentRoom != null && CurrentRoom.Contains(p)) return false;

        foreach (var r in rooms)
        {
            if (r == null || !r.Contains(p)) continue;
            bool changed = r != CurrentRoom;
            CurrentRoom = r;
            roomLeft = r.Left;
            roomRight = r.Right;
            if (changed) GameLog.Info($"방 이동: {r.RoomName}");
            return changed && !force;
        }
        return false;
    }

    // 다른 스크립트에서 범위를 직접 바꿀 때
    public void SetRoomBounds(float left, float right)
    {
        roomLeft = left;
        roomRight = right;
    }

    void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(roomLeft, -10f, 0f), new Vector3(roomLeft, 10f, 0f));
        Gizmos.DrawLine(new Vector3(roomRight, -10f, 0f), new Vector3(roomRight, 10f, 0f));
    }
}
