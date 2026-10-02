using UnityEditor;
using UnityEngine;
using System.Text;
namespace LostRealms {
 public static class DesertLayoutProbe {
  public static void Run(){
   var report=new StringBuilder();var camera=new GameObject("Probe camera").AddComponent<Camera>();camera.tag="MainCamera";
   foreach(int stage in new[]{5,6}){
    var game=new GameObject("Probe game").AddComponent<RealmGame>();RealmGame.I=game;game.Level=stage;game.Realm=1;game.Save=new Progress();
    var world=new GameObject("Probe world").AddComponent<RealmWorld>();game.World=world;world.Build(stage,1);Physics.SyncTransforms();
    report.AppendLine("CHAPTER "+stage);
    foreach(var t in world.GetComponentsInChildren<Transform>()){
     if(!t.GetComponent<RealmCheckpoint>()&&!t.GetComponent<BouncePad>()&&!t.GetComponent<SpeedRing>()&&!t.GetComponent<SpikeTrap>()&&t.name!="Realm gate")continue;
     report.AppendLine(t.name+" at "+t.position+" parent="+t.parent.name);
     foreach(var prop in world.GetComponentsInChildren<Transform>())if(ChapterLayout.IsProp(prop)&&Vector3.Distance(prop.position,t.position)<6f&&ChapterLayout.BoundsOf(prop,out var bounds))report.AppendLine("  "+prop.name+" at "+prop.position+" bounds="+bounds);
    }
    var gate=world.transform.Find("Realm gate");
    if(gate)for(float z=-7;z<0;z+=.5f){
     var p=gate.position+new Vector3(0,.45f,z);
     foreach(var hit in Physics.RaycastAll(p,Vector3.forward,.65f,~0,QueryTriggerInteraction.Ignore))report.AppendLine("LANE z="+z+" hit="+hit.collider.name+" parent="+hit.transform.parent.name+" point="+hit.point);
    }
    Object.DestroyImmediate(world.gameObject);Object.DestroyImmediate(game.gameObject);
   }
   Object.DestroyImmediate(camera.gameObject);System.IO.File.WriteAllText("Validation/desert-playtest-layout.txt",report.ToString());
  }
 }
}

