using UnityEngine;
using System.Collections;
using System.IO;
using System;
namespace LostRealms {
 [DefaultExecutionOrder(10)] public class RuntimeProbe:MonoBehaviour {
  Vector2 move;bool jump;int assertions;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void StartIfRequested(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-realmTest")>=0)new GameObject("Runtime validation").AddComponent<RuntimeProbe>();}
  void Update(){var g=RealmGame.I;if(g&&g.Screen==GameScreen.Playing){g.MoveInput=move;g.JumpPressed=jump;jump=false;}}
  void Check(bool okay,string message){if(!okay){Debug.LogError("REALM_TEST_FAILED: "+message);Quit(1);throw new Exception(message);}assertions++;Debug.Log("PASS: "+message);}
  IEnumerator Start(){yield return null;var g=RealmGame.I;g.LoadLevel(1);yield return new WaitForSeconds(.6f);Check(g.Player.Grounded,"Aster spawns on walkable ground");
   move=Vector2.up;yield return new WaitForSeconds(.55f);jump=true;yield return new WaitForSeconds(.3f);float firstHeight=g.Player.transform.position.y;jump=true;yield return new WaitForSeconds(.25f);Check(g.Player.transform.position.y>firstHeight,"Second jump adds altitude");yield return new WaitForSeconds(.65f);move=Vector2.zero;yield return new WaitForSeconds(.6f);Check(g.Player.transform.position.z>6&&g.Player.Grounded,"Double jump crosses the first island gap");
   for(int level=1;level<=10;level++){g.LoadLevel(level);yield return new WaitForSeconds(.25f);Check(g.World.Route.Count>=8,"Route populated: "+level);Check(g.Player.Visual.GetComponentInChildren<Renderer>()!=null,"Player model present: "+level);if(g.World.IsBoss)Check(g.Enemies.Exists(e=>e&&e.Boss),"Guardian present: "+level);
    foreach(var enemy in g.Enemies.ToArray()){Check(enemy.Visual.GetComponentInChildren<Renderer>()!=null,"Enemy model present");}
   }
   g.LoadLevel(1);yield return new WaitForSeconds(.3f);var target=g.Enemies[0];g.Player.Warp(target.transform.position);int health=g.Player.Health;yield return new WaitForSeconds(.15f);Check(g.Player.Health==health,"No passive enemy contact damage");g.Player.Warp(g.World.Spawn);
   target.Hit(999,0,true);Check(target.Health==0,"Combat can defeat enemies");
   float clock=g.Elapsed;g.Pause();yield return new WaitForSeconds(.3f);Check(Mathf.Approximately(clock,g.Elapsed),"Pause stops gameplay time");g.Resume();g.ActivateCheckpoint(g.World.Spawn+Vector3.forward);g.Player.Warp(g.World.Spawn+Vector3.forward*2);g.Respawn();Check(Vector3.Distance(g.Player.transform.position,g.Checkpoint)<.1f,"Respawn restores checkpoint position");
   g.LoadLevel(1);yield return new WaitForSeconds(.6f);string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));Directory.CreateDirectory(outDir);Capture(outDir+"/Verdant.png");
   g.LoadLevel(7);yield return new WaitForSeconds(.4f);g.Player.Warp(g.World.Route[g.World.Route.Count-1]+new Vector3(0,.1f,-5));yield return new WaitForSeconds(.4f);Capture(outDir+"/Sunscar.png");
   g.LoadLevel(10);yield return new WaitForSeconds(.4f);g.Player.Warp(g.World.Route[g.World.Route.Count-1]+new Vector3(0,.1f,-5));yield return new WaitForSeconds(.4f);Capture(outDir+"/Whiteout.png");
   File.WriteAllText(outDir+"/runtime-results.txt",$"Passed {assertions} runtime assertions: movement, double jump, ten routes, models, three bosses, passive-contact damage, defeat, pause and checkpoint respawn.\n");Debug.Log("REALM_RUNTIME_TESTS_PASSED "+assertions);Quit(0);
  }
  static void Capture(string path){var camera=Camera.main;var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Destroy(rt);Destroy(tex);}
  static void Quit(int code){
#if UNITY_EDITOR
   UnityEditor.EditorApplication.Exit(code);
#else
   Application.Quit(code);
#endif
  }
 }
}
