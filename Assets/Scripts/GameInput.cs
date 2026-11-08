using UnityEngine;
using UnityEngine.SceneManagement;

// 조작을 막아야 하는 때를 한곳에서 확인 (오브젝트에 붙이지 않음)
// 팝업(문서·침대·금고·인벤토리)이 열려 있거나, 연출 중이거나, 계단 이동 중이거나, 게임이 끝났을 때
public static class GameInput
{
    public static bool PopupOpen { get; private set; }
    public static bool Cutscene { get; private set; }

    private static int releaseFrame = -1;   // 팝업을 닫은 프레임 (같은 키 입력이 다른 곳에서 또 처리되지 않게)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        ResetAll();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetAll();
    }

    private static void ResetAll()
    {
        PopupOpen = false;
        Cutscene = false;
        releaseFrame = -1;
    }

    // 고양이 조작·조사·숨기를 막아야 하는가
    public static bool Blocked =>
        PopupOpen || Cutscene || Time.frameCount == releaseFrame || Stairs.IsTraveling
        || (GameManager.Instance != null && GameManager.Instance.IsGameOver);

    // 팝업을 열면 게임이 멈추고, 닫으면 다시 움직인다
    public static void SetPopup(bool open)
    {
        if (PopupOpen && !open) releaseFrame = Time.frameCount;
        PopupOpen = open;
        Time.timeScale = open ? 0f : 1f;
    }

    // 연출 중에는 조작만 막는다 (시간은 흐름)
    public static void SetCutscene(bool on)
    {
        if (Cutscene && !on) releaseFrame = Time.frameCount;
        Cutscene = on;
    }
}
