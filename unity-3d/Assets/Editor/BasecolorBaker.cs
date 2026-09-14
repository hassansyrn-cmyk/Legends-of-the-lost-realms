using System.IO;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // One-shot texture baker for the new enemy packs: imports a raw albedo png/tga,
 // scales down to the 512² runtime character texture convention and writes
 // Resources/Characters/Textures/<Role>_basecolor.jpg (same pattern as the
 // weapon and existing enemy conversions). Sources are parked under
 // Assets/Art/Enemies/<Role>/ and can be deleted after a green run.
 public static class BasecolorBaker {
  [MenuItem("Lost Realms/Diag/BakeBasecolor")]
  public static void Bake(){
   string outDir=Application.dataPath+"/Resources/Characters/Textures";
   Directory.CreateDirectory(outDir);
   BakeOne("DogKnight","Albedo.png",outDir);
   BakeOne("Spider","spider_01.tga",outDir);
   AssetDatabase.Refresh();
   Debug.Log("BASECOLOR_DONE");
   EditorApplication.Exit(0);
  }
  static void BakeOne(string role,string file,string outDir){
   var raw=ReadRaw(role,file);
   if(raw==null){Debug.LogError("no source for "+role);return;}
   EditorUtility.CompressTexture(raw,TextureFormat.RGB24,TextureCompressionQuality.Best);
   int size=512;
   var rt=new RenderTexture(size,size,0,RenderTextureFormat.ARGB32);
   Graphics.Blit(raw,rt);
   RenderTexture.active=rt;
   var tex=new Texture2D(size,size,TextureFormat.RGB24,false);
   tex.ReadPixels(new Rect(0,0,size,size),0,0);
   tex.Apply();
   RenderTexture.active=null;
   rt.Release();
   var bytes=ImageConversion.EncodeToJPG(tex,88);
   File.WriteAllBytes(outDir+"/"+role+"_basecolor.jpg",bytes);
   Object.DestroyImmediate(tex);
   Debug.Log(role+" -> "+bytes.Length+" bytes");
  }
  static Texture2D ReadRaw(string role,string file){
   var path=Application.dataPath+"/Art/Enemies/"+role+"/"+file;
   if(!File.Exists(path))return null;
   var bytes=File.ReadAllBytes(path);
   var tex=new Texture2D(2,2,TextureFormat.RGBA32,false);
   if(!tex.LoadImage(bytes)){
    // TGA fallback: hand-roll the header (BPP 24/32, uncompressed).
    if(bytes.Length<18)return null;
    int w=bytes[12]|bytes[13]<<8,h=bytes[14]|bytes[15]<<8,bpp=bytes[16];
    if(bpp!=24&&bpp!=32)return null;
    var pix=new Color32[w*h];
    int stride=bpp/8,off=18;
    for(int y=0;y<h;y++){
     for(int x=0;x<w;x++){
      int i=off+((h-1-y)*w+x)*stride;
      byte b=bytes[i],g=bytes[i+1],r=bytes[i+2],a=bpp==32?bytes[i+3]:(byte)255;
      pix[y*w+x]=new Color32(r,g,b,a);
     }
    }
    Object.DestroyImmediate(tex);
    tex=new Texture2D(w,h,TextureFormat.RGBA32,false);
    tex.SetPixels32(pix);tex.Apply();
   }
   return tex;
  }
 }
}