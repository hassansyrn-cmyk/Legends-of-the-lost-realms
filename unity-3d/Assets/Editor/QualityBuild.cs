using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LostRealms {
 public static class QualityBuild {
  [MenuItem("Lost Realms/Quality/Build Android APK")]
  public static void Android()=>BuildAndroid("LostRealms3D-quality.apk");
  [MenuItem("Lost Realms/Quality/Build Debug Android APK")]
  public static void AndroidDebug()=>BuildAndroid("LostRealms3D-debug.apk");
  static void BuildAndroid(string filename){
   // Use the validated assets as-is; never rebake characters while packaging.
   if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new Exception("Android Build Support is not installed.");
   Directory.CreateDirectory("Builds/Android");
   EditorUserBuildSettings.buildAppBundle=false;
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
    scenes=new[]{"Assets/Scenes/Main.unity"},
    locationPathName="Builds/Android/"+filename,
    target=BuildTarget.Android,options=BuildOptions.Development
   });
   File.WriteAllText("Validation/quality-build.txt","Build: "+report.summary.result+"\nUTC: "+DateTime.UtcNow.ToString("O")+"\nSize bytes: "+report.summary.totalSize+"\nDuration: "+report.summary.totalTime+"\nErrors: "+report.summary.totalErrors+"\n");
   if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Quality Android build failed: "+report.summary.result);
   Debug.Log("QUALITY_ANDROID_BUILD_PASSED");
  }
 }
}
