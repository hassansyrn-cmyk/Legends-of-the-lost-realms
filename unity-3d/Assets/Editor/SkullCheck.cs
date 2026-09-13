using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Orientation probe for the rigged Skeleton.fbx: census extents cannot tell
 // head from feet, so this compares the head bone against the hips bone in
 // world space (the head bone sits inside the skull) plus an extremal-vertex
 // analysis in bind pose (the skull crown is a tight single cluster, the feet
 // a wider pair).
 public static class SkullCheck {
  static Transform FindDeep(Transform t,string n){
   if(!t)return null;if(t.name==n)return t;
   for(int i=0;i<t.childCount;i++){var f=FindDeep(t.GetChild(i),n);if(f)return f;}
   return null;
  }
  public static void Run(){
   var sb=new StringBuilder();
   var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Characters/Skeleton.fbx");
   if(!prefab){sb.AppendLine("MISSING");Write(sb);return;}
   var inst=Object.Instantiate(prefab);
   inst.transform.position=Vector3.zero;inst.transform.rotation=Quaternion.identity;
   var head=FindDeep(inst.transform,"head");
   var hips=FindDeep(inst.transform,"hips");
   var handR=FindDeep(inst.transform,"hand.R");
   var faL=FindDeep(inst.transform,"forearm.L");
   sb.AppendLine("head_y="+(head?head.position.y.ToString("0.###"):"?")+" hips_y="+(hips?hips.position.y.ToString("0.###"):"?"));
   if(head&&hips)sb.AppendLine(head.position.y>hips.position.y?"ORIENTATION HEAD_UP":"ORIENTATION HEAD_DOWN");
   if(handR)sb.AppendLine("handR="+handR.position.ToString("0.##"));
   if(faL)sb.AppendLine("forearmL="+faL.position.ToString("0.##"));
   var smr=inst.GetComponentInChildren<SkinnedMeshRenderer>();
   if(smr){
    sb.AppendLine("meshbounds="+smr.bounds.size.ToString("0.###")+" min="+smr.bounds.min.ToString("0.###"));
    var mesh=smr.sharedMesh;var verts=mesh.vertices;var m=smr.transform.localToWorldMatrix;
    float topY=float.NegativeInfinity,botY=float.PositiveInfinity;
    foreach(var v in verts){var w=m.MultiplyPoint3x4(v);if(w.y>topY)topY=w.y;if(w.y<botY)botY=w.y;}
    Vector3 topCTR=Vector3.zero,botCTR=Vector3.zero;int topN=0,botN=0;
    foreach(var v in verts){var w=m.MultiplyPoint3x4(v);if(w.y>topY-0.06f){topCTR+=w;topN++;}if(w.y<botY+0.06f){botCTR+=w;botN++;}}
    topCTR/=Mathf.Max(1,topN);botCTR/=Mathf.Max(1,botN);
    sb.AppendLine("top_strip_n="+topN+" c="+topCTR.ToString("0.##")+" bot_strip_n="+botN+" c="+botCTR.ToString("0.##"));
    sb.AppendLine(topN<botN?"CROWN_AT_TOP":"CROWN_AT_BOTTOM");
   }else sb.AppendLine("NO_SKINNED_MESH");
   Object.DestroyImmediate(inst);
   Write(sb);
   Debug.Log("SKULL_CHECK_DONE");
  }
  static void Write(StringBuilder sb){
   string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
   Directory.CreateDirectory(outDir);
   File.WriteAllText(Path.Combine(outDir,"skull-check.txt"),sb.ToString());
  }
 }
}