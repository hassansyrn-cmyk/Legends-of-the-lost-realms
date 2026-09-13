using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 // Pins every shader that runtime code / Resources materials need into
 // Always Included Shaders, so APK builds cannot strip them into magenta
 // (Shader.Find("Standard") and imported VFX shaders are the common victims).
 public static class PinShaders {
  public static void Run(){
   var sb=new StringBuilder();
   var gsAssets=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
   if(gsAssets==null||gsAssets.Length==0){sb.AppendLine("NO_GRAPHICS_SETTINGS");Write(sb);EditorApplication.Exit(0);return;}
   var so=new SerializedObject(gsAssets[0]);
   var arr=so.FindProperty("m_AlwaysIncludedShaders");
   var have=new HashSet<Shader>();
   for(int i=0;i<arr.arraySize;i++){var s=arr.GetArrayElementAtIndex(i).objectReferenceValue as Shader;if(s)have.Add(s);}
   int added=0;
   System.Action<Shader,string> pin=(sh,name)=>{if(sh&&!have.Contains(sh)){arr.InsertArrayElementAtIndex(arr.arraySize);arr.GetArrayElementAtIndex(arr.arraySize-1).objectReferenceValue=sh;have.Add(sh);added++;sb.AppendLine("pinned "+name);}};
   foreach(var name in new[]{"Standard","Sprites/Default","Particles/Standard Unlit","Particles/Standard Surface","Mobile/Particles/Additive","Legacy Shaders/Particles/Additive","Unlit/Transparent","Unlit/Texture"})pin(Shader.Find(name),name);
   int mats=0;
   foreach(var guid in AssetDatabase.FindAssets("",new[]{"Assets/Resources"})){
    string path=AssetDatabase.GUIDToAssetPath(guid);
    foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(path)){
     if(obj is Material m&&m.shader){pin(m.shader,m.shader.name);mats++;}
     if(obj is GameObject go)foreach(var r in go.GetComponentsInChildren<Renderer>(true))foreach(var mm in r.sharedMaterials)if(mm&&mm.shader){pin(mm.shader,mm.shader.name);mats++;}
    }
   }
   so.ApplyModifiedProperties();
   AssetDatabase.SaveAssets();
   sb.AppendLine("materialsScanned="+mats+" added="+added+" total="+arr.arraySize);
   Write(sb);
   Debug.Log("PIN_SHADERS_DONE added="+added);
   EditorApplication.Exit(0);
  }
  static void Write(StringBuilder sb){
   string outDir=Path.GetFullPath(Path.Combine(Application.dataPath,"../Validation"));
   Directory.CreateDirectory(outDir);
   File.WriteAllText(Path.Combine(outDir,"pin-shaders.txt"),sb.ToString());
  }
 }
}
