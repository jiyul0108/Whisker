using UnityEngine;

// 그 자리에서 계속 나는 소리 (예: 지하 창고의 고장 난 두꺼비집이 지지직거리는 소리)
// 고양이와 같은 층에 있을 때만 들리고, 가까울수록 크게, 왼쪽·오른쪽 방향도 들린다
// Stop Flag를 넣으면 그 진행이 생긴 뒤에는 조용해진다 (예: POWER = 퓨즈를 끼워 전기가 들어옴)
public class SoundEmitter : MonoBehaviour
{
    [SerializeField] private AudioClip clip;                      // 반복할 소리 (비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float volume = 0.7f;  // 바로 앞에서의 크기
    [SerializeField] private float hearDistance = 28f;            // 이 거리보다 멀면 안 들림
    [SerializeField] private string stopFlag = "";

    private AudioSource source;
    private CatController cat;

    void Start()
    {
        cat = FindAnyObjectByType<CatController>();
        if (clip == null) return;
        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;   // 크기·방향은 코드로
        source.volume = 0f;
        source.time = Random.Range(0f, clip.length);   // 매번 같은 곳부터 시작하지 않게
        source.Play();
    }

    void Update()
    {
        if (source == null || cat == null) return;
        float target = 0f;
        bool stopped = !string.IsNullOrEmpty(stopFlag) && GameFlags.Has(stopFlag);
        bool over = GameManager.Instance != null && GameManager.Instance.IsGameOver;
        Vector2 c = cat.transform.position;
        if (!stopped && !over && Stairs.SameFloor(c.y, transform.position.y))
        {
            float dx = transform.position.x - c.x;
            float k = 1f - Mathf.Clamp01(Mathf.Abs(dx) / hearDistance);
            target = volume * k;
            source.panStereo = Mathf.Clamp(dx / 10f, -1f, 1f) * 0.8f;   // 오른쪽에 있으면 오른쪽에서 들림
        }
        source.volume = Mathf.MoveTowards(source.volume, target, 2f * Time.unscaledDeltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
        Gizmos.DrawLine(transform.position + Vector3.left * hearDistance, transform.position + Vector3.right * hearDistance);
    }
}
