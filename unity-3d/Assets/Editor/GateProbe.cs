using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Diagnoses a missing level-end gate. Writes + flushes after EVERY step with
 // a run timestamp, so a silent/odd failure still leaves the exact last step.
 public static class GateProbe {
  static StringBuilder sb;
  static string runId;
  static void Flush(string step){
   sb.AppendLine(runId+" "+step);
   string outDir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation"));
   System.IO.Directory.CreateDirectory(outDir);
   System.IO.File.WriteAllText(System.IO.Path.Combine(outDir,"gate-diagnose.txt"),sb.ToString());
  }
  public static void Diagnose(){
   sb=new StringBuilder();
   runId="run"+System.DateTime.Now.ToString("HHmmss");
   try{
    Flush("start");
    bool hasCam=!ReferenceEquals(Camera.main,null);
    Flush("hasCam="+hasCam);
    if(!hasCam){var cam=new GameObject("ProbeCamera").AddComponent<Camera>();cam.tag="MainCamera";}
    Flush("cam-ok");
    bool nullBefore=ReferenceEquals(RealmGame.I,null);
    Flush("I-null-before="+nullBefore);
    if(nullBefore){
     var gr=new GameObject("ProbeGame").AddComponent<RealmGame>();
     RealmGame.I=gr;
     gr.Save=new Progress();
     gr.Enemies.Clear();
     gr.Level=1;gr.Realm=0;gr.Screen=GameScreen.Menu;
     var wgo=new GameObject("ProbeWorld");
     var w=wgo.AddComponent<RealmWorld>();
     gr.World=w;
     try{
      w.Build(1,0);
      Flush("build-done");
     }catch(System.Exception e){Flush("BUILD_THREW "+(e.InnerException??e).GetType().Name+": "+(e.InnerException??e).Message);}
    }
    Flush("after-addcomponent");
    var g=RealmGame.I;
    Flush("got-g null="+ReferenceEquals(g,null));
    if(ReferenceEquals(g,null)){Flush("g-null-end");return;}
    Flush("world-managed-null="+ReferenceEquals(g.World,null));
    Flush("world-unity-null="+(g.World==null));
    if(ReferenceEquals(g.World,null)){Flush("world-null-end");return;}
    int islands=0;Transform last=null;int kids=0;
    foreach(Transform t in g.World.transform){
     kids++;
     if(ReferenceEquals(t,null)){Flush("kid-null");continue;}
     if(t.name!="Island")continue;
     islands++;
     if(ReferenceEquals(last,null)||t.position.z>last.position.z)last=t;
    }
    Flush("islands="+islands+" kids="+kids+" hasLast="+(!ReferenceEquals(last,null)));
    if(!ReferenceEquals(last,null)){
     Flush("lastPos="+last.position.ToString("0.##"));
     foreach(Transform c in last)Dump(c,"island");
    }
    foreach(Transform t in g.World.transform){
     if(ReferenceEquals(t,null)||t.name!="Realm gate")continue;
     Dump(t,"gate");
     Flush("gateWORLD="+t.position.ToString("0.##"));
    }
    Flush("gate-loop-done");
    Flush("worldPos="+g.World.transform.position.ToString("0.##")+" EndZ="+g.World.EndZ.ToString("0.##"));
   }catch(System.Exception e){Flush("THREW "+e.GetType().Name+": "+e.Message);}
   Debug.Log("GATE_DIAGNOSE_DONE");
   if (Application.isBatchMode) EditorApplication.Exit(0);
  }
  static void Dump(Transform t,string tag){
   var col=t.GetComponent<Collider>();
   var ren=t.GetComponent<Renderer>();
   bool hasGate=t.GetComponent<RealmGate>()!=null;
   sb.AppendLine(runId+" "+tag+" "+t.name+" local="+(t.parent?t.localPosition.ToString("0.##"):"-")
    +" active="+t.gameObject.activeSelf
    +" collider="+(col?(col.enabled?"on":"OFF"):"-")
    +" renderer="+(ren?(ren.enabled?"on":"OFF"):"-")
    +" gate="+(hasGate?"YES":"-"));
   Flush("dumped "+tag+" "+t.name);
   foreach(Transform c in t)Dump(c,tag+">"+c.name);
  }
 }
}