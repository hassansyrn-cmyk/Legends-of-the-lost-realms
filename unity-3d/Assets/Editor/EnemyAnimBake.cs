using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Bakes the rigged enemy FBX clips into standalone .anim assets under
// Resources/Animations/<Role>_<state>.anim, where CharacterVisual's enemy
// clip lookup finds them ("Animations/"+role+"_"+state). Generated only by
// this menu — never hand-edit those .anim files.
public static class EnemyAnimBake {
 struct Spec {
  public string Role; public string Fbx; public string[][] States; // {state, actionName, loop, sourceFbx}
  public Spec(string role,string fbx,string[][] states){Role=role;Fbx=fbx;States=states;}
 }
 // Build the spec table. Empty sourceFbx = the role's own FBX.
 static Spec[] Table(){
  var list=new List<Spec>();
  string[][] States()=>new[]{
   new[]{"idle","idle","true",""},new[]{"walk","walk","true",""},
   new[]{"attack","attack","false",""},new[]{"death","death","false",""}};
  foreach(var role in new[]{"Goblin","Demon","Frost","Elemental","Caster"})
   list.Add(new Spec(role,"Assets/Resources/Characters/"+role+".fbx",States()));
  // SazenGames skeleton: one take FBX per state (same rig hierarchy as the model).
  string take="Assets/Art/Enemies/Skeleton/";
list.Add(new Spec("Skeleton","Assets/Resources/Characters/Skeleton.fbx",new[]{
    new[]{"idle","idle","true",take+"Skeleton_idle.fbx"},
    new[]{"walk","walk","true",take+"Skeleton_walk_forward.fbx"},
    new[]{"attack","attack","false",take+"Skeleton_slash01.fbx"},
    new[]{"death","death","false",take+"Skeleton_death.fbx"}}));
   // Dungeon Mason Mini Legion Footman: imported RAW from the pack (do NOT
   // Blender-re-export — the pack's own cm-rigged file is what renders
   // upright in Unity; hand re-exports turned invisible). Model = Footman_Idle
   // (idle take in-file just to satisfy the clip scan); the 4 states are taken
   // from the separate per-state take files (Skeleton pattern).
   string fman="Assets/Art/Enemies/Footman/";
   list.Add(new Spec("Footman","Assets/Resources/Characters/Footman.fbx",new[]{
    new[]{"idle","idle","true",fman+"Footman_Idle.fbx"},
    new[]{"walk","walk","true",fman+"Footman_Walk.fbx"},
    new[]{"attack","attack","false",fman+"Footman_Attack01.fbx"},
    new[]{"death","death","false",fman+"Footman_Death.fbx"}}));
   return list.ToArray();
 }

 static void ConfigureGeneric(string fbx){
  var importer=AssetImporter.GetAtPath(fbx) as ModelImporter;
  if(importer==null)throw new Exception("Missing enemy FBX: "+fbx);
  importer.animationType=ModelImporterAnimationType.Generic;
  importer.importAnimation=true;
  importer.materialImportMode=ModelImporterMaterialImportMode.None;
  importer.importCameras=false;importer.importLights=false;
  importer.optimizeGameObjects=false;
  importer.SaveAndReimport();
 }

