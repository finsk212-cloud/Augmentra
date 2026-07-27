using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AutomatedBuildRunner
{
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    // Invoked from the command line via:
    // -batchmode -nographics -quit -executeMethod AutomatedBuildRunner.RunAll
    public static void RunAll()
    {
        try
        {
            SettingsAndPauseUISetup.Setup();
            RunInScene(GameplayScenePath, GameOverUIBuilder.Build);
            RunInScene(GameplayScenePath, AugmentUIBuilder.Build);
            ShopCanvasBuilder.Build();

            Debug.Log("AutomatedBuildRunner: all builder tools ran and saved successfully.");
        }
        catch (Exception exception)
        {
            Debug.LogError("AutomatedBuildRunner failed: " + exception);

            // Only force-quit when running headless (-batchmode); never close
            // an interactive Editor session that AutoBuildWatcher triggered us from.
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
            else
            {
                throw;
            }
        }
    }

    private static void RunInScene(string scenePath, Action build)
    {
        UnityEngine.SceneManagement.Scene scene =
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        build();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
