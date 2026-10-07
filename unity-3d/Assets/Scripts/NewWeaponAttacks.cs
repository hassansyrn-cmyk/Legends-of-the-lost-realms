using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 public sealed class EarthQuakeSlam : MonoBehaviour {
  int element=-1;
  public static EarthQuakeSlam Create(Vector3 heroPos, Vector3 forward, float damage, int element=-1) {
   var g = RealmGame.I; if (!g || !g.World) return null;
   var go = new GameObject("EarthQuakeSlam");
   go.transform.SetParent(g.World.transform, false);
   go.transform.position = heroPos;
   var slam = go.AddComponent<EarthQuakeSlam>();
   slam.element=element;
   slam.StartCoroutine(slam.Execute(heroPos, forward.normalized, damage));
   return slam;
  }

  IEnumerator Execute(Vector3 origin, Vector3 forward, float damage) {
   var g = RealmGame.I;
   Vector3 impactPoint = origin + forward * 1.25f;
   g.CameraRig.Shake = 0.45f;
   g.CameraRig.Kick(-forward, 0.6f);
   g.Sound(element==1?"frost_cast":"impact");
   g.HitStop(0.12f, 0.05f);
   DamageTip.Show(impactPoint + Vector3.up * 1.8f, element==1?"GLACIER BREAK!":"AETHER QUAKE!",element==1?new Color(.55f,.85f,1f):new Color(1f,.85f,.3f));

   Vfx.Play("ga_vfx_Shockwave_01", impactPoint + Vector3.up * 0.1f, Quaternion.identity, 1.4f);
   Vfx.Play("ga_vfx_Impact_01", impactPoint + Vector3.up * 0.3f, Quaternion.identity, 1.3f);
   Vfx.Play("ga_vfx_Hyperdrive_01", impactPoint + Vector3.up * 0.2f, Quaternion.LookRotation(forward), 0.85f);

   HitFoes(impactPoint, 3.8f, damage * 1.35f, forward, true,element);

   for (int step = 1; step <= 4; step++) {
    yield return new WaitForSeconds(0.065f);
    if (!g || !g.World) yield break;
    Vector3 fissurePt = origin + forward * (1.25f + step * 1.85f);
    if (Physics.Raycast(fissurePt + Vector3.up * 2f, Vector3.down, out var hit, 5f, ~0, QueryTriggerInteraction.Ignore)) {
     fissurePt.y = hit.point.y;
    }
    Vfx.Play("ga_vfx_Shockwave_01", fissurePt + Vector3.up * 0.05f, Quaternion.identity, 1.0f + step * 0.15f);
    Vfx.Play(element==1?"ga_vfx_Electricity_01":"ga_vfx_Explosion_02",fissurePt+Vector3.up*.2f,Quaternion.identity,element==1?.5f:.45f);
    g.Sound("attack_chop_1");
    HitFoes(fissurePt, 2.3f, damage * 0.85f, forward, false,element);
   }
   Destroy(gameObject, 0.5f);
  }

  void HitFoes(Vector3 center, float radius, float dmg, Vector3 dir, bool guardBreak,int element) {
   var g = RealmGame.I; if (!g) return;
   foreach (var e in g.Enemies.ToArray()) {
    if (!e || e.Health <= 0) continue;
    Vector3 d = e.transform.position - center; d.y = 0;
    if (d.magnitude < radius + e.Radius) {
     e.Hit(dmg,element,element>=0,(dir+Vector3.up*.35f).normalized,guardBreak);
    }
   }
   for (int i = BreakableCrate.All.Count - 1; i >= 0; i--) {
    var c = BreakableCrate.All[i]; if (!c) continue;
    Vector3 d = c.transform.position - center; d.y = 0;
    if (d.magnitude < radius + 0.8f) c.Break();
   }
  }
 }

 public sealed class InfernoWave : MonoBehaviour {
  Vector3 direction;
  float damage, distance, maxDistance = 15f;
  readonly HashSet<Enemy> hitEnemies = new HashSet<Enemy>();

  public static InfernoWave Fire(Vector3 origin, Vector3 forward, float damage) {
   var g = RealmGame.I; if (!g || !g.World) return null;
   var go = new GameObject("InfernoWave");
   go.transform.SetParent(g.World.transform, false);
   go.transform.position = origin;

   var wave = go.AddComponent<InfernoWave>();
   wave.direction = forward.normalized;
   wave.damage = damage;

   Vfx.Attach("mayker_Slash Fire VFX", go.transform, Vector3.zero, Vector3.one * 0.85f);
   Vfx.Attach("ga_vfx_Flames_01", go.transform, Vector3.zero, Vector3.one * 0.55f);

   var lightObj = new GameObject("FireLight");
   lightObj.transform.SetParent(go.transform, false);
   var light = lightObj.AddComponent<Light>();
   light.type = LightType.Point;
   light.range = 4.5f;
   light.color = new Color(1f, 0.45f, 0.1f);
   light.intensity = 2.5f;

   g.Sound("ember_cast");
   DamageTip.Show(origin + Vector3.up * 0.8f, "INFERNO CRESCENT!", new Color(1f, 0.45f, 0.1f));
   return wave;
  }

  void Update() {
   var g = RealmGame.I; if (!g || !g.Player) { Destroy(gameObject); return; }
   if (g.Screen != GameScreen.Playing) return;

   float step = 16f * Time.deltaTime;
   Vector3 from = transform.position;
   Vector3 to = from + direction * step;

   bool blocked = false;
   float bestDist = step;
   foreach (var hit in Physics.RaycastAll(from, direction, step, ~0, QueryTriggerInteraction.Ignore)) {
    if (hit.collider.GetComponentInParent<Hero>() || hit.collider.GetComponentInParent<Enemy>()) continue;
    if (hit.distance < bestDist) { bestDist = hit.distance; blocked = true; }
   }

   Enemy target = null;
   foreach (var enemy in g.Enemies.ToArray()) {
    if (!enemy || enemy.Health <= 0 || hitEnemies.Contains(enemy)) continue;
    if (ProjectileSweep.Hits(from, to, enemy.transform.position + Vector3.up * 0.85f, enemy.Radius + 0.65f, out _)) {
     target = enemy;
     break;
    }
   }

   if (target != null || blocked) {
    Vector3 detonatePos = target ? target.transform.position + Vector3.up * 0.85f : from + direction * bestDist;
    Detonate(detonatePos, target);
    return;
   }

   transform.position = to;
   distance += step;
   if (distance >= maxDistance) {
    Detonate(transform.position, null);
   }
  }

  void Detonate(Vector3 pos, Enemy directTarget) {
   var g = RealmGame.I; if (!g) return;
   Vfx.Play("ga_vfx_Explosion_02", pos, Quaternion.identity, 1.15f);
   Vfx.Play("ga_vfx_Flames_01", pos, Quaternion.identity, 0.95f);
   g.Sound("impact");
   g.CameraRig.Shake = 0.28f;

   if (directTarget) {
    directTarget.Hit(damage * 1.35f, 0, false, direction);
    hitEnemies.Add(directTarget);
   }

   foreach (var e in g.Enemies.ToArray()) {
    if (!e || e.Health <= 0 || e == directTarget) continue;
    float d = Vector3.Distance(e.transform.position + Vector3.up * 0.85f, pos);
    if (d < 3.2f) {
     e.Hit(damage * 0.75f, 0, true, (e.transform.position - pos).normalized);
    }
   }
   for (int i = BreakableCrate.All.Count - 1; i >= 0; i--) {
    var c = BreakableCrate.All[i]; if (!c) continue;
    if (Vector3.Distance(c.transform.position, pos) < 3.5f) c.Break();
   }
   Destroy(gameObject);
  }
 }

 public sealed class GaleVortex : MonoBehaviour {
  Vector3 direction;
  float damage, distance, maxDistance = 14f;
  float tickTimer;
  readonly Dictionary<Enemy, int> hitCounts = new Dictionary<Enemy, int>();

  public static GaleVortex Fire(Vector3 origin, Vector3 forward, float damage) {
   var g = RealmGame.I; if (!g || !g.World) return null;
   var go = new GameObject("GaleVortex");
   go.transform.SetParent(g.World.transform, false);
   go.transform.position = origin;

   var vortex = go.AddComponent<GaleVortex>();
   vortex.direction = forward.normalized;
   vortex.damage = damage;

   Vfx.Attach("ga_vfx_Tornado_01", go.transform, Vector3.zero, Vector3.one * 0.65f);
   Vfx.Attach("ga_vfx_Heal_01", go.transform, Vector3.zero, Vector3.one * 0.45f);

   var trail = go.AddComponent<TrailRenderer>();
   var mat = new Material(Shader.Find("Sprites/Default"));
   mat.color = new Color(0.25f, 0.95f, 0.65f, 0.8f);
   trail.material = mat;
   trail.time = 0.25f;
   trail.startWidth = 0.5f;
   trail.endWidth = 0.05f;

   g.Sound("gale_cast");
   DamageTip.Show(origin + Vector3.up * 0.8f, "GALE VORTEX!", new Color(0.35f, 1f, 0.65f));
   return vortex;
  }

  void Update() {
   var g = RealmGame.I; if (!g || !g.Player) { Destroy(gameObject); return; }
   if (g.Screen != GameScreen.Playing) return;

   float step = 9.5f * Time.deltaTime;
   Vector3 nextPos = transform.position + direction * step;

   if (Physics.Raycast(transform.position, direction, out var hit, step, ~0, QueryTriggerInteraction.Ignore)) {
    if (!hit.collider.GetComponentInParent<Hero>() && !hit.collider.GetComponentInParent<Enemy>()) {
     Dissipate();
     return;
    }
   }

   transform.position = nextPos;
   distance += step;

   tickTimer += Time.deltaTime;
   bool canTick = tickTimer >= 0.16f;
   if (canTick) tickTimer = 0f;

   foreach (var enemy in g.Enemies.ToArray()) {
    if (!enemy || enemy.Health <= 0) continue;
    Vector3 enemyCenter = enemy.transform.position + Vector3.up * 0.85f;
    Vector3 toVortex = transform.position - enemyCenter;
    float dist = toVortex.magnitude;

    if (dist < 3.8f) {
     if (!enemy.Boss) {
      enemy.PushBack(toVortex,Mathf.Min(3.5f*.08f,dist),.08f);
     }

     if (canTick && dist < 2.5f) {
      hitCounts.TryGetValue(enemy, out int hits);
      if (hits < 4) {
       hitCounts[enemy] = hits + 1;
       enemy.Hit(damage * 0.48f, 2, false, direction);
       Vfx.Play("mayker_Slash Eletric VFX", enemyCenter, Quaternion.LookRotation(direction), 0.4f);
       g.Sound("sword_slash");
      }
     }
    }
   }

   for (int i = BreakableCrate.All.Count - 1; i >= 0; i--) {
    var c = BreakableCrate.All[i]; if (!c) continue;
    if (Vector3.Distance(c.transform.position, transform.position) < 2.8f) c.Break();
   }

   if (distance >= maxDistance) {
    Dissipate();
   }
  }

  void Dissipate() {
   Vfx.Play("ga_vfx_Hyperdrive_01", transform.position, Quaternion.identity, 0.75f);
   Vfx.Play("ga_vfx_Electricity_01", transform.position, Quaternion.identity, 0.5f);
   Destroy(gameObject);
  }
 }
}
