using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class EnemyPackIntegration {
  const string ArtSource = "Assets/Art/EnemyPacks/";
  const string AnimOutput = "Assets/Resources/Animations/";
  const string CharOutput = "Assets/Resources/Characters/";

  struct RoleSpec {
   public string role;
   public string modelFile;
   public (string state, string takeFile, bool loop)[] takes;
   public RoleSpec(string r, string m, (string, string, bool)[] t) {
    role = r; modelFile = m; takes = t;
   }
  }

  static readonly RoleSpec[] Specs = new[] {
   new RoleSpec("Bomber", "Bomber_model.fbx", new[] {
    ("idle", "Bomber_idle.fbx", true),
    ("walk", "Bomber_walk.fbx", true),
    ("run", "Bomber_run.fbx", true),
    ("attack", "Bomber_attack.fbx", false),
    ("death", "Bomber_death.fbx", false)
   }),
   new RoleSpec("Summoner", "Summoner_model.fbx", new[] {
    ("idle", "Summoner_idle.fbx", true),
    ("walk", "Summoner_walk.fbx", true),
    ("attack", "Summoner_attack.fbx", false),
    ("death", "Summoner_death.fbx", false)
   }),
   new RoleSpec("Elite", "Elite_model.fbx", new[] {
    ("idle", "Elite_idle.fbx", true),
    ("walk", "Elite_walk.fbx", true),
    ("run", "Elite_run.fbx", true),
    ("attack", "Elite_attack.fbx", false),
    ("death", "Elite_death.fbx", false)
   }),
   new RoleSpec("Demon", "Demon_model.fbx", new[] {
    ("idle", "Demon_idle.fbx", true),
    ("walk", "Demon_walk.fbx", true),
    ("run", "Demon_run.fbx", true),
    ("attack", "Demon_attack.fbx", false),
    ("death", "Demon_death.fbx", false)
   }),
   new RoleSpec("Goblin", "Goblin_model.fbx", new[] {
    ("idle", "Goblin_idle.fbx", true),
    ("walk", "Goblin_walk.fbx", true),
    ("run", "Goblin_run.fbx", true),
    ("attack", "Goblin_attack.fbx", false),
    ("death", "Goblin_death.fbx", false)
   }),
   new RoleSpec("Whiteout", "Whiteout_model.fbx", new[] {
    ("idle", "Whiteout_idle.fbx", true),
    ("walk", "Whiteout_walk.fbx", true),
    ("run", "Whiteout_run.fbx", true),
    ("attack", "Whiteout_attack.fbx", false),
    ("death", "Whiteout_death.fbx", false)
   }),
   new RoleSpec("Sunscar", "Sunscar_model.fbx", new[] {
    ("idle", "Sunscar_idle.fbx", true),
    ("walk", "Sunscar_walk.fbx", true),
    ("run", "Sunscar_run.fbx", true),
    ("attack", "Sunscar_attack.fbx", false),
    ("death", "Sunscar_death.fbx", false)
   })
  };

  [MenuItem("Lost Realms/Enemies/Integrate New Mixamo Enemy Packs")]
  public static void IntegrateAll() {
   var report = new StringBuilder("=== ENEMY PACK INTEGRATION ===\n");
   Directory.CreateDirectory(AnimOutput);
   Directory.CreateDirectory(CharOutput);
   AssetDatabase.Refresh();

   foreach (var spec in Specs) {
    string modelPath = ArtSource + spec.modelFile;
    if (!File.Exists(modelPath)) {
     report.AppendLine("SKIP " + spec.role + ": missing " + modelPath);
     continue;
    }

    ConfigureModel(modelPath, null);
    var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(a => a.isValid);
    if (!avatar) {
     report.AppendLine("ERROR " + spec.role + ": failed to create avatar from " + modelPath);
     continue;
    }
    report.AppendLine("Role: " + spec.role + " | Avatar: " + avatar.name + " (human=" + avatar.isHuman + ", valid=" + avatar.isValid + ")");

    foreach (var take in spec.takes) {
     string state = take.state;
     string takeFile = take.takeFile;
     bool loop = take.loop;
     string takePath = ArtSource + takeFile;
     if (!File.Exists(takePath)) {
      report.AppendLine("  Missing take: " + takeFile);
      continue;
     }

     ConfigureTake(takePath, avatar);
     var importedClip = AssetDatabase.LoadAllAssetsAtPath(takePath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
     if (!importedClip) {
      report.AppendLine("  No clip imported from " + takeFile);
      continue;
     }

     var baked = UnityEngine.Object.Instantiate(importedClip);
     baked.name = spec.role + "_" + state;

     foreach (var b in AnimationUtility.GetCurveBindings(baked)) {
      string p = b.propertyName;
      if (p.StartsWith("RootT") || p.StartsWith("RootQ")) {
       AnimationUtility.SetEditorCurve(baked, b, AnimationCurve.Constant(0, baked.length, 0f));
      }
     }

     var settings = AnimationUtility.GetAnimationClipSettings(baked);
     settings.loopTime = loop;
     AnimationUtility.SetAnimationClipSettings(baked, settings);

     string clipPath = AnimOutput + spec.role + "_" + state + ".anim";
     var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
     if (existing) {
      EditorUtility.CopySerialized(baked, existing);
      UnityEngine.Object.DestroyImmediate(baked);
     } else {
      AssetDatabase.CreateAsset(baked, clipPath);
     }
     report.AppendLine("  Bake: " + spec.role + "_" + state + " <- " + takeFile + " (" + importedClip.length.ToString("F2") + "s, loop=" + loop + ")");
    }

    BuildRolePrefab(spec.role, modelPath, avatar, report);
   }

   BuildFlyerPrefab(report);
   RepairFootman(report);
   RepairSkeleton(report);

   AssetDatabase.SaveAssets();
   File.WriteAllText("Validation/enemy-pack-integration.txt", report.ToString());
   Debug.Log(report.ToString());
   PropCensus.Run();
  }

  static void ConfigureModel(string path, Avatar avatar) {
   var mi = (ModelImporter)AssetImporter.GetAtPath(path);
   mi.animationType = ModelImporterAnimationType.Human;
   mi.avatarSetup = avatar ? ModelImporterAvatarSetup.CopyFromOther : ModelImporterAvatarSetup.CreateFromThisModel;
   if (avatar) mi.sourceAvatar = avatar;
   mi.importAnimation = true;
   mi.importCameras = false;
   mi.importLights = false;
   mi.optimizeGameObjects = false;
   mi.animationCompression = ModelImporterAnimationCompression.Off;
   mi.SaveAndReimport();
  }

  static void ConfigureTake(string path, Avatar avatar) {
   var mi = (ModelImporter)AssetImporter.GetAtPath(path);
   mi.animationType = ModelImporterAnimationType.Human;
   mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
   mi.sourceAvatar = avatar;
   mi.importAnimation = true;
   mi.importCameras = false;
   mi.importLights = false;
   mi.optimizeGameObjects = false;
   mi.animationCompression = ModelImporterAnimationCompression.Off;
   mi.SaveAndReimport();
  }

  static void BuildRolePrefab(string role, string modelPath, Avatar avatar, StringBuilder report) {
   string prefabPath = CharOutput + role + ".prefab";
   var sourceGo = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
   if (!sourceGo) return;

   var go = UnityEngine.Object.Instantiate(sourceGo);
   try {
    go.name = role;
    var a = go.GetComponent<Animator>();
    if (!a) a = go.AddComponent<Animator>();
    a.avatar = avatar;
    a.applyRootMotion = false;
    a.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

    var tex = Resources.Load<Texture2D>("Characters/Textures/" + role + "_basecolor");
    if (tex) {
     var mat = new Material(Shader.Find("Standard")) { name = role + "_mat", color = Color.white };
     mat.mainTexture = tex;
     mat.SetFloat("_Metallic", 0.1f);
     mat.SetFloat("_Glossiness", 0.3f);
     string matPath = "Assets/Resources/Materials/" + role + ".mat";
     var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
     if (existingMat) {
      EditorUtility.CopySerialized(mat, existingMat);
      UnityEngine.Object.DestroyImmediate(mat);
      mat = existingMat;
     } else {
      AssetDatabase.CreateAsset(mat, matPath);
     }
     foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
    }

    PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
    report.AppendLine("  Prefab built: " + prefabPath);
   } finally {
    UnityEngine.Object.DestroyImmediate(go);
   }
  }

  static void BuildFlyerPrefab(StringBuilder report) {
   string modelPath = ArtSource + "Flyer_model.fbx";
   if (!File.Exists(modelPath)) return;
   string prefabPath = CharOutput + "Flyer.prefab";

   var sourceGo = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
   if (!sourceGo) return;

   var go = UnityEngine.Object.Instantiate(sourceGo);
   try {
    go.name = "Flyer";
    var tex = Resources.Load<Texture2D>("Characters/Textures/Flyer_basecolor");
    if (tex) {
     var mat = new Material(Shader.Find("Standard")) { name = "Flyer_mat", color = Color.white };
     mat.mainTexture = tex;
     mat.EnableKeyword("_EMISSION");
     mat.SetColor("_EmissionColor", new Color(0.4f, 0.2f, 0.7f));
     string matPath = "Assets/Resources/Materials/Flyer.mat";
     var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
     if (existingMat) {
      EditorUtility.CopySerialized(mat, existingMat);
      UnityEngine.Object.DestroyImmediate(mat);
      mat = existingMat;
     } else {
      AssetDatabase.CreateAsset(mat, matPath);
     }
     foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.sharedMaterial = mat;
    }
    PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
    report.AppendLine("  Flyer prefab built: " + prefabPath);
   } finally {
    UnityEngine.Object.DestroyImmediate(go);
   }
  }

  static void RepairFootman(StringBuilder report) {
   foreach (string state in new[] { "idle", "walk", "attack", "death" }) {
    var eliteClip = Resources.Load<AnimationClip>("Animations/Elite_" + state);
    if (eliteClip) {
     string path = AnimOutput + "Footman_" + state + ".anim";
     var copy = UnityEngine.Object.Instantiate(eliteClip);
     copy.name = "Footman_" + state;
     var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
     if (existing) {
      EditorUtility.CopySerialized(copy, existing);
      UnityEngine.Object.DestroyImmediate(copy);
     } else {
      AssetDatabase.CreateAsset(copy, path);
     }
     report.AppendLine("  Footman_" + state + " updated from Elite take");
    }
   }
  }

  static void RepairSkeleton(StringBuilder report) {
   string path = AnimOutput + "Skeleton_idle.anim";
   var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
   if (clip) {
    var spineBinding = AnimationUtility.GetCurveBindings(clip).FirstOrDefault(b => b.propertyName.Contains("Rotation") || b.propertyName.Contains("Rot"));
    if (spineBinding.propertyName == null) {
     var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.66f, 0.08f), new Keyframe(1.33f, 0f));
     clip.SetCurve("mixamorig:Spine", typeof(Transform), "localEulerAngles.x", curve);
     EditorUtility.SetDirty(clip);
     report.AppendLine("  Skeleton_idle curve enhanced");
    }
   }
  }
 }
}
