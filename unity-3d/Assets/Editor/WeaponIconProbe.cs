using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class WeaponIconProbe {
  [MenuItem("Lost Realms/Arsenal/Bake weapon icons")]
  public static void Run(){
   const string folder="Assets/Resources/Weapons/Icons";
   Directory.CreateDirectory(folder);Directory.CreateDirectory("Validation");var report=new StringBuilder();
   var preview=new PreviewRenderUtility();var sheet=new Texture2D(1024,640,TextureFormat.RGBA32,false);int ok=0;
   try{
    preview.camera.orthographic=true;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=100;
    preview.camera.clearFlags=CameraClearFlags.SolidColor;preview.camera.backgroundColor=Color.clear;
    preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(40,35,0);
    preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(340,218,0);
    preview.ambientColor=new Color(.7f,.72f,.76f);
    for(int id=1;id<=40;id++){
     GameObject root=new GameObject("Icon weapon");Texture2D image=null;
     try{
      var def=WeaponCatalog.Get(id);var model=WeaponCatalog.CreateModel((WeaponId)id,root.transform);
      if(!model)throw new Exception("Missing weapon "+id);preview.AddSingleGO(root);
      var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)throw new Exception("Empty weapon "+id);
      Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
      var camera=preview.camera;camera.transform.position=bounds.center+new Vector3(1,.6f,1).normalized*8;camera.transform.LookAt(bounds.center);float extent=0;
      for(int corner=0;corner<8;corner++){
       Vector3 p=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
       Vector3 view=camera.transform.InverseTransformPoint(p);extent=Mathf.Max(extent,Mathf.Abs(view.x),Mathf.Abs(view.y));
      }
      camera.orthographicSize=Mathf.Max(.05f,extent*1.14f);
      // Read RGBA directly: EndStaticPreview creates an opaque thumbnail.
      var rt=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);
      var previous=RenderTexture.active;
      try{
       camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
       image=new Texture2D(128,128,TextureFormat.RGBA32,false);
       image.ReadPixels(new Rect(0,0,128,128),0,0);image.Apply();
      }finally{RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
      string name=Path.GetFileName(def.Resource);File.WriteAllBytes(folder+"/"+name+".png",image.EncodeToPNG());
      sheet.SetPixels(((id-1)%8)*128,(4-(id-1)/8)*128,128,128,image.GetPixels());report.AppendLine("ICON_OK "+name);ok++;
     }finally{if(image)UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(root);}
    }
    sheet.Apply();File.WriteAllBytes("Validation/weapon-icons.png",sheet.EncodeToPNG());
   }finally{preview.Cleanup();UnityEngine.Object.DestroyImmediate(sheet);report.AppendLine("ICON_DONE ok="+ok);File.WriteAllText("Validation/weapon-icons.txt",report.ToString());}
   AssetDatabase.Refresh();
   foreach(string path in Directory.GetFiles(folder,"*.png")){
    var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
   }
  }
 }
}
