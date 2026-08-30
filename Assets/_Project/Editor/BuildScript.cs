using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NeonHorde.EditorTools
{
    /// <summary>
    /// CI entry points. Called headless by Codemagic:
    ///   Unity -batchmode -nographics -quit -projectPath . \
    ///         -executeMethod NeonHorde.EditorTools.BuildScript.BuildAndroid
    /// Version code / build number are read from the BUILD_NUMBER env var when present.
    /// </summary>
    public static class BuildScript
    {
        static string[] EnabledScenes =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        static int BuildNumber
        {
            get
            {
                var v = Environment.GetEnvironmentVariable("BUILD_NUMBER");
                return int.TryParse(v, out var n) && n > 0 ? n : 1;
            }
        }

        [MenuItem("NeonHorde/Build/Android AAB")]
        public static void BuildAndroid()
        {
            ApplyVersion();
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            var outDir = Path.GetFullPath("build/android");
            Directory.CreateDirectory(outDir);

            Build(new BuildPlayerOptions
            {
                scenes = EnabledScenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                locationPathName = Path.Combine(outDir, "NeonHorde.aab"),
                options = BuildOptions.None
            });
        }

        [MenuItem("NeonHorde/Build/Android APK (device test)")]
        public static void BuildAndroidApk()
        {
            ApplyVersion();
            EditorUserBuildSettings.buildAppBundle = false;
            var outDir = Path.GetFullPath("build/android");
            Directory.CreateDirectory(outDir);
            Build(new BuildPlayerOptions
            {
                scenes = EnabledScenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                locationPathName = Path.Combine(outDir, "NeonHorde.apk"),
                options = BuildOptions.None
            });
        }

        [MenuItem("NeonHorde/Build/iOS Xcode project")]
        public static void BuildIOS()
        {
            ApplyVersion();
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.iOS.appleEnableAutomaticSigning = false; // CI signs with profiles

            var outDir = Path.GetFullPath("build/ios");
            Directory.CreateDirectory(outDir);

            Build(new BuildPlayerOptions
            {
                scenes = EnabledScenes,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS,
                locationPathName = outDir,
                options = BuildOptions.None
            });
        }

        static void ApplyVersion()
        {
            int n = BuildNumber;
            PlayerSettings.Android.bundleVersionCode = n;
            PlayerSettings.iOS.buildNumber = n.ToString();
            var name = Environment.GetEnvironmentVariable("APP_VERSION_NAME");
            if (!string.IsNullOrEmpty(name)) PlayerSettings.bundleVersion = name;
            Debug.Log($"[Build] version={PlayerSettings.bundleVersion} build={n}");
        }

        static void Build(BuildPlayerOptions opts)
        {
            BuildReport report = BuildPipeline.BuildPlayer(opts);
            BuildSummary s = report.summary;
            Debug.Log($"[Build] {s.platform} {s.result} size={s.totalSize}B errors={s.totalErrors} time={s.totalTime}");
            if (s.result != BuildResult.Succeeded)
            {
                Debug.LogError("[Build] FAILED");
                EditorApplication.Exit(1);
            }
        }
    }
}
