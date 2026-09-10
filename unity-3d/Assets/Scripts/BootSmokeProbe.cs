using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LostRealms {
 /// <summary>
 /// Player-level startup regression test. Unlike source checks, this runs the
 /// compiled game for several frames and fails if Aster, the world, or camera
 /// handoff does not complete, or if Unity logs an exception/error.
 /// </summary>
 [DefaultExecutionOrder(-10000)] public sealed class BootSmokeProbe:MonoBehaviour {
  readonly List<string> failures=new List<string>();
  static bool Requested=>Array.IndexOf(Environment.GetCommandLineArgs(),"-realmBootSmoke")>=0;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void Install(){
   if(!Requested)return;
   var go=new GameObject("Boot smoke probe");DontDestroyOnLoad(go);go.AddComponent<BootSmokeProbe>();
  }

  void Awake(){Application.logMessageReceived+=Capture;}
  void Capture(string message,string stack,LogType type){
   if(type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;
   if(message.StartsWith("BOOT_SMOKE_FAILED",StringComparison.Ordinal))return;
   failures.Add(type+": "+message+"\n"+stack);
  }

  IEnumerator Start(){
   // RealmGame is installed AfterSceneLoad and constructs level one synchronously.
   // Allow lifecycle Start/Update/LateUpdate and render initialization to execute.
   float deadline=Time.realtimeSinceStartup+8f;
   while(Time.realtimeSinceStartup<deadline&&(!RealmGame.I||!RealmGame.I.Player||!RealmGame.I.CameraRig||!RealmGame.I.World))yield return null;
   yield return null;yield return new WaitForEndOfFrame();yield return null;

   var game=RealmGame.I;
   Require(game,"RealmGame singleton was not created");
   if(game){
    Require(game.World,"Realm world construction did not complete");
    Require(game.Player,"Aster was not created");
    Require(game.CameraRig,"Adventure camera was not created");
    if(game.Player&&game.CameraRig)Require(game.CameraRig.Target==game.Player.transform,"Camera target was not handed to Aster");
    if(game.World)Require(game.World.Route.Count>=8,"Level-one route construction was incomplete");
    if(game.Player)Require(game.Player.Visual&&game.Player.Visual.GetComponentInChildren<Renderer>(true),"Aster visual was not rendered");
    if(game.CameraRig&&game.World)Require(game.CameraRig.transform.position.y>game.World.Spawn.y+1f,"Camera remained beneath the starting island");
   }

   // Build folders are read-only when the player is launched by GameCI; use
   // Unity's writable per-user data directory for the probe report.
   string output=Application.persistentDataPath;Directory.CreateDirectory(output);
   string report=Path.Combine(output,"boot-smoke.txt");
   if(failures.Count==0){File.WriteAllText(report,"BOOT_SMOKE_PASSED\n");Debug.Log("BOOT_SMOKE_PASSED");Quit(0);yield break;}
   File.WriteAllText(report,"BOOT_SMOKE_FAILED\n\n"+string.Join("\n\n",failures));
   foreach(string failure in failures)Debug.LogError("BOOT_SMOKE_FAILED: "+failure);
   Quit(1);
  }

  void Require(bool condition,string message){if(!condition)failures.Add(message);}
  void OnDestroy(){Application.logMessageReceived-=Capture;}
  static void Quit(int code){
#if UNITY_EDITOR
   UnityEditor.EditorApplication.Exit(code);
#else
   Application.Quit(code);
#endif
  }
 }
}
