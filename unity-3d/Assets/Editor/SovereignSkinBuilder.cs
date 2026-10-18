using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LostRealms {
 // Only builds this outfit. Existing Aster models and generated clips are untouched.
 public static class SovereignSkinBuilder {
  const string ModelPath="Assets/Art/Models/Skins/Aster_Sovereign.fbx";
  const string TexturePath="Assets/Art/Textures/Skins/Aster_Sovereign_Diffuse.png";
  const string MaterialPath="Assets/Resources/Materials/Aster_Sovereign.mat";
  const string PrefabPath="Assets/Resources/Characters/Aster_Sovereign.prefab";
  const string IconPath="Assets/Resources/Characters/Icons/Aster_Sovereign.png";

  [MenuItem("Lost Realms/Skins/Build Aether Sovereign")]
  public static void Build(){
   AssetDatabase.Refresh();
   // Clean checkouts generate these through the existing Phase 1 menu baker.
   if(!Resources.Load<AnimationClip>("Animations/Aster/idle"))AsterPhase1.Prepare();
   var textureImporter=(TextureImporter)AssetImporter.GetAtPath(TexturePath);
   textureImporter.maxTextureSize=2048;textureImporter.mipmapEnabled=true;
   textureImporter.textureCompression=TextureImporterCompression.CompressedHQ;
   textureImporter.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_4x4,compressionQuality=100});
   textureImporter.SaveAndReimport();
   var importer=(ModelImporter)AssetImporter.GetAtPath(ModelPath);
   importer.materialImportMode=ModelImporterMaterialImportMode.None;
   importer.importCameras=false;importer.importLights=false;importer.importBlendShapes=false;
   importer.importAnimation=false;importer.isReadable=false;importer.meshCompression=ModelImporterMeshCompression.Off;
   importer.animationType=ModelImporterAnimationType.Human;
   importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
   importer.humanDescription=new HumanDescription{human=Array.Empty<HumanBone>(),skeleton=Array.Empty<SkeletonBone>(),upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f};
   importer.optimizeGameObjects=false;importer.SaveAndReimport();
   var source=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
   var sourceAnimator=source.GetComponentInChildren<Animator>();
   if(!sourceAnimator||!sourceAnimator.avatar||!sourceAnimator.avatar.isValid||!sourceAnimator.avatar.isHuman)
    throw new Exception("Sovereign requires a valid humanoid avatar.");
   var mat=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
   if(!mat){mat=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(mat,MaterialPath);}
   mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
   mat.SetFloat("_Metallic",.3f);mat.SetFloat("_Glossiness",.4f);EditorUtility.SetDirty(mat);
   var root=new GameObject("Aster_Sovereign");
   try{
    var model=(GameObject)PrefabUtility.InstantiatePrefab(source);model.transform.SetParent(root.transform,false);
    // Store only the reduced mesh and avatar in the runtime prefab. The supplied
    // high-detail FBX stays an editor source asset outside Resources.
    PrefabUtility.UnpackPrefabInstance(model,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
    foreach(var r in model.GetComponentsInChildren<Renderer>()){
     r.sharedMaterial=mat;
     if(r is SkinnedMeshRenderer skin)skin.sharedMesh=BuildMobileMesh(skin.sharedMesh,60000,"Mobile");
    }
    var animator=model.GetComponentInChildren<Animator>();animator.applyRootMotion=false;
    var avatar=UnityEngine.Object.Instantiate(animator.avatar);avatar.name="Aster_Sovereign_Avatar";
    animator.avatar=SaveGenerated(avatar,"Assets/Art/Models/Skins/Aster_Sovereign_Avatar.asset");
    animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
    Sample(animator,Resources.Load<AnimationClip>("Animations/Aster/idle"),0);
    var bounds=PoseBounds(root);float scale=1.8f/bounds.size.y;
    model.transform.localScale*=scale;
    Sample(animator,Resources.Load<AnimationClip>("Animations/Aster/idle"),0);
    bounds=PoseBounds(root);
    model.transform.position+=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z);
    animator.Rebind();animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
    var renderers=root.GetComponentsInChildren<Renderer>();
    var distant=new System.Collections.Generic.List<Renderer>();
    foreach(var high in renderers)if(high is SkinnedMeshRenderer skin){
     var lowObject=new GameObject("Sovereign distance mesh");lowObject.transform.SetParent(skin.transform.parent,false);
     lowObject.transform.localPosition=skin.transform.localPosition;lowObject.transform.localRotation=skin.transform.localRotation;lowObject.transform.localScale=skin.transform.localScale;
     var low=lowObject.AddComponent<SkinnedMeshRenderer>();low.sharedMesh=BuildMobileMesh(skin.sharedMesh,20000,"Distance");
     low.bones=skin.bones;low.rootBone=skin.rootBone;low.sharedMaterials=skin.sharedMaterials;low.localBounds=skin.localBounds;low.quality=skin.quality;
     distant.Add(low);
    }
    var lod=root.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.12f,renderers),new LOD(.025f,distant.ToArray())});lod.RecalculateBounds();
    if(!PrefabUtility.SaveAsPrefabAsset(root,PrefabPath))throw new Exception("Could not save Sovereign prefab");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
   AssetDatabase.SaveAssets();BakePortrait();Validate();
  }

  static T SaveGenerated<T>(T value,string path) where T:UnityEngine.Object{
   var existing=AssetDatabase.LoadAssetAtPath<T>(path);
   if(existing){
    if(existing is Mesh oldMesh)oldMesh.Clear(false);
    EditorUtility.CopySerialized(value,existing);
    if(existing is Mesh updatedMesh)updatedMesh.UploadMeshData(false);
    UnityEngine.Object.DestroyImmediate(value);EditorUtility.SetDirty(existing);return existing;
   }
   AssetDatabase.CreateAsset(value,path);return value;
  }

  static Mesh BuildMobileMesh(Mesh source,int targetTriangles,string suffix){
   var input=UnityEngine.Object.Instantiate(source);
   // Normalize coordinates for the simplifier's error tolerance, then restore
   // them so the original bind poses and bone weights remain compatible.
   float unit=Mathf.Max(.00001f,input.bounds.size.y);var vertices=input.vertices;
   for(int i=0;i<vertices.Length;i++)vertices[i]/=unit;input.vertices=vertices;input.RecalculateBounds();
   var simplifier=new UnityMeshSimplifier.MeshSimplifier();simplifier.Initialize(input);
   simplifier.SimplifyMesh(Mathf.Min(1f,targetTriangles/(float)(source.triangles.Length/3)));
   var mesh=simplifier.ToMesh();vertices=mesh.vertices;
   for(int i=0;i<vertices.Length;i++)vertices[i]*=unit;mesh.vertices=vertices;mesh.RecalculateBounds();
   mesh.name="Aster_Sovereign_"+suffix;UnityEngine.Object.DestroyImmediate(input);
   return SaveGenerated(mesh,"Assets/Art/Models/Skins/Aster_Sovereign_"+suffix+".asset");
  }

  public static void Sample(Animator animator,AnimationClip clip,float time){
   if(!clip)throw new Exception("Missing Aster animation");
   animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   var graph=PlayableGraph.Create("Sovereign validation pose");
   try{
    graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
    var play=AnimationClipPlayable.Create(graph,clip);play.SetApplyFootIK(false);play.SetTime(time);
    var output=AnimationPlayableOutput.Create(graph,"Pose",animator);output.SetSourcePlayable(play);
    graph.Play();graph.Evaluate(0);
   }finally{graph.Destroy();}
  }

  // BakeMesh measures the posed geometry rather than stale bind-pose renderer bounds.
  public static Bounds PoseBounds(GameObject root){
   Bounds bounds=default;bool first=true;
   foreach(var r in root.GetComponentsInChildren<SkinnedMeshRenderer>()){
    var mesh=new Mesh();r.BakeMesh(mesh,true);
    foreach(var vertex in mesh.vertices){var p=r.transform.TransformPoint(vertex);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
    UnityEngine.Object.DestroyImmediate(mesh);
   }
   if(first)throw new Exception("No skinned mesh in Sovereign outfit");
   return bounds;
  }

  static void BakePortrait(){
   var preview=new PreviewRenderUtility();GameObject model=null;
   try{
    model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));preview.AddSingleGO(model);
    model.transform.rotation=Quaternion.Euler(0,-18,0);
    Sample(model.GetComponentInChildren<Animator>(),Resources.Load<AnimationClip>("Animations/Aster/idle"),.3f);
    var bounds=PoseBounds(model);var camera=preview.camera;
    camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=20;
    camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
    camera.transform.position=bounds.center+Vector3.forward*4;camera.transform.LookAt(bounds.center);
    camera.orthographicSize=Mathf.Max(bounds.extents.y,bounds.extents.x)*1.13f;
    preview.lights[0].intensity=1.3f;preview.lights[0].transform.rotation=Quaternion.Euler(30,-35,0);
    preview.lights[1].intensity=.8f;preview.lights[1].transform.rotation=Quaternion.Euler(10,150,0);
    var frame=new RenderTexture(512,512,24,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;
    var image=new Texture2D(512,512,TextureFormat.RGBA32,false);
    try{
     camera.targetTexture=frame;camera.Render();RenderTexture.active=frame;
     image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes(IconPath,image.EncodeToPNG());
     Directory.CreateDirectory("Validation/PremiumSkin");File.WriteAllBytes("Validation/PremiumSkin/portrait.png",image.EncodeToPNG());
    }finally{RenderTexture.active=previous;camera.targetTexture=null;frame.Release();UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(image);}
   }finally{if(model)UnityEngine.Object.DestroyImmediate(model);preview.Cleanup();}
   AssetDatabase.ImportAsset(IconPath);
   var importer=(TextureImporter)AssetImporter.GetAtPath(IconPath);importer.alphaIsTransparency=true;
   importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
  }

  public static void Validate(){
   var model=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/Aster_Sovereign"));
   var report=new System.Text.StringBuilder();
   try{
    var animator=model.GetComponentInChildren<Animator>();
    if(!animator||!animator.avatar.isValid||!animator.avatar.isHuman)throw new Exception("Invalid avatar");
    if(!animator.GetBoneTransform(HumanBodyBones.RightHand))throw new Exception("Weapon hand missing");
    int triangles=model.GetComponentsInChildren<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3);
    var lods=model.GetComponent<LODGroup>().GetLODs();
    int nearTriangles=lods[0].renderers.OfType<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3);
    int farTriangles=lods[1].renderers.OfType<SkinnedMeshRenderer>().Sum(r=>r.sharedMesh.triangles.Length/3);
    if(nearTriangles>65000||farTriangles>25000)throw new Exception("Skin LOD exceeds mobile budgets");
    report.AppendLine("Detailed near LOD="+nearTriangles+" triangles; distance LOD="+farTriangles+" triangles; texture max=2048 ASTC 4x4 on Android");
    if(AssetDatabase.GetDependencies(PrefabPath,true).Contains(ModelPath))throw new Exception("Runtime prefab still depends on high-detail source FBX");
    report.AppendLine("Valid humanoid, right-hand weapon socket, triangles="+triangles);
    foreach(string state in new[]{"idle","walk","run","attack_1","roll","double_jump","cast"}){
     var clip=Resources.Load<AnimationClip>("Animations/Aster/"+state);
     Sample(animator,clip,clip.length*.3f);var bounds=PoseBounds(model);
     if(bounds.size.y<.6f||bounds.size.y>3.5f||bounds.center.magnitude>3f)throw new Exception("Invalid pose "+state+": "+bounds);
     report.AppendLine(state+" posed bounds="+bounds);
     if(state=="walk"||state=="run"||state=="attack_1"){
      var hand=animator.GetBoneTransform(HumanBodyBones.RightHand);var foot=animator.GetBoneTransform(HumanBodyBones.LeftFoot);
      var handPosition=hand.position;var footPosition=foot.position;
      Sample(animator,clip,clip.length*.7f);
      float motion=Vector3.Distance(handPosition,hand.position)+Vector3.Distance(footPosition,foot.position);
      if(motion<.01f)throw new Exception("Static humanoid animation: "+state);
      report.AppendLine(state+" hand/foot motion="+motion);
     }
    }
    Sample(animator,Resources.Load<AnimationClip>("Animations/Aster/idle"),0);
    if(Mathf.Abs(PoseBounds(model).min.y)>.08f)throw new Exception("Idle feet not grounded: "+PoseBounds(model));
    foreach(string path in Directory.GetFiles("Assets/Resources/Animations/Aster","*.anim")){
     var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
     foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(binding.propertyName.StartsWith("RootT."))
      foreach(var key in AnimationUtility.GetEditorCurve(clip,binding).keys)if(Mathf.Abs(key.value)>.00001f)throw new Exception("Aster root translation regression: "+path);
    }
    var portrait=new Texture2D(2,2);portrait.LoadImage(File.ReadAllBytes(IconPath));int visible=0,magenta=0;
    foreach(var pixel in portrait.GetPixels32())if(pixel.a>32){visible++;if(pixel.r>220&&pixel.b>220&&pixel.g<35)magenta++;}
    UnityEngine.Object.DestroyImmediate(portrait);
    if(visible<5000||magenta>visible*.01f)throw new Exception("Blank or magenta Sovereign portrait");
    report.AppendLine("Portrait pixels="+visible+", magenta="+magenta+"; all Aster root translation curves remain zero; source FBX excluded from runtime dependencies");
    SovereignSkinChecks.Run();report.AppendLine("SOVEREIGN_SKIN_VALIDATION_PASSED");
    Directory.CreateDirectory("Validation/PremiumSkin");File.WriteAllText("Validation/PremiumSkin/results.txt",report.ToString());Debug.Log(report.ToString());
   }finally{UnityEngine.Object.DestroyImmediate(model);}
  }
 }
}
