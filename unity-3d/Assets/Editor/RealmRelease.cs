using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Passwords enter through the process environment, never source control or command arguments.
public static class RealmRelease {
 static string Required(string name) {
  var value=Environment.GetEnvironmentVariable(name);
  if(string.IsNullOrEmpty(value))throw new InvalidOperationException("Missing release setting: "+name);
  return value;
 }
 static string First(string primary,string fallback) {
  var value=Environment.GetEnvironmentVariable(primary);
  if(!string.IsNullOrEmpty(value))return value;
  return Required(fallback);
 }
 public static void AndroidBundle() {
  string keystoreSetting=Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
  // GameCI's supported Android inputs create the keystore inside the project
  // and expose its relative name to the Unity container. Keep the explicit
  // ANDROID_KEYSTORE_PATH contract as the primary path for local builds.
  string keystore=Path.GetFullPath(string.IsNullOrEmpty(keystoreSetting)?Required("ANDROID_KEYSTORE_NAME"):keystoreSetting);
  if(!File.Exists(keystore))throw new FileNotFoundException("Release keystore not found.");
  string alias=First("ANDROID_KEY_ALIAS","ANDROID_KEYALIAS_NAME");
  string storePass=First("ANDROID_KEYSTORE_PASSWORD","ANDROID_KEYSTORE_PASS");
  string keyPass=First("ANDROID_KEY_PASSWORD","ANDROID_KEYALIAS_PASS");
  if(!int.TryParse(Required("ANDROID_VERSION_CODE"),out int code)||code<1)throw new InvalidOperationException("ANDROID_VERSION_CODE must be a positive integer.");
  string version=First("ANDROID_VERSION_NAME","VERSION");
  if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Install Unity Android Build Support.");
  // Preserve the tested animation assets. A clean checkout must use the canonical baker.
  if(!File.Exists("Assets/Resources/Characters/Aster.prefab"))AsterPhase1.Prepare();
  var target=NamedBuildTarget.Android;
  string oldStore=PlayerSettings.Android.keystoreName,oldAlias=PlayerSettings.Android.keyaliasName;
  bool oldCustom=PlayerSettings.Android.useCustomKeystore;
  try {
   PlayerSettings.SetApplicationIdentifier(target,"com.manus.lostrealms3d");
   PlayerSettings.bundleVersion=version;PlayerSettings.Android.bundleVersionCode=code;
   PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
   PlayerSettings.Android.targetSdkVersion=(AndroidSdkVersions)36;
   PlayerSettings.SetScriptingBackend(target,ScriptingImplementation.IL2CPP);
   PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
   PlayerSettings.SetManagedStrippingLevel(target,ManagedStrippingLevel.Medium);
   PlayerSettings.Android.minifyRelease=true;
   PlayerSettings.Android.useCustomKeystore=true;PlayerSettings.Android.keystoreName=keystore;
   PlayerSettings.Android.keyaliasName=alias;PlayerSettings.Android.keystorePass=storePass;PlayerSettings.Android.keyaliasPass=keyPass;
   EditorUserBuildSettings.buildAppBundle=true;EditorUserBuildSettings.development=false;
   EditorUserBuildSettings.androidCreateSymbols=AndroidCreateSymbols.Public;
   Directory.CreateDirectory("Builds/Android");
   var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
    scenes=new[]{"Assets/Scenes/Main.unity"},locationPathName="Builds/Android/LostRealms3D.aab",
    target=BuildTarget.Android,options=BuildOptions.None
   });
   if(result.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Signed Android bundle build failed: "+result.summary.result);
   Debug.Log("REALM_SIGNED_AAB_PASSED version="+version+" code="+code);
  } finally {
   PlayerSettings.Android.keystorePass="";PlayerSettings.Android.keyaliasPass="";
   PlayerSettings.Android.keystoreName=oldStore;PlayerSettings.Android.keyaliasName=oldAlias;PlayerSettings.Android.useCustomKeystore=oldCustom;
   AssetDatabase.SaveAssets();
  }
 }
}
