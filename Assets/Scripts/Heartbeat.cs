using UnityEngine;

// 심장 소리 (고양이 오브젝트에 붙임)
// 같은 층에서 존재가 가까워질수록 심장이 빨리, 크게 뛴다
// 박스·책상 밑에 숨어 있을 때는 존재가 멀어도 뛰고, 평소보다 더 빨리 뛴다
// Beat Clip에 심장 소리 한 번("쿵") 파일을 넣어야 들림 (비워두면 아무 소리 없음)
[RequireComponent(typeof(AudioSource))]
public class Heartbeat : MonoBehaviour
{
    [SerializeField] private PresenceAI presence;
    [SerializeField] private AudioClip beatClip;

    [Header("거리")]
    [SerializeField] private float hearDistance = 14f;   // 이보다 멀면 안 들림 (숨어 있을 때는 들림)
    [SerializeField] private float closeDistance = 2f;   // 이보다 가까우면 가장 빠름

    [Header("빠르기 (초마다 한 번)")]
    [SerializeField] private float slowInterval = 1.1f;
    [SerializeField] private float fastInterval = 0.32f;

    [Header("숨어 있을 때")]
    [SerializeField, Range(0f, 1f)] private float hiddenMinTension = 0.4f;   // 존재가 멀어도 이 정도 긴장 (0~1)
    [SerializeField, Range(0.3f, 1f)] private float hiddenSpeedUp = 0.7f;    // 박자 간격에 곱함 (작을수록 빠름)

    [Header("소리 크기")]
    [SerializeField, Range(0f, 1f)] private float minVolume = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;

    private AudioSource source;
    private float timer;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 2D 소리
    }

    void Start()
    {
        if (presence == null) presence = FindAnyObjectByType<PresenceAI>();
    }

    void Update()
    {
        if (beatClip == null || Time.timeScale == 0f) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        bool hidden = HideController.IsHidden;
        float k = Tension();                       // -1 = 안 들림, 0 = 멀다, 1 = 가깝다
        if (hidden) k = Mathf.Max(k, hiddenMinTension);
        if (k < 0f) { timer = 0f; return; }

        float interval = Mathf.Lerp(slowInterval, fastInterval, k);
        if (hidden) interval *= hiddenSpeedUp;

        timer += Time.deltaTime;
        if (timer < interval) return;
        timer = 0f;
        source.pitch = Mathf.Lerp(0.95f, 1.1f, k);
        source.PlayOneShot(beatClip, Mathf.Lerp(minVolume, maxVolume, k));
    }

    // 존재와의 거리로 정한 긴장도 (같은 층이 아니거나 너무 멀면 -1)
    private float Tension()
    {
        if (presence == null || !presence.isActiveAndEnabled) return -1f;
        Vector2 me = transform.position;
        Vector2 p = presence.transform.position;
        if (!Stairs.SameFloor(me.y, p.y)) return -1f;

        float d = Mathf.Abs(p.x - me.x);
        if (d > hearDistance) return -1f;
        return 1f - Mathf.InverseLerp(closeDistance, hearDistance, d);
    }
}
