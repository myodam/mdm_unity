using UnityEditor;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public class SetupBuildScenes
{
    static SetupBuildScenes()
    {
        EditorApplication.delayCall += AddScenes;
    }

    static void AddScenes()
    {
        string[] scenes = new string[] 
        { 
            "Assets/01_Scenes/Loby.unity",
            "Assets/01_Scenes/Scene_01.unity", 
            "Assets/01_Scenes/Scene_02.unity",
            "Assets/01_Scenes/Scene_03.unity"
        };
        
        var buildScenes = EditorBuildSettings.scenes.ToList();
        bool changed = false;

        foreach (var scene in scenes)
        {
            if (!buildScenes.Any(s => s.path == scene))
            {
                buildScenes.Add(new EditorBuildSettingsScene(scene, true));
                changed = true;
            }
        }

        if (changed)
        {
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("Added scenes to Build Settings automatically.");
        }
    }
}
