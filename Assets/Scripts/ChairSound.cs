using UnityEngine;

// 밀 수 있는 물건이 바닥에 끌리는 소리 (2번 방 의자에 붙임)
// 움직이는 동안만 반복 재생하고, 빠를수록 크게. 멈추면 천천히 작아진다
[RequireComponent(typeof(Rigidbody2D))]
public class ChairSound : MonoBehaviour
{
    [SerializeField] private AudioClip dragClip;              // 끌리는 소리 (반복용, 비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.7f;
    [SerializeField] private float minSpeed = 0.15f;          // 이보다 느리면 소리 없음
    [SerializeField] private float fullSpeed = 2.5f;          // 이 속도에서 가장 큼
    [SerializeField] private float fade = 8f;                 // 커지고 작아지는 빠르기

    private Rigidbody2D rb;
    private AudioSource source;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (dragClip == null) return;
        source = gameObject.AddComponent<AudioSource>();
        source.clip = dragClip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
    }

    void Update()
    {
        if (source == null) return;
        float v = Mathf.Abs(rb.linearVelocity.x);
        float target = v < minSpeed ? 0f : maxVolume * Mathf.InverseLerp(minSpeed, fullSpeed, v);
        if (Time.timeScale == 0f) target = 0f;
        source.volume = Mathf.MoveTowards(source.volume, target, fade * Time.unscaledDeltaTime);

        if (source.volume > 0.001f && !source.isPlaying) source.Play();
        else if (source.volume <= 0.001f && source.isPlaying) source.Pause();
    }
}
