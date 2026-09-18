using System;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class PickupAndPropProbe {
  [MenuItem("Lost Realms/Diagnose Pickups and Props")]
  public static void Diagnose() {
   var sb = new StringBuilder();
   sb.AppendLine("=== PICKUP AND PROP ELEVATION & BOUNDARY PROBE (CHAPTERS 1 - 15) ===");

   if (Camera.main == null) {
    var cam = new GameObject("ProbeCamera").AddComponent<Camera>();
    cam.tag = "MainCamera";
   }

   var gameObj = new GameObject("ProbeRealmGame");
   var game = gameObj.AddComponent<RealmGame>();
   RealmGame.I = game;
   game.Save = new Progress();

   int totalSunkPickups = 0;
   int totalFloatingProps = 0;

   for (int level = 1; level <= 15; level++) {
    int realm = level <= 4 ? 0 : level <= 7 ? 1 : level <= 10 ? 2 : 3;
    var worldObj = new GameObject($"World_Level_{level}");
    var world = worldObj.AddComponent<RealmWorld>();
    game.World = world;
    game.Level = level;
    game.Realm = realm;
    game.Enemies.Clear();

    try {
     world.Build(level, realm);
     Physics.SyncTransforms();

     // Check Pickups
     int healCount = 0, weaponCount = 0, gemCount = 0, coinCount = 0;
     int sunkInChapter = 0;

     var heals = world.GetComponentsInChildren<RealmHeal>(true);
     foreach (var h in heals) {
      healCount++;
      var renderers = h.GetComponentsInChildren<Renderer>(true);
      if (renderers.Length > 0) {
       Bounds b = renderers[0].bounds;
       for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
       if (TryGetGroundY(h.transform.position, out float groundY)) {
        float clearance = b.min.y - groundY;
        if (clearance < -0.05f) {
         sb.AppendLine($"  [SUNK HEAL] Ch {level}: clearance={clearance:F3}m, pos={h.transform.position}, groundY={groundY:F3}");
         sunkInChapter++;
        }
       }
      }
     }

     var weapons = world.GetComponentsInChildren<WeaponDrop>(true);
     foreach (var w in weapons) {
      weaponCount++;
      var renderers = w.GetComponentsInChildren<Renderer>(true);
      if (renderers.Length > 0) {
       Bounds b = renderers[0].bounds;
       for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
       if (TryGetGroundY(w.transform.position, out float groundY)) {
        float clearance = b.min.y - groundY;
        if (clearance < -0.05f) {
         sb.AppendLine($"  [SUNK WEAPON] Ch {level}: clearance={clearance:F3}m, pos={w.transform.position}, groundY={groundY:F3}");
         sunkInChapter++;
        }
       }
      }
     }

     var pickups = world.GetComponentsInChildren<RealmPickup>(true);
     foreach (var p in pickups) {
      if (p.Gem) gemCount++; else coinCount++;
      var renderers = p.GetComponentsInChildren<Renderer>(true);
      if (renderers.Length > 0) {
       Bounds b = renderers[0].bounds;
       for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
       if (TryGetGroundY(p.transform.position, out float groundY)) {
        float clearance = b.min.y - groundY;
        if (clearance < -0.05f) {
         sb.AppendLine($"  [SUNK PICKUP (Gem={p.Gem})] Ch {level}: clearance={clearance:F3}m, pos={p.transform.position}, groundY={groundY:F3}");
         sunkInChapter++;
        }
       }
      }
     }

     // Check Props for floating outside island boundaries
     int propCount = 0;
     int floatingPropsInChapter = 0;
     foreach (Transform t in world.transform) {
      if (t.name == "Island") {
       foreach (Transform c in t) {
        if (c.name.StartsWith("Prop ") && !c.name.StartsWith("Prop Shard")) {
         propCount++;
         // Raycast downward from prop position to see if it hits an island collider
         var hits = Physics.RaycastAll(c.position + Vector3.up * 2f, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore);
         bool hitIsland = false;
         for (int i = 0; i < hits.Length; i++) {
          if (hits[i].transform.IsChildOf(t) && hits[i].normal.y >= 0.4f) {
           hitIsland = true;
           break;
          }
         }
         if (!hitIsland) {
          sb.AppendLine($"  [FLOATING PROP] Ch {level} Island '{t.name}': prop '{c.name}' at {c.position} misses island surface!");
          floatingPropsInChapter++;
         }
        }
       }
      }
     }

     totalSunkPickups += sunkInChapter;
     totalFloatingProps += floatingPropsInChapter;

     sb.AppendLine($"Chapter {level:D2} (Realm {realm}): Heals={healCount}, Weapons={weaponCount}, Gems={gemCount}, Coins={coinCount} | Sunk={sunkInChapter} | Props={propCount} | FloatingProps={floatingPropsInChapter}");

    } catch (Exception ex) {
     sb.AppendLine($"[EXCEPTION] Chapter {level:D2}: {ex.Message}\n{ex.StackTrace}");
    } finally {
     UnityEngine.Object.DestroyImmediate(worldObj);
    }
   }

   UnityEngine.Object.DestroyImmediate(gameObj);
   sb.AppendLine($"\nTOTAL SUMMARY: Sunk Pickups={totalSunkPickups}, Floating Props={totalFloatingProps}");
   string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Validation/pickup-prop-probe.txt"));
   System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
   System.IO.File.WriteAllText(outPath, sb.ToString());
   Debug.Log(sb.ToString());
   if (Application.isBatchMode) EditorApplication.Exit(0);
  }

  static bool TryGetGroundY(Vector3 pos, out float groundY) {
   groundY = float.NegativeInfinity;
   var hits = Physics.RaycastAll(pos + Vector3.up * 5f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
   for (int i = 0; i < hits.Length; i++) {
    var h = hits[i];
    if (h.normal.y < 0.5f) continue;
    if (h.collider.GetComponentInParent<Enemy>() != null) continue;
    if (h.collider.GetComponentInParent<TeleportGateController>() != null || h.collider.name == "GateModel") continue;
    if (h.collider.name.StartsWith("Prop ") || h.collider.name.StartsWith("Breakable") || h.collider.name.StartsWith("Pushable")) continue;
    if (h.point.y > groundY) groundY = h.point.y;
   }
   return groundY > float.NegativeInfinity;
  }

  static string GetPath(Transform t) {
   if (t == null) return "";
   string path = t.name;
   while (t.parent != null) {
    t = t.parent;
    path = t.name + "/" + path;
   }
   return path;
  }
 }
}
