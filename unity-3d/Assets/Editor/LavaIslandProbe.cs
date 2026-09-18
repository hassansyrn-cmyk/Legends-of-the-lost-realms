using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class LavaIslandProbe {
  [MenuItem("Lost Realms/Diagnose Lava Islands")]
  public static void Diagnose() {
   var sb = new StringBuilder();
   sb.AppendLine("=== LAVA REALM ISLAND PROBE (CHAPTERS 11 - 15) ===");

   if (Camera.main == null) {
    var cam = new GameObject("ProbeCamera").AddComponent<Camera>();
    cam.tag = "MainCamera";
   }

   var gameObj = new GameObject("ProbeRealmGame");
   var game = gameObj.AddComponent<RealmGame>();
   RealmGame.I = game;
   game.Save = new Progress();

   for (int level = 11; level <= 15; level++) {
    int realm = 3;
    var worldObj = new GameObject($"World_Level_{level}");
    var world = worldObj.AddComponent<RealmWorld>();
    game.World = world;
    game.Level = level;
    game.Realm = realm;
    game.Enemies.Clear();

    try {
     world.Build(level, realm);

     int meshVisualCount = 0;
     int cragVisualCount = 0;
     int stepVisualCount = 0;
     var modelsSeen = new System.Collections.Generic.HashSet<string>();

     foreach (Transform t in world.transform) {
      if (t.name == "Island") {
       foreach (Transform c in t) {
        if (c.name == "IslandMeshVisual" || c.name.StartsWith("StepVisual_")) {
         if (c.name == "IslandMeshVisual") meshVisualCount++;
         else stepVisualCount++;

         var r = c.GetComponentInChildren<Renderer>();
         if (r != null && r.sharedMaterial != null) {
          modelsSeen.Add(r.sharedMaterial.name);
         }
        }
       }
      } else if (t.name == "Distant floating crag") {
       var crag = t.Find("CragVisual");
       if (crag != null) {
        cragVisualCount++;
        var r = crag.GetComponentInChildren<Renderer>();
        if (r != null && r.sharedMaterial != null) {
         modelsSeen.Add(r.sharedMaterial.name);
        }
       }
      }
     }

     sb.AppendLine($"Chapter {level:D2}: IslandMeshVisuals={meshVisualCount}, StepVisuals={stepVisualCount}, Crags={cragVisualCount}");
     sb.AppendLine($"  Materials: {string.Join(", ", modelsSeen)}");
    } catch (Exception ex) {
     sb.AppendLine($"[EXCEPTION] Chapter {level:D2}: {ex.Message}\n{ex.StackTrace}");
    } finally {
     UnityEngine.Object.DestroyImmediate(worldObj);
    }
   }

   UnityEngine.Object.DestroyImmediate(gameObj);
   string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Validation/lava-island-probe.txt"));
   System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
   System.IO.File.WriteAllText(outPath, sb.ToString());
   Debug.Log(sb.ToString());
   if (Application.isBatchMode) EditorApplication.Exit(0);
  }
 }
}
