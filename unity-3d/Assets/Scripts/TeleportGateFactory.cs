using System;
using UnityEngine;

namespace LostRealms {
 public static class TeleportGateFactory {
  public static readonly string[] RealmNames = new[] { "Verdant", "Desert", "Snow", "Lava" };

  public static readonly Color[] PrimaryColors = new[] {
   new Color(0.0f, 0.78f, 0.33f),  // Verdant: Emerald green
   new Color(1.0f, 0.84f, 0.0f),   // Desert: Golden yellow
   new Color(0.0f, 0.90f, 1.0f),   // Snow: Cyan
   new Color(1.0f, 0.43f, 0.0f)    // Lava: Orange
  };

  public static readonly Color[] SecondaryColors = new[] {
   new Color(0.11f, 0.91f, 0.71f), // Verdant: Turquoise
   new Color(1.0f, 0.67f, 0.0f),   // Desert: Amber
   new Color(0.50f, 0.85f, 1.0f),  // Snow: Ice blue
   new Color(0.84f, 0.0f, 0.0f)    // Lava: Red
  };

  public static GameObject Create(Transform parent, int realm, Vector3 position, bool isBoss) {
   realm = Mathf.Clamp(realm, 0, 3);
   string realmName = RealmNames[realm];
   Color prime = PrimaryColors[realm];
   Color sec = SecondaryColors[realm];

   var root = new GameObject("Realm gate");
   root.transform.SetParent(parent, false);
   root.transform.position = position;
   if (isBoss) root.transform.localScale = Vector3.one * 1.25f;

   var controller = root.AddComponent<TeleportGateController>();
   var realmGate = root.AddComponent<RealmGate>();

   // 1. GateModel
   var modelRoot = new GameObject("GateModel");
   modelRoot.transform.SetParent(root.transform, false);
   var fbxPrefab = Resources.Load<GameObject>($"Gates/Gate_{realmName}");
   if (fbxPrefab) {
    var model = UnityEngine.Object.Instantiate(fbxPrefab, modelRoot.transform, false);
    model.name = $"Visual_{realmName}";
    var tex = Resources.Load<Texture2D>($"Gates/Textures/Gate_{realmName}_basecolor");
    if (tex) {
     var mat = new Material(Shader.Find("Standard")) { name = $"Gate_{realmName}_Mat" };
     mat.mainTexture = tex;
     mat.SetFloat("_Glossiness", 0.25f);
     foreach (var rend in model.GetComponentsInChildren<Renderer>(true)) {
      rend.sharedMaterial = mat;
     }
    }
   }
   // Left & Right pillar colliders
   var colLeft = modelRoot.AddComponent<BoxCollider>();
   colLeft.center = new Vector3(-2.1f, 2.2f, 0f);
   colLeft.size = new Vector3(1.3f, 4.4f, 1.6f);
   var colRight = modelRoot.AddComponent<BoxCollider>();
   colRight.center = new Vector3(2.1f, 2.2f, 0f);
   colRight.size = new Vector3(1.3f, 4.4f, 1.6f);
   var colLintel = modelRoot.AddComponent<BoxCollider>();
   colLintel.center = new Vector3(0f, 4.3f, 0f);
   colLintel.size = new Vector3(4.4f, 0.9f, 1.6f);

   // 2. PortalSurface
   var surfObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
   surfObj.name = "PortalSurface";
   surfObj.transform.SetParent(root.transform, false);
   surfObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
   surfObj.transform.localScale = new Vector3(2.2f, 3.2f, 1f);
   var surfCol = surfObj.GetComponent<Collider>();
   if (surfCol) UnityEngine.Object.Destroy(surfCol);
   var surfRend = surfObj.GetComponent<Renderer>();
   var surfShader = Resources.Load<Shader>("Shaders/PortalEnergy") ?? Shader.Find("LostRealms/PortalEnergy");
   if (surfShader) {
    var smat = new Material(surfShader) { name = $"PortalEnergy_{realmName}" };
    smat.SetColor("_Color", prime);
    smat.SetColor("_SecondaryColor", sec);
    smat.SetFloat("_Speed", 1.0f);
    smat.SetFloat("_Distort", 0.32f);
    smat.SetFloat("_Opacity", 0.88f);
    smat.SetFloat("_Softness", 0.35f);
    smat.SetFloat("_Emission", 2.0f);
    smat.SetFloat("_Rotation", 1.5f);
    surfRend.sharedMaterial = smat;
   }

   // 3. PortalGlow
   var glowObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
   glowObj.name = "PortalGlow";
   glowObj.transform.SetParent(root.transform, false);
   glowObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
   glowObj.transform.localScale = new Vector3(2.6f, 3.6f, 1f);
   var glowCol = glowObj.GetComponent<Collider>();
   if (glowCol) UnityEngine.Object.Destroy(glowCol);
   var glowRend = glowObj.GetComponent<Renderer>();
   var glowShader = Shader.Find("Sprites/Default");
   var glowMat = new Material(glowShader) { name = $"PortalGlow_{realmName}" };
   glowMat.color = new Color(prime.r, prime.g, prime.b, 0.32f);
   var smokeTex = Resources.Load<Texture2D>("VFX/Textures/smoke_04");
   if (smokeTex) glowMat.mainTexture = smokeTex;
   glowRend.sharedMaterial = glowMat;

   // 4. PortalParticles
   var ppObj = new GameObject("PortalParticles");
   ppObj.transform.SetParent(root.transform, false);
   ppObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
   var pps = ppObj.AddComponent<ParticleSystem>();
   ConfigurePortalParticles(pps, prime, sec);

   // 5. RealmParticles
   var rpObj = new GameObject("RealmParticles");
   rpObj.transform.SetParent(root.transform, false);
   rpObj.transform.localPosition = new Vector3(0f, 1.0f, 0f);
   ConfigureRealmParticles(rpObj, realm, prime, sec);

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
   pAudio.spatialBlend = 1.0f;
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

   // Audio Clips
   var loopClip = Resources.Load<AudioClip>($"Audio/Gates/{realmName}_Portal_Loop");
   var actClip = Resources.Load<AudioClip>($"Audio/Gates/{realmName}_Portal_Activate");
   var telClip = Resources.Load<AudioClip>($"Audio/Gates/{realmName}_Portal_Teleport");

   // Wire Controller
   controller.GateModel = modelRoot.transform;
   controller.PortalSurface = surfObj.transform;
   controller.PortalGlow = glowObj.transform;
   controller.PortalParticles = pps;
   controller.RealmParticles = rpObj.GetComponentsInChildren<ParticleSystem>(true);
   controller.PortalLight = pLight;
   controller.PortalAudio = pAudio;
   controller.TeleportTrigger = triggerCol;
   controller.ArrivalPoint = arrObj.transform;
   controller.RealmIndex = realm;
   controller.RealmColor = prime;
   controller.SecondaryColor = sec;
   controller.BaseLightIntensity = 0.8f;
   controller.ActivationDuration = 1.2f;
   controller.AmbientLoop = loopClip;
   controller.ActivationClip = actClip;
   controller.TeleportClip = telClip;

   return root;
  }

