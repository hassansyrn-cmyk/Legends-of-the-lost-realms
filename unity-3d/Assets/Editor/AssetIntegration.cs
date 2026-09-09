using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
public static class AssetIntegration {
 public static void Run(){
  AsterPhase1.Prepare();Directory.CreateDirectory("Assets/Resources/Characters");Directory.CreateDirectory("Assets/Art/Materials");Directory.CreateDirectory("Assets/Resources/Materials");var report=new List<string>{"Aster: supplied Mixamo character prepared by AsterPhase1"};
  foreach(string role in new[]{"Goblin","Elemental","Demon","Heartwood","Sunscar","Whiteout","Caster","Frost"}){
   string modelRole=role=="Heartwood"?"Elemental":role=="Whiteout"||role=="Frost"?"IceGuardian":role;string path="Assets/Art/Models/"+modelRole+".fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception("Missing source "+path);
   var before=AssetDatabase.LoadAssetAtPath<GameObject>(path);bool skinned=before.GetComponentsInChildren<SkinnedMeshRenderer>().Length>0;bool mixamo=before.GetComponentsInChildren<Transform>().Any(t=>t.name.Contains("mixamorig"));var desc=skinned&&!mixamo?HumanoidRig.Description(before):new HumanDescription();
   importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Low;importer.animationType=skinned?ModelImporterAnimationType.Human:ModelImporterAnimationType.None;if(skinned)importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;if(skinned&&!mixamo)importer.humanDescription=desc;importer.SaveAndReimport();
   var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);var importedAnimator=original.GetComponent<Animator>();var avatar=importedAnimator?importedAnimator.avatar:null;if(skinned&&(!avatar||!avatar.isHuman||!avatar.isValid))throw new Exception("Invalid humanoid rig: "+role);
   var root=new GameObject(role);var model=(GameObject)PrefabUtility.InstantiatePrefab(original);model.transform.SetParent(root.transform,false);
   var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);float height=role=="Aster"?1.8f:role=="Goblin"?1.65f:role=="Demon"?1.9f:role=="Elemental"?2.5f:role=="Caster"?2f:role=="Frost"?1.8f:3.6f;float scale=height/Mathf.Max(.01f,bounds.size.y);model.transform.localScale*=scale;model.transform.localPosition-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z)*scale;
   var mat=new Material(Shader.Find("Standard")){name=role,color=Color.white};mat.SetFloat("_Glossiness",.17f);var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/"+modelRole+"_1.jpg");if(!tex)throw new Exception("Missing albedo: "+role);mat.mainTexture=tex;
   var normalPath="Assets/Art/Textures/"+modelRole+"_0.jpg";var normalImporter=AssetImporter.GetAtPath(normalPath) as TextureImporter;if(normalImporter){normalImporter.textureType=TextureImporterType.NormalMap;normalImporter.maxTextureSize=1024;normalImporter.SaveAndReimport();mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mat.EnableKeyword("_NORMALMAP");}
  var texImporter=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(tex));texImporter.maxTextureSize=role=="Aster"?2048:1024;texImporter.textureCompression=TextureImporterCompression.Compressed;texImporter.SaveAndReimport();string matPath=role=="Aster"?"Assets/Resources/Materials/Aster.mat":"Assets/Art/Materials/"+role+".mat";var existingMat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(existingMat){EditorUtility.CopySerialized(mat,existingMat);UnityEngine.Object.DestroyImmediate(mat);mat=existingMat;}else AssetDatabase.CreateAsset(mat,matPath);foreach(var r in renderers){r.sharedMaterials=r.sharedMaterials.Select(_=>mat).ToArray();if(r is SkinnedMeshRenderer skin)skin.updateWhenOffscreen=false;}
   var animator=model.GetComponent<Animator>();if(animator){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}
   var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.025f,renderers)});lod.RecalculateBounds();PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Characters/"+role+".prefab");report.Add(role+": humanoid="+skinned+", height="+height+", textured=true");UnityEngine.Object.DestroyImmediate(root);
  }
  File.WriteAllLines("MODEL_INTEGRATION.txt",report);AssetDatabase.SaveAssets();RealmBuild.Prepare();Debug.Log("MODEL_INTEGRATION_PASSED");
 }
}


