using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class AsterSkinsBuilder {
  public struct SkinInfo {
   public string Key;
   public string DisplayName;
   public string FbxPath;
   public string DiffusePath;
   public string NormalPath;
   public string MatPath;
   public string PrefabPath;

   public SkinInfo(string key, string displayName, string fbxName, string diffuseName, string normalName) {
    Key = key;
    DisplayName = displayName;
    FbxPath = "Assets/Art/Models/Skins/" + fbxName;
    DiffusePath = "Assets/Art/Textures/Skins/" + diffuseName;
    NormalPath = "Assets/Art/Textures/Skins/" + normalName;
    MatPath = "Assets/Resources/Materials/" + key + ".mat";
    PrefabPath = "Assets/Resources/Characters/" + key + ".prefab";
   }
  }

  public static readonly SkinInfo[] Skins = new[] {
   new SkinInfo("Aster_Assassin", "Shadow Assassin", "Aster_Assassin.fbx", "Aster_Assassin_Diffuse.jpg", "Aster_Assassin_Normal.png"),
   new SkinInfo("Aster_Knight", "Royal Knight", "Aster_Knight.fbx", "Aster_Knight_Diffuse.jpg", "Aster_Knight_Normal.png"),
   new SkinInfo("Aster_KnightArmored", "Armored Juggernaut", "Aster_KnightArmored.fbx", "Aster_KnightArmored_Diffuse.jpg", "Aster_KnightArmored_Normal.png"),
   new SkinInfo("Aster_Viking", "Valhalla Viking", "Aster_Viking.fbx", "Aster_Viking_Diffuse.jpg", "Aster_Viking_Normal.png")
  };

  [MenuItem("Lost Realms/Skins/Build All Aster Skins")]
  public static void BuildAll() {
   Debug.Log("=== BUILDING ASTER SKINS ===");
   EnsureDirectories();

   // Baseline root offset from Aster base idle
   float baselineRootY = 0f;
   var asterClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Resources/Animations/Aster/idle.anim");
   if (asterClip) {
    // idle is normalized to 0 in baked anims; baselineRootY baked in Aster.prefab is ~1.0546f
    baselineRootY = 1.0546f;
   }

   foreach (var skin in Skins) {
    Debug.Log($"Processing skin: {skin.DisplayName} ({skin.Key})");
    PrepareTexture(skin.DiffusePath, false, 2048);
    PrepareTexture(skin.NormalPath, true, 2048);

    ConfigureFBX(skin.FbxPath);

    var mat = CreateOrUpdateMaterial(skin);
    BuildSkinPrefab(skin, mat, baselineRootY);
   }

   AssetDatabase.SaveAssets();
   AssetDatabase.Refresh();
   Debug.Log("SKINS_BUILD_COMPLETED_SUCCESSFULLY");
  }

  static void EnsureDirectories() {
   EnsureFolder("Assets/Resources/Materials");
   EnsureFolder("Assets/Resources/Characters");
  }

  static void EnsureFolder(string path) {
   if (AssetDatabase.IsValidFolder(path)) return;
   string parent = Path.GetDirectoryName(path).Replace('\\', '/');
   EnsureFolder(parent);
   AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
  }

  static void PrepareTexture(string path, bool normalMap, int maxSize) {
   var importer = AssetImporter.GetAtPath(path) as TextureImporter;
   if (!importer) {
    Debug.LogWarning("Texture not found: " + path);
    return;
   }
   bool changed = false;
   var targetType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
   if (importer.textureType != targetType) { importer.textureType = targetType; changed = true; }
   if (importer.sRGBTexture == normalMap) { importer.sRGBTexture = !normalMap; changed = true; }
   if (importer.maxTextureSize != maxSize) { importer.maxTextureSize = maxSize; changed = true; }
   if (importer.textureCompression != TextureImporterCompression.Compressed) { importer.textureCompression = TextureImporterCompression.Compressed; changed = true; }
   if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
   if (changed) importer.SaveAndReimport();
  }

  static void ConfigureFBX(string path) {
   var importer = AssetImporter.GetAtPath(path) as ModelImporter;
   if (!importer) throw new Exception("Missing FBX model: " + path);
   importer.materialImportMode = ModelImporterMaterialImportMode.None;
   importer.importCameras = false;
   importer.importLights = false;
   importer.importBlendShapes = true;
   importer.isReadable = false;
   importer.meshCompression = ModelImporterMeshCompression.Low;
   importer.optimizeGameObjects = false;
   importer.globalScale = 1f;
   importer.useFileScale = true;
   importer.importAnimation = false;
   importer.animationType = ModelImporterAnimationType.Human;
   importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
   importer.SaveAndReimport();
  }

  static Material CreateOrUpdateMaterial(SkinInfo skin) {
   var mat = AssetDatabase.LoadAssetAtPath<Material>(skin.MatPath);
   if (!mat) {
    var shader = Shader.Find("Standard");
    if (!shader) shader = Shader.Find("Mobile/Diffuse");
    mat = new Material(shader);
    mat.name = skin.Key;
    AssetDatabase.CreateAsset(mat, skin.MatPath);
   }

   var diffuse = AssetDatabase.LoadAssetAtPath<Texture2D>(skin.DiffusePath);
   var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(skin.NormalPath);

   if (diffuse) mat.mainTexture = diffuse;
   if (normal) {
    mat.EnableKeyword("_NORMALMAP");
    mat.SetTexture("_BumpMap", normal);
   }
   EditorUtility.SetDirty(mat);
   return mat;
  }

  static void BuildSkinPrefab(SkinInfo skin, Material mat, float baselineRootY) {
   var sourceModel = AssetDatabase.LoadAssetAtPath<GameObject>(skin.FbxPath);
   if (!sourceModel) throw new Exception("Missing skin model: " + skin.FbxPath);

   var sourceAnimator = sourceModel.GetComponentInChildren<Animator>(true);
   var avatar = sourceAnimator ? sourceAnimator.avatar : null;
   if (!avatar || !avatar.isHuman || !avatar.isValid) {
    Debug.LogWarning($"Avatar for {skin.DisplayName} is not valid humanoid or auto-mapping failed; checking bones...");
   } else {
    Debug.Log($"Avatar for {skin.DisplayName} is valid Humanoid!");
   }

   var root = new GameObject(skin.Key);
   try {
    var model = (GameObject)PrefabUtility.InstantiatePrefab(sourceModel);
    model.name = skin.DisplayName + " Character";
    model.transform.SetParent(root.transform, false);

    var renderers = model.GetComponentsInChildren<Renderer>(true);
    if (renderers.Length == 0) throw new Exception($"Skin FBX {skin.FbxPath} contains no renderers.");

    foreach (var renderer in renderers) {
     renderer.sharedMaterials = Enumerable.Repeat(mat, Math.Max(1, renderer.sharedMaterials.Length)).ToArray();
     if (renderer is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = false;
    }

    var bounds = renderers[0].bounds;
    foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
    float scale = 1.8f / Mathf.Max(.01f, bounds.size.y);
    model.transform.localScale = Vector3.one * scale;
    model.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) * scale;
    model.transform.localPosition += Vector3.up * (baselineRootY * scale);

    var animator = model.GetComponentInChildren<Animator>(true);
    if (!animator) animator = model.AddComponent<Animator>();
    if (avatar) animator.avatar = avatar;
    animator.applyRootMotion = false;
    animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

    var lod = root.AddComponent<LODGroup>();
    lod.SetLODs(new[] { new LOD(.025f, renderers) });
    lod.RecalculateBounds();

    if (!PrefabUtility.SaveAsPrefabAsset(root, skin.PrefabPath)) {
     throw new Exception("Failed to save prefab: " + skin.PrefabPath);
    }
    Debug.Log($"Successfully built prefab: {skin.PrefabPath}");
   } finally {
    UnityEngine.Object.DestroyImmediate(root);
   }
  }
 }
}
