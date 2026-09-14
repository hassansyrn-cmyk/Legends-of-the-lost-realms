using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class FootmanProbe {
  [MenuItem("Lost Realms/Diag/FootmanRuntime")]
  public static void Run(){
   var sb=new StringBuilder();
   try{
    var holder=new GameObject("probe root");
    foreach(var role in new[]{"Footman","BriarGoblin"}){
     try{
      var v=CharacterVisual.Create(role,holder.transform,role=="Footman"?1.75f:1.35f,Color.white);
      sb.AppendLine("== "+role+" ==");
      var model=v.transform.GetChild(0);
      sb.AppendLine("  model.localScale="+model.localScale.ToString("0.0000"));
      var renderers=model.GetComponentsInChildren<Renderer>(true);
      Bounds nb=renderers[0].bounds;
      for(int i=1;i<renderers.Length;i++)nb.Encapsulate(renderers[i].bounds);
      sb.AppendLine("  worldBounds size="+nb.size.ToString("0.000")+" min="+nb.min.ToString("0.000"));
      var sk=model.GetComponentsInChildren<SkinnedMeshRenderer>();
      if(sk.Length>0)sb.AppendLine("  smr.localScale="+sk[0].transform.localScale.ToString("0.0000"));
     }catch(System.Exception e){sb.AppendLine("== "+role+" == ERR "+e.Message);}
    }
    Object.DestroyImmediate(holder);
   }catch(System.Exception e2){
    sb.AppendLine("FATAL "+e2.ToString());
   }
   string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
   Directory.CreateDirectory(outDir);
   File.WriteAllText(Path.Combine(outDir,"footman-probe.txt"),sb.ToString());
   Debug.Log("FOOTMAN_PROBE_DONE");
  }
 }
}