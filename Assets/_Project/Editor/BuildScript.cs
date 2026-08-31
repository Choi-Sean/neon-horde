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
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            // NOTE: OptimizeSize + Debug keeps the IL2CPP native compile within RAM on this
            // dev box. Rebuild with Release/Master before a public launch on a bigger machine.
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Debug);
            PlayerSettings.SetIl2CppCodeGeneration(android, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            ApplyAndroidSigning();

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
            ReimportArtFolder();
            EditorUserBuildSettings.buildAppBundle = false;
            // Lighter IL2CPP so the native (clang) compile doesn't OOM on a dev box.
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetIl2CppCompilerConfiguration(android, Il2CppCompilerConfiguration.Debug);
            PlayerSettings.SetIl2CppCodeGeneration(android, UnityEditor.Build.Il2CppCodeGeneration.OptimizeSize);
            UseDebugSigning(); // device-test APK: debug-signed (Play needs the keystore build)
            EditorUserBuildSettings.androidCreateSymbols = AndroidCreateSymbols.Disabled;
            var outDir = Path.GetFullPath("build/android");
            Directory.CreateDirectory(outDir);
            Build(new BuildPlayerOptions
            {
                scenes = EnabledScenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                locationPathName = Path.Combine(outDir, "NeonHorde.apk"),
                // release Gradle variant (smaller, symbols stripped); gradleTemplate keeps
                // the daemon off so lintVitalAnalyzeRelease no longer OOMs.
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

        /// <summary>
        /// Wire the upload keystore for release Android builds. Passwords come from env
        /// (ANDROID_KEYSTORE_PASS / ANDROID_KEY_PASS); path + alias are fixed. If the
        /// keystore file or passwords are missing, falls back to the debug key (fine for
        /// local device APKs, rejected by Play).
        /// </summary>
        static void ApplyAndroidSigning()
        {
            string keystore = Path.GetFullPath("keystore/neonhorde-upload.keystore");
            string storePass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS");
            string keyPass = Environment.GetEnvironmentVariable("ANDROID_KEY_PASS") ?? storePass;

            if (File.Exists(keystore) && !string.IsNullOrEmpty(storePass))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = "neonhorde";
                PlayerSettings.Android.keyaliasPass = keyPass;
                Debug.Log("[Build] Android: signing with upload keystore.");
            }
            else
            {
                UseDebugSigning();
                Debug.LogWarning("[Build] Android: no upload keystore/password — using DEBUG key (not Play-uploadable).");
            }
        }

        static void UseDebugSigning()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = string.Empty;
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasName = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
        }

        // Mirror Assets/_Project/Art/ (the user's drop folder, left untouched) into
        // Resources/art/ so SpriteBank can load them, then force a reimport through
        // ArtImportSettings (Sprite mode). Extensions Unity can't import (.jfif) are
        // copied with a .jpg name.
        static void ReimportArtFolder()
        {
            const string src = "Assets/_Project/Art";
            const string dst = "Assets/_Project/Resources/art";
            Directory.CreateDirectory(dst);

            if (Directory.Exists(src))
            {
                foreach (var f in Directory.GetFiles(src))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".meta") continue;
                    string name = Path.GetFileNameWithoutExtension(f);
                    if (ext == ".jfif") ext = ".jpg";
                    if (ext != ".png" && ext != ".jpg" && ext != ".jpeg") continue;
                    string target = Path.Combine(dst, name + ext);
                    if (!File.Exists(target) || File.GetLastWriteTimeUtc(f) > File.GetLastWriteTimeUtc(target))
                    {
                        File.Copy(f, target, true);
                        Debug.Log($"[Build] art sync {name}{ext}");
                    }
                }
            }

            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            Debug.Log("[Build] reimported " + dst);
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
