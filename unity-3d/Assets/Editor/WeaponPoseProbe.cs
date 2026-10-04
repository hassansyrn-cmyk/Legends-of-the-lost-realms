using System.IO;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class WeaponPoseProbe {
  public static void Validate(){WeaponIconProbe.RunNewWeapons();QualityValidation.Run();}
  public static void Inspect(){
   Directory.CreateDirectory("Validation");
   foreach(var id in new[]{WeaponId.FantasyGreatsword,WeaponId.FierySword,WeaponId.OrnateCurvedBlade}){
    var preview=new PreviewRenderUtility();var root=new GameObject("Pose model");
    try{
     var model=WeaponCatalog.CreateModel(id,root.transform);preview.AddSingleGO(root);
     var r=model.GetComponentInChildren<Renderer>();var b=r.bounds;
     // Unlit texture inspection makes the actual grip visible independently of scene lighting.
     var mat=new Material(Shader.Find("Unlit/Texture"));mat.mainTexture=r.sharedMaterial.mainTexture;r.sharedMaterial=mat;
     var cam=preview.camera;cam.orthographic=true;cam.orthographicSize=b.size.y*.6f;
     cam.transform.position=b.center-Vector3.forward*4;cam.transform.LookAt(b.center);
     cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.16f,.18f,.22f);
     var rt=new RenderTexture(600,600,24);var old=RenderTexture.active;cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
     var image=new Texture2D(600,600,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,600,600),0,0);image.Apply();
     File.WriteAllBytes("Validation/pose-model-"+id+".png",image.EncodeToPNG());
     Debug.Log(id+" bounds "+b.ToString("F5")+" modelScale "+model.transform.localScale.ToString("F5"));
     RenderTexture.active=old;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(image);Object.DestroyImmediate(mat);
    }finally{Object.DestroyImmediate(root);preview.Cleanup();}
   }
  }
 }
}
