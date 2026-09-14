using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Deploys the Dungeon Mason rigged Rock Golem as the three realm-boss visuals:
// the same rigged FBX under three role names, a per-realm tinted basecolor,
// and its pre-authored clips baked into Resources/Animations/<Role>_<state>.
public static class GolemBossSetup {
 const string SrcFbx="Assets/Art/Enemies/Golem/GolemNormalized.fbx";
 const string SrcTex="Assets/Art/Enemies/Golem/AlbedoGolem.png";
 static readonly string[][] Clips={
  new[]{"idle","Idle","true"},new[]{"walk","Walk","true"},
  new[]{"attack","Attack01","false"},new[]{"death","Die","false"}};
 static readonly string[] Roles={"Heartwood","Sunscar","Whiteout"};
 static readonly Color[] Tints={
  new Color(.62f,.95f,.58f),   // Heartwood: mossy green
  new Color(1f,.84f,.58f),     // Sunscar: sun-bleached sand
  new Color(.62f,.85f,1.05f)}; // Whiteout: glacial blue

 [MenuItem("Lost Realms/Phase 2/Prepare Golem Bosses")]
 public static void Prepare(){
  var report=new List<string>{"Golem boss setup"};
  // Source texture: small + readable for tinting.
  var texImporter=AssetImporter.GetAtPath(SrcTex) as TextureImporter;
  if(texImporter==null)throw new Exception("Missing "+SrcTex);
  texImporter.maxTextureSize=512;texImporter.isReadable=true;texImporter.textureType=TextureImporterType.Default;
  texImporter.sRGBTexture=true;texImporter.SaveAndReimport();
  var src=AssetDatabase.LoadAssetAtPath<Texture2D>(SrcTex);
  if(!src)throw new Exception("Albedo not loadable");

  for(int r=0;r<Roles.Length;r++){
   string role=Roles[r];
   string dstFbx="Assets/Resources/Characters/"+role+".fbx";
   File.Copy(SrcFbx,dstFbx,true);
   AssetDatabase.ImportAsset(dstFbx);
   var imp=AssetImporter.GetAtPath(dstFbx) as ModelImporter;
   if(imp==null)throw new Exception("Import failed for "+dstFbx);
   imp.animationType=ModelImporterAnimationType.Generic;
   imp.importAnimation=false;
   imp.materialImportMode=ModelImporterMaterialImportMode.None;
   imp.importCameras=false;imp.importLights=false;
   imp.optimizeGameObjects=false;
   imp.SaveAndReimport();
   // Tinted basecolor for CharacterSkin.
   var tinted=new Texture2D(src.width,src.height,TextureFormat.RGB24,false);
   var px=src.GetPixels32();
   for(int i=0;i<px.Length;i++){
    px[i].r=(byte)Mathf.Clamp(px[i].r*Tints[r].r,0,255);
    px[i].g=(byte)Mathf.Clamp(px[i].g*Tints[r].g,0,255);
    px[i].b=(byte)Mathf.Clamp(px[i].b*Tints[r].b,0,255);
   }
   tinted.SetPixels32(px);tinted.Apply();
   string texPath="Assets/Resources/Characters/Textures/"+role+"_basecolor.jpg";
   File.WriteAllBytes(texPath.Replace("Assets/",Application.dataPath+"/"),tinted.EncodeToJPG(88));
   AssetDatabase.ImportAsset(texPath);
   // Clips.
   foreach(var c in Clips){
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/Enemies/Golem/"+c[1]+".anim");
    if(!clip)throw new Exception("Missing clip "+c[1]);
    var copy=UnityEngine.Object.Instantiate(clip);
    copy.name=role+"_"+c[0];
    var settings=AnimationUtility.GetAnimationClipSettings(copy);
    settings.loopTime=c[2]=="true";
    AnimationUtility.SetAnimationClipSettings(copy,settings);
    string outPath="Assets/Resources/Animations/"+role+"_"+c[0]+".anim";
    if(AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath))AssetDatabase.DeleteAsset(outPath);
    AssetDatabase.CreateAsset(copy,outPath);
    report.Add(role+"_"+c[0]+": "+copy.length.ToString("0.000")+"s loop="+c[2]);
   }
   report.Add(role+": fbx="+dstFbx+" tint="+Tints[r]);
  }
  AssetDatabase.SaveAssets();
  File.WriteAllLines("GOLEM_BOSS_REPORT.txt",report);
  Debug.Log("GOLEM_BOSS_SETUP_PASSED");
 }
}
