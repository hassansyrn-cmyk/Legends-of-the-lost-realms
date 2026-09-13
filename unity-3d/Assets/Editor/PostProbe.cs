using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Foreground shader validation for the post layer (the RealmSky 'fract' bug
 // proved import parsing cannot catch CG errors — only a real render can).
 public static class PostProbe {
  public static void Diagnose(){
   var sb=new StringBuilder();
   var shader=Resources.Load<Shader>("Shaders/RealmPost");
   sb.AppendLine("shader="+(shader?"OK":"NULL"));
   if(!shader){Write(sb);EditorApplication.Exit(0);return;}
   var mat=new Material(shader);
   var cam=new GameObject("postprobe").AddComponent<Camera>();
   cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.4f,.5f,.6f);
   cam.gameObject.AddComponent<RealmPostFx>();
   var rt=new RenderTexture(128,72,24);
   cam.targetTexture=rt;cam.Render();
   RenderTexture.active=rt;
   var tex=new Texture2D(128,72,TextureFormat.RGB24,false);
   tex.ReadPixels(new Rect(0,0,128,72),0,0);tex.Apply();
   RenderTexture.active=null;cam.targetTexture=null;
   int mag=0;var px=tex.GetPixels();
   foreach(var c in px)if(c.r>.75f&&c.g<.35f&&c.b>.75f)mag++;
   sb.AppendLine("magenta%="+((float)mag/px.Length*100).ToString("0.0"));
   Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(mat);
   Write(sb);
   Debug.Log("POST_PROBE_DONE");
   EditorApplication.Exit(0);
  }
  static void Write(StringBuilder sb){
   string outDir=System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation"));
   System.IO.Directory.CreateDirectory(outDir);
   System.IO.File.WriteAllText(System.IO.Path.Combine(outDir,"post-probe.txt"),sb.ToString());
  }
 }
}
