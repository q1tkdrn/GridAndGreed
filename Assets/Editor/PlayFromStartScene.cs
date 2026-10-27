using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 에디터에서 Play를 누르면 어떤 씬을 열어 두었든 타이틀(Start) 씬부터 실행한다.
/// 실제 게임과 같은 순서로 매니저(인벤토리, 업적, 아이템)가 만들어지므로 저장/이어하기 검증에 필요하다.
/// 전투 씬의 "Debug Start Stage"로 보스를 바로 테스트하려면
/// 메뉴 GridAndGreed > Play From Start Scene 체크를 끄면 현재 열린 씬에서 실행된다. (개인 설정, 팀원에게 공유되지 않음)
/// </summary>
[InitializeOnLoad]
public static class PlayFromStartScene
{
    private const string StartScenePath = "Assets/JDY/Scenes/Start.unity";
    private const string MenuPath = "GridAndGreed/Play From Start Scene";
    private const string PrefKey = "GridAndGreed.PlayFromStartScene";

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    static PlayFromStartScene()
    {
        // 에디터가 켜진 직후에는 에셋 DB가 준비되지 않았을 수 있어 한 번 미뤄서 적용한다.
        EditorApplication.delayCall += Apply;
        // 에디터가 백그라운드에 있으면 delayCall이 늦게 실행될 수 있으므로
        // Play를 누르는 순간(에디트 모드를 벗어나기 직전)에도 반드시 다시 적용한다.
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingEditMode) Apply();
    }

    [MenuItem(MenuPath, priority = 0)]
    private static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return !EditorApplication.isPlaying;
    }

    private static void Apply()
    {
        if (!Enabled)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(StartScenePath);
        if (scene == null)
        {
            Debug.LogWarning($"타이틀 씬을 찾지 못해 현재 씬에서 Play합니다: {StartScenePath}");
            EditorSceneManager.playModeStartScene = null;
            return;
        }
        EditorSceneManager.playModeStartScene = scene;
    }
}
