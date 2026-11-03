using UnityEngine;

// 카메라가 고양이를 부드럽게 따라감
// 방 범위(왼쪽 끝 ~ 오른쪽 끝) 밖은 보이지 않게 막고, 높이는 고정할 수 있음
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float smoothTime = 0.15f;

    [Header("높이 고정 (켜면 위아래로 안 움직임)")]
    [SerializeField] private bool lockY = true;
    [SerializeField] private float fixedY = -0.6f;

    [Header("방 가로 범위 (배경 왼쪽 끝 ~ 오른쪽 끝, 월드 좌표)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private float roomLeft = -8f;
    [SerializeField] private float roomRight = 9f;

    private Camera cam;
    private Vector3 velocity;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null) return;

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

        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
    }

    // 방이 바뀔 때 다른 스크립트에서 범위를 바꿀 수 있게
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
