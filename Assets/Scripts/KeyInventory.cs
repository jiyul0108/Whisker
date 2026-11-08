using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// 가진 아이템 목록 (열쇠, 퓨즈, 조각 등. 오브젝트에 붙이지 않음)
// 게임을 다시 시작하면(씬 다시 불러오기) 비워진다
public static class KeyInventory
{
    private static readonly List<string> ids = new List<string>();   // 얻은 순서대로

    // 목록이 바뀔 때마다 1씩 올라감 (InventoryUI가 다시 그릴지 판단)
    public static int Version { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Init()
    {
        ids.Clear();
        Version++;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        ids.Clear();
        Version++;
    }

    public static int Count => ids.Count;
    public static IReadOnlyList<string> Ids => ids;

    public static void Add(string id, string name = null)
    {
        if (string.IsNullOrEmpty(id) || ids.Contains(id)) return;
        ids.Add(id);
        Version++;
        GameLog.Info($"아이템 획득: {ItemDB.Name(id)} ({id}) / 가진 아이템 {ids.Count}개");
    }

    public static bool Has(string id) => !string.IsNullOrEmpty(id) && ids.Contains(id);

    public static void Remove(string id)
    {
        if (!string.IsNullOrEmpty(id) && ids.Remove(id)) Version++;
    }
}
