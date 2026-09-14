using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Verifies the new rigged/anim families end-to-end the way they run in-game:
 // model prefab, per-state clips, clip bindings resolving against the model's
 // instantiated hierarchy, and a rendered silhouette ratio vs a known-good
 // rigged character. Runs foreground-only (needs a real camera); UnityEditor
 // exits via EditorApplication.Exit(0).
 public static class EnemyVisualProbe {
  static readonly string[] States={"idle","walk","attack","death"};
  [MenuItem("Lost Realms/Diag/EnemyVisual")]
  public static void Diagnose(){
   camInit();
   var sb=new StringBuilder();
   foreach(var role in new[]{"Skeleton","BriarGoblin","EmberDemon","DogKnight","Spider"}){
    sb.AppendLine("=="+role+"==");
    try{
     var holder=new GameObject("probe "+role);
     float height=role=="Spider"?0.6f:1.4f;
     var v=CharacterVisual.Create(role,holder.transform,height,Color.white);
     sb.AppendLine("animated="+(v.animator!=null)+" fallback="+v.UsesFallback);
     if(role=="Spider"){
      int found=0;string miss="";
      foreach(var n in new[]{"Box09","Box10","Box11","Box20","Box18","Box19","Box25","Box22","Box23","Box26","Box21","Box24","Box31","Box28","Box29","Box35","Box32","Box36","Box37","Box33","Box34","Box38","Box27","Box30","Dummy02"}){
       if(SpiderBone(v.transform,n))found++;else miss+=" "+n;
      }
      sb.AppendLine("spiderBones="+found+"/25 miss="+miss);
     }
     sb.AppendLine("bones="+CountBones(v.transform)+" smr="+v.transform.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
     // the runtime model is the visual's first child gameObject; its first child
     // transform is the pack rig root ("root"/"Box01"), and the Animator sits on
     // the model gameObject root — clip paths resolve from there.
     Transform model=v.transform.childCount>0?v.transform.GetChild(0):null;
     for(int i=0;i<States.Length;i++){
      var clip=Resources.Load<AnimationClip>("Animations/"+role+"_"+States[i]);
      if(!clip){sb.AppendLine("  "+States[i]+": MISSING");continue;}
      sb.AppendLine("  "+States[i]+": "+(clip.length.ToString("0.000")+"s loop="+clip.isLooping)+" "+BindCheck(clip,model)+" "+MotionCheck(clip));
     }
     if(v.animator)v.Play("idle");
     cam.Render();
     sb.AppendLine(role+" "+Measure(rt));
     Object.DestroyImmediate(holder);
    }catch(System.Exception e){sb.AppendLine(role+" ERR "+e.Message);}
   }
   // reference for ratio
   {
    var holder=new GameObject("probe briar");
    var v=CharacterVisual.Create("BriarGoblin",holder.transform,1.35f,Color.white);
    cam.Render();
    sb.AppendLine("BriarGoblin "+Measure(rt));
    Object.DestroyImmediate(holder);
   }
   Object.DestroyImmediate(camGoDestro);Object.DestroyImmediate(rt);
   System.IO.File.WriteAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation/enemy-visual.txt")),sb.ToString());
   Debug.Log("ENEMY_VISUAL_DONE");
   EditorApplication.Exit(0);
  }
  // Reports how many distinct transform paths a clip animates and whether those
  // paths resolve against the live model (the way the PlayableGraph drives it).
  static string BindCheck(AnimationClip clip,Transform model){
   if(!model)return "no model";
   var bindings=AnimationUtility.GetCurveBindings(clip);
   int missing=0,resolved=0;
   var seen=new System.Collections.Generic.HashSet<string>();
   var samples=new System.Collections.Generic.List<string>();
   foreach(var b in bindings){
    if(seen.Add(b.path)){
     if(samples.Count<8)samples.Add(b.path);
     if(FindPath(model,b.path)!=null)resolved++;
     else missing++;
    }
   }
   return "paths="+seen.Count+" resolved="+resolved+" missing="+missing+" ex="+string.Join(",",samples);
  }
  // Samples every curve at t=0 vs mid-clip and reports the largest bone motion:
  // proves the clip is not a silent static pose and shows an amplitude hint.
  static string MotionCheck(AnimationClip clip){
   float maxPos=0f,maxRot=0f;int curved=0;
   foreach(var b in AnimationUtility.GetCurveBindings(clip)){
    var curve=AnimationUtility.GetEditorCurve(clip,b.path,b.type,b.propertyName);
    if(curve==null||curve.length==0)continue;
    curved++;
    float t1=Mathf.Min(clip.length*.5f,curve.keys[curve.length-1].time);
    float d=Mathf.Abs(curve.Evaluate(t1)-curve.Evaluate(0f));
    if(b.propertyName.IndexOf("Position",System.StringComparison.Ordinal)>=0)maxPos=Mathf.Max(maxPos,d);
    else maxRot=Mathf.Max(maxRot,d);
   }
   return "curved="+curved+" maxPos="+maxPos.ToString("0.000")+" maxRot="+maxRot.ToString("0.0");
  }
  static int CountBones(Transform root){return root?root.GetComponentsInChildren<Transform>(true).Length:0;}
  static Transform FindPath(Transform root,string path){
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
  static bool SpiderBone(Transform root,string n){
   if(!root)return false;if(root.name==n)return true;
   for(int i=0;i<root.childCount;i++)if(SpiderBone(root.GetChild(i),n))return true;
   return false;
  }
  static RenderTexture rt;static Camera cam;static GameObject camGoDestro;
  static void camInit(){
   rt=new RenderTexture(256,256,24);
   var camGo=new GameObject("diagcam");
   camGo.transform.SetPositionAndRotation(new Vector3(0,2.4f,6),Quaternion.LookRotation(Vector3.back));
   cam=camGo.AddComponent<Camera>();
   cam.orthographic=true;cam.orthographicSize=4.5f;
   cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
   cam.targetTexture=rt;
   camGoDestro=camGo;
  }
  static string Measure(RenderTexture rt){
   RenderTexture.active=rt;
   var tex=new Texture2D(256,256,TextureFormat.RGB24,false);
   tex.ReadPixels(new Rect(0,0,256,256),0,0);tex.Apply();
   RenderTexture.active=null;
   int lit=0,minX=256,maxX=-1,minY=256,maxY=-1;
   var px=tex.GetPixels();
   for(int y=0;y<256;y++)for(int x=0;x<256;x++){
    var c=px[y*256+x];
    if(c.r+c.g+c.b>0.05f){
     lit++;if(x<minX)minX=x;if(x>maxX)maxX=x;if(y<minY)minY=y;if(y>maxY)maxY=y;
    }
   }
   Object.DestroyImmediate(tex);
   int w=maxX-minX+1,h=maxY-minY+1;
   return "lit="+lit+" box="+w+"x"+h+" ratio="+(w>0?((float)h/w).ToString("0.00"):"0");
  }
 }
}