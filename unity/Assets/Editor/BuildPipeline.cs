using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LostRealms.EditorTools
{
    /// Builds the runtime-bootstrap scene and wires the hero model reference. Everything else
    /// (level geometry, enemies, pickups, UI) is constructed at runtime by the game scripts.
    public static class SceneBootstrap
    {
        private const string ScenePath = "Assets/Scenes/Game.unity";
        private const string HeroFbx = "Assets/Models/Hero/Aster_Mixamo.fbx";
        private const string BruteFbx = "Assets/Models/StoneBrute/Clips/Golem_Punch.fbx";

        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Camera
            var cam = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam.AddComponent<LostRealms.CameraFollow>();
            cam.transform.position = new Vector3(5f, 5.6f, -10f);
            cam.tag = "MainCamera";

            // Bootstrap object
            var boot = new GameObject("GameBootstrap");
            boot.AddComponent<LostRealms.GameManager>();
            var builder = boot.AddComponent<LostRealms.LevelBuilder>();
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HeroFbx);
            if (heroPrefab != null) builder.heroModelPrefab = heroPrefab;
            else Debug.LogWarning($"SceneBootstrap: hero FBX not found at {HeroFbx} — capsule placeholder will be used");
            var brutePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BruteFbx);
            if (brutePrefab != null) builder.golemModelPrefab = brutePrefab;
            boot.AddComponent<LostRealms.TouchControls>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"SceneBootstrap: scene written to {ScenePath}, hero prefab = {(heroPrefab != null ? heroPrefab.name : "MISSING")}");
        }

        public static void SetupAndBuild()
        {
            RigIntegration.Run(heroPrefab: true);
            Setup();
            AndroidBuild.Build();
        }
    }

    /// Configures Android player settings and produces the debug APK.
    public static class AndroidBuild
    {
        private const string ApkPath = "Builds/Legends_Unity_Debug.apk";

        public static void Build()
        {
            PlayerSettings.companyName = "Lost Realms Studio";
            PlayerSettings.productName = "Legends of the Lost Realms";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.manus.lostrealms.unity");

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.accelerometerFrequency = 0;

            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            Directory.CreateDirectory("Builds");
            var scenes = new[] { SceneBootstrap_ScenePath() };
            var report = BuildPipeline.BuildPlayer(scenes, ApkPath, BuildTarget.Android,
                BuildOptions.Development | BuildOptions.AllowDebugging);

            var summary = report.summary;
            Debug.Log($"AndroidBuild: result={summary.result} size={summary.totalSize / (1024 * 1024)}MB output={ApkPath}");
            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new System.Exception($"APK build failed: {summary.result} — {summary.totalErrors} errors");
        }

        private static string SceneBootstrap_ScenePath() => "Assets/Scenes/Game.unity";
    }
}