  static void ConfigurePortalParticles(ParticleSystem ps, Color prime, Color sec) {
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
   var pmat = new Material(Shader.Find("Sprites/Default"));
   pmat.color = Color.white;
   var smokeTex = Resources.Load<Texture2D>("VFX/Textures/smoke_04");
   if (smokeTex) pmat.mainTexture = smokeTex;
   pr.material = pmat;
  }

  static void ConfigureRealmParticles(GameObject parent, int realm, Color prime, Color sec) {
   var smokeTex = Resources.Load<Texture2D>("VFX/Textures/smoke_04");

   if (realm == 0) {
    var leaves = CreatePS(parent, "Leaves", new Color(0.2f, 0.85f, 0.35f, 0.8f), smokeTex, 12, 35, 0.18f, new Vector3(3f, 3f, 2f), -0.6f);
    var sparks = CreatePS(parent, "Sparks", prime, smokeTex, 14, 25, 0.08f, new Vector3(2f, 1.5f, 1f), 0.7f);
   } else if (realm == 1) {
    var sand = CreatePS(parent, "SandDrift", new Color(0.95f, 0.8f, 0.3f, 0.6f), smokeTex, 16, 40, 0.10f, new Vector3(2.5f, 1.0f, 2f), 0.4f);
    var sparks = CreatePS(parent, "GoldSparks", sec, smokeTex, 10, 20, 0.09f, new Vector3(2f, 2f, 1f), 0.6f);
   } else if (realm == 2) {
    var snow = CreatePS(parent, "Snowflakes", new Color(0.9f, 0.98f, 1f, 0.85f), smokeTex, 18, 45, 0.10f, new Vector3(3f, 3.5f, 2f), -0.8f);
    var sparks = CreatePS(parent, "IceSparks", prime, smokeTex, 12, 30, 0.08f, new Vector3(2f, 2f, 1.5f), 0.5f);
   } else {
    var embers = CreatePS(parent, "Embers", new Color(1f, 0.45f, 0.1f, 0.9f), smokeTex, 15, 35, 0.12f, new Vector3(2.5f, 0.5f, 1.5f), 0.9f);
    var ash = CreatePS(parent, "Ash", new Color(0.3f, 0.25f, 0.25f, 0.7f), smokeTex, 10, 25, 0.14f, new Vector3(2.5f, 3f, 2f), -0.5f);
    var smoke = CreatePS(parent, "Smoke", new Color(0.2f, 0.15f, 0.15f, 0.4f), smokeTex, 6, 20, 0.35f, new Vector3(1.5f, 3.5f, 1f), 0.6f);
   }
  }

  static ParticleSystem CreatePS(GameObject parent, string name, Color col, Texture2D tex, float rate, int max, float size, Vector3 boxScale, float ySpeed) {
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
   var mat = new Material(Shader.Find("Sprites/Default")) { color = Color.white };
   if (tex) mat.mainTexture = tex;
   pr.material = mat;

   return ps;
  }
 }
}
