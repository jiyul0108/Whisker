using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 주운 열쇠 목록 (오브젝트에 붙이지 않음)
// 게임을 다시 시작하면(씬 다시 불러오기) 비워진다
public static class KeyInventory
{
    private static readonly Dictionary<string, string> keys = new Dictionary<string, string>(); // id → 이름

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        keys.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) keys.Clear();
    }

    public static int Count => keys.Count;

    public static void Add(string id, string name)
    {
        if (string.IsNullOrEmpty(id)) return;
        keys[id] = name;
        Debug.Log($"열쇠 획득: {name} ({id}) / 가진 열쇠 {keys.Count}개");
    }

    public static bool Has(string id) => !string.IsNullOrEmpty(id) && keys.ContainsKey(id);

    public static void Remove(string id)
    {
        if (!string.IsNullOrEmpty(id)) keys.Remove(id);
    }
}
