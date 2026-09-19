using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class CheckpointProbe {
  [MenuItem("Lost Realms/Diagnose Checkpoints")]
  public static void Diagnose() {
   var sb = new StringBuilder();
   sb.AppendLine("=== CHECKPOINT VALIDATION PROBE (CHAPTERS 1 - 15) ===");

   if (Camera.main == null) {
    var cam = new GameObject("ProbeCamera").AddComponent<Camera>();
    cam.tag = "MainCamera";
   }

   var gameObj = new GameObject("ProbeRealmGame");
   var game = gameObj.AddComponent<RealmGame>();
   RealmGame.I = game;
   game.Save = new Progress();

   int passCount = 0;
   int failCount = 0;

   for (int level = 1; level <= 15; level++) {
    int realm = level <= 4 ? 0 : level <= 7 ? 1 : level <= 10 ? 2 : 3;
    bool isBoss = level == 4 || level == 7 || level == 10 || level == 15;
    int expectedIslands = isBoss ? 8 : 12;
    int expectedMid = expectedIslands / 2;

    var worldObj = new GameObject($"World_Level_{level}");
    var world = worldObj.AddComponent<RealmWorld>();
    game.World = world;
    game.Level = level;
    game.Realm = realm;
    game.Enemies.Clear();

    try {
     world.Build(level, realm);
     Physics.SyncTransforms();

     var checkpoints = world.GetComponentsInChildren<RealmCheckpoint>(true);
     if (checkpoints.Length != 2) {
      sb.AppendLine($"[FAIL] Ch {level:D2}: Expected exactly 2 checkpoints, found {checkpoints.Length}");
      failCount++;
      continue;
     }

     bool allOk = true;
     for (int ci = 0; ci < checkpoints.Length; ci++) {
      var cp = checkpoints[ci];
      var visual = cp.GetComponent<CheckpointVisual>();
      if (visual == null) {
       sb.AppendLine($"[FAIL] Ch {level:D2} CP#{ci}: Missing CheckpointVisual component!");
       allOk = false;
       continue;
      }

      bool hasCore = visual.Core != null;
      bool hasShards = visual.Shards.Count == 4;
      bool hasHalos = visual.Halos.Count == 2;
      bool hasAura = visual.Aura != null;
      var pedestal = cp.transform.Find("Checkpoint 3D Pedestal");
      bool has3dModel = pedestal != null;

      if (!hasCore || !hasShards || !hasHalos || !hasAura || !has3dModel) {
       sb.AppendLine($"[FAIL] Ch {level:D2} CP#{ci}: Visual incomplete: Core={hasCore}, Shards={visual.Shards.Count}, Halos={visual.Halos.Count}, Aura={hasAura}, 3DPedestal={has3dModel}");
       allOk = false;
       continue;
      }

      // Test activation
      visual.Activate();
      if (!visual.Activated) {
       sb.AppendLine($"[FAIL] Ch {level:D2} CP#{ci}: Checkpoint failed to activate!");
       allOk = false;
       continue;
      }
     }

     if (!allOk) {
      failCount++;
      continue;
     }

     sb.AppendLine($"[PASS] Chapter {level:D2} (Realm {realm}): 2 Checkpoints at [{checkpoints[0].transform.position}, {checkpoints[1].transform.position}], 3D Pedestals=OK, Shards=4, Halos=2, Aura=OK, Activated=OK");
     passCount++;

    } catch (Exception ex) {
     sb.AppendLine($"[EXCEPTION] Chapter {level:D2}: {ex.Message}\n{ex.StackTrace}");
     failCount++;
    } finally {
     UnityEngine.Object.DestroyImmediate(worldObj);
    }
   }

   UnityEngine.Object.DestroyImmediate(gameObj);
   sb.AppendLine($"\n=== SUMMARY: {passCount}/15 PASSED, {failCount} FAILED ===");

   string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Validation/checkpoint-probe.txt"));
   System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
   System.IO.File.WriteAllText(outPath, sb.ToString());
   Debug.Log(sb.ToString());
   if (Application.isBatchMode) EditorApplication.Exit(0);
  }
 }
}
