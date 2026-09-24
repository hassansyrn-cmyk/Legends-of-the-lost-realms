using System;
using System.Collections;
using UnityEngine;

namespace LostRealms {
 public enum GateState {
  Locked,
  Activating,
  Active,
  Teleporting,
  Completed
 }

 [SelectionBase]
 public class TeleportGateController : MonoBehaviour {
  [Header("Hierarchy References")]
  public Transform GateModel;
  public Transform PortalSurface;
  public Transform PortalGlow;
  public ParticleSystem PortalParticles;
  public ParticleSystem[] RealmParticles;
  public Light PortalLight;
  public AudioSource PortalAudio;
  public Collider TeleportTrigger;
  public Transform ArrivalPoint;

  [Header("Gate Settings")]
  public int RealmIndex = 0;
  public GateState State = GateState.Active;
  public Color RealmColor = Color.cyan;
  public Color SecondaryColor = Color.white;
  public float BaseLightIntensity = 0.8f;
  public float ActivationDuration = 1.2f;

  [Header("Audio Clips")]
  public AudioClip AmbientLoop;
  public AudioClip ActivationClip;
  public AudioClip TeleportClip;

   float activationTimer = 0f;
   Material portalMat;
   Material glowMat;
   bool initialized = false;

   static readonly System.Collections.Generic.Dictionary<string, Material> gateMats = new System.Collections.Generic.Dictionary<string, Material>();
   static readonly System.Collections.Generic.Dictionary<string, Material> portalMats = new System.Collections.Generic.Dictionary<string, Material>();
   static readonly System.Collections.Generic.Dictionary<string, Material> glowMats = new System.Collections.Generic.Dictionary<string, Material>();
   static Material particleMat;

   public static string GetRealmName(int index) {
    string[] names = new[] { "Verdant", "Desert", "Snow", "Lava" };
    if (index >= 0 && index < names.Length) return names[index];
    return "Verdant";
   }

   public static Material GetGateMaterial(string realm) {
    if (gateMats.TryGetValue(realm, out var m) && m != null) return m;
    var mat = Resources.Load<Material>($"Gates/Materials/Gate_{realm}_Mat");
    if (!mat) {
     var tex = Resources.Load<Texture2D>($"Gates/Textures/Gate_{realm}_basecolor");
     var s = Shader.Find("Standard");
     if (s) {
      mat = new Material(s) { name = $"Gate_{realm}_RuntimeMat" };
      if (tex) mat.mainTexture = tex;
      mat.SetFloat("_Glossiness", 0.25f);
      mat.SetFloat("_Metallic", 0.05f);
     }
    }
    if (mat) gateMats[realm] = mat;
    return mat;
   }

   public static Material GetPortalEnergyMaterial(string realm, Color prime, Color sec) {
    if (portalMats.TryGetValue(realm, out var m) && m != null) return m;
    var mat = Resources.Load<Material>($"Gates/Materials/PortalEnergy_{realm}");
    if (!mat) {
     var s = Resources.Load<Shader>("Shaders/PortalEnergy") ?? Shader.Find("LostRealms/PortalEnergy");
     if (s) {
      mat = new Material(s) { name = $"PortalEnergy_{realm}_Runtime" };
      mat.SetColor("_Color", prime);
      mat.SetColor("_SecondaryColor", sec);
      mat.SetFloat("_Speed", 1.0f);
      mat.SetFloat("_Distort", 0.32f);
      mat.SetFloat("_Opacity", 0.88f);
      mat.SetFloat("_Softness", 0.35f);
      mat.SetFloat("_Emission", 2.0f);
      mat.SetFloat("_Rotation", 1.5f);
     }
    }
    if (mat) portalMats[realm] = mat;
    return mat;
   }

   public static Material GetPortalGlowMaterial(string realm, Color prime) {
    if (glowMats.TryGetValue(realm, out var m) && m != null) return m;
    var mat = Resources.Load<Material>($"Gates/Materials/PortalGlow_{realm}");
    if (!mat) {
     var s = Shader.Find("Sprites/Default");
     if (s) {
      mat = new Material(s) { name = $"PortalGlow_{realm}_Runtime" };
      mat.color = new Color(prime.r, prime.g, prime.b, 0.32f);
      var smokeTex = Resources.Load<Texture2D>("VFX/Textures/smoke_04");
      if (smokeTex) mat.mainTexture = smokeTex;
     }
    }
    if (mat) glowMats[realm] = mat;
    return mat;
   }

