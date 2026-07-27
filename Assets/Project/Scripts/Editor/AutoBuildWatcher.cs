using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// Lets an external process (Claude) trigger AutomatedBuildRunner.RunAll()
// while the Editor stays open, by dropping a marker file at
// <project>/AutoBuild/AutoBuildTrigger.txt containing the text "pending".
// Uses a dedicated folder rather than Temp/, since Unity's own Temp/
// housekeeping can sweep away files it doesn't recognize.
// No manual menu clicks or closing the Editor required.
[InitializeOnLoad]
public static class AutoBuildWatcher
{
    private const string TriggerFileName = "AutoBuildTrigger.txt";
    private const string ResultFileName = "AutoBuildResult.txt";

    static AutoBuildWatcher()
    {
        EditorApplication.update += Tick;
    }

    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
    private static string AutomationDir => Path.Combine(ProjectRoot, "AutoBuild");
    private static string TriggerPath => Path.Combine(AutomationDir, TriggerFileName);
    private static string ResultPath => Path.Combine(AutomationDir, ResultFileName);

    private static void Tick()
    {
        string path = TriggerPath;

        if (!File.Exists(path))
        {
            return;
        }

        string state = File.ReadAllText(path).Trim();

        switch (state)
        {
            case "pending":
                File.WriteAllText(path, "refreshed");
                AssetDatabase.Refresh();
                break;
            case "refreshed":
                File.WriteAllText(path, "grace1");
                break;
            case "grace1":
                File.WriteAllText(path, "grace2");
                break;
            case "grace2":
                if (EditorApplication.isCompiling)
                {
                    File.WriteAllText(path, "compiling");
                }
                else
                {
                    RunAndCleanup(path);
                }
                break;
            case "compiling":
                if (!EditorApplication.isCompiling)
                {
                    RunAndCleanup(path);
                }
                break;
        }
    }

    private static void RunAndCleanup(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception)
        {
            // ignore - not critical if the trigger file lingers
        }

        try
        {
            AutomatedBuildRunner.RunAll();
            File.WriteAllText(ResultPath, "OK " + DateTime.Now);
        }
        catch (Exception exception)
        {
            Debug.LogError("AutoBuildWatcher: build run failed: " + exception);
            File.WriteAllText(ResultPath, "FAIL " + DateTime.Now + "\n" + exception);
        }
    }
}
