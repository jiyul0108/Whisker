using System.Collections;
using UnityEngine;

// 두꺼비집에 퓨즈를 끼웠을 때의 연출 (씬에 하나)
//  1) "쿵" 소리 + 화면 번쩍·흔들림 + 지하 불이 깜빡이다 켜짐
//  2) 카메라가 1층 계단실로 넘어가 2층 철문(전자 잠금)이 열리는 장면을 보여주고 돌아옴
//  3) 소리가 너무 커서 존재가 지하 계단으로 내려옴 → 지하 전체를 훑고 지나감
//     그동안 계단은 막히고, 주인과 함께 7번 창고 상자에 숨지 않으면 잡힌다
public class PowerEvent : MonoBehaviour
{
    public static PowerEvent Instance { get; private set; }
    public static bool Active { get; private set; }        // 존재가 지하를 훑는 중
    public static bool BlockStairs { get; private set; }   // 계단 이용 막기

    [Header("연결")]
    [SerializeField] private Stairs doorStairs;        // 열리는 2층 철문 (Stairs_1F_to2F)
    [SerializeField] private Stairs arrivalStairs;     // 존재가 내려오는 계단 (Stairs_B1_to1F)
    [SerializeField] private PresenceAI presence;
    [SerializeField] private CameraFollow cam;

    [Header("존재가 훑는 순서 (X 좌표)")]
    [SerializeField] private float[] sweepXs = { 46.2f, 80.2f, 55f };
    [SerializeField] private float sweepSpeed = 2.4f;
    [SerializeField] private float hideTime = 6f;      // 문이 열린 뒤 존재가 나타나기까지 (숨을 시간)

    [Header("소리 (비워두면 소리 없음)")]
    [SerializeField] private AudioClip thudClip;       // 전기가 들어오는 "쿵"
    [SerializeField] private AudioClip doorClip;       // 철문 열리는 소리
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("문구")]
    [SerializeField] private string powerMessage = "쿵—! 전기가 들어왔다";
    [SerializeField] private string doorMessage = "2층으로 가는 문의 잠금이 풀렸다";
    [SerializeField] private string warnMessage = "소리가 너무 컸다… 무언가 내려온다. 주인과 함께 숨어야 한다!";
    [SerializeField] private string passedMessage = "…지나갔다";

    private bool started;

    void Awake()
    {
        Instance = this;
        Active = false;
        BlockStairs = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Active = false;
        BlockStairs = false;
    }

    void Start()
    {
        if (presence == null) presence = FindAnyObjectByType<PresenceAI>();
        if (cam == null) cam = FindAnyObjectByType<CameraFollow>();
    }

    // 두꺼비집(Interactable, Action = PowerOn)이 호출
    public void Begin()
    {
        if (started) return;
        started = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        // 1) 전기가 들어옴
        GameFlags.Set("POWER");
        Play(thudClip);
        if (cam != null) cam.Shake(0.5f, 0.12f);
        StartCoroutine(ScreenFade.Flash(0.4f));
        Room.PowerOnAll();
        Message(powerMessage, 2f);
        yield return new WaitForSeconds(1.5f);

        // 2) 2층 철문이 열리는 장면
        GameInput.SetCutscene(true);
        if (presence != null) presence.SetPaused(true);

        yield return ScreenFade.Fade(0f, 1f, 0.4f);
        if (cam != null && doorStairs != null) cam.ShowPoint(doorStairs.transform.position);
        yield return null;
        yield return ScreenFade.Fade(1f, 0f, 0.4f);
        yield return new WaitForSeconds(0.4f);

        Play(doorClip);
        if (doorStairs != null) yield return doorStairs.RemoteUnlock();
        Message(doorMessage, 2f);
        yield return new WaitForSeconds(1.4f);

        yield return ScreenFade.Fade(0f, 1f, 0.4f);
        if (cam != null) cam.ClearShowPoint();
        yield return null;
        yield return ScreenFade.Fade(1f, 0f, 0.4f);

        if (presence != null) presence.SetPaused(false);
        GameInput.SetCutscene(false);

        // 3) 존재가 내려옴 → 숨어야 함
        Active = true;
        BlockStairs = true;
        Message(warnMessage, 3.5f);
        yield return new WaitForSeconds(hideTime);

        if (presence != null && arrivalStairs != null)
        {
            bool done = false;
            presence.StartSweep(new Vector2(arrivalStairs.X, arrivalStairs.FloorY), sweepXs, sweepSpeed, () => done = true);
            while (!done) yield return null;
        }

        Active = false;
        BlockStairs = false;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameOver) Message(passedMessage, 2f);
        GameLog.Info("전원 이벤트 끝");
    }

    private void Play(AudioClip clip)
    {
        if (clip == null) return;
        Vector3 at = cam != null ? cam.transform.position : Vector3.zero;
        AudioSource.PlayClipAtPoint(clip, at, volume);
    }

    private static void Message(string text, float seconds)
    {
        if (HintUI.Instance != null) HintUI.Instance.ShowMessage(text, seconds);
    }
}
