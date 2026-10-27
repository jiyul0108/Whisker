using UnityEngine;

// 주인 애니메이션(대기·걷기) + 항상 떨고 있는 효과
[RequireComponent(typeof(OwnerController))]
public class OwnerAnimator : MonoBehaviour
{
    [Header("그림을 그리는 곳 (자식 Visual)")]
    [SerializeField] private SpriteRenderer sr;

    [Header("대기")]
    [SerializeField] private Sprite[] idleFrames;      // owner_idle_01~02
    [SerializeField] private float idleFps = 3f;

    [Header("걷기")]
    [SerializeField] private Sprite[] walkFrames;      // owner_walk_01~04
    [SerializeField] private float walkFps = 6f;

    [Header("떨림")]
    [SerializeField] private float shakeAmount = 0.02f;   // 0.01 = 1픽셀
    [SerializeField] private float shakeInterval = 0.05f; // 몇 초마다 위치를 바꿀지
    [Range(0f, 3f)]
    [SerializeField] private float fear = 1f;             // 나중에 존재가 가까우면 올릴 값

    private OwnerController owner;
    private Transform visual;
    private Vector3 basePos;
    private float timer;
    private float shakeTimer;
    private OwnerController.State lastState;

    void Awake()
    {
        owner = GetComponent<OwnerController>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        visual = sr.transform;
        basePos = visual.localPosition;
        lastState = owner.CurrentState;
    }

    // 다른 스크립트에서 공포 정도 조절 (0 = 안 떪, 1 = 기본, 3 = 심하게)
    public void SetFear(float value)
    {
        fear = Mathf.Clamp(value, 0f, 3f);
    }

    void Update()
    {
        // 상태가 바뀌면 첫 프레임부터
        if (owner.CurrentState != lastState)
        {
            lastState = owner.CurrentState;
            timer = 0f;
        }
        timer += Time.deltaTime;

        if (owner.CurrentState == OwnerController.State.Moving)
            Play(walkFrames, walkFps);
        else
            Play(idleFrames, idleFps);

        Shake();
    }

    private void Play(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;
        int i = (int)(timer * fps) % frames.Length;
        sr.sprite = frames[i];
    }

    private void Shake()
    {
        shakeTimer -= Time.deltaTime;
        if (shakeTimer > 0f) return;
        shakeTimer = shakeInterval;

        // 좌우로 주로 떨고, 위아래는 조금만
        float x = Random.Range(-1f, 1f) * shakeAmount * fear;
        float y = Random.Range(-0.5f, 0.5f) * shakeAmount * fear;
        visual.localPosition = basePos + new Vector3(x, y, 0f);
    }
}