 [MenuItem("Lost Realms/Phase 2/Prepare Enemy Rig Animations")]
 public static void Prepare(){
  var report=new List<string>{"Enemy rig animation bake"};
  DeploySkeletonModel();
  foreach(var spec in Table()){
   ConfigureGeneric(spec.Fbx);
   var importer=AssetImporter.GetAtPath(spec.Fbx) as ModelImporter;
   var clips=importer.defaultClipAnimations;
   if(clips==null||clips.Length==0)throw new Exception("No clips imported from "+spec.Fbx);
   Debug.Log("BAKECLIPS "+spec.Role+": ["+string.Join(", ",clips.Select(c=>c.name))+"]");
   foreach(var clip in clips){
    // Blender exports takes as "Armature|Action" — match on the action part.
    int pipe=clip.name.LastIndexOf('|');
    string actionPart=pipe>=0?clip.name.Substring(pipe+1):clip.name;
    foreach(var st in spec.States){
     if(!string.Equals(actionPart,st[1],StringComparison.OrdinalIgnoreCase))continue;
     clip.name=st[0];clip.loopTime=st[2]=="true";
     clip.lockRootRotation=true;clip.keepOriginalPositionXZ=true;clip.lockRootPositionXZ=true;
     clip.keepOriginalPositionY=true;clip.lockRootHeightY=true;
    }
   }
   importer.clipAnimations=clips;
   importer.SaveAndReimport();
   foreach(var st in spec.States){
    string sourceFbx=st.Length>3&&!string.IsNullOrEmpty(st[3])?st[3]:spec.Fbx;
    AnimationClip imported;
    if(sourceFbx!=spec.Fbx){
     // Per-take source FBX: configure it, then grab its first real clip.
     ConfigureGeneric(sourceFbx);
     var ti=AssetImporter.GetAtPath(sourceFbx) as ModelImporter;
     var tc=ti.defaultClipAnimations;
if(tc.Length>0){
       tc[0].name=st[0];tc[0].loopTime=st[2]=="true";
       tc[0].lockRootRotation=true;tc[0].keepOriginalPositionXZ=true;tc[0].lockRootPositionXZ=true;
       tc[0].keepOriginalPositionY=true;tc[0].lockRootHeightY=true;
       ti.clipAnimations=tc;ti.SaveAndReimport();
      }
     imported=AssetDatabase.LoadAllAssetsAtPath(sourceFbx).OfType<AnimationClip>()
      .FirstOrDefault(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal));
    }else{
     imported=AssetDatabase.LoadAllAssetsAtPath(spec.Fbx).OfType<AnimationClip>()
      .FirstOrDefault(c=>c.name==st[0]);
    }
    if(!imported)throw new Exception("State clip missing after bake: "+spec.Role+" "+st[0]+" from "+sourceFbx);
    string outPath="Assets/Resources/Animations/"+spec.Role+"_"+st[0]+".anim";
    var copy=UnityEngine.Object.Instantiate(imported);
    copy.name=spec.Role+"_"+st[0];
    var settings=AnimationUtility.GetAnimationClipSettings(copy);
    settings.loopTime=st[2]=="true";
    AnimationUtility.SetAnimationClipSettings(copy,settings);
    if(AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath))AssetDatabase.DeleteAsset(outPath);
    AssetDatabase.CreateAsset(copy,outPath);
    report.Add(spec.Role+"_"+st[0]+": "+copy.length.ToString("0.000")+"s loop="+st[2]);
   }
  }
  BakeSkeletonTexture();
  AssetDatabase.SaveAssets();
  File.WriteAllLines("ENEMY_ANIM_REPORT.txt",report);
  Debug.Log("ENEMY_ANIM_BAKE_PASSED: "+report.Count+" clips");
 }

 // Replace the old Dzen skeleton with the rigged SazenGames model.
 static void DeploySkeletonModel(){
  string src="Assets/Art/Enemies/Skeleton/Skeleton_Model_110.fbx";
  string dst="Assets/Resources/Characters/Skeleton.fbx";
  if(!File.Exists(src))throw new Exception("Missing "+src);
  File.Copy(src,dst,true);
  AssetDatabase.ImportAsset(dst);
 }

 // Skeleton_BaseColor.psd -> 512 JPG for CharacterSkin.
 static void BakeSkeletonTexture(){
  string src="Assets/Art/Enemies/Skeleton/Skeleton_BaseColor.psd";
  var imp=AssetImporter.GetAtPath(src) as TextureImporter;
  if(imp==null)throw new Exception("Missing "+src);
  imp.maxTextureSize=512;imp.isReadable=true;imp.textureType=TextureImporterType.Default;
  imp.sRGBTexture=true;imp.SaveAndReimport();
  var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(src);
  if(!tex)throw new Exception("Skeleton basecolor not loadable");
  var rgb=new Texture2D(tex.width,tex.height,TextureFormat.RGB24,false);
  rgb.SetPixels32(tex.GetPixels32());rgb.Apply();
  string outPath="Assets/Resources/Characters/Textures/Skeleton_basecolor.jpg";
  File.WriteAllBytes(outPath.Replace("Assets/",Application.dataPath+"/"),rgb.EncodeToJPG(88));
  AssetDatabase.ImportAsset(outPath);
 }
}
