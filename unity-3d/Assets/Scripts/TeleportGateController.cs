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

  void Awake() {
   InitializeHierarchy();
  }

  void Start() {
   if (!initialized) InitializeHierarchy();
   ApplyRealmColors();

   // Boss stages start locked until the guardian is defeated
   if (RealmGame.I != null && RealmGame.I.World != null && RealmGame.I.World.IsBoss) {
    SetState(GateState.Locked);
   } else {
    SetState(GateState.Active);
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

   if (PortalSurface) {
    var r = PortalSurface.GetComponent<Renderer>();
    if (r) portalMat = r.material;
   }
   if (PortalGlow) {
    var r = PortalGlow.GetComponent<Renderer>();
    if (r) glowMat = r.material;
   }

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
     HitSpark.Burst(transform.position + Vector3.up * 2.2f, Vector3.up, RealmColor, 30);
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
   yield return new WaitForSeconds(0.8f);
   SetState(GateState.Completed);
  }
 }
}
