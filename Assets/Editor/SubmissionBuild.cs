using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SubmissionBuild
{
    private const string BootstrapPath = "Assets/Scenes/Bootstrap.unity";

    [MenuItem("Tools/Tower Defense/Build Windows Submission")]
    public static void BuildWindows()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Build Windows submission outside Play Mode.");
        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone) != ScriptingImplementation.Mono2x)
            throw new InvalidOperationException("Windows submission requires the existing Mono backend.");

        EnsureBootstrapScene();
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        int index = scenes.FindIndex(scene => scene.path == BootstrapPath);
        if (index < 0) scenes.Add(new EditorBuildSettingsScene(BootstrapPath, true));
        else scenes[index] = new EditorBuildSettingsScene(BootstrapPath, true);
        EditorBuildSettings.scenes = scenes.ToArray();

        string projectDirectory = Path.GetDirectoryName(Application.dataPath);
        string executable = Path.Combine(projectDirectory, "Builds", "Windows", "TouHouNights.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(executable));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { BootstrapPath },
            locationPathName = executable,
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded || !File.Exists(executable))
            throw new InvalidOperationException("Windows submission build failed: " + report.summary.result);

        Debug.Log("SUBMISSION_BUILD PASS: " + executable);
    }

    private static void EnsureBootstrapScene()
    {
        string absolutePath = Path.Combine(Application.dataPath, "Scenes", "Bootstrap.unity");
        if (File.Exists(absolutePath)) return;
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");

        // 用附加场景生成构建入口，避免替换用户当前打开或尚未保存的场景。
        var original = SceneManager.GetActiveScene();
        if (!Application.isBatchMode && string.IsNullOrEmpty(original.path))
            throw new InvalidOperationException("Save the current scene before creating the bootstrap scene.");
        var mode = Application.isBatchMode ? NewSceneMode.Single : NewSceneMode.Additive;
        var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
        try
        {
            if (!EditorSceneManager.SaveScene(bootstrap, BootstrapPath))
                throw new InvalidOperationException("Could not save the bootstrap scene.");
        }
        finally
        {
            if (!Application.isBatchMode)
            {
                EditorSceneManager.CloseScene(bootstrap, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
        }
    }
}
