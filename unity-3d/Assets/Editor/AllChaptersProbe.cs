using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class AllChaptersProbe {
  [MenuItem("Lost Realms/Diagnose All Chapters")]
  public static void DiagnoseAll() {
   var sb = new StringBuilder();
   sb.AppendLine("=== ALL CHAPTERS PROBE (1 TO 15) ===");
   int passCount = 0;
   int failCount = 0;

   // Ensure camera exists
   if (Camera.main == null) {
    var cam = new GameObject("ProbeCamera").AddComponent<Camera>();
    cam.tag = "MainCamera";
   }

   // Temporary game object for RealmGame
   var gameObj = new GameObject("ProbeRealmGame");
   var game = gameObj.AddComponent<RealmGame>();
   RealmGame.I = game;
   game.Save = new Progress();

   for (int level = 1; level <= 15; level++) {
    int realm = level <= 4 ? 0 : level <= 7 ? 1 : level <= 10 ? 2 : 3;
    bool isBoss = level == 4 || level == 7 || level == 10 || level == 15;
    int expectedIslands = isBoss ? 8 : 12;

    var worldObj = new GameObject($"World_Level_{level}");
    var world = worldObj.AddComponent<RealmWorld>();
    game.World = world;
    game.Level = level;
    game.Realm = realm;
    game.Enemies.Clear();

    try {
     world.Build(level, realm);

     // Check islands count
     int islandCount = 0;
     Transform endGate = null;
     foreach (Transform t in world.transform) {
      if (t.name == "Island") islandCount++;
      if (t.name == "Realm gate") endGate = t;
     }

     // Test RealmTrials.Build
     var trial = RealmTrials.Build(world, level);

     bool islandOk = islandCount >= expectedIslands;
     bool gateOk = endGate != null;
     bool routeOk = world.Route.Count >= expectedIslands;

     if (islandOk && gateOk && routeOk) {
      sb.AppendLine($"[PASS] Chapter {level:D2} (Realm {realm}, Boss={isBoss}): {islandCount} islands, Gate={gateOk}, Route={world.Route.Count}");
      passCount++;
     } else {
      sb.AppendLine($"[FAIL] Chapter {level:D2} (Realm {realm}): islands={islandCount}/{expectedIslands}, Gate={gateOk}, Route={world.Route.Count}");
      failCount++;
     }
    } catch (Exception ex) {
     sb.AppendLine($"[EXCEPTION] Chapter {level:D2} (Realm {realm}): {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
     failCount++;
    } finally {
     UnityEngine.Object.DestroyImmediate(worldObj);
    }
   }

   UnityEngine.Object.DestroyImmediate(gameObj);
   sb.AppendLine($"=== SUMMARY: {passCount}/15 PASSED, {failCount} FAILED ===");

   string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Validation/all-chapters-probe.txt"));
   System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
   System.IO.File.WriteAllText(outPath, sb.ToString());
   Debug.Log(sb.ToString());
   if (Application.isBatchMode) EditorApplication.Exit(0);
  }
 }
}
