using UnityEngine;

// 걷는 소리 (고양이·주인 오브젝트에 붙임). 걷는 동안만 반복 재생하고, 멈추면 천천히 작아진다
// 고양이: 바닥에서 좌우로 움직일 때 / 주인: 걷는 중일 때 (IsWalking)
public class FootstepLoop : MonoBehaviour
{
    [SerializeField] private AudioClip stepClip;                 // 반복용 발소리 (비워두면 소리 없음)
    [SerializeField, Range(0f, 1f)] private float volume = 0.25f;
    [SerializeField] private float pitch = 1f;
    [SerializeField] private float fade = 6f;

    private CatController cat;
    private OwnerController owner;
    private AudioSource source;

    void Awake()
    {
        cat = GetComponent<CatController>();
        owner = GetComponent<OwnerController>();
        if (stepClip == null) return;
        source = gameObject.AddComponent<AudioSource>();
        source.clip = stepClip;
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.pitch = pitch;
        source.volume = 0f;
    }

    void Update()
    {
        if (source == null) return;
        bool walking = false;
        if (cat != null)
            walking = cat.IsGrounded && Mathf.Abs(cat.MoveInput) > 0f && !HideController.IsHidden && !ClimbSpot.IsClimbing;
        else if (owner != null)
            walking = owner.IsWalking;
        if (Time.timeScale == 0f || GameInput.Blocked) walking = false;

        source.volume = Mathf.MoveTowards(source.volume, walking ? volume : 0f, fade * volume * Time.unscaledDeltaTime);
        if (source.volume > 0.001f && !source.isPlaying) source.Play();
        else if (source.volume <= 0.001f && source.isPlaying) source.Pause();
    }
}
