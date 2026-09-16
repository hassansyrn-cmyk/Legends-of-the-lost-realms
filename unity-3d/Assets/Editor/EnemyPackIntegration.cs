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

    ConfigureModel(modelPath);
    var avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(a => a.isHuman && a.isValid);
    if (!avatar) avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault(a => a.isValid);
    if (!avatar) {
     report.AppendLine("ERROR " + spec.role + ": failed to create avatar from " + modelPath);
     continue;
    }
    report.AppendLine("Role: " + spec.role + " | Avatar: " + avatar.name + " (human=" + avatar.isHuman + ", valid=" + avatar.isValid + ")");

    float rootY = 0f;
    Quaternion rootQ = Quaternion.identity;

    foreach (var take in spec.takes) {
     string state = take.state;
     string takeFile = take.takeFile;
     bool loop = take.loop;
     string takePath = ArtSource + takeFile;
     if (!File.Exists(takePath)) {
      report.AppendLine("  Missing take: " + takeFile);
      continue;
     }

     ConfigureTake(takePath, avatar, loop);
     var importedClip = AssetDatabase.LoadAllAssetsAtPath(takePath).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview__"));
     if (!importedClip) {
      report.AppendLine("  No clip imported from " + takeFile);
      continue;
     }

     var baked = UnityEngine.Object.Instantiate(importedClip);
     baked.name = spec.role + "_" + state;

     if (state == "idle") {
      foreach (var b in AnimationUtility.GetCurveBindings(baked)) {
       var curve = AnimationUtility.GetEditorCurve(baked, b);
       if (b.propertyName == "RootT.y") rootY = curve.Evaluate(0);
       if (b.propertyName == "RootQ.x") rootQ.x = curve.Evaluate(0);
       if (b.propertyName == "RootQ.y") rootQ.y = curve.Evaluate(0);
       if (b.propertyName == "RootQ.z") rootQ.z = curve.Evaluate(0);
       if (b.propertyName == "RootQ.w") rootQ.w = curve.Evaluate(0);
      }
      if (rootQ.x == 0 && rootQ.y == 0 && rootQ.z == 0 && rootQ.w == 0) rootQ = Quaternion.identity;
     }

     foreach (var b in AnimationUtility.GetCurveBindings(baked)) {
      string p = b.propertyName;
      if (p.StartsWith("RootT")) {
       float val = (p == "RootT.y") ? rootY : 0f;
       AnimationUtility.SetEditorCurve(baked, b, AnimationCurve.Constant(0, baked.length, val));
      }
      if (p.StartsWith("RootQ")) {
       float val = (p == "RootQ.x") ? rootQ.x : (p == "RootQ.y") ? rootQ.y : (p == "RootQ.z") ? rootQ.z : rootQ.w;
       AnimationUtility.SetEditorCurve(baked, b, AnimationCurve.Constant(0, baked.length, val));
      }
     }

     var settings = AnimationUtility.GetAnimationClipSettings(baked);
     settings.loopTime = loop;
     AnimationUtility.SetAnimationClipSettings(baked, settings);

     string roleFolder = AnimOutput + spec.role + "/";
     Directory.CreateDirectory(roleFolder);

     SaveClip(baked, AnimOutput + spec.role + "_" + state + ".anim");
     SaveClip(baked, roleFolder + state + ".anim");
     if (state == "attack") {
      SaveClip(baked, AnimOutput + spec.role + "_attack_1.anim");
      SaveClip(baked, roleFolder + "attack_1.anim");
     }

     report.AppendLine("  Bake: " + spec.role + "_" + state + " <- " + takeFile + " (" + importedClip.length.ToString("F2") + "s, loop=" + loop + ")");
     UnityEngine.Object.DestroyImmediate(baked);
    }

    BuildRolePrefab(spec.role, modelPath, avatar, report);
   }

   BuildFlyerPrefab(report);
   RepairFootman(report);

   AssetDatabase.SaveAssets();
   File.WriteAllText("Validation/enemy-pack-integration.txt", report.ToString());
   Debug.Log(report.ToString());
   PropCensus.Run();
  }

  static void SaveClip(AnimationClip clip, string clipPath) {
   var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
   if (existing) {
    EditorUtility.CopySerialized(clip, existing);
   } else {
    var copy = UnityEngine.Object.Instantiate(clip);
    copy.name = Path.GetFileNameWithoutExtension(clipPath);
    AssetDatabase.CreateAsset(copy, clipPath);
   }
  }

  static void ConfigureModel(string path) {
   var mi = (ModelImporter)AssetImporter.GetAtPath(path);
   mi.animationType = ModelImporterAnimationType.Human;
   mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
   mi.importAnimation = true;
   mi.importCameras = false;
   mi.importLights = false;
   mi.optimizeGameObjects = false;
   mi.animationCompression = ModelImporterAnimationCompression.Off;
   mi.SaveAndReimport();
  }

  static void ConfigureTake(string path, Avatar avatar, bool loop) {
   var mi = (ModelImporter)AssetImporter.GetAtPath(path);
   mi.animationType = ModelImporterAnimationType.Human;
   mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
   mi.sourceAvatar = avatar;
   mi.importAnimation = true;
   mi.importCameras = false;
   mi.importLights = false;
   mi.optimizeGameObjects = false;
   mi.animationCompression = ModelImporterAnimationCompression.Off;

   var specs = mi.defaultClipAnimations;
   if (specs != null && specs.Length > 0) {
    foreach (var spec in specs) {
     spec.loopTime = loop;
     spec.keepOriginalOrientation = true;
     spec.lockRootRotation = true;
     spec.keepOriginalPositionY = true;
     spec.lockRootHeightY = true;
     spec.keepOriginalPositionXZ = true;
     spec.lockRootPositionXZ = true;
    }
    mi.clipAnimations = specs;
   }
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
    a.cullingMode = AnimatorCullingMode.AlwaysAnimate;

    var tex = Resources.Load<Texture2D>("Characters/Textures/" + role + "_basecolor");
    if (!tex) tex = Resources.Load<Texture2D>("Characters/Textures/" + role);
    if (tex) {
     var mat = new Material(Shader.Find("Standard")) { name = role + "_mat", color = Color.white };
     mat.mainTexture = tex;
     mat.SetFloat("_Metallic", 0.05f);
     mat.SetFloat("_Glossiness", 0.25f);
     string matPath = "Assets/Resources/Materials/" + role + ".mat";
     var existingMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
     if (existingMat) {
      EditorUtility.CopySerialized(mat, existingMat);
      UnityEngine.Object.DestroyImmediate(mat);
      mat = existingMat;
     } else {
      AssetDatabase.CreateAsset(mat, matPath);
     }
     foreach (var r in go.GetComponentsInChildren<Renderer>(true)) {
      r.sharedMaterial = mat;
      if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
     }
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
     SaveClip(eliteClip, path);
     report.AppendLine("  Footman_" + state + " updated from Elite take");
    }
   }
  }
 }
}
