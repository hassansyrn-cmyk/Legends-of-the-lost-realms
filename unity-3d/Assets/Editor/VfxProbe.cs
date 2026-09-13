using System;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Instantiates every VFX prefab through Vfx.Play (which now repairs particle
 // curve modes + materials) and counts logged errors, proving the
 // "velocity curves must all be in the same mode" flood is gone.
 public static class VfxProbe {
  public static void Diagnose(){
   var sb=new StringBuilder();int errs=0;
   Application.logMessageReceived+=(condition,stack,type)=>{if(type==LogType.Error||type==LogType.Exception){errs++;if(errs<=25)sb.AppendLine("ERR "+condition);}};
   var all=Resources.LoadAll<GameObject>("VFX");
   sb.AppendLine("prefabs="+all.Length);
   if(RealmGame.I==null){
    var g=new GameObject("VfxProbeGame").AddComponent<RealmGame>();
    RealmGame.I=g;g.Save=new Progress();g.Enemies.Clear();g.Level=1;g.Realm=0;g.Screen=GameScreen.Menu;
   }
   var world=new GameObject("VfxProbeWorld");RealmGame.I.World=world.AddComponent<RealmWorld>();
   int played=0;
   foreach(var pf in all){
    if(!pf)continue;
    try{Vfx.Play(pf.name,Vector3.zero);played++;}
    catch(Exception e){sb.AppendLine("PLAY_THREW "+pf.name+" "+e.Message);}
   }
   sb.AppendLine("played="+played+" errorsAfterRepair="+errs);
   string outDir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation"));
   System.IO.Directory.CreateDirectory(outDir);
   System.IO.File.WriteAllText(System.IO.Path.Combine(outDir,"vfx-probe.txt"),sb.ToString());
   Debug.Log("VFX_PROBE_DONE errs="+errs);
   EditorApplication.Exit(0);
  }
 }
}
