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
    [SerializeField] private float fixedY = -0.6f;

    [Header("방 가로 범위 (Room이 없을 때만 사용)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float roomLeft = -8f;
    [SerializeField] private float roomRight = 9f;

    public Room CurrentRoom { get; private set; }

    private Camera cam;
    private Vector3 velocity;
    private Room[] rooms;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        rooms = FindObjectsByType<Room>(FindObjectsSortMode.None);
        UpdateRoom(true);
    }

    void LateUpdate()
    {
        if (target == null) return;

        bool changed = UpdateRoom(false);

        Vector3 goal = target.position + offset;
        if (lockY) goal.y = fixedY;

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
    }

    // 고양이가 있는 방을 찾아 범위를 바꿈. 방이 바뀌었으면 true
    private bool UpdateRoom(bool force)
    {
        if (rooms == null || rooms.Length == 0 || target == null) return false;
        if (!force && CurrentRoom != null && CurrentRoom.Contains(target.position.x)) return false;

        foreach (var r in rooms)
        {
            if (r == null || !r.Contains(target.position.x)) continue;
            bool changed = r != CurrentRoom;
            CurrentRoom = r;
            roomLeft = r.Left;
            roomRight = r.Right;
            if (changed) Debug.Log($"방 이동: {r.RoomName}");
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
