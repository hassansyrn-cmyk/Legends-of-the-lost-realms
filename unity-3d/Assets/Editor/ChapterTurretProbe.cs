using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class ChapterTurretProbe {
  // Run in an isolated editor process with -batchmode -executeMethod
  // LostRealms.ChapterTurretProbe.Run; never replace a live game's singleton.
  public static void Run(){
   if(!Application.isBatchMode)throw new System.InvalidOperationException("Run ChapterTurretProbe in batch mode.");
   var report=new StringBuilder();int failures=0;
   var camera=new GameObject("Turret probe camera").AddComponent<Camera>();camera.tag="MainCamera";
   for(int stage=1;stage<=15;stage++){
    TrapArt.Reserved.Clear();
    var game=new GameObject("Turret probe game").AddComponent<RealmGame>();RealmGame.I=game;
    game.Save=new Progress();game.Level=stage;game.Realm=stage<=4?0:stage<=7?1:stage<=10?2:3;game.Screen=GameScreen.Menu;
    var world=new GameObject("Turret probe world").AddComponent<RealmWorld>();game.World=world;
    try{
     world.Build(stage,game.Realm);Physics.SyncTransforms();
     int count=0,early=0;
     foreach(var turret in world.GetComponentsInChildren<DartTurret>()){
      count++;if(Mathf.Abs(turret.transform.parent.position.z-10f)<1f)early++;
      bool model=false;foreach(var t in turret.GetComponentsInChildren<Transform>())if(t.name.StartsWith("Trap_Turret_"))model=true;
      bool supported=ChapterLayout.Supported(turret.transform.parent,turret.transform,new Bounds(turret.transform.position,new Vector3(2,.1f,2)),out _);
      report.AppendLine($"stage={stage} turret={turret.transform.position} model={model} supported={supported}");
      if(!model||!supported)failures++;
     }
     report.AppendLine($"STAGE {stage}: turrets={count} first-route-island={early}");
     if(early!=1)failures++;
    }catch(System.Exception e){report.AppendLine(e.ToString());failures++;}
    finally{Object.DestroyImmediate(world.gameObject);Object.DestroyImmediate(game.gameObject);}
   }
   Object.DestroyImmediate(camera.gameObject);RealmGame.I=null;TrapArt.Reserved.Clear();
   report.AppendLine("FAILURES="+failures);
   System.IO.Directory.CreateDirectory("Validation");System.IO.File.WriteAllText("Validation/chapter-turrets.txt",report.ToString());
   Debug.Log(report.ToString());if(Application.isBatchMode)EditorApplication.Exit(failures==0?0:1);
  }
 }
}
