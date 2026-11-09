using UnityEngine;

// 배경 소리 (폐병원의 낮은 웅웅거림·바람). 씬에 하나
// 시작하면 천천히 커지고, 게임이 끝나면(잡힘·탈출) 천천히 사라진다
public class Ambience : MonoBehaviour
{
    [SerializeField] private AudioClip loopClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField] private float fadeInTime = 3f;
    [SerializeField] private float fadeOutTime = 1.5f;

    private AudioSource source;

    void Start()
    {
        if (loopClip == null) return;
        source = gameObject.AddComponent<AudioSource>();
        source.clip = loopClip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.Play();
    }

    void Update()
    {
        if (source == null) return;
        bool over = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        float target = over ? 0f : volume;
        float speed = volume / Mathf.Max(0.01f, over ? fadeOutTime : fadeInTime);
        source.volume = Mathf.MoveTowards(source.volume, target, speed * Time.unscaledDeltaTime);
    }
}
