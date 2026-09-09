using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class AsterPhase1 {
 const string ModelPath = "Assets/Art/Models/Aster.fbx";
 const string MaterialPath = "Assets/Resources/Materials/Aster.mat";
 const string PrefabPath = "Assets/Resources/Characters/Aster.prefab";
 const string AnimationSourceRoot = "Assets/Art/Animations/AsterMixamo";
 const string AnimationOutputRoot = "Assets/Resources/Animations/Aster";

 readonly struct ClipSpec {
  public readonly string State;
  public readonly string AssetPath;
  public readonly bool Loop;
  public ClipSpec(string state,string assetPath,bool loop){State=state;AssetPath=assetPath;Loop=loop;}
 }

 static readonly ClipSpec[] Clips={
  new ClipSpec("idle",ModelPath,true),
  new ClipSpec("walk",AnimationSourceRoot+"/Aster_walk.fbx",true),
  new ClipSpec("run",AnimationSourceRoot+"/Aster_run.fbx",true),
  new ClipSpec("attack_1",AnimationSourceRoot+"/Aster_attack_1.fbx",false),
  new ClipSpec("attack_2",AnimationSourceRoot+"/Aster_attack_2.fbx",false),
  new ClipSpec("attack_3",AnimationSourceRoot+"/Aster_attack_3.fbx",false),
  new ClipSpec("charged",AnimationSourceRoot+"/Aster_charged.fbx",false),
  new ClipSpec("jump",AnimationSourceRoot+"/Aster_jump.fbx",false),
  new ClipSpec("double_jump",AnimationSourceRoot+"/Aster_double_jump.fbx",false),
  new ClipSpec("dodge",AnimationSourceRoot+"/Aster_dodge.fbx",false),
  new ClipSpec("hit",AnimationSourceRoot+"/Aster_hit.fbx",false),
  new ClipSpec("death",AnimationSourceRoot+"/Aster_death.fbx",false)
 };

 [MenuItem("Lost Realms/Phase 1/Prepare Aster Mixamo Character")]
 public static void Prepare(){
  EnsureDirectories();
  PrepareTexture("Assets/Art/Textures/Aster_0.jpg",true,1024);
  PrepareTexture("Assets/Art/Textures/Aster_1.jpg",false,2048);
  ConfigureBaseModel();

  var sourceModel=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
  if(!sourceModel)throw new Exception("Missing supplied Aster model: "+ModelPath);
  var sourceAnimator=sourceModel.GetComponentInChildren<Animator>(true);
  var avatar=sourceAnimator?sourceAnimator.avatar:null;
  if(!avatar||!avatar.isHuman||!avatar.isValid)throw new Exception("Supplied Aster FBX did not produce a valid Humanoid avatar.");

  var report=new List<string>{"Aster Phase 1 Mixamo integration","Model: "+ModelPath,"Avatar: valid humanoid"};
  foreach(var spec in Clips){
   if(spec.AssetPath!=ModelPath)ConfigureAnimation(spec,avatar);
   var imported=FindImportedClip(spec.AssetPath);
   if(!imported)throw new Exception("No animation clip imported from "+spec.AssetPath);
   string outputPath=AnimationOutputRoot+"/"+spec.State+".anim";
   SaveClip(imported,outputPath,spec.State,spec.Loop);
   var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
   if(!saved)throw new Exception("Failed to create "+outputPath);
   report.Add(spec.State+": "+saved.length.ToString("0.000")+"s, loop="+spec.Loop);
  }

  BuildPrefab(sourceModel,avatar);
  AssetDatabase.SaveAssets();
  File.WriteAllLines("ASTER_PHASE1_REPORT.txt",report);
  Debug.Log("MODEL_INTEGRATION_PASSED: supplied Mixamo Aster model, material, and 12 animation states prepared.");
 }

 static void EnsureDirectories(){
  EnsureFolder("Assets/Resources/Characters");
  EnsureFolder("Assets/Resources/Animations/Aster");
  EnsureFolder("Assets/Resources/Materials");
 }

 static void EnsureFolder(string path){
  if(AssetDatabase.IsValidFolder(path))return;
  string parent=Path.GetDirectoryName(path).Replace('\\','/');
  EnsureFolder(parent);
  AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
 }

 static void PrepareTexture(string path,bool normalMap,int maxSize){
  var importer=AssetImporter.GetAtPath(path) as TextureImporter;
  if(!importer)throw new Exception("Missing supplied Aster texture: "+path);
  importer.textureType=normalMap?TextureImporterType.NormalMap:TextureImporterType.Default;
  importer.sRGBTexture=!normalMap;
  importer.maxTextureSize=maxSize;
  importer.textureCompression=TextureImporterCompression.Compressed;
  importer.mipmapEnabled=true;
  importer.SaveAndReimport();
 }

 static void ConfigureBaseModel(){
  var importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
  if(!importer)throw new Exception("Missing supplied Aster FBX: "+ModelPath);
  ConfigureShared(importer);
  importer.importAnimation=true;
  importer.animationType=ModelImporterAnimationType.Human;
  importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
  // A new FBX's defaultClipAnimations list is populated only after its first
  // animation-enabled import. Import once, reload the importer, then configure it.
  importer.SaveAndReimport();
  importer=AssetImporter.GetAtPath(ModelPath) as ModelImporter;
  var clips=importer.defaultClipAnimations;
  ConfigureClips(clips,"idle",true);
  importer.clipAnimations=clips;
  importer.SaveAndReimport();
 }

 static void ConfigureAnimation(ClipSpec spec,Avatar avatar){
  var importer=AssetImporter.GetAtPath(spec.AssetPath) as ModelImporter;
  if(!importer)throw new Exception("Missing supplied animation FBX: "+spec.AssetPath);
  ConfigureShared(importer);
  importer.importAnimation=true;
  importer.animationType=ModelImporterAnimationType.Human;
  importer.avatarSetup=ModelImporterAvatarSetup.CopyFromOther;
  importer.sourceAvatar=avatar;
  importer.SaveAndReimport();
  importer=AssetImporter.GetAtPath(spec.AssetPath) as ModelImporter;
  var clips=importer.defaultClipAnimations;
  ConfigureClips(clips,spec.State,spec.Loop);
  importer.clipAnimations=clips;
  importer.SaveAndReimport();
 }

 static void ConfigureShared(ModelImporter importer){
  importer.materialImportMode=ModelImporterMaterialImportMode.None;
  importer.importCameras=false;
  importer.importLights=false;
  importer.importBlendShapes=true;
  importer.isReadable=false;
  importer.meshCompression=ModelImporterMeshCompression.Low;
  importer.optimizeGameObjects=false;
  importer.globalScale=1f;
  importer.useFileScale=true;
 }

 static void ConfigureClips(ModelImporterClipAnimation[] clips,string state,bool loop){
  if(clips==null||clips.Length==0)throw new Exception("FBX has no default animation clip for state "+state);
  foreach(var clip in clips){
   clip.name=state;
   clip.loopTime=loop;
   clip.keepOriginalOrientation=true;
   clip.lockRootRotation=true;
   clip.keepOriginalPositionY=true;
   clip.lockRootHeightY=true;
   clip.keepOriginalPositionXZ=true;
   clip.lockRootPositionXZ=true;
  }
 }

 static AnimationClip FindImportedClip(string path){
  return AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal));
 }

 static void SaveClip(AnimationClip source,string outputPath,string state,bool loop){
  var clip=UnityEngine.Object.Instantiate(source);
  clip.name=state;
  var settings=AnimationUtility.GetAnimationClipSettings(clip);
  settings.loopTime=loop;
  AnimationUtility.SetAnimationClipSettings(clip,settings);
  if(AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath))AssetDatabase.DeleteAsset(outputPath);
  AssetDatabase.CreateAsset(clip,outputPath);
 }

 static void BuildPrefab(GameObject sourceModel,Avatar avatar){
  var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
  if(!material||!material.mainTexture)throw new Exception("Aster runtime material is missing its supplied albedo texture.");

  var root=new GameObject("Aster");
  try{
   var model=(GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
   model.name="Aster Mixamo Character";
   model.transform.SetParent(root.transform,false);
   var renderers=model.GetComponentsInChildren<Renderer>(true);
   if(renderers.Length==0)throw new Exception("Supplied Aster FBX contains no renderers.");
   foreach(var renderer in renderers){
    renderer.sharedMaterials=Enumerable.Repeat(material,Math.Max(1,renderer.sharedMaterials.Length)).ToArray();
    if(renderer is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=false;
   }

   var bounds=renderers[0].bounds;
   foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
   float scale=1.8f/Mathf.Max(.01f,bounds.size.y);
   model.transform.localScale=Vector3.one*scale;
   model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;

   var animator=model.GetComponentInChildren<Animator>(true);
   if(!animator)animator=model.AddComponent<Animator>();
   animator.avatar=avatar;
   animator.applyRootMotion=false;
   animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;

   var lod=root.AddComponent<LODGroup>();
   lod.SetLODs(new[]{new LOD(.025f,renderers)});
   lod.RecalculateBounds();
   if(!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))throw new Exception("Failed to save Aster prefab.");
  }finally{
   UnityEngine.Object.DestroyImmediate(root);
  }
 }
}