   public static Material GetParticleMaterial() {
    if (particleMat != null) return particleMat;
    particleMat = Resources.Load<Material>("Gates/Materials/Gate_Particle_Smoke");
    if (!particleMat) {
     var s = Shader.Find("Sprites/Default");
     if (s) {
      particleMat = new Material(s) { name = "GateParticle_Runtime" };
      var smokeTex = Resources.Load<Texture2D>("VFX/Textures/smoke_04");
      if (smokeTex) particleMat.mainTexture = smokeTex;
     }
    }
    return particleMat;
   }

   void Awake() {
    InitializeHierarchy();
    EnsureMaterials();
    EnsureBaseColliders();
   }

   void OnEnable() {
    EnsureMaterials();
   }

#if UNITY_EDITOR
   void OnValidate() {
    EnsureMaterials();
   }
#endif

   void Start() {
    if (!initialized) InitializeHierarchy();
    EnsureMaterials();
    EnsureBaseColliders();
    ApplyRealmColors();

    // Boss stages start locked until the guardian is defeated
    if (RealmGame.I != null && RealmGame.I.World != null && RealmGame.I.World.IsBoss) {
     SetState(GateState.Locked);
    } else {
     SetState(GateState.Active);
    }
   }

   public void EnsureBaseColliders() {
    if (!GateModel) GateModel = transform.Find("GateModel");
    if (!GateModel) return;

    // Avoid duplicate colliders if already created
    var colliders = GateModel.GetComponents<BoxCollider>();
    foreach (var c in colliders) {
     if (Mathf.Abs(c.center.x) < 0.1f && c.center.y < 1.0f) return;
    }

    float baseH = RealmIndex == 1 ? 0.43f : RealmIndex == 2 ? 0.40f : RealmIndex == 3 ? 0.50f : 0.52f;

    // 1. Central platform floor collider (Aster stands on the stone base)
    var colBase = GateModel.gameObject.AddComponent<BoxCollider>();
    colBase.center = new Vector3(0f, baseH * 0.5f, 0f);
    colBase.size = new Vector3(2.9f, baseH, 2.4f);

    // 2. Front step collider (rise is ~0.26m <= Aster's stepOffset 0.35m)
    var colStepFront = GateModel.gameObject.AddComponent<BoxCollider>();
    colStepFront.center = new Vector3(0f, baseH * 0.25f, -1.65f);
    colStepFront.size = new Vector3(2.7f, baseH * 0.5f, 0.9f);

    // 3. Back step collider
    var colStepBack = GateModel.gameObject.AddComponent<BoxCollider>();
    colStepBack.center = new Vector3(0f, baseH * 0.25f, 1.65f);
    colStepBack.size = new Vector3(2.7f, baseH * 0.5f, 0.9f);
   }

   public void EnsureMaterials() {
    string realm = GetRealmName(RealmIndex);

    // 1. GateModel: ensure all mesh renderers have valid non-error materials
    if (GateModel) {
     var gmat = GetGateMaterial(realm);
     if (gmat) {
      foreach (var r in GateModel.GetComponentsInChildren<Renderer>(true)) {
       if (r is MeshRenderer mr) {
        if (mr.sharedMaterial == null || mr.sharedMaterial.shader == null || mr.sharedMaterial.shader.name.Contains("InternalError") || mr.sharedMaterial.name.StartsWith("Default")) {
         mr.sharedMaterial = gmat;
        }
       }
      }
     }
    }

    // 2. PortalSurface: ensure valid portal energy material
    if (PortalSurface) {
     var r = PortalSurface.GetComponent<Renderer>();
      if (r) {
       if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name.Contains("InternalError")) {
        var pmat = GetPortalEnergyMaterial(realm, RealmColor, SecondaryColor);
        if (pmat) r.sharedMaterial = pmat;
       }
       if (portalMat == null) portalMat = r.sharedMaterial;
      }
     }

