using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class FbxDiag {
  [MenuItem("Lost Realms/Diag/FbxImport")]
  public static void Run(){
   var sb=new StringBuilder();
foreach(var path in new[]{"Assets/Resources/Characters/Goblin.fbx","Assets/Resources/Characters/Footman.fbx","Assets/Resources/Characters/DogKnight.fbx","Assets/Resources/Characters/Spider.fbx"}){
    try{
     AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
     var imp=AssetImporter.GetAtPath(path) as ModelImporter;
     sb.AppendLine("=="+path+"==");
     if(imp!=null){
      sb.AppendLine("globalScale="+imp.globalScale);
      sb.AppendLine("useFileScale="+imp.useFileScale);
      sb.AppendLine("bakeAxisConversion="+imp.bakeAxisConversion);
     }
     var go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
     sb.AppendLine("root "+go.name+" scale="+go.transform.localScale+" pos="+go.transform.localPosition);
     foreach(var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>()){
      sb.AppendLine("SMR "+smr.name+" bounds="+smr.bounds.ToString("0.000")+" verts="+smr.sharedMesh.vertexCount+" bones="+smr.bones.Length+" bind="+smr.sharedMesh.bindposes.Length);
      if(smr.bones.Length>1)sb.AppendLine("  bone0 pos="+smr.bones[0].localPosition+" scl="+smr.bones[0].localScale+" rootBone="+smr.rootBone.name);
      var vs=smr.sharedMesh.vertices;
      Vector3 mn=vs[0],mx=vs[0];
      for(int k=1;k<vs.Length;k++){mn=Vector3.Min(mn,vs[k]);mx=Vector3.Max(mx,vs[k]);}
      sb.AppendLine("  VERTSPAN "+mn.ToString("0.000")+" .. "+mx.ToString("0.000")+" ("+vs.Length+")");
      if(smr.sharedMesh.bindposes.Length>0)sb.AppendLine("  BINDP0 "+smr.sharedMesh.bindposes[0]);
     }
foreach(var tr in go.GetComponentsInChildren<Transform>(true)){
       if(tr!=null&&tr!=go.transform&&tr.parent!=null)sb.AppendLine("N "+tr.name+" parent="+tr.parent.name+" pos="+tr.localPosition.ToString("0.000")+" rot="+tr.localRotation.eulerAngles.ToString("0.0"));
      }
    }catch(System.Exception e){sb.AppendLine("ERR "+path+": "+e.Message);}
   }
   string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
   Directory.CreateDirectory(outDir);
   File.WriteAllText(Path.Combine(outDir,"fbx-diag.txt"),sb.ToString());
   Debug.Log("FBX_DIAG_DONE");
  }
 }
}