using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class GatePrefabBuilder {
  [InitializeOnLoadMethod]
  public static void AutoBuildOnCompile() {
   EditorApplication.delayCall += () => {
    if (!File.Exists(Path.Combine(Application.dataPath, "Resources/Gates/TeleportGate_Verdant.prefab"))) {
     BuildAll();
    }
   };
  }

  [MenuItem("Lost Realms/Build All Gate Prefabs")]
  public static void BuildAll() {
   string[] realmNames = new[] { "Verdant", "Desert", "Snow", "Lava" };
   Color[] primaryColors = new[] {
    new Color(0.0f, 0.78f, 0.33f), // Emerald green
    new Color(1.0f, 0.84f, 0.0f),  // Golden yellow
    new Color(0.0f, 0.90f, 1.0f),  // Cyan
    new Color(1.0f, 0.43f, 0.0f)   // Orange
   };
   Color[] secondaryColors = new[] {
    new Color(0.11f, 0.91f, 0.71f), // Turquoise
    new Color(1.0f, 0.67f, 0.0f),   // Amber
    new Color(0.50f, 0.85f, 1.0f),  // Ice blue
    new Color(0.84f, 0.0f, 0.0f)    // Red
   };

   string outDir = "Assets/Resources/Gates";
   string matDir = "Assets/Resources/Gates/Materials";
   if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
   if (!Directory.Exists(matDir)) Directory.CreateDirectory(matDir);

   // Pre-create / load particle material asset on disk
   string partMatPath = $"{matDir}/Gate_Particle_Smoke.mat";
   var partMatAsset = AssetDatabase.LoadAssetAtPath<Material>(partMatPath);
   var smokeTex = Resources.Load<Texture2D>("VFX/Textures/smoke_04");
   if (!partMatAsset) {
    var pShader = Shader.Find("Sprites/Default");
    partMatAsset = new Material(pShader) { name = "Gate_Particle_Smoke" };
    if (smokeTex) partMatAsset.mainTexture = smokeTex;
    AssetDatabase.CreateAsset(partMatAsset, partMatPath);
   } else {
    if (smokeTex && partMatAsset.mainTexture != smokeTex) partMatAsset.mainTexture = smokeTex;
    EditorUtility.SetDirty(partMatAsset);
   }

   for (int r = 0; r < 4; r++) {
    string realm = realmNames[r];
    Color prime = primaryColors[r];
    Color sec = secondaryColors[r];

    // 1. Create / Load Gate Model Material Asset on disk
    string gateMatPath = $"{matDir}/Gate_{realm}_Mat.mat";
    var gateMat = AssetDatabase.LoadAssetAtPath<Material>(gateMatPath);
    var tex = Resources.Load<Texture2D>($"Gates/Textures/Gate_{realm}_basecolor");
    if (!gateMat) {
     var standardShader = Shader.Find("Standard");
     gateMat = new Material(standardShader) { name = $"Gate_{realm}_Mat" };
     if (tex) gateMat.mainTexture = tex;
     gateMat.SetFloat("_Glossiness", 0.25f);
     gateMat.SetFloat("_Metallic", 0.05f);
     AssetDatabase.CreateAsset(gateMat, gateMatPath);
    } else {
     if (tex) gateMat.mainTexture = tex;
     gateMat.SetFloat("_Glossiness", 0.25f);
     gateMat.SetFloat("_Metallic", 0.05f);
     EditorUtility.SetDirty(gateMat);
    }

    // 2. Create / Load Portal Energy Material Asset on disk
    string surfMatPath = $"{matDir}/PortalEnergy_{realm}.mat";
    var surfMat = AssetDatabase.LoadAssetAtPath<Material>(surfMatPath);
    var surfShader = Resources.Load<Shader>("Shaders/PortalEnergy") ?? Shader.Find("LostRealms/PortalEnergy");
    if (!surfMat) {
     surfMat = new Material(surfShader) { name = $"PortalEnergy_{realm}" };
     AssetDatabase.CreateAsset(surfMat, surfMatPath);
    }
    surfMat.shader = surfShader;
    surfMat.SetColor("_Color", prime);
    surfMat.SetColor("_SecondaryColor", sec);
    surfMat.SetFloat("_Speed", 1.0f);
    surfMat.SetFloat("_Distort", 0.32f);
    surfMat.SetFloat("_Opacity", 0.88f);
    surfMat.SetFloat("_Softness", 0.35f);
    surfMat.SetFloat("_Emission", 2.0f);
    surfMat.SetFloat("_Rotation", 1.5f);
    EditorUtility.SetDirty(surfMat);

    // 3. Create / Load Portal Glow Material Asset on disk
    string glowMatPath = $"{matDir}/PortalGlow_{realm}.mat";
    var glowMatAsset = AssetDatabase.LoadAssetAtPath<Material>(glowMatPath);
    if (!glowMatAsset) {
     var glowShader = Shader.Find("Sprites/Default");
     glowMatAsset = new Material(glowShader) { name = $"PortalGlow_{realm}" };
     AssetDatabase.CreateAsset(glowMatAsset, glowMatPath);
    }
    glowMatAsset.color = new Color(prime.r, prime.g, prime.b, 0.32f);
    if (smokeTex) glowMatAsset.mainTexture = smokeTex;
    EditorUtility.SetDirty(glowMatAsset);

    AssetDatabase.SaveAssets();

    var root = new GameObject($"TeleportGate_{realm}");
    var controller = root.AddComponent<TeleportGateController>();
    var realmGate = root.AddComponent<RealmGate>();

    // 1. GateModel
    var modelRoot = new GameObject("GateModel");
    modelRoot.transform.SetParent(root.transform, false);
    var fbxPrefab = Resources.Load<GameObject>($"Gates/Gate_{realm}");
    if (fbxPrefab) {
     var model = UnityEngine.Object.Instantiate(fbxPrefab, modelRoot.transform, false);
     model.name = $"Visual_{realm}";
     foreach (var rend in model.GetComponentsInChildren<Renderer>(true)) {
      rend.sharedMaterial = gateMat;
     }
    }
    // Pillar colliders (left, right, lintel) so opening remains clear
    var colLeft = modelRoot.AddComponent<BoxCollider>();
    colLeft.center = new Vector3(-2.1f, 2.2f, 0f);
    colLeft.size = new Vector3(1.3f, 4.4f, 1.6f);
    var colRight = modelRoot.AddComponent<BoxCollider>();
    colRight.center = new Vector3(2.1f, 2.2f, 0f);
    colRight.size = new Vector3(1.3f, 4.4f, 1.6f);
    var colLintel = modelRoot.AddComponent<BoxCollider>();
    colLintel.center = new Vector3(0f, 4.3f, 0f);
    colLintel.size = new Vector3(4.4f, 0.9f, 1.6f);

    float baseH = r == 1 ? 0.43f : r == 2 ? 0.40f : r == 3 ? 0.50f : 0.52f;
    var colBase = modelRoot.AddComponent<BoxCollider>();
    colBase.center = new Vector3(0f, baseH * 0.5f, 0f);
    colBase.size = new Vector3(2.9f, baseH, 2.4f);

    var colStepFront = modelRoot.AddComponent<BoxCollider>();
    colStepFront.center = new Vector3(0f, baseH * 0.25f, -1.65f);
    colStepFront.size = new Vector3(2.7f, baseH * 0.5f, 0.9f);

    var colStepBack = modelRoot.AddComponent<BoxCollider>();
    colStepBack.center = new Vector3(0f, baseH * 0.25f, 1.65f);
    colStepBack.size = new Vector3(2.7f, baseH * 0.5f, 0.9f);

    // 2. PortalSurface
    var surfObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
    surfObj.name = "PortalSurface";
    surfObj.transform.SetParent(root.transform, false);
    surfObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
    surfObj.transform.localScale = new Vector3(2.2f, 3.2f, 1f);
    var surfCol = surfObj.GetComponent<Collider>();
    if (surfCol) UnityEngine.Object.DestroyImmediate(surfCol);
    var surfRend = surfObj.GetComponent<Renderer>();
    surfRend.sharedMaterial = surfMat;

    // 3. PortalGlow
    var glowObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
    glowObj.name = "PortalGlow";
    glowObj.transform.SetParent(root.transform, false);
    glowObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
    glowObj.transform.localScale = new Vector3(2.6f, 3.6f, 1f);
    var glowCol = glowObj.GetComponent<Collider>();
    if (glowCol) UnityEngine.Object.DestroyImmediate(glowCol);
    var glowRend = glowObj.GetComponent<Renderer>();
    glowRend.sharedMaterial = glowMatAsset;

    // 4. PortalParticles
    var ppObj = new GameObject("PortalParticles");
    ppObj.transform.SetParent(root.transform, false);
    ppObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
    var pps = ppObj.AddComponent<ParticleSystem>();
    ConfigurePortalParticles(pps, prime, sec, partMatAsset);

    // 5. RealmParticles
    var rpObj = new GameObject("RealmParticles");
    rpObj.transform.SetParent(root.transform, false);
    rpObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
    ConfigureRealmParticles(rpObj, r, prime, sec, partMatAsset);

    // 6. PortalLight
    var plObj = new GameObject("PortalLight");
    plObj.transform.SetParent(root.transform, false);
    plObj.transform.localPosition = new Vector3(0f, 2.4f, 0f);
    var pLight = plObj.AddComponent<Light>();
    pLight.type = LightType.Point;
    pLight.color = prime;
    pLight.intensity = 0.8f;
    pLight.range = 7.5f;
    pLight.shadows = LightShadows.None;

    // 7. PortalAudio
    var paObj = new GameObject("PortalAudio");
    paObj.transform.SetParent(root.transform, false);
    paObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
    var pAudio = paObj.AddComponent<AudioSource>();
    pAudio.playOnAwake = false;
    pAudio.spatialBlend = 1.0f; // 3D
    pAudio.minDistance = 2.0f;
    pAudio.maxDistance = 18.0f;
    pAudio.rolloffMode = AudioRolloffMode.Linear;

    // 8. TeleportTrigger
    var ttObj = new GameObject("TeleportTrigger");
    ttObj.transform.SetParent(root.transform, false);
    ttObj.transform.localPosition = new Vector3(0f, 2.0f, 0f);
    var triggerCol = ttObj.AddComponent<BoxCollider>();
    triggerCol.isTrigger = true;
    triggerCol.size = new Vector3(2.2f, 3.2f, 1.2f);

    // 9. ArrivalPoint
    var arrObj = new GameObject("ArrivalPoint");
    arrObj.transform.SetParent(root.transform, false);
    arrObj.transform.localPosition = new Vector3(0f, 0.1f, -2.2f);

    // 10. Scripts
    var scriptsObj = new GameObject("Scripts");
    scriptsObj.transform.SetParent(root.transform, false);

    // Load Audio Clips
    var loopClip = Resources.Load<AudioClip>($"Audio/Gates/{realm}_Portal_Loop");
    var actClip = Resources.Load<AudioClip>($"Audio/Gates/{realm}_Portal_Activate");
    var telClip = Resources.Load<AudioClip>($"Audio/Gates/{realm}_Portal_Teleport");

    // Wire Controller fields
    controller.GateModel = modelRoot.transform;
    controller.PortalSurface = surfObj.transform;
    controller.PortalGlow = glowObj.transform;
    controller.PortalParticles = pps;
    controller.RealmParticles = rpObj.GetComponentsInChildren<ParticleSystem>(true);
    controller.PortalLight = pLight;
    controller.PortalAudio = pAudio;
    controller.TeleportTrigger = triggerCol;
    controller.ArrivalPoint = arrObj.transform;
    controller.RealmIndex = r;
    controller.RealmColor = prime;
    controller.SecondaryColor = sec;
    controller.BaseLightIntensity = 0.8f;
    controller.ActivationDuration = 1.2f;
    controller.AmbientLoop = loopClip;
    controller.ActivationClip = actClip;
    controller.TeleportClip = telClip;

    string prefabPath = $"{outDir}/TeleportGate_{realm}.prefab";
    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
    Debug.Log($"Built gate prefab: {prefabPath}");

    if (r == 0) {
     string rootPrefabPath = $"{outDir}/TeleportGate_Root.prefab";
     PrefabUtility.SaveAsPrefabAsset(root, rootPrefabPath);
    }

    UnityEngine.Object.DestroyImmediate(root);
   }

   AssetDatabase.SaveAssets();
   AssetDatabase.Refresh();
   Debug.Log("ALL GATE PREFABS BUILT WITH PERSISTENT DISK MATERIALS SUCCESSFULLY!");
  }

  static void ConfigurePortalParticles(ParticleSystem ps, Color prime, Color sec, Material mat) {
   var main = ps.main;
   main.playOnAwake = true;
   main.loop = true;
   main.duration = 4f;
   main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.0f);
   main.startSpeed = new ParticleSystem.MinMaxCurve(-0.8f, -1.5f);
   main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
   main.startColor = new ParticleSystem.MinMaxGradient(prime, sec);
   main.maxParticles = 40;
   main.simulationSpace = ParticleSystemSimulationSpace.Local;

   var emission = ps.emission;
   emission.rateOverTime = 16f;

   var shape = ps.shape;
   shape.shapeType = ParticleSystemShapeType.Circle;
   shape.radius = 1.3f;
   shape.radiusThickness = 0.4f;

   var pr = ps.GetComponent<ParticleSystemRenderer>();
   if (mat) pr.sharedMaterial = mat;
  }

  static void ConfigureRealmParticles(GameObject parent, int realm, Color prime, Color sec, Material mat) {
   if (realm == 0) {
    CreatePS(parent, "Leaves", new Color(0.2f, 0.85f, 0.35f, 0.8f), mat, 12, 35, 0.18f, new Vector3(3f, 3f, 2f), -0.6f);
    CreatePS(parent, "Sparks", prime, mat, 14, 25, 0.08f, new Vector3(2f, 1.5f, 1f), 0.7f);
   } else if (realm == 1) {
    CreatePS(parent, "SandDrift", new Color(0.95f, 0.8f, 0.3f, 0.6f), mat, 16, 40, 0.10f, new Vector3(2.5f, 1.0f, 2f), 0.4f);
    CreatePS(parent, "GoldSparks", sec, mat, 10, 20, 0.09f, new Vector3(2f, 2f, 1f), 0.6f);
   } else if (realm == 2) {
    CreatePS(parent, "Snowflakes", new Color(0.9f, 0.98f, 1f, 0.85f), mat, 18, 45, 0.10f, new Vector3(3f, 3.5f, 2f), -0.8f);
    CreatePS(parent, "IceSparks", prime, mat, 12, 30, 0.08f, new Vector3(2f, 2f, 1.5f), 0.5f);
   } else {
    CreatePS(parent, "Embers", new Color(1f, 0.45f, 0.1f, 0.9f), mat, 15, 35, 0.12f, new Vector3(2.5f, 0.5f, 1.5f), 0.9f);
    CreatePS(parent, "Ash", new Color(0.3f, 0.25f, 0.25f, 0.7f), mat, 10, 25, 0.14f, new Vector3(2.5f, 3f, 2f), -0.5f);
    CreatePS(parent, "Smoke", new Color(0.2f, 0.15f, 0.15f, 0.4f), mat, 6, 20, 0.35f, new Vector3(1.5f, 3.5f, 1f), 0.6f);
   }
  }

  static ParticleSystem CreatePS(GameObject parent, string name, Color col, Material mat, float rate, int max, float size, Vector3 boxScale, float ySpeed) {
   var go = new GameObject(name);
   go.transform.SetParent(parent.transform, false);
   var ps = go.AddComponent<ParticleSystem>();
   var main = ps.main;
   main.playOnAwake = true;
   main.loop = true;
   main.duration = 4f;
   main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
   main.startSpeed = new ParticleSystem.MinMaxCurve(Mathf.Abs(ySpeed) * 0.6f, Mathf.Abs(ySpeed) * 1.2f);
   main.startSize = size;
   main.startColor = col;
   main.maxParticles = max;
   main.simulationSpace = ParticleSystemSimulationSpace.Local;

   var emission = ps.emission;
   emission.rateOverTime = rate;

   var shape = ps.shape;
   shape.shapeType = ParticleSystemShapeType.Box;
   shape.scale = boxScale;
   if (ySpeed < 0) {
    shape.position = new Vector3(0, boxScale.y * 0.5f, 0);
    go.transform.localRotation = Quaternion.Euler(180, 0, 0);
   } else {
    shape.position = new Vector3(0, -boxScale.y * 0.2f, 0);
   }

   var pr = ps.GetComponent<ParticleSystemRenderer>();
   if (mat) pr.sharedMaterial = mat;

   return ps;
  }
 }
}
