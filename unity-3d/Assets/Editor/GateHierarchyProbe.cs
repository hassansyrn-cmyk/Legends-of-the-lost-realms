using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class GateHierarchyProbe {
  [MenuItem("Lost Realms/Diagnose Gate Hierarchy")]
  public static void Diagnose() {
   var sb = new StringBuilder();
   sb.AppendLine("=== GATE HIERARCHY & COMPONENT PROBE ===");

   string artifactDir = @"C:\Users\X1 YOGA\.gemini\antigravity\brain\8161ef4f-085a-40af-8fa6-849260c8d0c7";

   var camObj = new GameObject("ProbeCam");
   var cam = camObj.AddComponent<Camera>();
   cam.clearFlags = CameraClearFlags.Color;
   cam.backgroundColor = new Color(0.08f, 0.12f, 0.16f);
   cam.fieldOfView = 50;

   // Test light
   var sunObj = new GameObject("ProbeSun");
   var sun = sunObj.AddComponent<Light>();
   sun.type = LightType.Directional;
   sun.intensity = 1.0f;
   sun.transform.rotation = Quaternion.Euler(35, -45, 0);

   string[] requiredChildren = new[] {
    "GateModel", "PortalSurface", "PortalGlow", "PortalParticles",
    "RealmParticles", "PortalLight", "PortalAudio", "TeleportTrigger",
    "ArrivalPoint", "Scripts"
   };

   string[] realmNames = new[] { "Verdant (Realm 0)", "Desert (Realm 1)", "Snow (Realm 2)", "Lava (Realm 3)" };

   for (int r = 0; r < 4; r++) {
    var dummyWorld = new GameObject($"World_Gate_{r}");
    var gateObj = TeleportGateFactory.Create(dummyWorld.transform, r, Vector3.zero, false);

    sb.AppendLine($"\n--- Testing {realmNames[r]} ---");
    sb.AppendLine($"Root Object: {gateObj.name}");

    // 1. Verify 10-node hierarchy
    int childrenFound = 0;
    foreach (var childName in requiredChildren) {
     var child = gateObj.transform.Find(childName);
     if (child != null) {
      childrenFound++;
      sb.AppendLine($"  [OK] Child: {childName}");
     } else {
      sb.AppendLine($"  [MISSING] Child: {childName}");
     }
    }
    sb.AppendLine($"  Hierarchy check: {childrenFound}/{requiredChildren.Length} nodes present.");

    // 2. Verify controller component & fields
    var controller = gateObj.GetComponent<TeleportGateController>();
    if (controller != null) {
     sb.AppendLine("  [OK] TeleportGateController attached.");
     bool fieldsOk = controller.GateModel && controller.PortalSurface && controller.PortalGlow &&
                     controller.PortalParticles && controller.PortalLight && controller.PortalAudio &&
                     controller.TeleportTrigger && controller.ArrivalPoint;
     sb.AppendLine($"  Fields Assigned: {(fieldsOk ? "ALL VALID" : "INCOMPLETE")}");

     // 3. Test State Transitions
     controller.SetState(GateState.Locked);
     bool lockedOk = !controller.PortalSurface.gameObject.activeSelf && !controller.PortalLight.enabled;
     sb.AppendLine($"  State Locked: {(lockedOk ? "PASS" : "FAIL")}");

     controller.SetState(GateState.Activating);
     bool actOk = controller.PortalSurface.gameObject.activeSelf && controller.PortalLight.enabled;
     sb.AppendLine($"  State Activating: {(actOk ? "PASS" : "FAIL")}");

     controller.SetState(GateState.Active);
     bool activeOk = controller.PortalSurface.gameObject.activeSelf && controller.PortalLight.enabled && controller.TeleportTrigger.enabled;
     sb.AppendLine($"  State Active: {(activeOk ? "PASS" : "FAIL")}");
    } else {
     sb.AppendLine("  [FAIL] TeleportGateController MISSING.");
    }

    // 4. Capture screenshot of the gate in Active state
    cam.transform.position = new Vector3(0, 2.2f, -5.5f);
    cam.transform.LookAt(gateObj.transform.position + Vector3.up * 2.2f);

    var rt = new RenderTexture(640, 640, 24);
    cam.targetTexture = rt;
    cam.Render();
    RenderTexture.active = rt;

    var shot = new Texture2D(640, 640, TextureFormat.RGB24, false);
    shot.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
    shot.Apply();

    RenderTexture.active = null;
    cam.targetTexture = null;
    UnityEngine.Object.DestroyImmediate(rt);

    string outPng = Path.Combine(artifactDir, $"gate_render_realm_{r}.png");
    File.WriteAllBytes(outPng, shot.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(shot);
    sb.AppendLine($"  Captured render: gate_render_realm_{r}.png");

    UnityEngine.Object.DestroyImmediate(dummyWorld);
   }

   UnityEngine.Object.DestroyImmediate(camObj);
   UnityEngine.Object.DestroyImmediate(sunObj);

   string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation/gate-hierarchy-probe.txt"));
   Directory.CreateDirectory(Path.GetDirectoryName(outPath));
   File.WriteAllText(outPath, sb.ToString());
   Debug.Log(sb.ToString());
  }
 }
}
