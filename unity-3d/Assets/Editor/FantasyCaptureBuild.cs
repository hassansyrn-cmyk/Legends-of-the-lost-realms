using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class FantasyCaptureBuild {
 [MenuItem("Lost Realms/Build Fantasy UI Capture Player (Linux)")]
 public static void Linux(){
  RealmBuild.Prepare();
  FantasyUIAssets.Prepare();
  string output=Path.GetFullPath("Builds/FantasyCapture/LostRealmsFantasyCapture.x86_64");
  Directory.CreateDirectory(Path.GetDirectoryName(output));
  var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
   scenes=new[]{"Assets/Scenes/Main.unity"},
   locationPathName=output,
   target=BuildTarget.StandaloneLinux64,
   options=BuildOptions.Development
  });
  if(result.summary.result!=BuildResult.Succeeded)
   throw new Exception("Fantasy UI capture build failed: "+result.summary.result);
  Debug.Log("FANTASY_UI_CAPTURE_BUILD_PASSED "+output);
 }
}