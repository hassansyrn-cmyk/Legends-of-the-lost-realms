using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Diagnoses a magenta sky: reports whether the sky shader resolves, whether
 // Unity considers it supported (compiled OK), and whether it survived in the
 // Always Included list (APK strip protection).
 public static class SkyProbe {
  public static void Run(){
   var sb=new StringBuilder();
   var shader=Shader.Find("LostRealms/RealmSkyBox");
   sb.AppendLine("find="+(shader?"OK":"NULL"));
   if(shader){
    sb.AppendLine("isSupported="+(shader.isSupported?"YES":"NO"));
    var mat=new Material(shader);
    sb.AppendLine("material="+(mat?"OK":"NULL")+" shaderOnMat="+(mat.shader?"OK":"NULL"));
    Object.DestroyImmediate(mat);
   }
   sb.AppendLine("renderPipeline="+(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?"SRP":"BIRP"));
   System.IO.File.WriteAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation/sky-probe.txt")),sb.ToString());
   Debug.Log("SKY_PROBE_DONE");
  }
  // Foreground-only render test: draws each skybox candidate through a real
  // camera and counts magenta pixels. Needs the editor closed (own instance).
  public static void Diagnose(){
   var sb=new StringBuilder();
   var shader=Shader.Find("LostRealms/RealmSkyBox");
   if(shader){
    var mat=new Material(shader);
    mat.SetColor("_TopColor",new Color(.13f,.38f,.52f));mat.SetColor("_HorizonColor",new Color(.24f,.39f,.34f));
    mat.SetColor("_GroundColor",new Color(.10f,.16f,.16f));mat.SetColor("_SunColor",new Color(1,.93f,.75f));
    mat.SetVector("_SunDir",Quaternion.Euler(48,-35,0)*Vector3.back);
    mat.SetFloat("_SunSize",0.9993f);mat.SetFloat("_StarAmount",0.25f);
    RenderSettings.skybox=mat;
    sb.AppendLine("mine="+Shoot());
    Object.DestroyImmediate(mat);
   }else sb.AppendLine("mine=findNULL");
   var shader2=Resources.Load<Shader>("Shaders/RealmSky");
   if(shader2){
    var mat2=new Material(shader2);
    mat2.SetColor("_Zenith",new Color(.12f,.29f,.35f));mat2.SetColor("_Horizon",new Color(.57f,.7f,.58f));
    RenderSettings.skybox=mat2;
    sb.AppendLine("scenery="+Shoot());
    Object.DestroyImmediate(mat2);
   }else sb.AppendLine("scenery=loadNULL");
   RenderSettings.skybox=null;
   System.IO.File.WriteAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,"../Validation/sky-diagnose.txt")),sb.ToString());
   Debug.Log("SKY_DIAGNOSE_DONE");
   EditorApplication.Exit(0);
  }
  static string Shoot(){
   var cam=new GameObject("diagcam").AddComponent<Camera>();
   cam.clearFlags=CameraClearFlags.Skybox;cam.fieldOfView=60;
   cam.transform.position=Vector3.zero;cam.transform.rotation=Quaternion.identity;
   var rt=new RenderTexture(256,144,24);
   cam.targetTexture=rt;cam.Render();
   RenderTexture.active=rt;
   var tex=new Texture2D(256,144,TextureFormat.RGB24,false);
   tex.ReadPixels(new Rect(0,0,256,144),0,0);tex.Apply();
   RenderTexture.active=null;cam.targetTexture=null;
   Object.DestroyImmediate(cam.gameObject);Object.DestroyImmediate(rt);
   int mag=0,bright=0;var px=tex.GetPixels();
   foreach(var c in px){if(c.r>0.75f&&c.g<0.35f&&c.b>0.75f)mag++;if(c.r+c.g+c.b>2.4f)bright++;}
   var avgR=0f;var avgG=0f;var avgB=0f;
   foreach(var c in px){avgR+=c.r;avgG+=c.g;avgB+=c.b;}
   int n=px.Length;
   Object.DestroyImmediate(tex);
   return "magenta%="+((float)mag/n*100).ToString("0.0")+" white%="+((float)bright/n*100).ToString("0.0")
    +" avg="+((avgR/n).ToString("0.00"))+","+((avgG/n).ToString("0.00"))+","+((avgB/n).ToString("0.00"));
  }
 }
}