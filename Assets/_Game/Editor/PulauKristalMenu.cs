using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Tools > Pulau Kristal. Rebuilds the generated assets and scenes from code.
/// Warning: "Build Scenes" overwrites Island.unity and MainMenu.unity.
/// </summary>
public static class PulauKristalMenu
{
    [MenuItem("Tools/Pulau Kristal/1. Setup Project")]
    public static void SetupProject() => AssetsBuilder.SetupProject();

    [MenuItem("Tools/Pulau Kristal/2. Build Assets (sprites, animations, prefabs)")]
    public static void BuildAssets()
    {
        var art = AssetsBuilder.BuildArt();
        foreach (var character in new[] { AssetsBuilder.Soldier, AssetsBuilder.Orc, AssetsBuilder.Demon, AssetsBuilder.BloodMonster })
            AssetsBuilder.BuildCharacter(character);
        AssetsBuilder.BuildPrefabs(art);
        Debug.Log("[Builder] Assets done.");
    }

    [MenuItem("Tools/Pulau Kristal/3. Build Scenes (overwrites!)")]
    public static void BuildScenes()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildScenesNoPrompt();
    }

    public static void BuildScenesNoPrompt()
    {
        var art = AssetsBuilder.LoadArt();
        var prefabs = AssetsBuilder.LoadPrefabs();
        IslandBuilder.BuildMainMenu(art);
        IslandBuilder.Build(art, prefabs);
        IslandBuilder.RegisterScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[Builder] Scenes done.");
    }

    /// <summary>Runs every step (used from the command line).</summary>
    public static void BuildAll()
    {
        SetupProject();
        BuildAssets();
        BuildScenesNoPrompt();
    }
}
