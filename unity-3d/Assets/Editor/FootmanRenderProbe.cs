using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Foreground-only proof probe: renders the Footman model through a real camera
 // and measures the pixel bounding box of its lit silhouette against a
 // known-good rigged enemy (BriarGoblin). Verdict: BROKEN if it renders
 // sideways, flat, tiny, or as a spike — the census/diag bounds for Footman
 // have been unreliable, so pixels are the ground truth.
 public static class FootmanRenderProbe {
  [MenuItem("Lost Realms/Diag/FootmanRender")]
  public static void Diagnose(){
   var sb=new StringBuilder();
   var rt=new RenderTexture(256,256,24);
   var camGo=new GameObject("diagcam");
   camGo.transform.SetPositionAndRotation(new Vector3(0,2.4f,6),Quaternion.LookRotation(Vector3.back));
   var cam=camGo.AddComponent<Camera>();
   cam.orthographic=true;cam.orthographicSize=4.5f;
   cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
   cam.targetTexture=rt;
   foreach(var role in new[]{"Footman","BriarGoblin","Skeleton"}){
    try{
     var holder=new GameObject("probe "+role);
     var v=CharacterVisual.Create(role,holder.transform,role=="Footman"?1.75f:1.35f,Color.white);
     if(role=="Footman"){
     try{
      var sk=v.transform.GetComponentsInChildren<SkinnedMeshRenderer>()[0];
      sb.AppendLine("  smrPos="+sk.transform.position.ToString("0.00")+" parent="+sk.transform.parent.name);
      var baked=new Mesh();
      sk.BakeMesh(baked);
      sb.AppendLine("  baked bounds="+baked.bounds.ToString("0.00")+" verts="+baked.vertexCount);
      Object.DestroyImmediate(baked);
     }catch(System.Exception e){sb.AppendLine("  bakedERR "+e.Message);}
    }
    cam.Render();
     sb.AppendLine(role+" "+Measure(rt));
     Object.DestroyImmediate(holder);
    }catch(System.Exception e){sb.AppendLine(role+" ERR "+e.Message);}
   }
   Object.DestroyImmediate(camGo);Object.DestroyImmediate(rt);
   System.IO.File.WriteAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation/footman-render.txt")),sb.ToString());
   Debug.Log("FOOTMAN_RENDER_DONE");
   EditorApplication.Exit(0);
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
   return "lit="+lit+" box="+w+"x"+h+" at("+minX+","+minY+") ratio="+(w>0?((float)h/w).ToString("0.00"):"0");
  }
 }
}