     // 3. PortalGlow: ensure valid glow material
     if (PortalGlow) {
      var r = PortalGlow.GetComponent<Renderer>();
      if (r) {
       if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name.Contains("InternalError")) {
        var gm = GetPortalGlowMaterial(realm, RealmColor);
        if (gm) r.sharedMaterial = gm;
       }
       if (glowMat == null) glowMat = r.sharedMaterial;
      }
     }

    // 4. Particle systems: ensure valid non-magenta particle material
    var pmatShared = GetParticleMaterial();
    if (pmatShared) {
     if (PortalParticles) {
      var psr = PortalParticles.GetComponent<ParticleSystemRenderer>();
      if (psr && (psr.sharedMaterial == null || psr.sharedMaterial.shader == null || psr.sharedMaterial.shader.name.Contains("InternalError"))) {
       psr.sharedMaterial = pmatShared;
      }
     }
     if (RealmParticles != null) {
      foreach (var ps in RealmParticles) {
       if (!ps) continue;
       var psr = ps.GetComponent<ParticleSystemRenderer>();
       if (psr && (psr.sharedMaterial == null || psr.sharedMaterial.shader == null || psr.sharedMaterial.shader.name.Contains("InternalError"))) {
        psr.sharedMaterial = pmatShared;
       }
      }
     }
    }
   }

   public void InitializeHierarchy() {
    if (initialized) return;

    if (!GateModel) GateModel = transform.Find("GateModel");
    if (!PortalSurface) PortalSurface = transform.Find("PortalSurface");
    if (!PortalGlow) PortalGlow = transform.Find("PortalGlow");
    if (!PortalParticles) {
     var pp = transform.Find("PortalParticles");
     if (pp) PortalParticles = pp.GetComponent<ParticleSystem>();
    }
    if (RealmParticles == null || RealmParticles.Length == 0) {
     var rp = transform.Find("RealmParticles");
     if (rp) RealmParticles = rp.GetComponentsInChildren<ParticleSystem>(true);
    }
    if (!PortalLight) {
     var pl = transform.Find("PortalLight");
     if (pl) PortalLight = pl.GetComponent<Light>();
    }
    if (!PortalAudio) {
     var pa = transform.Find("PortalAudio");
     if (pa) PortalAudio = pa.GetComponent<AudioSource>();
    }
    if (!TeleportTrigger) {
     var tt = transform.Find("TeleportTrigger");
     if (tt) TeleportTrigger = tt.GetComponent<Collider>();
    }
    if (!ArrivalPoint) ArrivalPoint = transform.Find("ArrivalPoint");

    EnsureMaterials();

    initialized = true;
   }

   public void ApplyRealmColors() {
    if (portalMat) {
     portalMat.SetColor("_Color", RealmColor);
     portalMat.SetColor("_SecondaryColor", SecondaryColor);
    }
    if (glowMat) {
     glowMat.color = new Color(RealmColor.r, RealmColor.g, RealmColor.b, 0.45f);
    }
    if (PortalLight) {
     PortalLight.color = RealmColor;
    }
   }

  public void SetState(GateState newState) {
   State = newState;
   switch (State) {
    case GateState.Locked:
     if (PortalSurface) PortalSurface.gameObject.SetActive(false);
     if (PortalGlow) PortalGlow.gameObject.SetActive(false);
     if (PortalLight) PortalLight.enabled = false;
     if (PortalParticles) PortalParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
     if (RealmParticles != null) {
      foreach (var ps in RealmParticles) if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
     }
     if (PortalAudio && PortalAudio.isPlaying) PortalAudio.Stop();
     if (TeleportTrigger) TeleportTrigger.enabled = false;
     break;

    case GateState.Activating:
     activationTimer = 0f;
     if (PortalSurface) PortalSurface.gameObject.SetActive(true);
     if (PortalGlow) PortalGlow.gameObject.SetActive(true);
     if (PortalLight) {
      PortalLight.enabled = true;
      PortalLight.intensity = BaseLightIntensity * 0.4f;
     }
     if (PortalParticles) PortalParticles.Play();
     if (RealmParticles != null) {
      foreach (var ps in RealmParticles) if (ps) ps.Play();
     }
     if (PortalAudio && ActivationClip) {
      PortalAudio.PlayOneShot(ActivationClip, 0.9f);
     }
     if (TeleportTrigger) TeleportTrigger.enabled = false;
     break;

    case GateState.Active:
     if (PortalSurface) PortalSurface.gameObject.SetActive(true);
     if (PortalGlow) PortalGlow.gameObject.SetActive(true);
     if (PortalLight) {
      PortalLight.enabled = true;
      PortalLight.intensity = BaseLightIntensity;
     }
     if (PortalParticles && !PortalParticles.isPlaying) PortalParticles.Play();
     if (RealmParticles != null) {
      foreach (var ps in RealmParticles) if (ps && !ps.isPlaying) ps.Play();
     }
     if (PortalAudio && AmbientLoop) {
      if (!PortalAudio.isPlaying || PortalAudio.clip != AmbientLoop) {
       PortalAudio.clip = AmbientLoop;
       PortalAudio.loop = true;
       PortalAudio.Play();
      }
     }
     if (TeleportTrigger) TeleportTrigger.enabled = true;
     break;

    case GateState.Teleporting:
     if (TeleportTrigger) TeleportTrigger.enabled = false;
     if (PortalLight) PortalLight.intensity = BaseLightIntensity * 2.2f;
     if (PortalAudio) {
      if (TeleportClip) PortalAudio.PlayOneShot(TeleportClip, 1.0f);
     }
     Vfx.Play("ga_vfx_Portal_02", transform.position + Vector3.up * 2.2f, Quaternion.identity, 1.3f);
     StartCoroutine(CompleteTeleportRoutine());
     break;

    case GateState.Completed:
     if (PortalParticles) PortalParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
     if (RealmParticles != null) {
      foreach (var ps in RealmParticles) if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
     }
     if (PortalAudio && PortalAudio.isPlaying) PortalAudio.Stop();
     if (TeleportTrigger) TeleportTrigger.enabled = false;
     break;
   }
  }

  void Update() {
   var g = RealmGame.I;
   if (!g || g.Screen != GameScreen.Playing) return;

   // 1. Check Locked -> Activating (e.g. boss defeated)
   if (State == GateState.Locked) {
    bool bossAlive = g.World != null && g.World.IsBoss && g.Enemies != null && g.Enemies.Exists(x => x && x.Boss && x.Health > 0);
    if (!bossAlive) {
     SetState(GateState.Activating);
    }
   }
   // 2. Activating transition
   else if (State == GateState.Activating) {
    activationTimer += Time.deltaTime;
    float t = Mathf.Clamp01(activationTimer / Mathf.Max(0.1f, ActivationDuration));
    if (PortalLight) PortalLight.intensity = Mathf.Lerp(BaseLightIntensity * 0.4f, BaseLightIntensity, t);
    if (t >= 1f) {
     SetState(GateState.Active);
    }
   }
   // 3. Distance proximity check for backup trigger reliability
   else if (State == GateState.Active) {
    if (g.Player != null) {
     float dist = Vector3.Distance(g.Player.transform.position, transform.position + Vector3.up * 1.5f);
     if (dist < 1.8f) {
      OnPlayerEntered();
     }
    }
   }
  }

  public void OnTriggerEnter(Collider other) {
   if (State != GateState.Active) return;
   if (other.CompareTag("Player") || other.GetComponentInParent<Hero>() != null) {
    OnPlayerEntered();
   }
  }

  void OnPlayerEntered() {
   if (State != GateState.Active) return;
   SetState(GateState.Teleporting);
  }

   IEnumerator CompleteTeleportRoutine() {
    yield return new WaitForSeconds(0.45f);
    if (RealmGame.I != null) {
     RealmGame.I.Finish();
    }
    // A sealed gate (or live guardian) refuses Finish: snap back to Active
    // so the player can retry after meeting the requirement instead of
    // wedging the gate in Completed forever.
    if (RealmGame.I == null || RealmGame.I.Screen != GameScreen.Complete) { SetState(GateState.Active); yield break; }
    yield return new WaitForSeconds(0.8f);
    SetState(GateState.Completed);
   }
 }
}
