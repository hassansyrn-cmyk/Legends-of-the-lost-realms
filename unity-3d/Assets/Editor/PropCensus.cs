using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class PropCensus {
  public static void Run(){
   var sb=new StringBuilder();
   string[] roots={"Assets/Resources/Props","Assets/Resources/Props/Nature","Assets/Resources/Props/Desert","Assets/Resources/Props/Snow","Assets/Resources/Props/Village","Assets/Resources/Props/Traps","Assets/Resources/Characters"};
   foreach(var root in roots){
    if(!Directory.Exists(root))continue;
    foreach(var file in Directory.GetFiles(root)){
     string ext=Path.GetExtension(file);
     if(ext!=".fbx"&&ext!=".FBX"&&ext!=".prefab")continue;
     var go=AssetDatabase.LoadAssetAtPath<GameObject>(file);
     if(!go){sb.AppendLine("NULL_ASSET "+file);continue;}
     var renderer=go.GetComponentInChildren<Renderer>();
     if(!renderer){sb.AppendLine("NO_RENDERER "+file);continue;}
     Bounds b=renderer.bounds;
     // object root local pose (pivot) of the prefab
     var rootT=go.transform;
     sb.AppendLine(Path.GetFileName(file)+" | root rot "+(rootT!=null?rootT.rotation.eulerAngles.ToString("0"):"none")
      +" | size "+b.size.x.ToString("0.###")+","+b.size.y.ToString("0.###")+","+b.size.z.ToString("0.###")
      +" | pivotMinY "+b.min.y.ToString("0.###")+" | pivotMinX "+b.min.x.ToString("0.###")+" | pivotMinZ "+b.min.z.ToString("0.###"));
    }
   }
   string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
   Directory.CreateDirectory(outDir);
   File.WriteAllText(Path.Combine(outDir,"prop-census.txt"),sb.ToString());
   Debug.Log("PROP_CENSUS_DONE");
  }
 }
}