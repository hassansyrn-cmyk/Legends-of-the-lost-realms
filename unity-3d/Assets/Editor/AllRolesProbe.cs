using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Edit-mode diagnosis for the whole enemy cast + pickup models. Runs from the
 // open editor (no play mode, no exit): Lost Realms/Diag/All Roles + Pickups.
 // Writes unity-3d/Validation/all-roles.txt — paste it back into chat.
 public static class AllRolesProbe {
  [MenuItem("Lost Realms/Diag/All Roles + Pickups")]
  public static void Diagnose(){
   var sb=new StringBuilder();
   sb.AppendLine("ALL-ROLES DIAGNOSIS "+System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
   string[] roles={"Goblin","Demon","Frost","Elemental","Caster","Flyer","Bomber","Summoner","Elite","Skeleton","BriarGoblin","EmberDemon","Spider","Footman","DogKnight","Heartwood","Sunscar","Whiteout","LavaBoss"};
   foreach(var role in roles){
    sb.AppendLine("=="+role+"==");
    GameObject holder=null;
    try{
     holder=new GameObject("probe "+role);
     holder.transform.position=new Vector3(5,1,60);
     float h=role=="Spider"?0.6f:(role=="Heartwood"||role=="Sunscar"||role=="Whiteout"||role=="LavaBoss")?3.6f:1.65f;
     var v=CharacterVisual.Create(role,holder.transform,h,Color.white);
     var renderers=v.transform.GetComponentsInChildren<Renderer>(true);
     sb.AppendLine("renderers="+renderers.Length+" smr="+v.transform.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length+" animator="+(v.animator!=null)+" fallback="+v.UsesFallback);
     if(renderers.Length>0){
      Bounds b=renderers[0].bounds;
      for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);
      sb.AppendLine("size="+b.size.ToString("0.00")+" center="+b.center.ToString("0.00")+" dist="+Vector3.Distance(b.center,holder.transform.position).ToString("0.00"));
      if(b.size.magnitude<0.05f)sb.AppendLine("WARN: near-zero bounds (likely invisible)");
      if(Vector3.Distance(b.center,holder.transform.position)>3f)sb.AppendLine("WARN: visual displaced from root (flying-model bug)");
     }else sb.AppendLine("WARN: no renderers at all");
     Transform model=v.transform.childCount>0?v.transform.GetChild(0):null;
     foreach(var st in new[]{"idle","walk","attack","death"}){
      var clip=Resources.Load<AnimationClip>("Animations/"+role+"/"+st)??Resources.Load<AnimationClip>("Animations/"+role+"_"+st)??Resources.Load<AnimationClip>("Animations/Shared/"+st);
      if(!clip){sb.AppendLine("  "+st+": MISSING (no role clip, no shared fallback)");continue;}
      string src=(Resources.Load<AnimationClip>("Animations/"+role+"/"+st)!=null||Resources.Load<AnimationClip>("Animations/"+role+"_"+st)!=null)?"role":"shared";
      var bindings=AnimationUtility.GetCurveBindings(clip);
      var seen=new HashSet<string>();
      int resolved=0,missing=0,curved=0;float maxRot=0f;
      foreach(var bnd in bindings){
       var curve=AnimationUtility.GetEditorCurve(clip,bnd.path,bnd.type,bnd.propertyName);
       if(curve==null||curve.length==0)continue;
       curved++;
       float t1=Mathf.Min(clip.length*.5f,curve.keys[curve.length-1].time);
       float d=Mathf.Abs(curve.Evaluate(t1)-curve.Evaluate(0f));
       if(bnd.propertyName.IndexOf("Position",System.StringComparison.Ordinal)<0)maxRot=Mathf.Max(maxRot,d);
       if(seen.Add(bnd.path)){if(FindPath(model,bnd.path)!=null)resolved++;else missing++;}
      }
      sb.AppendLine("  "+st+" ["+src+"]: len="+clip.length.ToString("0.00")+"s paths="+seen.Count+" resolved="+resolved+" missing="+missing+" maxRot="+maxRot.ToString("0.0"));
      if(curved>0&&maxRot<0.5f)sb.AppendLine("  WARN: clip is nearly static (T-pose suspect)");
     }
    }catch(System.Exception e){sb.AppendLine(role+" ERR "+e.Message);}
    finally{if(holder)Object.DestroyImmediate(holder);}
   }
   // Pickup models anchored far down-route (the old bug parked them at origin).
   sb.AppendLine("==Pickups @ (5,0.8,60)==");
   foreach(var name in new[]{"Coin","5SideDiamond","Heart"}){
    GameObject root=null;
    try{
     root=new GameObject("probe "+name);
     root.transform.position=new Vector3(5,.8f,60);
     GameObject model=null;
     if(name=="Heart")Art.PickupModel(name,root.transform,new Color(.92f,.12f,.16f),.55f,null);
     else if(name=="Coin")Art.CoinMesh(root.transform);
     else RelicArt.Gem(root.transform,new Color(1f,.35f,.28f));
     foreach(Transform c in root.transform){if(c.gameObject.name==name||c.gameObject.name=="5SideDiamond"){model=c.gameObject;break;}}
     if(model)sb.AppendLine(name+": childLocal="+model.transform.localPosition.ToString("0.00")+" childScale="+model.transform.localScale.ToString("0.00"));
     var rs=root.GetComponentsInChildren<Renderer>(true);
     if(rs.Length>0){
      Bounds b=rs[0].bounds;
      for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
      sb.AppendLine(name+": renderers="+rs.Length+" size="+b.size.ToString("0.00")+" dist="+Vector3.Distance(b.center,root.transform.position).ToString("0.00"));
     }else sb.AppendLine(name+": WARN no renderers");
     var gem=root.GetComponent<GemVisual>();
     if(gem&&gem.Core)sb.AppendLine(name+": gemCoreScale="+gem.Core.localScale.ToString("0.00"));
    }catch(System.Exception e){sb.AppendLine(name+" ERR "+e.Message);}
    finally{if(root)Object.DestroyImmediate(root);}
   }
   string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation/all-roles.txt"));
   System.IO.File.WriteAllText(path,sb.ToString());
   Debug.Log("ALL_ROLES_DONE -> "+path);
  }
  [MenuItem("Lost Realms/Diag/Deep Dive")]
  public static void DeepDive(){
   var sb=new StringBuilder();
   sb.AppendLine("DEEP DIVE "+System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
   foreach(var fbx in new[]{"Assets/Resources/Characters/Goblin.fbx","Assets/Resources/Characters/Flyer.fbx","Assets/Resources/Characters/Bomber.fbx"}){
    sb.AppendLine("=="+fbx+"==");
    try{
     var imp=AssetImporter.GetAtPath(fbx) as ModelImporter;
     if(imp==null){sb.AppendLine("no importer");continue;}
     sb.AppendLine("animType="+imp.animationType+" importAnim="+imp.importAnimation);
     var takes=imp.defaultClipAnimations;
     if(takes==null)sb.AppendLine("takes=null");
     else if(takes.Length==0)sb.AppendLine("takes=0");
     else foreach(var c in takes)sb.AppendLine("take="+c.name+" loop="+c.loopTime);
    }catch(System.Exception e){sb.AppendLine("ERR "+e.Message);}
   }
   foreach(var role in new[]{"Goblin","Footman","DogKnight"}){
    foreach(var st in new[]{"idle","walk","attack"}){
     var clip=Resources.Load<AnimationClip>("Animations/"+role+"_"+st);
     if(!clip){sb.AppendLine(role+"_"+st+" MISSING");continue;}
     float maxP=0f,maxR=0f;int cp=0,cr=0;string ex="";
     foreach(var b in AnimationUtility.GetCurveBindings(clip)){
      var curve=AnimationUtility.GetEditorCurve(clip,b.path,b.type,b.propertyName);
      if(curve==null||curve.length==0)continue;
      float t1=Mathf.Min(clip.length*.5f,curve.keys[curve.length-1].time);
      float d=Mathf.Abs(curve.Evaluate(t1)-curve.Evaluate(0f));
      if(b.propertyName.IndexOf("Position",System.StringComparison.Ordinal)>=0){maxP=Mathf.Max(maxP,d);cp++;}
      else{maxR=Mathf.Max(maxR,d);cr++;if(ex.Length<160)ex+=" "+b.path+"="+b.propertyName;}
     }
     sb.AppendLine(role+"_"+st+": len="+clip.length.ToString("0.00")+" posCurves="+cp+" maxPos="+maxP.ToString("0.000")+" rotCurves="+cr+" maxRot="+maxR.ToString("0.00")+" ex="+ex);
    }
   }
   foreach(var role in new[]{"Flyer","Bomber"}){
    GameObject holder=null;
    try{
     sb.AppendLine("==tree "+role+"==");
     holder=new GameObject("deep "+role);
     var v=CharacterVisual.Create(role,holder.transform,1.65f,Color.white);
     Dump(holder.transform,0,sb);
    }catch(System.Exception e){sb.AppendLine(role+" ERR "+e.Message);}
    finally{if(holder)Object.DestroyImmediate(holder);}
   }
   string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation/deep-dive.txt"));
   System.IO.File.WriteAllText(path,sb.ToString());
   Debug.Log("DEEP_DIVE_DONE -> "+path);
  }
  [MenuItem("Lost Realms/Diag/Source Check")]
  public static void SourceCheck(){
   var sb=new StringBuilder();
   sb.AppendLine("SOURCE CHECK "+System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
   var go=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/TempSrc/model_11.fbx");
   if(!go)sb.AppendLine("model_11 NOT IMPORTED YET");
   else{
    var mfs=go.GetComponentsInChildren<MeshFilter>(true);
    var smrs=go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
    sb.AppendLine("meshFilters="+mfs.Length+" skinned="+smrs.Length);
    foreach(var mf in mfs){
     var m=mf.sharedMesh;
     if(!m){sb.AppendLine("MF null mesh under "+FullPath(mf.transform));continue;}
     m.RecalculateBounds();
     sb.AppendLine("MF "+FullPath(mf.transform)+": verts="+m.vertexCount+" tris="+(m.triangles.Length/3)+" bounds="+m.bounds.size.ToString("0.00")+" c="+m.bounds.center.ToString("0.00"));
    }
    foreach(var sm in smrs){
     var m=sm.sharedMesh;
     if(!m){sb.AppendLine("SMR null mesh under "+FullPath(sm.transform));continue;}
     sb.AppendLine("SMR "+FullPath(sm.transform)+": verts="+m.vertexCount+" tris="+(m.triangles.Length/3)+" bones="+(sm.bones!=null?sm.bones.Length:0));
    }
   }
   string path=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation/source-check.txt"));
   System.IO.File.WriteAllText(path,sb.ToString());
   Debug.Log("SOURCE_CHECK_DONE -> "+path);
  }
  static string FullPath(Transform t){string p=t.name;while(t.parent){t=t.parent;p=t.name+"/"+p;}return p;}
  static void Dump(Transform t,int d,StringBuilder sb){
   string extra="";
   var mf=t.GetComponent<MeshFilter>();if(mf)extra+=" mesh="+(mf.sharedMesh?("v"+mf.sharedMesh.vertexCount):"null");
   var smr=t.GetComponent<SkinnedMeshRenderer>();if(smr)extra+=" skin="+(smr.sharedMesh?("v"+smr.sharedMesh.vertexCount):"null")+(" bones="+(smr.bones!=null?smr.bones.Length:0));
   var mr=t.GetComponent<Renderer>();if(mr)extra+=" mat="+(mr.sharedMaterial?(mr.sharedMaterial.name+"/"+mr.sharedMaterial.shader.name):"null");
   sb.AppendLine(new string(' ',d*2)+t.name+" lp="+t.localPosition.ToString("0.00")+" ls="+t.localScale.ToString("0.00")+extra);
   if(d<7)foreach(Transform c in t)Dump(c,d+1,sb);
  }
  static Transform FindPath(Transform root,string path){
   if(root==null)return null;
   if(string.IsNullOrEmpty(path))return root;
   var cur=root;
   foreach(var token in path.Split('/')){
    Transform child=null;
    foreach(Transform t in cur){if(t.name==token){child=t;break;}}
    if(!child)return null;
    cur=child;
   }
   return cur;
  }
 }
}
