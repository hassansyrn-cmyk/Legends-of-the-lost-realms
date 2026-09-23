using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Chapter organization sweep: builds every chapter headless and reports
 // trap-trap overlaps, prop-trap overlaps, floating/sunken props, traps off
 // the deck, and per-island prop clutter. Run: -executeMethod
 // LostRealms.OrganizeSweep.Run -sweepStages A-B  (e.g. 1-5).
 public static class OrganizeSweep {
  static StringBuilder sb;
  static string OutDir=>System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation"));
  static void Flush(string step){
   sb.AppendLine(step);
   System.IO.Directory.CreateDirectory(OutDir);
   System.IO.File.WriteAllText(System.IO.Path.Combine(OutDir,"organize-sweep.txt"),sb.ToString());
  }
  struct Foot{public string name;public Transform island;public Vector3 pos;public float r;
   public bool isSeg;public Vector3 a,b;public float halfW;}
  public static void Run(){
   sb=new StringBuilder();
   int from=1,to=15;
   try{
    var args=System.Environment.GetCommandLineArgs();
    for(int i=0;i<args.Length-1;i++)if(args[i]=="-sweepStages"){
     var parts=args[i+1].Split('-');
     from=Mathf.Max(1,int.Parse(parts[0]));to=Mathf.Min(15,int.Parse(parts[1]));
    }
   }catch(System.Exception e){Flush("ARG_PARSE "+e.Message);}
   if(ReferenceEquals(Camera.main,null)){var cam=new GameObject("ProbeCamera").AddComponent<Camera>();cam.tag="MainCamera";}
   for(int stage=from;stage<=to;stage++){
    try{SweepStage(stage);}catch(System.Exception e){Flush("STAGE "+stage+" THREW "+(e.InnerException??e).GetType().Name+": "+(e.InnerException??e).Message);}
    // Tear down so the next stage scans clean.
    var old=GameObject.Find("ProbeGame");if(old)Object.DestroyImmediate(old);
    var oldW=GameObject.Find("ProbeWorld");if(oldW)Object.DestroyImmediate(oldW);
   }
   Flush("SWEEP_DONE "+from+"-"+to);
  }
  static int WorldFor(int stage)=>stage<=4?0:stage<=7?1:stage<=10?2:3;
  static void SweepStage(int stage){
   Flush("STAGE "+stage+" start");
   var gr=new GameObject("ProbeGame").AddComponent<RealmGame>();
   RealmGame.I=gr;
   gr.Save=new Progress();
   gr.Enemies.Clear();
   gr.Level=stage;gr.Realm=WorldFor(stage);gr.Screen=GameScreen.Menu;
   var wgo=new GameObject("ProbeWorld");
   var w=wgo.AddComponent<RealmWorld>();
   gr.World=w;
   w.Build(stage,WorldFor(stage));
   Flush("STAGE "+stage+" built islands="+w.Route.Count);
   Physics.SyncTransforms();
   var props=new List<Bounds>();
   foreach(var t in w.GetComponentsInChildren<Transform>()){
    if(t.name=="Thorn"||t.name=="Explosive barrel"||t.name=="Prop Lantern"||t.name.StartsWith("Prop Statue")||t.name=="VentGrate"||(t.name=="Rune ring"&&t.parent&&t.parent.name=="Island"))
     Flush("WARN stage="+stage+" UNWANTED_SHAPE "+t.name);
    if(!ChapterLayout.IsProp(t)||!ChapterLayout.BoundsOf(t,out var bounds))continue;
    foreach(var other in props)if(ChapterLayout.Overlap(bounds,other))Flush("WARN stage="+stage+" PROP_ON_PROP "+t.name);
    props.Add(bounds);
    var parent=t.parent;
    if(parent&&parent.name=="Island"&&!ChapterLayout.Supported(parent,t,bounds,out _))Flush("WARN stage="+stage+" PROP_UNSUPPORTED "+t.name);
   }
   // Collect islands.
   var islands=new List<Transform>();
   foreach(Transform t in w.transform)if(t.name=="Island")islands.Add(t);
   // Collect traps with footprints.
   var traps=new List<Foot>();
   AddTrap<SawTrap>(traps,true,0f);
   AddTrap<FloorBladeTrap>(traps,false,1.15f);
   AddTrap<SpikeTrap>(traps,false,1.05f);
   AddTrap<PendulumTrap>(traps,true,0f);
   AddTrap<FireGeyser>(traps,false,1f);
   AddTrap<CrusherPillar>(traps,false,1.3f);
   AddTrap<DartTurret>(traps,false,.8f);
   AddTrap<RollingBoulder>(traps,true,0f);
   AddTrap<WindVent>(traps,false,1f);
   AddTrap<FlameBrazier>(traps,false,.9f);
   AddTrap<FrostTotem>(traps,false,.9f);
   AddTrap<SerpentStatue>(traps,false,1f);
   AddTrap<SpeedRing>(traps,false,1.6f);
   // Trap-trap overlaps (same island only).
   for(int i=0;i<traps.Count;i++)for(int j=i+1;j<traps.Count;j++){
    var A=traps[i];var B=traps[j];
    if(!A.island||!B.island||A.island!=B.island)continue;
    if(FootDist(A,B)<-0.4f)Flush("WARN stage="+stage+" island="+IslandIdx(A.island)+" TRAP_OVERLAP "+A.name+" vs "+B.name+" gap="+FootDist(A,B).ToString("0.00"));
   }
   // Traps sitting inside another trap's reserved circle (roll paths, rings).
   // Ring-takeoff reserves (3.2, registered after trap lanes) are exempt:
   // rings fly 1.5 m up, so ground traps underneath are visually fine.
   foreach(var t in traps){
    if(!t.island)continue;
    foreach(var s in TrapArt.Reserved){
     if(!s.island||s.island!=t.island)continue;
     if(s.radius>=3.19f)continue;
     Vector3 sw=t.island.TransformPoint(s.local);
     float d=Dist2D(t.pos,sw);
     if(d>.6f&&d<t.r+s.radius-.4f)
      Flush("WARN stage="+stage+" island="+IslandIdx(t.island)+" TRAP_ON_RESERVED "+t.name+" gap="+(d-t.r-s.radius).ToString("0.00"));
    }
   }
   foreach(var t in traps){
    if(!t.island)continue;
    var surf=t.island.Find("Realm surface");
    if(!surf)continue;
    float hw=surf.localScale.x*.5f,hl=surf.localScale.z*.5f;
    Vector3 lp=t.island.InverseTransformPoint(t.pos);
    if(Mathf.Abs(lp.x)>hw+.5f||Mathf.Abs(lp.z)>hl+.5f)
     Flush("WARN stage="+stage+" island="+IslandIdx(t.island)+" TRAP_OFF_DECK "+t.name+" local="+lp.ToString("0.##"));
   }
   // Props per island.
   foreach(var isl in islands){
    int count=0;
    foreach(Transform c in isl){
     if(!c.name.StartsWith("Prop ")&&!c.name.StartsWith("Breakable")&&!c.name.StartsWith("Pushable"))continue;
     count++;
     var r=c.GetComponentInChildren<Renderer>();
     if(!r){Flush("WARN stage="+stage+" island="+IslandIdx(isl)+" PROP_NO_RENDERER "+c.name);continue;}
     Bounds b=r.bounds;
     foreach(var r2 in c.GetComponentsInChildren<Renderer>())b.Encapsulate(r2.bounds);
     float pr=Mathf.Max(.3f,Mathf.Max(b.size.x,b.size.z)*.5f);
     // vs traps on this island
     foreach(var t in traps){
      if(t.island!=isl)continue;
      if(FootDistPoint(t,b.center,pr)<-0.25f)
       Flush("WARN stage="+stage+" island="+IslandIdx(isl)+" PROP_ON_TRAP "+c.name+" vs "+t.name+" gap="+FootDistPoint(t,b.center,pr).ToString("0.00"));
     }
     // grounding: shared deck query (same ground truth as the settle pass).
     Vector3 bl=isl.InverseTransformPoint(b.center);
     if(RealmProps.TryDeckSurface(isl,bl.x,bl.z,c,out float sly)){
      float surfY=isl.TransformPoint(new Vector3(bl.x,sly,bl.z)).y;
      float d=b.min.y-surfY;
      if(d>.35f)Flush("WARN stage="+stage+" island="+IslandIdx(isl)+" PROP_FLOATING "+c.name+" above="+d.ToString("0.00"));
      else if(d<-.35f)Flush("WARN stage="+stage+" island="+IslandIdx(isl)+" PROP_SUNKEN "+c.name+" below="+d.ToString("0.00"));
     }
    }
    if(count>24)Flush("WARN stage="+stage+" island="+IslandIdx(isl)+" CLUTTER props="+count);
    else Flush("STAGE "+stage+" island="+IslandIdx(isl)+" props="+count);
   }
   Flush("STAGE "+stage+" done traps="+traps.Count);
  }
  static string IslandIdx(Transform isl)=>isl?isl.GetSiblingIndex().ToString():"?";
  static void AddTrap<T>(List<Foot> traps,bool seg,float r) where T:Component{
   foreach(var c in Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None)){
    var f=new Foot{name=typeof(T).Name+"@"+c.transform.position.ToString("0.#"),island=FindIsland(c.transform),pos=c.transform.position,r=r,isSeg=seg};
    if(seg&&(typeof(T)==typeof(SawTrap)||typeof(T)==typeof(PendulumTrap))){
     // Swing/rail corridor along local X: half-length from the track collider.
     var col=c.GetComponent<BoxCollider>();
     float half=col?col.size.x*.5f:2.6f;
     Vector3 right=c.transform.right;
     f.a=c.transform.position-right*half;f.b=c.transform.position+right*half;f.halfW=typeof(T)==typeof(SawTrap)?.7f:.9f;
    }else if(seg){
     // Boulder roll path: forward 10 m down its local -z... derive from motion dir.
     f.isSeg=false;f.r=1.1f;
    }
    traps.Add(f);
   }
  }
  static Transform FindIsland(Transform t){while(t!=null){if(t.name=="Island")return t;t=t.parent;}return null;}
  static float Dist2D(Vector3 x,Vector3 z){x.y=0;z.y=0;return Vector3.Distance(x,z);}
  static float SegDist(Vector3 a,Vector3 b,Vector3 p){
   Vector3 ab=b-a;float t=ab.sqrMagnitude>1e-6f?Mathf.Clamp01(Vector3.Dot(p-a,ab)/ab.sqrMagnitude):0f;
   return Dist2D(a+ab*t,p);
  }
  static float FootDist(Foot A,Foot B){
   if(A.isSeg&&B.isSeg)return SegSegDist(A.a,A.b,B.a,B.b)-A.halfW-B.halfW;
   if(A.isSeg)return SegDist(A.a,A.b,B.pos)-A.halfW-B.r;
   if(B.isSeg)return SegDist(B.a,B.b,A.pos)-B.halfW-A.r;
   return Dist2D(A.pos,B.pos)-A.r-B.r;
  }
  static float FootDistPoint(Foot T,Vector3 p,float pr){
   if(T.isSeg)return SegDist(T.a,T.b,p)-T.halfW-pr;
   return Dist2D(T.pos,p)-T.r-pr;
  }
  static float SegSegDist(Vector3 a1,Vector3 b1,Vector3 a2,Vector3 b2){
   // Sample-based segment-segment 2D distance (rails rarely cross).
   float best=float.MaxValue;
   for(int k=0;k<=6;k++){
    Vector3 p=a1+(b1-a1)*(k/6f);
    if(SegDist(a2,b2,p)<best)best=SegDist(a2,b2,p);
   }
   return best;
  }
 }
}
