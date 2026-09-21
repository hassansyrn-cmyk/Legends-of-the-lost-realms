using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class ChapterInspectionProbe {
  [MenuItem("Lost Realms/Inspect All Chapters In Depth")]
  public static void Run() {
   var sb = new StringBuilder();
   sb.AppendLine("================================================================================");
   sb.AppendLine("          LEGEND OF THE LOST REALMS - DEEP CHAPTER-BY-CHAPTER INSPECTION        ");
   sb.AppendLine("================================================================================");
   sb.AppendLine($"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
   sb.AppendLine();

   string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation/ChapterInspection"));
   Directory.CreateDirectory(outDir);

   // Ensure probe camera
   Camera cam = Camera.main;
   GameObject camObj = null;
   if (cam == null) {
    camObj = new GameObject("InspectionCamera");
    cam = camObj.AddComponent<Camera>();
    cam.tag = "MainCamera";
   }

   var gameObj = new GameObject("InspectionRealmGame");
   var game = gameObj.AddComponent<RealmGame>();
   RealmGame.I = game;
   game.Save = new Progress();

   int totalAnomalies = 0;

   try {
    for (int level = 1; level <= 15; level++) {
     int realm = level <= 4 ? 0 : level <= 7 ? 1 : level <= 10 ? 2 : 3;
     bool isBoss = level == 4 || level == 7 || level == 10 || level == 15;
     string realmName = realm == 0 ? "Verdant" : realm == 1 ? "Sunscar" : realm == 2 ? "Whiteout" : "Emberfall";

     sb.AppendLine("--------------------------------------------------------------------------------");
     sb.AppendLine($"CHAPTER {level:D2} - REALM {realm} ({realmName.ToUpper()}) {(isBoss ? "[BOSS CHAPTER]" : "")}");
     sb.AppendLine("--------------------------------------------------------------------------------");

     var worldObj = new GameObject($"World_Level_{level}");
     var world = worldObj.AddComponent<RealmWorld>();
     game.World = world;
     game.Level = level;
     game.Realm = realm;
     game.Enemies.Clear();

     try {
      world.Build(level, realm);
      Physics.SyncTransforms();

      // 1. ISLANDS & ROUTE
      var islands = new List<Transform>();
      foreach (Transform t in world.transform) {
       if (t.name == "Island") islands.Add(t);
      }
      sb.AppendLine($"[1. ISLANDS & ROUTE]");
      sb.AppendLine($"  Island Count: {islands.Count} (Route Points: {world.Route.Count})");
      if (world.Route.Count >= 2) {
       float totalDist = 0;
       for (int r = 1; r < world.Route.Count; r++) totalDist += Vector3.Distance(world.Route[r], world.Route[r - 1]);
       sb.AppendLine($"  Route Span: Start={world.Route[0]:F1} -> End={world.Route[world.Route.Count - 1]:F1} (Total Path Length: {totalDist:F1}m)");
      }

      int movingIslands = 0;
      for (int i = 0; i < islands.Count; i++) {
       var mover = islands[i].GetComponent<MovingIsland>();
       if (mover != null) {
        movingIslands++;
        sb.AppendLine($"  - Island #{i:D2} (MOVER): Style={mover.Style}, Offset={mover.Offset}, Speed={mover.Speed:F2}, Origin={mover.Origin:F1}");
       }
      }
      sb.AppendLine($"  Moving Islands: {movingIslands}");

      // 2. ASTER SPAWN POINT INSPECTION
      sb.AppendLine($"[2. ASTER SPAWN]");
      Vector3 spawn = world.Spawn;
      sb.AppendLine($"  Spawn Pos: {spawn:F2}");

      // Check ground below spawn
      if (Physics.Raycast(spawn + Vector3.up * 0.5f, Vector3.down, out var groundHit, 3.0f, ~0, QueryTriggerInteraction.Ignore)) {
       sb.AppendLine($"  Ground Contact: Solid! Distance={groundHit.distance:F2}m, Normal={groundHit.normal:F2}, Hit={groundHit.transform.name}");
      } else {
       sb.AppendLine($"  [ANOMALY] Spawn NOT grounded! No surface within 3m downward!");
       totalAnomalies++;
      }

      // Check obstacles around spawn
      var spawnBlockers = Physics.OverlapSphere(spawn, 2.2f, ~0, QueryTriggerInteraction.Collide);
      var obstacleList = new List<string>();
      foreach (var col in spawnBlockers) {
       if (col.gameObject.name == "Realm surface" || col.gameObject.name == "Island" || col.gameObject.name.Contains("Mesh")) continue;
       float d = Vector3.Distance(col.ClosestPoint(spawn), spawn);
       obstacleList.Add($"{col.transform.name} ({col.GetType().Name}, dist={d:F2}m)");
      }
      if (obstacleList.Count > 0) {
       sb.AppendLine($"  Colliders within 2.2m of spawn: {string.Join(", ", obstacleList)}");
      } else {
       sb.AppendLine($"  Spawn Clearance: Clear (no blocking colliders within 2.2m)");
      }

      // 3. CHECKPOINTS
      sb.AppendLine($"[3. CHECKPOINTS]");
      var checkpoints = world.GetComponentsInChildren<RealmCheckpoint>(true);
      sb.AppendLine($"  Count: {checkpoints.Length}");
      for (int ci = 0; ci < checkpoints.Length; ci++) {
       var cp = checkpoints[ci];
       var visual = cp.GetComponent<CheckpointVisual>();
       var col = cp.GetComponent<Collider>();
       float distToSpawn = Vector3.Distance(cp.transform.position, cp.SpawnPoint);
       bool solidCol = col != null && !col.isTrigger;

       // Grounding check
       bool cpGrounded = Physics.Raycast(cp.transform.position + Vector3.up * 2f, Vector3.down, out var cpHit, 5f, ~0, QueryTriggerInteraction.Ignore);
       float cpGroundOffset = cpGrounded ? Mathf.Abs(cp.transform.position.y - cpHit.point.y) : 999f;

       sb.AppendLine($"  - Checkpoint #{ci + 1}: Pos={cp.transform.position:F2}, SpawnPoint={cp.SpawnPoint:F2}, RespawnDist={distToSpawn:F2}m, GroundOffset={cpGroundOffset:F2}m, SolidCol={solidCol}, VisualOk={(visual != null)}");
       if (distToSpawn < 1.0f) {
        sb.AppendLine($"    [ANOMALY] SpawnPoint too close to Checkpoint pedestal ({distToSpawn:F2}m)! Player may wedge inside.");
        totalAnomalies++;
       }
       if (cpGroundOffset > 0.6f) {
        sb.AppendLine($"    [ANOMALY] Checkpoint floating above ground by {cpGroundOffset:F2}m!");
        totalAnomalies++;
       }
       if (Vector3.Distance(cp.transform.position, spawn) < 1.5f) {
        sb.AppendLine($"    [CRITICAL BUG] Checkpoint is sitting directly on Aster's spawn point ({Vector3.Distance(cp.transform.position, spawn):F2}m)!");
        totalAnomalies++;
       }
      }

      // 4. TRAPS & HAZARDS
      sb.AppendLine($"[4. TRAPS & HAZARDS]");
      var trapNames = new[] {
       "Flame brazier", "Frost totem", "Serpent statue",
       "Crusher pillar", "Dart turret", "Rolling boulder", "Wind vent", "Saw blade", "Fire geyser", "Spike trap", "Floor blade"
      };
      var allTransforms = world.GetComponentsInChildren<Transform>(true);
      var foundTraps = new List<GameObject>();
      foreach (var t in allTransforms) {
       foreach (var tn in trapNames) {
        if (t.name.IndexOf(tn, StringComparison.OrdinalIgnoreCase) >= 0) {
         if (!foundTraps.Contains(t.gameObject)) foundTraps.Add(t.gameObject);
         break;
        }
       }
      }
      sb.AppendLine($"  Total Traps: {foundTraps.Count}");
      var trapPosList = new List<Vector3>();
      foreach (var trap in foundTraps) {
       var rends = trap.GetComponentsInChildren<Renderer>();
       Bounds b = new Bounds(trap.transform.position, Vector3.zero);
       if (rends.Length > 0) {
        b = rends[0].bounds;
        for (int ri = 1; ri < rends.Length; ri++) b.Encapsulate(rends[ri].bounds);
       }
       var col = trap.GetComponentInChildren<Collider>();
       sb.AppendLine($"  - Trap '{trap.name}': Pos={trap.transform.position:F2}, BoundsSize={b.size:F2}, Collider={(col != null ? col.GetType().Name : "NONE")}");

       // Check overlap with spawn
       float dToSpawn = Vector3.Distance(trap.transform.position, spawn);
       if (dToSpawn < 3.5f) {
        sb.AppendLine($"    [ANOMALY] Trap '{trap.name}' placed too close to spawn ({dToSpawn:F2}m)!");
        totalAnomalies++;
       }

       // Check overlap with other traps
       foreach (var otherPos in trapPosList) {
        float dOther = Vector3.Distance(trap.transform.position, otherPos);
        if (dOther < 1.5f) {
         sb.AppendLine($"    [ANOMALY] Trap '{trap.name}' overlaps another trap (dist={dOther:F2}m)!");
         totalAnomalies++;
        }
       }
       trapPosList.Add(trap.transform.position);
      }

      // 5. ENEMIES & COMBAT
      sb.AppendLine($"[5. ENEMIES]");
      var enemies = world.GetComponentsInChildren<Enemy>(true);
      sb.AppendLine($"  Total Enemies: {enemies.Length}");
      int enemiesOverVoid = 0;
      int enemiesMisanchored = 0;
      foreach (var foe in enemies) {
       bool overVoid = false;
       if (foe.Kind != 11) { // Flyer is exempt
        if (!Physics.Raycast(foe.transform.position + Vector3.up * 0.5f, Vector3.down, 6f, ~0, QueryTriggerInteraction.Ignore)) {
         overVoid = true;
         enemiesOverVoid++;
        }
       }
       float anchorDist = 0;
       var foeRends = foe.GetComponentsInChildren<Renderer>(true);
       if (foeRends.Length > 0) {
        anchorDist = Vector3.Distance(foeRends[0].bounds.center, foe.transform.position);
       }
       float anchorLimit = foe.Boss ? 5.5f : 3.2f;
       if (anchorDist > anchorLimit) {
        enemiesMisanchored++;
       }

       sb.AppendLine($"  - Enemy Kind={foe.Kind} ({(foe.Boss ? "BOSS" : "Minion")}): Pos={foe.transform.position:F2}, HP={foe.Health:F0}/{foe.MaxHealth:F0}, OverVoid={overVoid}, AnchorDist={anchorDist:F2}m");
      }
      if (enemiesOverVoid > 0) {
       sb.AppendLine($"  [ANOMALY] {enemiesOverVoid} enemy(ies) spawned over the void!");
       totalAnomalies++;
      }
      if (enemiesMisanchored > 0) {
       sb.AppendLine($"  [ANOMALY] {enemiesMisanchored} enemy(ies) visual misanchored from root!");
       totalAnomalies++;
      }

      // 6. BOSS DETAILS (if boss chapter)
      if (isBoss) {
       sb.AppendLine($"[6. BOSS SPECIFICATION]");
       Enemy boss = null;
       foreach (var foe in enemies) {
        if (foe.Boss) { boss = foe; break; }
       }
       if (boss != null) {
        sb.AppendLine($"  Boss Name: {boss.gameObject.name}, Kind={boss.Kind}, MaxHP={boss.MaxHealth}, WeakElement={boss.WeakElement}");
        sb.AppendLine($"  Golem Boss Controller (Combo/Slam/Charge): {boss.IsGolemBoss}");
        sb.AppendLine($"  Lava Boss Controller (Warden/Greatsword): {boss.IsLavaBoss}");
        sb.AppendLine($"  Visual Model: {(boss.Visual != null ? boss.Visual.name : "null")}, UsesFallback: {boss.Visual?.UsesFallback}, HasAnimator: {boss.Visual?.animator != null}");
        if (boss.Visual != null && boss.Visual.UsesFallback) {
         sb.AppendLine($"  [CRITICAL BUG] Boss is using primitive fallback (capsule/stick) instead of authentic character visual!");
         totalAnomalies++;
        }

        // Arena dimensions: last route island
        Vector3 arenaCenter = world.Route[world.Route.Count - 1];
        float bossDistToCenter = Vector3.Distance(boss.transform.position, arenaCenter);
        sb.AppendLine($"  Boss Distance to Arena Center: {bossDistToCenter:F2}m (Arena Center: {arenaCenter:F1})");
       } else {
        sb.AppendLine($"  [ANOMALY] Boss marked chapter but NO boss enemy found!");
        totalAnomalies++;
       }
      }

      // 7. PICKUPS & PROPS
      sb.AppendLine($"[7. PICKUPS & PROPS]");
      var pickups = world.GetComponentsInChildren<RealmPickup>(true);
      var heals = world.GetComponentsInChildren<RealmHeal>(true);
      var weaponDrops = world.GetComponentsInChildren<WeaponDrop>(true);
      sb.AppendLine($"  Pickups: {pickups.Length} gems/coins, Heals: {heals.Length}, Weapon Drops: {weaponDrops.Length}");
      foreach (var wd in weaponDrops) {
       sb.AppendLine($"  - Weapon Drop: Id={wd.Id} ({WeaponCatalog.Get(wd.Id).Name}) at {wd.transform.position:F2}");
      }

      // Prop check across all descendants
      int propCount = 0;
      int floatingProps = 0;
      foreach (var child in allTransforms) {
       if (!child.name.StartsWith("Prop ")) continue;
       propCount++;
       var rends = child.GetComponentsInChildren<Renderer>();
       if (rends.Length == 0) continue;
       Bounds rb = rends[0].bounds;
       for (int ri = 1; ri < rends.Length; ri++) rb.Encapsulate(rends[ri].bounds);
       if (rb.size.y > 0.01f && Physics.Raycast(rb.center + Vector3.up * 1.5f, Vector3.down, out var phit, 9f, ~0, QueryTriggerInteraction.Ignore)) {
        if (rb.min.y > phit.point.y + 0.65f) floatingProps++;
       }
      }
      sb.AppendLine($"  Total Scenery Props: {propCount}, Floating Props: {floatingProps}");
      if (floatingProps > 0) {
       sb.AppendLine($"  [WARNING] {floatingProps} prop(s) detected as floating (>0.65m above deck).");
      }

      // 8. LEVEL EXIT GATE
      sb.AppendLine($"[8. LEVEL EXIT GATE]");
      Transform gate = null;
      foreach (Transform child in world.transform) {
       if (child.name == "Realm gate") { gate = child; break; }
      }
      if (gate != null) {
       var gateCol = gate.GetComponentInChildren<Collider>();
       var gateLight = gate.GetComponentInChildren<Light>();
       sb.AppendLine($"  Gate Pos: {gate.position:F2}, Trigger Col={(gateCol != null ? gateCol.GetType().Name : "NONE")}, Light={(gateLight != null ? "YES" : "NO")}");
      } else {
       sb.AppendLine($"  [ANOMALY] Level exit gate is MISSING!");
       totalAnomalies++;
      }

      // 9. SCREENSHOT CAPTURE
      try {
       RenderTexture rt = new RenderTexture(1280, 720, 24);
       var prevRt = RenderTexture.active;
       var prevTarget = cam.targetTexture;

       cam.targetTexture = rt;
       cam.clearFlags = CameraClearFlags.SolidColor;
       cam.backgroundColor = RenderSettings.fogColor;

       if (isBoss) {
        // Aim at boss arena center (last route point)
        Vector3 arenaCenter = world.Route[world.Route.Count - 1];
        cam.transform.position = arenaCenter + new Vector3(0, 7.5f, -14.0f);
        cam.transform.LookAt(arenaCenter + Vector3.up * 2.0f);
       } else {
        // Panoramic shot from above spawn looking down the route
        cam.transform.position = spawn + new Vector3(0, 8.5f, -11.0f);
        Vector3 lookTarget = spawn + new Vector3(0, 1.5f, 18.0f);
        cam.transform.LookAt(lookTarget);
       }

       cam.Render();
       RenderTexture.active = rt;
       var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
       tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
       tex.Apply();

       string imgPath = Path.Combine(outDir, $"Chapter_{level:D2}.png");
       File.WriteAllBytes(imgPath, tex.EncodeToPNG());

       RenderTexture.active = prevRt;
       cam.targetTexture = prevTarget;
       UnityEngine.Object.DestroyImmediate(rt);
       UnityEngine.Object.DestroyImmediate(tex);

       sb.AppendLine($"[9. VISUAL CAPTURE] Saved screenshot: Chapter_{level:D2}.png");
      } catch (Exception ex) {
       sb.AppendLine($"[9. VISUAL CAPTURE] Screenshot failed: {ex.Message}");
      }

      sb.AppendLine();

     } catch (Exception ex) {
      sb.AppendLine($"[CRITICAL EXCEPTION] Building Chapter {level}: {ex.Message}\n{ex.StackTrace}");
      totalAnomalies++;
     } finally {
      UnityEngine.Object.DestroyImmediate(worldObj);
     }
    }
   } finally {
    UnityEngine.Object.DestroyImmediate(gameObj);
    if (camObj != null) UnityEngine.Object.DestroyImmediate(camObj);
   }

   sb.AppendLine("================================================================================");
   sb.AppendLine($"CHAPTER INSPECTION COMPLETE. Total Anomalies Flagged: {totalAnomalies}");
   sb.AppendLine("================================================================================");

   string reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation/chapter-by-chapter-inspection.txt"));
   File.WriteAllText(reportPath, sb.ToString());
   Debug.Log(sb.ToString());
   if (Application.isBatchMode) EditorApplication.Exit(0);
  }
 }
}
