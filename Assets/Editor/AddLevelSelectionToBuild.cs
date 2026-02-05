using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public static class AddLevelSelectionToBuild
{
    [MenuItem("Labyrinth/Add LevelSelection to Build")]
    public static void AddScene()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        string scenePath = "Assets/Scenes/LevelSelection.unity";
        
        // Check if already added
        if (scenes.Any(s => s.path == scenePath))
        {
            UnityEngine.Debug.Log("LevelSelection scene already in build settings");
            return;
        }
        
        // Insert after MainMenu (index 0), before Game
        var newScene = new EditorBuildSettingsScene(scenePath, true);
        scenes.Insert(1, newScene);
        
        EditorBuildSettings.scenes = scenes.ToArray();
        UnityEngine.Debug.Log("Added LevelSelection scene to build settings at index 1");
    }
}