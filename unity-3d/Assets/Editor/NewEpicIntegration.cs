using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace LostRealms {
 public static class NewEpicIntegration {
  public static readonly string[] Roles={"IronGoblin","AshDemon","StoneBrute"};
  public static void Validate(){Prepare();WeaponIconProbe.RunEpicWeapons();QualityValidation.Run();}
  [MenuItem("Lost Realms/Assets/Prepare new epic weapons and enemies")]
  public static void Prepare(){PrepareRoles(Roles);}
  static void PrepareRoles(IEnumerable<string> roles){
   foreach(var role in roles){
    string path="Assets/Art/Enemies/NewEpic/"+role+".fbx";
    var imp=(ModelImporter)AssetImporter.GetAtPath(path);if(imp==null)throw new Exception("Missing "+path);
    if(imp.animationType!=ModelImporterAnimationType.Human){
    imp.animationType=ModelImporterAnimationType.Generic;imp.importAnimation=false;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.optimizeGameObjects=false;imp.importCameras=false;imp.importLights=false;imp.SaveAndReimport();
    var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var transforms=model.GetComponentsInChildren<Transform>(true);
    var map=new Dictionary<string,string>{{"CC_Base_Hip","Hips"},{"CC_Base_Waist","Spine"},{"CC_Base_Spine01","Chest"},{"CC_Base_Spine02","UpperChest"},{"CC_Base_NeckTwist01","Neck"},{"CC_Base_Head","Head"}};
    foreach(var side in new[]{"L","R"}){string full=side=="L"?"Left":"Right";foreach(var p in new[]{("Thigh","UpperLeg"),("Calf","LowerLeg"),("Foot","Foot"),("ToeBase","Toes"),("Clavicle","Shoulder"),("Upperarm","UpperArm"),("Forearm","LowerArm"),("Hand","Hand")})map["CC_Base_"+side+"_"+p.Item1]=full+p.Item2;}
    var human=transforms.Where(t=>map.ContainsKey(t.name)).Select(t=>new HumanBone{boneName=t.name,humanName=map[t.name],limit=new HumanLimit{useDefaultValues=true}}).ToArray();
    imp.humanDescription=new HumanDescription{human=human,skeleton=transforms.Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray(),upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f};
    imp.animationType=ModelImporterAnimationType.Human;imp.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;imp.SaveAndReimport();
    }
    var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault(a=>a.isHuman&&a.isValid);if(!avatar)throw new Exception("Invalid humanoid "+role);
    var obj=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));obj.name=role;
    var animator=obj.GetComponent<Animator>()??obj.AddComponent<Animator>();animator.avatar=avatar;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    string texpath="Assets/Resources/Characters/Textures/"+role+"_basecolor.png";
    var mat=new Material(Shader.Find("Standard")){name=role,color=Color.white,mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texpath)};if(!mat.mainTexture)throw new Exception("Missing texture "+role);mat.SetFloat("_Glossiness",.2f);
    string normalPath="Assets/Resources/Characters/Textures/"+role+"_normal.png";if(File.Exists(normalPath)){var ti=(TextureImporter)AssetImporter.GetAtPath(normalPath);ti.textureType=TextureImporterType.NormalMap;ti.SaveAndReimport();mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));mat.EnableKeyword("_NORMALMAP");}
    string matpath="Assets/Resources/Characters/Materials/"+role+".mat";if(AssetDatabase.LoadAssetAtPath<Material>(matpath))AssetDatabase.DeleteAsset(matpath);AssetDatabase.CreateAsset(mat,matpath);
    foreach(var r in obj.GetComponentsInChildren<SkinnedMeshRenderer>()){OptimizeMesh(role,r);r.sharedMaterial=mat;r.updateWhenOffscreen=true;}
    foreach(var st in new[]{"idle","walk","run","attack","hit","death"}){
     string source=st=="attack"?"Assets/Resources/Animations/Aster/unarmed_2.anim":st=="run"||st=="hit"?"Assets/Resources/Animations/Aster/"+st+".anim":"Assets/Resources/Animations/Shared/"+st+".anim";
     var clip=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<AnimationClip>(source));if(!clip||!clip.isHumanMotion)throw new Exception("Not humanoid "+source);clip.name=role+"_"+st;
     foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.type==typeof(Animator)&&binding.propertyName.StartsWith("RootT."))AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,binding.propertyName=="RootT.y"?.84f:0));
     var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=st=="idle"||st=="walk"||st=="run";AnimationUtility.SetAnimationClipSettings(clip,settings);
     string dst="Assets/Resources/Animations/"+role+"_"+st+".anim";if(AssetDatabase.LoadAssetAtPath<AnimationClip>(dst))AssetDatabase.DeleteAsset(dst);AssetDatabase.CreateAsset(clip,dst);
    }
    PrefabUtility.SaveAsPrefabAsset(obj,"Assets/Resources/Characters/"+role+".prefab");UnityEngine.Object.DestroyImmediate(obj);Debug.Log("NEW_ENEMY_PREPARED "+role+" avatar="+avatar.isValid);
   }
   foreach(var name in new[]{"Duskblade","Soulreaper"}){var imp=(ModelImporter)AssetImporter.GetAtPath("Assets/Resources/Weapons/"+name+".fbx");imp.importAnimation=false;imp.materialImportMode=ModelImporterMaterialImportMode.None;imp.SaveAndReimport();}
   AssetDatabase.SaveAssets();Debug.Log("NEW_EPIC_PREPARED");
  }
  public static void PrepareDesertGuardian(){PrepareGuardianClips();QualityValidation.Run();}
  static void PrepareGuardianClips(){
   foreach(var pair in new[]{("attack","unarmed_2"),("attack_2","unarmed_2"),("victory","idle"),("gethit","hit"),("cast","unarmed_2")}){
    var source=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Animations/Aster/"+pair.Item2+".anim");
    var clip=UnityEngine.Object.Instantiate(source);clip.name="StoneBrute_"+pair.Item1;
    foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.type==typeof(Animator)&&binding.propertyName.StartsWith("RootT."))AnimationUtility.SetEditorCurve(clip,binding,AnimationCurve.Constant(0,clip.length,binding.propertyName=="RootT.y"?.84f:0));
    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=false;settings.mirror=pair.Item1=="attack_2";AnimationUtility.SetAnimationClipSettings(clip,settings);
    string path="Assets/Resources/Animations/"+clip.name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
    if(old){EditorUtility.CopySerialized(clip,old);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,path);
   }
   AssetDatabase.SaveAssets();
   GuardianPoseSetup.Bake();
  }
  public static void PreparePolish(){
   PrepareRoles(new[]{"StoneBrute"});PrepareGuardianClips();
   var importer=(ModelImporter)AssetImporter.GetAtPath("Assets/Resources/Weapons/Soulreaper.fbx");importer.isReadable=true;importer.SaveAndReimport();
   var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Weapons/Soulreaper.fbx");var mesh=model.GetComponentInChildren<MeshFilter>().sharedMesh;
   var report=new System.Text.StringBuilder();for(int k=0;k<10;k++){float y=-.5f+(k+.5f)*.1f;var points=mesh.vertices.Where(v=>Mathf.Abs(v.y-y)<.025f).ToArray();if(points.Length>0)report.AppendLine(y+" center="+points.Aggregate(Vector3.zero,(sum,v)=>sum+v)/points.Length);}
   File.WriteAllText("../Temp/opencode/soulreaper-handle.txt",report.ToString());
  }
  static void OptimizeMesh(string role,SkinnedMeshRenderer target){
   var reference=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Enemies/NewEpic/"+role+"_Optimized.fbx");
   var preview=UnityEngine.Object.Instantiate(reference);var source=preview.GetComponentInChildren<SkinnedMeshRenderer>();if(!source)throw new Exception("Missing optimized skin "+role);
   var original=source.sharedMesh;
   var mesh=new Mesh{name=role+" mobile mesh",indexFormat=original.indexFormat,vertices=original.vertices,normals=original.normals,tangents=original.tangents,uv=original.uv,uv2=original.uv2,colors32=original.colors32,boneWeights=original.boneWeights};
   mesh.subMeshCount=original.subMeshCount;for(int i=0;i<original.subMeshCount;i++)mesh.SetTriangles(original.GetTriangles(i),i);
   var vertices=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;
   var from=source.bounds;var to=target.bounds;
   for(int i=0;i<vertices.Length;i++){
    var world=source.transform.TransformPoint(vertices[i]);
    world=to.center+Vector3.Scale(world-from.center,new Vector3(to.size.x/from.size.x,to.size.y/from.size.y,to.size.z/from.size.z));
    vertices[i]=target.transform.InverseTransformPoint(world);
    normals[i]=target.transform.InverseTransformDirection(source.transform.TransformDirection(normals[i])).normalized;
    if(tangents.Length==vertices.Length){var n=target.transform.InverseTransformDirection(source.transform.TransformDirection(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z))).normalized;tangents[i]=new Vector4(n.x,n.y,n.z,tangents[i].w);}
   }
   var indices=source.bones.Select(b=>Array.FindIndex(target.bones,t=>t.name==b.name)).ToArray();
   var weights=mesh.boneWeights;
   int Map(int index,float weight){if(indices[index]<0&&weight>.00001f)throw new Exception("Optimized weighted bone missing "+role+" "+source.bones[index].name);return Mathf.Max(0,indices[index]);}
   for(int i=0;i<weights.Length;i++){var w=weights[i];w.boneIndex0=Map(w.boneIndex0,w.weight0);w.boneIndex1=Map(w.boneIndex1,w.weight1);w.boneIndex2=Map(w.boneIndex2,w.weight2);w.boneIndex3=Map(w.boneIndex3,w.weight3);weights[i]=w;}
   mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=tangents;mesh.boneWeights=weights;mesh.bindposes=target.sharedMesh.bindposes;mesh.RecalculateBounds();mesh.UploadMeshData(false);
   string path="Assets/Art/Enemies/NewEpic/"+role+"_Mobile.asset";if(AssetDatabase.LoadAssetAtPath<Mesh>(path))AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);target.sharedMesh=mesh;target.localBounds=mesh.bounds;
   Debug.Log("OPTIMIZED_SKIN "+role+" vertices="+mesh.vertexCount+" triangles="+mesh.triangles.Length/3);
   UnityEngine.Object.DestroyImmediate(preview);
  }
 }
}
