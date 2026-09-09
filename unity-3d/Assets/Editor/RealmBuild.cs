using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Linq;
using LostRealms;
public static class RealmBuild {
 [MenuItem("Lost Realms/Prepare project")]
 public static void Prepare(){
  if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
  Directory.CreateDirectory("Assets/Scenes");
  if (!File.Exists("Assets/Scenes/Main.unity")) { var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity"); }
  EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};
  PlayerSettings.companyName="Lost Realms Studio";PlayerSettings.productName="Legends of the Lost Realms 3D";PlayerSettings.bundleVersion="0.1.0";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultIsNativeResolution=false;
  PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeLeft;PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.manus.lostrealms3d");PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
  PlayerSettings.colorSpace=ColorSpace.Linear;PlayerSettings.runInBackground=false;
  var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);var input=settings.FindProperty("activeInputHandler");if(input!=null){input.intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();}
  QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowDistance=35;QualitySettings.antiAliasing=2;
  AssetDatabase.SaveAssets();Debug.Log("REALM_PROJECT_READY");
 }
 [MenuItem("Lost Realms/Build Windows")]
 public static void Windows(){AsterPhase1.Prepare();Prepare();Directory.CreateDirectory("Builds/Windows");var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName="Builds/Windows/LostRealms3D.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+result.summary.result);Debug.Log("REALM_WINDOWS_BUILD_PASSED");}
 [MenuItem("Lost Realms/Build Android APK")]
 public static void Android(){if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new System.Exception("Install Android Build Support, SDK, NDK and OpenJDK for this Unity version in Unity Hub.");AsterPhase1.Prepare();Prepare();PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.SetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Android,ManagedStrippingLevel.Minimal);Directory.CreateDirectory("Builds/Android");EditorUserBuildSettings.buildAppBundle=false;var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName="Builds/Android/LostRealms3D.apk",target=BuildTarget.Android,options=BuildOptions.Development});if(result.summary.result!=BuildResult.Succeeded)throw new System.Exception("Android build failed.");Debug.Log("REALM_ANDROID_BUILD_PASSED");}
 public static void PackageWindows(){AssetIntegration.Run();VisualReview.Render();Windows();} public static void IntegrateAndTest(){AssetIntegration.Run();PlayTest();} public static void PlayTest(){Prepare();VisualReview.Render();EditorApplication.EnterPlaymode();}
}



