using UnityEngine;

// 배경 소리 (폐병원의 낮은 웅웅거림·바람). 씬에 하나
// 시작하면 천천히 커지고, 게임이 끝나면(잡힘·탈출) 천천히 사라진다
// 존재가 비명을 지르고 쫓아오는 동안에는 쫓기는 음악(Chase Clip)만 나온다 (배경 소리는 꺼짐)
// 쫓기는 게 끝나면 음악은 천천히 사라지고 배경 소리가 다시 돌아온다
public class Ambience : MonoBehaviour
{
    [SerializeField] private AudioClip loopClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private float fadeInTime = 3f;
    [SerializeField] private float fadeOutTime = 1.5f;

    [Header("쫓길 때 음악 (비워두면 바뀌지 않음)")]
    [SerializeField] private AudioClip chaseClip;
    [SerializeField, Range(0f, 1f)] private float chaseVolume = 0.6f;
    [SerializeField] private float chaseFadeIn = 0.6f;    // 쫓기기 시작하면 이 시간 안에 바뀜 (초)
    [SerializeField] private float chaseFadeOut = 3f;     // 쫓기는 게 끝나면 이 시간 동안 천천히 돌아옴

    private AudioSource source;
    private AudioSource chase;
    private PresenceAI presence;
    private float level;      // 0 = 배경 소리만, 1 = 쫓기는 음악만
    private float started;    // 시작 후 배경 소리가 천천히 커지는 정도 (0~1)

    void Start()
    {
        presence = FindAnyObjectByType<PresenceAI>();
        source = MakeSource(loopClip, true);
        chase = MakeSource(chaseClip, false);
    }

    private AudioSource MakeSource(AudioClip clip, bool play)
    {
        if (clip == null) return null;
        var s = gameObject.AddComponent<AudioSource>();
        s.clip = clip;
        s.loop = true;
        s.playOnAwake = false;
        s.spatialBlend = 0f;
        s.volume = 0f;
        if (play) s.Play();
        return s;
    }

    void Update()
    {
        bool over = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        float dt = Time.unscaledDeltaTime;

        // 쫓기는 중인가
        bool chased = chase != null && !over && presence != null && presence.isActiveAndEnabled && presence.IsChasing;
        if (chased && level <= 0f)
        {
            chase.time = 0f;   // 쫓기기 시작할 때마다 음악을 처음부터
            chase.Play();
        }
        level = Mathf.MoveTowards(level, chased ? 1f : 0f, dt / Mathf.Max(0.05f, chased ? chaseFadeIn : chaseFadeOut));
        if (chase != null && level <= 0f && chase.isPlaying) chase.Stop();

        // 게임 시작·끝의 전체 크기
        started = over ? Mathf.MoveTowards(started, 0f, dt / Mathf.Max(0.01f, fadeOutTime))
                       : Mathf.MoveTowards(started, 1f, dt / Mathf.Max(0.01f, fadeInTime));

        // 둘이 겹치지 않게 바꿔 끼움: 음악이 커지는 만큼 배경 소리는 작아짐
        if (source != null) source.volume = volume * started * (1f - level);
        if (chase != null) chase.volume = over ? Mathf.MoveTowards(chase.volume, 0f, dt / Mathf.Max(0.01f, fadeOutTime))
                                               : chaseVolume * level;
    }
}
