using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace LostRealms {
 // Headless weapon attachment audit (Play mode, -realmTest -weaponAudit, no -quit).
 // Measures the REAL runtime transforms: sheath placement at four facings, the
 // held grip through every attack clip, blade direction, and weapon-vs-body
 // penetration against Aster's baked skinned mesh. Report + contact sheets go
 // to Temp/weapon-audit (git-ignored); nothing is written to Validation/.
 public static class WeaponAudit {
  public static readonly WeaponId[] Focus={WeaponId.AstralStaff,WeaponId.GlacierMaul,WeaponId.VoidReaper,WeaponId.FrostHalberd,WeaponId.FantasyGreatsword,WeaponId.FierySword,WeaponId.OrnateCurvedBlade};
  const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static string outDir;static StringBuilder report;

  // ---------- body sampling ----------
  sealed class Body {
   readonly List<SkinnedMeshRenderer> renderers=new List<SkinnedMeshRenderer>();
   readonly List<bool[]> rightMask=new List<bool[]>(),leftMask=new List<bool[]>();
   readonly List<bool> useFullMatrix=new List<bool>();
   public Vector3[] P;public Vector3[] N;public bool[] Right,Left;
   readonly Dictionary<long,List<int>> grid=new Dictionary<long,List<int>>();
   const float Cell=.08f;readonly Mesh scratch=new Mesh();
   public Body(Hero hero){
    var anim=hero.Visual.animator;
    var rightRoot=anim.GetBoneTransform(HumanBodyBones.RightHand);var leftRoot=anim.GetBoneTransform(HumanBodyBones.LeftHand);
    foreach(var smr in hero.Visual.GetComponentsInChildren<SkinnedMeshRenderer>()){
     if(smr.GetComponentInParent<EquippedWeapon>())continue;
     var bones=smr.bones;var weights=smr.sharedMesh.boneWeights;
     var r=new bool[weights.Length];var l=new bool[weights.Length];
     var underRight=new bool[bones.Length];var underLeft=new bool[bones.Length];
     for(int b=0;b<bones.Length;b++){underRight[b]=bones[b]&&rightRoot&&bones[b].IsChildOf(rightRoot);underLeft[b]=bones[b]&&leftRoot&&bones[b].IsChildOf(leftRoot);}
     for(int v=0;v<weights.Length;v++){int b=weights[v].boneIndex0;r[v]=b<underRight.Length&&underRight[b];l[v]=b<underLeft.Length&&underLeft[b];}
     renderers.Add(smr);rightMask.Add(r);leftMask.Add(l);
     // BakeMesh scale semantics differ per version: pick the matrix whose
     // result lands on the renderer's own world bounds.
     smr.BakeMesh(scratch);var b0=scratch.bounds;
     Vector3 cFull=smr.transform.localToWorldMatrix.MultiplyPoint3x4(b0.center);
     Vector3 cRigid=Matrix4x4.TRS(smr.transform.position,smr.transform.rotation,Vector3.one).MultiplyPoint3x4(b0.center);
     useFullMatrix.Add(Vector3.Distance(cFull,smr.bounds.center)<=Vector3.Distance(cRigid,smr.bounds.center));
    }
   }
   public int Count=>P==null?0:P.Length;
   public void Bake(){
    var ps=new List<Vector3>();var ns=new List<Vector3>();var rs=new List<bool>();var ls=new List<bool>();
    for(int i=0;i<renderers.Count;i++){
     var smr=renderers[i];if(!smr||!smr.enabled||!smr.gameObject.activeInHierarchy)continue;
     smr.BakeMesh(scratch);
     var m=useFullMatrix[i]?smr.transform.localToWorldMatrix:Matrix4x4.TRS(smr.transform.position,smr.transform.rotation,Vector3.one);
     var v=scratch.vertices;var n=scratch.normals;
     for(int k=0;k<v.Length;k++){ps.Add(m.MultiplyPoint3x4(v[k]));ns.Add(m.MultiplyVector(k<n.Length?n[k]:Vector3.up).normalized);rs.Add(k<rightMask[i].Length&&rightMask[i][k]);ls.Add(k<leftMask[i].Length&&leftMask[i][k]);}
    }
    P=ps.ToArray();N=ns.ToArray();Right=rs.ToArray();Left=ls.ToArray();
    grid.Clear();
    for(int k=0;k<P.Length;k++){long key=Key(P[k]);if(!grid.TryGetValue(key,out var list))grid[key]=list=new List<int>();list.Add(k);}
   }
   static long Key(Vector3 p){return Key(Mathf.FloorToInt(p.x/Cell),Mathf.FloorToInt(p.y/Cell),Mathf.FloorToInt(p.z/Cell));}
   static long Key(int x,int y,int z){return ((long)(x&0x1FFFFF)<<42)|((long)(y&0x1FFFFF)<<21)|(long)(z&0x1FFFFF);}
   public bool Nearest(Vector3 q,out int best,out float dist){
    best=-1;dist=float.MaxValue;int cx=Mathf.FloorToInt(q.x/Cell),cy=Mathf.FloorToInt(q.y/Cell),cz=Mathf.FloorToInt(q.z/Cell);
    for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++){
     if(!grid.TryGetValue(Key(cx+x,cy+y,cz+z),out var list))continue;
     foreach(int k in list){float d=(P[k]-q).sqrMagnitude;if(d<dist){dist=d;best=k;}}
    }
    if(best<0)return false;dist=Mathf.Sqrt(dist);return dist<=Cell;
   }
  }

  // ---------- weapon sampling ----------
  sealed class WeaponSamples {
   public readonly List<Transform> Owner=new List<Transform>();public readonly List<Vector3> Local=new List<Vector3>();
   public WeaponSamples(GameObject model){
    var rng=new System.Random(7);
    foreach(var mf in model.GetComponentsInChildren<MeshFilter>()){
     var mesh=mf.sharedMesh;if(!mesh)continue;
     if(mesh.isReadable){
      var v=mesh.vertices;var t=mesh.triangles;
      int stride=Mathf.Max(1,v.Length/1500);for(int i=0;i<v.Length;i+=stride){Owner.Add(mf.transform);Local.Add(v[i]);}
      var cum=new float[t.Length/3];float total=0;
      for(int i=0;i<cum.Length;i++){total+=Vector3.Cross(v[t[i*3+1]]-v[t[i*3]],v[t[i*3+2]]-v[t[i*3]]).magnitude;cum[i]=total;}
      for(int s=0;s<1500&&total>0;s++){
       float pick=(float)rng.NextDouble()*total;int lo=0,hi=cum.Length-1;while(lo<hi){int mid=(lo+hi)/2;if(cum[mid]<pick)lo=mid+1;else hi=mid;}
       float a=(float)rng.NextDouble(),b=(float)rng.NextDouble();if(a+b>1){a=1-a;b=1-b;}
       Vector3 p0=v[t[lo*3]],p1=v[t[lo*3+1]],p2=v[t[lo*3+2]];Owner.Add(mf.transform);Local.Add(p0+(p1-p0)*a+(p2-p0)*b);
      }
     }else if(model.name.IndexOf("Verdant",StringComparison.OrdinalIgnoreCase)>=0||mf.name.IndexOf("Verdant",StringComparison.OrdinalIgnoreCase)>=0){
      var rot=Quaternion.LookRotation(new Vector3(-0.04481f,0.46121f,0.88616f).normalized,new Vector3(-0.48027f,0.76787f,-0.42393f).normalized);
      for(int s=0;s<300;s++){
       float x=.0510f+(float)rng.NextDouble()*.3037f,y=-.2319f+(float)rng.NextDouble()*1.2203f,z=.1776f+(float)rng.NextDouble()*.1013f;
       Owner.Add(mf.transform);Local.Add(rot*new Vector3(x,y,z));
      }
     }else{
      var b=mesh.bounds;Vector3 min=b.min,max=b.max;
      for(int s=0;s<300;s++){
       float fx=(float)rng.NextDouble(),fy=(float)rng.NextDouble(),fz=(float)rng.NextDouble();
       Owner.Add(mf.transform);Local.Add(new Vector3(Mathf.Lerp(min.x,max.x,fx),Mathf.Lerp(min.y,max.y,fy),Mathf.Lerp(min.z,max.z,fz)));
      }
     }
    }
    if(Local.Count==0){Owner.Add(model.transform);Local.Add(Vector3.zero);}
   }
   public Vector3 World(int i)=>Owner[i].TransformPoint(Local[i]);
   public int Count=>Local.Count;
  }

  struct Contact {public int Inside,LeftHand,RightHand;public float Depth,Gap;public float Fraction(int total)=>total>0?(float)Inside/total:0;}
  // Weapon samples that sit behind the nearest body surface (signed distance
  // along the vertex normal). The gripping hand is reported separately.
  static Contact Measure(Body body,WeaponSamples w,float tolerance=.015f){
   var c=new Contact{Gap=float.MaxValue};
   for(int i=0;i<w.Count;i++){
    Vector3 p=w.World(i);
    if(!body.Nearest(p,out int k,out float d))continue;
    c.Gap=Mathf.Min(c.Gap,d);
    float signed=Vector3.Dot(p-body.P[k],body.N[k]);
    if(signed<-tolerance){
     if(body.Right[k]){c.RightHand++;continue;}
     if(body.Left[k]){c.LeftHand++;continue;}
     c.Inside++;c.Depth=Mathf.Max(c.Depth,-signed);
    }
   }
   return c;
  }
  // Exact minimum gap (brute force on a thinned set): used for floating checks.
  static float MinGap(Body body,WeaponSamples w){
   float best=float.MaxValue;
   for(int i=0;i<w.Count;i+=4){Vector3 p=w.World(i);for(int k=0;k<body.Count;k+=3){float d=(body.P[k]-p).sqrMagnitude;if(d<best)best=d;}}
   return Mathf.Sqrt(best);
  }

  // ---------- capture ----------
  sealed class Sheet {
   public readonly Texture2D Image;readonly int cols,cell;int next;public readonly List<string> Legend=new List<string>();
   public Sheet(int cols,int rows,int cell){this.cols=cols;this.cell=cell;Image=new Texture2D(cols*cell,rows*cell,TextureFormat.RGB24,false);var fill=new Color32[cols*rows*cell*cell];for(int i=0;i<fill.Length;i++)fill[i]=new Color32(30,33,40,255);Image.SetPixels32(fill);}
   public void Shot(Hero hero,Vector3 cameraOffsetLocal,float size,string label){
    int rows=Image.height/cell;int col=next%cols,row=next/cols;if(row>=rows)return;
    Legend.Add("["+row+","+col+"] "+label);
    var t=hero.transform;Vector3 center=t.position+Vector3.up*1f;
    var renderers=hero.GetComponentsInChildren<Renderer>();var layers=new int[renderers.Length];
    for(int i=0;i<renderers.Length;i++){layers[i]=renderers[i].gameObject.layer;renderers[i].gameObject.layer=30;}
    var go=new GameObject("Audit camera");var cam=go.AddComponent<Camera>();cam.cullingMask=1<<30;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.12f,.13f,.16f);cam.orthographic=true;cam.orthographicSize=size;cam.nearClipPlane=.05f;cam.farClipPlane=30;
    cam.transform.position=center+t.TransformDirection(cameraOffsetLocal);cam.transform.LookAt(center);
    var lamp=new GameObject("Audit light");var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.6f;light.cullingMask=1<<30;lamp.transform.rotation=Quaternion.LookRotation(cam.transform.forward+Vector3.down*.4f);
    var rt=RenderTexture.GetTemporary(cell,cell,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
    Image.ReadPixels(new Rect(0,0,cell,cell),col*cell,(rows-1-row)*cell);
    RenderTexture.active=old;cam.targetTexture=null;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(lamp);
    for(int i=0;i<renderers.Length;i++)if(renderers[i])renderers[i].gameObject.layer=layers[i];
    next++;
   }
   public void Save(string name){Image.Apply();File.WriteAllBytes(Path.Combine(outDir,name+".png"),Image.EncodeToPNG());File.WriteAllLines(Path.Combine(outDir,name+".legend.txt"),Legend);UnityEngine.Object.Destroy(Image);}
  }
  static readonly Vector3 BackView=new Vector3(1.3f,.35f,-4f),SideView=new Vector3(4f,.2f,0),FrontSide=new Vector3(3.2f,.6f,2.6f),TopView=new Vector3(.01f,5f,0);

  // ---------- helpers ----------
  static void Log(string line){report.AppendLine(line);Debug.Log("WEAPON_AUDIT "+line);}
  static string V(Vector3 v)=>v.ToString("F2");
  static IEnumerator Frames(int n){for(int i=0;i<n;i++)yield return null;}
  static void SetDraw(EquippedWeapon eq,float draw,float until){typeof(EquippedWeapon).GetField("draw",Private).SetValue(eq,draw);typeof(EquippedWeapon).GetField("drawnUntil",Private).SetValue(eq,until);}
  static Transform Facing(Hero hero)=>hero.Visual.transform;
  static Vector3 Grip(EquippedWeapon eq)=>eq.transform.position;
  // Farthest weapon point from the grip = striking end (tip/head).
  static void Ends(EquippedWeapon eq,WeaponSamples w,out Vector3 tip,out float tipLen,out float pommelLen,out float foreArea){
   Vector3 g=Grip(eq);tip=g;tipLen=0;
   for(int i=0;i<w.Count;i++){Vector3 p=w.World(i);float d=(p-g).magnitude;if(d>tipLen){tipLen=d;tip=p;}}
   Vector3 axis=(tip-g).normalized;pommelLen=0;int fore=0;
   for(int i=0;i<w.Count;i++){float s=Vector3.Dot(w.World(i)-g,axis);if(s<0)pommelLen=Mathf.Max(pommelLen,-s);else fore++;}
   foreArea=w.Count>0?(float)fore/w.Count:0;
  }
  static float OldSheathBlend(EquippedWeapon eq,Hero hero,out Vector3 pos,out Quaternion rot){
   // Exact pre-d3019f1 sheath formula, for regression comparison only.
   var renderer=eq.GetComponentInChildren<Renderer>();var size=renderer.localBounds.size;
   Vector3 bladeDir=(Vector3)typeof(EquippedWeapon).GetField("bladeDir",Private).GetValue(eq);
   Vector3 center=(Vector3)typeof(EquippedWeapon).GetField("center",Private).GetValue(eq);
   Vector3 longAxis=size.x>=size.y&&size.x>=size.z?Vector3.right:size.y>=size.x&&size.y>=size.z?Vector3.up:Vector3.forward;
   bladeDir=eq.transform.InverseTransformDirection(renderer.transform.TransformDirection(longAxis));
   Vector3 thin=size.x<=size.y&&size.x<=size.z?Vector3.right:size.y<=size.z?Vector3.up:Vector3.forward;
   bool disc=eq.Id==WeaponId.MoonChakram;
   Vector3 flatDir=eq.transform.InverseTransformDirection(renderer.transform.TransformDirection(disc?thin:Vector3.forward));
   var t=hero.transform;var chest=hero.Visual.animator.GetBoneTransform(HumanBodyBones.Chest);
   Vector3 socket=chest?chest.position:t.position+Vector3.up*1.3f;socket+=t.forward*-.15f+t.up*.04f;
   rot=Quaternion.FromToRotation(bladeDir,t.up);if(Vector3.Dot(rot*flatDir,t.forward)>=0)rot=Quaternion.AngleAxis(180f,t.up)*rot;
   if(disc){rot=Quaternion.LookRotation(-t.forward,t.up)*Quaternion.Inverse(Quaternion.LookRotation(flatDir,bladeDir));socket=t.position+t.up*1.2f-t.forward*.23f;}
   pos=socket-rot*(disc?Vector3.Scale(center,eq.transform.lossyScale):center);return 0;
  }

  public static IEnumerator Run(Action<bool,string> check){
   outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"..","Temp","weapon-audit"));Directory.CreateDirectory(outDir);
   report=new StringBuilder();
   var g=RealmGame.I;g.LoadLevel(1);yield return null;
   int oldCapture=Time.captureFramerate;Time.captureFramerate=60;
   foreach(var e in g.Enemies.ToArray())if(e)UnityEngine.Object.Destroy(e.gameObject);g.Enemies.Clear();yield return null;
   var hero=g.Player;hero.Warp(new Vector3(100,30,100));hero.enabled=false;hero.transform.rotation=Quaternion.identity;
   yield return Frames(3);
   var body=new Body(hero);body.Bake();
   Log("Aster body samples="+body.Count+" visualScale="+V(hero.Visual.transform.lossyScale)+" skin="+g.Save.equippedSkin);
   bool onlyFocus=Array.IndexOf(Environment.GetCommandLineArgs(),"-weaponAuditFocus")>=0;
   var ids=new List<WeaponId>();for(int id=0;id<=WeaponCatalog.MaxId;id++)if(!onlyFocus||Array.IndexOf(Focus,(WeaponId)id)>=0)ids.Add((WeaponId)id);
   var sheathSheet=new Sheet(8,(ids.Count*2+7)/8,220);var heldSheet=new Sheet(8,(ids.Count+7)/8,220);
   foreach(var id in ids){
    bool focus=Array.IndexOf(Focus,id)>=0;var def=WeaponCatalog.Get(id);string style=WeaponCatalog.AttackStyle(def);
    g.EquipWeapon(id);hero.transform.rotation=Quaternion.identity;hero.Visual.Play("idle");yield return Frames(30);
    var eq=hero.GetComponentInChildren<EquippedWeapon>();var model=eq.GetComponentInChildren<MeshRenderer>().gameObject;var w=new WeaponSamples(eq.transform.GetChild(0).gameObject);
    Log("=== "+(int)id+" "+id+" style="+style+" scale="+def.ModelScale+" reach="+def.Reach+" euler="+V(def.EquipEuler)+" samples="+w.Count+(focus?" [FOCUS]":""));
    // Weapon must never carry physics into the hero hierarchy.
    int colliders=eq.GetComponentsInChildren<Collider>(true).Length,bodies=eq.GetComponentsInChildren<Rigidbody>(true).Length;
    Log(" physics colliders="+colliders+" rigidbodies="+bodies);check(colliders==0&&bodies==0,id+" equipped weapon has no colliders or rigidbodies");
    // ----- sheathed, 4 facings -----
    Vector3 rest=Vector3.zero;Quaternion restRot=Quaternion.identity;
    for(int i=0;i<4;i++){
     hero.transform.rotation=Quaternion.Euler(0,90*i,0);yield return Frames(2);body.Bake();
     var f=Facing(hero);Vector3 c=f.InverseTransformPoint(eq.transform.position);Quaternion r=Quaternion.Inverse(f.rotation)*eq.transform.rotation;
     if(i==0){rest=c;restRot=r;}
     var contact=Measure(body,w);
     Bounds lb=new Bounds(f.InverseTransformPoint(w.World(0)),Vector3.zero);for(int k=0;k<w.Count;k+=3)lb.Encapsulate(f.InverseTransformPoint(w.World(k)));
     float gap=i==0?MinGap(body,w):-1;
     Log(" sheath yaw="+(90*i)+" localPos="+V(c)+" localBounds min="+V(lb.min)+" max="+V(lb.max)+" inside="+contact.Inside+"/"+w.Count+" ("+(contact.Fraction(w.Count)*100).ToString("F1")+"%) depth="+contact.Depth.ToString("F3")+(i==0?" minGap="+gap.ToString("F3"):""));
     check(Vector3.Distance(c,rest)<.01f&&Quaternion.Angle(r,restRot)<1f,id+" sheath follows facing at yaw "+90*i);
     check(lb.center.z<-.05f,id+" sheath sits behind Aster at yaw "+90*i);
     if(focus||true)check(contact.Fraction(w.Count)<.02f&&contact.Depth<.05f,id+" sheathed mesh does not pass through Aster at yaw "+90*i+" ("+contact.Inside+" pts, depth "+contact.Depth.ToString("F3")+")");
     if(i==0){check(gap<.12f,id+" sheath rests close to the back (gap "+gap.ToString("F3")+")");sheathSheet.Shot(hero,BackView,1.3f,id+" sheath back");sheathSheet.Shot(hero,SideView,1.3f,id+" sheath side");}
    }
    hero.transform.rotation=Quaternion.identity;yield return Frames(2);
    if(id<=WeaponId.SageStaff||true){
     // Regression reference: the pre-commit chest-bone sheath at the same pose.
     body.Bake();var savedPos=eq.transform.position;var savedRot=eq.transform.rotation;
     OldSheathBlend(eq,hero,out var op,out var or);eq.transform.SetPositionAndRotation(op,or);
     var oc=Measure(body,w);float ogap=MinGap(body,w);
     Log(" oldSheath inside="+oc.Inside+" ("+(oc.Fraction(w.Count)*100).ToString("F1")+"%) depth="+oc.Depth.ToString("F3")+" minGap="+ogap.ToString("F3"));
     eq.transform.SetPositionAndRotation(savedPos,savedRot);
    }
    // ----- sheathed during locomotion (info) -----
    foreach(string loco in new[]{"walk","run","jump","roll"}){
     hero.Visual.Play(loco);int worst=0;float depth=0;
     for(int frame=0;frame<36;frame++){yield return null;if(frame%4!=0)continue;body.Bake();var lc=Measure(body,w);if(lc.Inside>worst)worst=lc.Inside;depth=Mathf.Max(depth,lc.Depth);}
     Log(" sheath "+loco+" worstInside="+worst+" ("+(100f*worst/w.Count).ToString("F1")+"%) depth="+depth.ToString("F3"));
    }
    hero.Visual.Play("idle");yield return Frames(10);
    // ----- held through each attack -----
    float tempo=def.Tempo/(style=="unarmed"||id==WeaponId.RiftDagger?.85f:1f);
    bool shot=false;
    foreach(int combo in new[]{1,2,3,0}){
     bool charged=combo==0;string state=charged?"charged":"attack_"+combo;
     float baseSpeed=charged?2.55f:combo==2?3.15f:combo==3?2.9f:3.05f;
     float duration=hero.Visual.ClipLength(state,style)/(baseSpeed*Mathf.Max(.4f,tempo));
     SetDraw(eq,1f,Time.time+5f);hero.Visual.PlayAttack(charged?1:combo,charged,tempo,style);
     int frames=Mathf.Max(4,Mathf.CeilToInt(duration*60f));
     float worstFrac=0,worstDepth=0,worstT=0,maxGrip=0,maxReach=0,peakSpeed=0,peakT=0,peakZ=0,peakDot=0,tipLen=0,pommel=0,fore=0;int worstLeft=0;Vector3 lastTip=Vector3.zero;
     for(int frame=0;frame<=frames;frame++){
      yield return null;body.Bake();var f=Facing(hero);
      var hc=Measure(body,w);float frac=hc.Fraction(w.Count);if(frac>worstFrac){worstFrac=frac;worstT=frame/(float)frames;}worstDepth=Mathf.Max(worstDepth,hc.Depth);worstLeft=Mathf.Max(worstLeft,hc.LeftHand);
      if(EquippedWeapon.HasGripAnchor(id)){
       var mesh=eq.GetComponentInChildren<MeshRenderer>();var hand=hero.Visual.animator.GetBoneTransform(HumanBodyBones.RightHand);var knuckle=hero.Visual.animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
       Vector3 palm=knuckle?Vector3.Lerp(hand.position,knuckle.position,.65f):hand.position;
       maxGrip=Mathf.Max(maxGrip,Vector3.Distance(mesh.transform.TransformPoint(EquippedWeapon.GripAnchorPoint(id,mesh.GetComponent<MeshFilter>().sharedMesh.bounds)),palm));
      }
      Ends(eq,w,out var tip,out tipLen,out pommel,out fore);
      Vector3 tipLocal=f.InverseTransformPoint(tip),gripLocal=f.InverseTransformPoint(Grip(eq));
      maxReach=Mathf.Max(maxReach,new Vector2(tipLocal.x,tipLocal.z).magnitude*(tipLocal.z>0?1:0));
      if(frame>0){float speed=(tip-lastTip).magnitude*60f;if(speed>peakSpeed){peakSpeed=speed;peakT=frame/(float)frames;peakZ=tipLocal.z;peakDot=Vector3.Dot((tipLocal-gripLocal).normalized,Vector3.forward);}}
      lastTip=tip;
      if(frame==frames/2&&!shot){heldSheet.Shot(hero,FrontSide,1.5f,id+" "+state+" mid");shot=true;}
     }
     Log(" held "+state+" dur="+duration.ToString("F2")+"s insideWorst="+(worstFrac*100).ToString("F1")+"%@t"+worstT.ToString("F2")+" depth="+worstDepth.ToString("F3")+" leftHandPts="+worstLeft+" gripErr="+maxGrip.ToString("F3")+" tipLen="+tipLen.ToString("F2")+" pommel="+pommel.ToString("F2")+" foreArea="+fore.ToString("F2")+" peakTipSpeed="+peakSpeed.ToString("F1")+"@t"+peakT.ToString("F2")+" tipZ@peak="+peakZ.ToString("F2")+" bladeFwd@peak="+peakDot.ToString("F2")+" visibleReach="+maxReach.ToString("F2")+" gameReach="+((charged?3.4f:2.7f)*def.Reach).ToString("F2"));
     if(EquippedWeapon.HasGripAnchor(id))check(maxGrip<.02f,id+" "+state+" handle stays in the palm");
     check(eq.transform.parent==hero.Visual.transform,id+" weapon stays parented to Aster through "+state);
     hero.Visual.Play("idle");yield return Frames(8);
    }
    SetDraw(eq,0f,0f);yield return Frames(30);
   }
   sheathSheet.Save("sheath-sheet");heldSheet.Save("held-sheet");
   File.WriteAllText(Path.Combine(outDir,"weapon-audit.txt"),report.ToString());
   Time.captureFramerate=oldCapture;hero.enabled=true;
  }
 }
}
