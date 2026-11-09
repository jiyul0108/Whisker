using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 에디터 메뉴: Whisker > 타이틀 씬 만들기
// Assets/Scenes/Title.unity를 만들고(이미 있으면 새로), 빌드 목록 맨 앞에 Title, 그 다음 Main을 넣는다
// 끝나면 Main 씬을 다시 연다
public static class TitleSceneBuilder
{
    private const string TitlePath = "Assets/Scenes/Title.unity";
    private const string MainPath = "Assets/Scenes/Prototype/Main.unity";

    [MenuItem("Whisker/타이틀 씬 만들기")]
    private static void Build()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Whisker", "Play를 끈 상태에서 실행해 주세요.", "확인");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 카메라 (검은 배경)
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        camGO.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGO.AddComponent<AudioListener>();

        // 타이틀 화면
        var title = new GameObject("Title").AddComponent<TitleScreen>();
        var so = new SerializedObject(title);
        so.FindProperty("background").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Backgrounds/room01_bg.png");
        so.FindProperty("catSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Characters/Cat/Sprites/meow/cat_meow_01.png");
        so.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Galmuri11.ttf");
        so.FindProperty("ambience").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/amb_hospital_loop.wav");
        so.ApplyModifiedPropertiesWithoutUndo();

        // 설정 창 (타이틀의 "설정" 버튼으로 열림. 게임을 멈추지 않고 뒤로 버튼만 있음)
        var settings = new GameObject("Settings").AddComponent<PauseMenu>();
        var sso = new SerializedObject(settings);
        sso.FindProperty("settingsOnly").boolValue = true;
        sso.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Galmuri11.ttf");
        sso.FindProperty("previewClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx_item_pickup.wav");
        sso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, TitlePath);

        // 빌드 목록: Title → Main → 나머지
        var list = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(TitlePath, true) };
        bool hasMain = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == TitlePath) continue;
            if (s.path == MainPath) hasMain = true;
            list.Add(s);
        }
        if (!hasMain) list.Insert(1, new EditorBuildSettingsScene(MainPath, true));
        EditorBuildSettings.scenes = list.ToArray();

        EditorSceneManager.OpenScene(MainPath);
        Debug.Log("타이틀 씬을 만들었어요: " + TitlePath + " (빌드 목록 맨 앞)");
    }
}
