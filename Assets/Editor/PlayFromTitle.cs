using UnityEditor;
using UnityEditor.SceneManagement;

// 에디터에서 Play를 누르면 지금 열린 씬(Main)이 아니라 타이틀 씬부터 시작하게 한다 (실제 빌드와 같은 순서)
// 메뉴 Whisker > Play를 타이틀부터 시작 으로 켜고 끌 수 있다 (끄면 열린 씬에서 바로 시작 → 테스트할 때 편함)
[InitializeOnLoad]
public static class PlayFromTitle
{
    private const string TitlePath = "Assets/Scenes/Title.unity";
    private const string MenuPath = "Whisker/Play를 타이틀부터 시작";
    private const string PrefKey = "Whisker_PlayFromTitle";

    static PlayFromTitle()
    {
        EditorApplication.delayCall += Apply;   // 에디터가 다 켜진 뒤 적용
    }

    private static bool On => EditorPrefs.GetBool(PrefKey, true);   // 기본: 켜짐

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        EditorPrefs.SetBool(PrefKey, !On);
        Apply();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, On);
        return true;
    }

    private static void Apply()
    {
        EditorSceneManager.playModeStartScene = On ? AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) : null;
    }
}
