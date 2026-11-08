using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 진행 상황 표시 (오브젝트에 붙이지 않음)
// 예: "FUSEBOX_CHECKED" = 두꺼비집을 조사했다, "POWER" = 전기가 들어왔다
// 게임을 다시 시작하면 비워진다
public static class GameFlags
{
    private static readonly HashSet<string> flags = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        flags.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) flags.Clear();
    }

    public static void Set(string flag)
    {
        if (string.IsNullOrEmpty(flag) || !flags.Add(flag)) return;
        GameLog.Info($"진행: {flag}");
    }

    public static bool Has(string flag) => !string.IsNullOrEmpty(flag) && flags.Contains(flag);
}
