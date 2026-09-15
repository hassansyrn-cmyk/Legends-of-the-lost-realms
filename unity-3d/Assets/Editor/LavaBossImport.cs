using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class LavaBossImport {
  const string Source="Assets/Art/LavaBoss/";
  const string Model=Source+"golem_from_glb_mixamo_ready.fbx";
  const string Output="Assets/Resources/Animations/LavaBoss/";
  static readonly string[] States={"idle","walk","run","attack_1","attack_2","attack_3","charged","jump","death","roar","flex","slash","overhead","cast"};
  static readonly string[] Files={"mutant breathing idle.fbx","Mutant Walking (1).fbx","Unarmed Run Forward.fbx","Mutant Swiping.fbx","Standing Melee Attack Downward.fbx","Jump Attack (2).fbx","Great Sword Jump Attack.fbx","Mutant Jumping.fbx","Dying (1).fbx","mutant roaring.fbx","Mutant Flexing Muscles (2).fbx","Great Sword Slash.fbx","Great Sword Attack.fbx","mutant idle.fbx"};
  [MenuItem("Lost Realms/Lava Boss/Prepare supplied Mixamo boss")]
  public static void Prepare(){
   Directory.CreateDirectory(Output);Directory.CreateDirectory("Assets/Art/LavaBoss/Textures");AssetDatabase.Refresh();
   Configure(Model,null);
   var avatar=AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Avatar>().FirstOrDefault(a=>a.isHuman&&a.isValid);
   if(!avatar)throw new Exception("Lava boss has no valid humanoid avatar");
   var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
   importer.ExtractTextures("Assets/Art/LavaBoss/Textures");AssetDatabase.Refresh();
   var report=new StringBuilder("Lava boss import\nValid humanoid avatar: "+avatar.name+"\n");
   float rootY=0;Quaternion rootQ=Quaternion.identity;
   for(int i=0;i<States.Length;i++){
    string path=Source+Files[i];Configure(path,avatar);
    var mi=(ModelImporter)AssetImporter.GetAtPath(path);var specs=mi.defaultClipAnimations;
    foreach(var spec in specs){spec.loopTime=i<3;spec.keepOriginalOrientation=true;spec.lockRootRotation=true;spec.keepOriginalPositionY=true;spec.lockRootHeightY=true;spec.keepOriginalPositionXZ=true;spec.lockRootPositionXZ=true;}
    mi.clipAnimations=specs;mi.SaveAndReimport();
    var imported=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
    if(!imported||!imported.isHumanMotion)throw new Exception("Missing humanoid clip: "+path);
    var clip=UnityEngine.Object.Instantiate(imported);clip.name=States[i];
    if(i==0){foreach(var b in AnimationUtility.GetCurveBindings(clip)){var curve=AnimationUtility.GetEditorCurve(clip,b);if(b.propertyName=="RootT.y")rootY=curve.Evaluate(0);if(b.propertyName=="RootQ.x")rootQ.x=curve.Evaluate(0);if(b.propertyName=="RootQ.y")rootQ.y=curve.Evaluate(0);if(b.propertyName=="RootQ.z")rootQ.z=curve.Evaluate(0);if(b.propertyName=="RootQ.w")rootQ.w=curve.Evaluate(0);}}
    foreach(var b in AnimationUtility.GetCurveBindings(clip)){
     string p=b.propertyName;float value=0;
     if(p.StartsWith("RootT")){value=p=="RootT.y"?rootY:0;AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,value));}
     if(p.StartsWith("RootQ")){value=p=="RootQ.x"?rootQ.x:p=="RootQ.y"?rootQ.y:p=="RootQ.z"?rootQ.z:rootQ.w;AnimationUtility.SetEditorCurve(clip,b,AnimationCurve.Constant(0,clip.length,value));}
    }
    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=i<3;AnimationUtility.SetAnimationClipSettings(clip,settings);
    string output=Output+States[i]+".anim";var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(output);
    if(existing){EditorUtility.CopySerialized(clip,existing);UnityEngine.Object.DestroyImmediate(clip);}else AssetDatabase.CreateAsset(clip,output);
    report.AppendLine(States[i]+" <- "+Files[i]+" | "+imported.length.ToString("F3")+"s");
   }
   var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
   try{
    model.name="LavaBoss";var a=model.GetComponent<Animator>();if(!a)a=model.AddComponent<Animator>();a.avatar=avatar;a.applyRootMotion=false;
    var textures=AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Art/LavaBoss"}).Select(g=>AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
    report.AppendLine("Textures: "+string.Join(", ",textures.Select(t=>t.name)));
    foreach(var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()){
     report.AppendLine("Mesh: "+r.sharedMesh.name+" vertices="+r.sharedMesh.vertexCount+" bones="+r.bones.Length);
     var mats=r.sharedMaterials;
     for(int m=0;m<mats.Length;m++){
      var original=mats[m];var material=new Material(Shader.Find("Standard")){name="LavaBoss_"+m};
      var texture=original?original.mainTexture:null;if(!texture)texture=textures.FirstOrDefault(t=>!t.name.ToLowerInvariant().Contains("normal"));
      material.mainTexture=texture;material.color=Color.white;material.SetFloat("_Glossiness",.2f);
      string mp="Assets/Resources/Materials/LavaBoss_"+m+".mat";var old=AssetDatabase.LoadAssetAtPath<Material>(mp);if(old){EditorUtility.CopySerialized(material,old);UnityEngine.Object.DestroyImmediate(material);material=old;}else AssetDatabase.CreateAsset(material,mp);mats[m]=material;
     }r.sharedMaterials=mats;
    }
    PrefabUtility.SaveAsPrefabAsset(model,"Assets/Resources/Characters/LavaBoss.prefab");
   }finally{UnityEngine.Object.DestroyImmediate(model);}
   RefreshMaterials();AssetDatabase.SaveAssets();File.WriteAllText("Validation/lava-boss-import.txt",report.ToString());Debug.Log("LAVA_BOSS_IMPORT_PASSED\n"+report);PropCensus.Run();
  }
  public static void RefreshMaterials(){
   const string path="Assets/Art/LavaBoss/Textures/golem_normal.png";
   var importer=AssetImporter.GetAtPath(path) as TextureImporter;
   if(importer){importer.textureType=TextureImporterType.NormalMap;importer.maxTextureSize=1024;importer.SaveAndReimport();}
   var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/LavaBoss_0.mat");
   var normal=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
   if(material&&normal){material.SetTexture("_BumpMap",normal);material.EnableKeyword("_NORMALMAP");material.SetFloat("_BumpScale",.65f);EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();}
  }
  static void Configure(string path,Avatar avatar){
   var mi=(ModelImporter)AssetImporter.GetAtPath(path);mi.animationType=ModelImporterAnimationType.Human;mi.avatarSetup=avatar?ModelImporterAvatarSetup.CopyFromOther:ModelImporterAvatarSetup.CreateFromThisModel;mi.sourceAvatar=avatar;mi.importAnimation=true;mi.importCameras=false;mi.importLights=false;mi.optimizeGameObjects=false;mi.animationCompression=ModelImporterAnimationCompression.Off;mi.SaveAndReimport();
  }
 }
}
