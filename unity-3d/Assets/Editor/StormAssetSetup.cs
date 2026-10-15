using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
namespace LostRealms {
 public static class StormAssetSetup {
  [MenuItem("Lost Realms/Expansion/Prepare Stormveil Assets")]
  public static void Prepare(){
   AssetDatabase.Refresh();Directory.CreateDirectory("Assets/Resources/Characters");Directory.CreateDirectory("Assets/Resources/Animations");
   foreach(string role in new[]{"StormStalker","StormScribe","TempestRegent"}){
    string path="Assets/Art/Stormveil/"+role+".fbx";var imp=AssetImporter.GetAtPath(path) as ModelImporter;if(!imp)throw new Exception("Storm model missing "+path);
    imp.animationType=ModelImporterAnimationType.Generic;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.importAnimation=true;imp.importCameras=imp.importLights=false;imp.optimizeGameObjects=false;imp.isReadable=true;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.animationCompression=ModelImporterAnimationCompression.Off;imp.SaveAndReimport();
    var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));go.name=role;var animator=go.GetComponent<Animator>()??go.AddComponent<Animator>();animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    // Keep pose bounds current for the grounding sampler and review cameras.
    foreach(var skin in go.GetComponentsInChildren<SkinnedMeshRenderer>())skin.updateWhenOffscreen=true;
    PrefabUtility.SaveAsPrefabAsset(go,"Assets/Resources/Characters/"+role+".prefab");UnityEngine.Object.DestroyImmediate(go);
    var imported=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
    foreach(var state in new[]{"idle","walk","attack","gethit","death","cast","attack_2","victory","run"}){
     var source=imported.FirstOrDefault(c=>c.name==state||c.name.EndsWith("|"+state));
     if(!source)throw new Exception("Missing Stormveil take "+role+" / "+state+": "+string.Join(",",imported.Select(c=>c.name)));
     var clip=UnityEngine.Object.Instantiate(source);clip.name=role+"_"+state;
     // In-place locomotion: bodies supply all translation. Do not touch Aster.
     foreach(var b in AnimationUtility.GetCurveBindings(clip)){
      // The FBX object root must retain the visual's normalized scale, facing
      // and deck offset. Animate only skeleton children, not the model object.
      if(b.type==typeof(Transform)&&(b.path==""||b.path=="Rig"||b.propertyName.StartsWith("m_LocalScale")||b.propertyName.StartsWith("m_LocalPosition")))AnimationUtility.SetEditorCurve(clip,b,null);
      else if(b.propertyName.StartsWith("RootT"))AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,0));
     }
     var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=state=="idle"||state=="walk"||state=="run";settings.keepOriginalPositionXZ=true;AnimationUtility.SetAnimationClipSettings(clip,settings);
     string output="Assets/Resources/Animations/"+clip.name+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,output);
    }
   }
   AssetDatabase.SaveAssets();Debug.Log("STORM_ASSET_BAKE_PASSED");
  }
 }
}
