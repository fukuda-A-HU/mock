using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreateMainScene
{
    public static void Create()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var controller = new GameObject("MainScene");
        controller.AddComponent<MainSceneController>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true) };
        AssetDatabase.SaveAssets();
        EditorApplication.Exit(0);
    }
}
