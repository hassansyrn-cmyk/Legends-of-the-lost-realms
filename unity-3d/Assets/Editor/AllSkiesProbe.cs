using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class AllSkiesProbe {
  [MenuItem("Lost Realms/Diagnose All Skies")]
  public static void Diagnose() {
   var sb = new StringBuilder();
   sb.AppendLine("=== ALL REALMS 360 SKY PROBE ===");

   string artifactDir = @"C:\Users\X1 YOGA\.gemini\antigravity\brain\8161ef4f-085a-40af-8fa6-849260c8d0c7";

   var camObj = new GameObject("SkyProbeCam");
   var cam = camObj.AddComponent<Camera>();
   cam.clearFlags = CameraClearFlags.Skybox;
   cam.fieldOfView = 60;
   cam.transform.position = Vector3.zero;

   string[] realmNames = new[] { "Verdant (Realm 0)", "Desert (Realm 1)", "Snow (Realm 2)", "Lava (Realm 3)" };

   for (int r = 0; r < 4; r++) {
    var tex = Resources.Load<Texture2D>($"Art/Realm{r}_Sky360");
    if (!tex) {
     sb.AppendLine($"[FAIL] {realmNames[r]}: Missing Texture2D at Art/Realm{r}_Sky360");
     continue;
    }

    var dummyWorld = new GameObject($"World_{r}");
    var world = dummyWorld.AddComponent<RealmWorld>();
    RealmAtmosphere.Apply(world, r);

    var sky = RenderSettings.skybox;
    if (!sky) {
     sb.AppendLine($"[FAIL] {realmNames[r]}: RenderSettings.skybox is null");
     UnityEngine.Object.DestroyImmediate(dummyWorld);
     continue;
    }

    sb.AppendLine($"[PASS] {realmNames[r]}: Sky material={sky.name}, Shader={sky.shader?.name}, Texture={tex.name} ({tex.width}x{tex.height})");

    // Capture in-game view looking forward along the route (+Z)
    cam.transform.rotation = Quaternion.Euler(8, 0, 0);
    var rt = new RenderTexture(640, 360, 24);
    cam.targetTexture = rt;
    cam.Render();
    RenderTexture.active = rt;

    var screenshot = new Texture2D(640, 360, TextureFormat.RGB24, false);
    screenshot.ReadPixels(new Rect(0, 0, 640, 360), 0, 0);
    screenshot.Apply();

    RenderTexture.active = null;
    cam.targetTexture = null;
    UnityEngine.Object.DestroyImmediate(rt);

    int magenta = 0;
    var pixels = screenshot.GetPixels();
    foreach (var c in pixels) {
     if (c.r > 0.75f && c.g < 0.35f && c.b > 0.75f) magenta++;
    }

    float magPercent = ((float)magenta / pixels.Length) * 100f;
    sb.AppendLine($"       Camera capture magenta: {magPercent:F1}% (0% expected)");

    string outPng = Path.Combine(artifactDir, $"sky_realm_{r}.png");
    File.WriteAllBytes(outPng, screenshot.EncodeToPNG());
    UnityEngine.Object.DestroyImmediate(screenshot);
    UnityEngine.Object.DestroyImmediate(dummyWorld);
   }

   UnityEngine.Object.DestroyImmediate(camObj);
   RenderSettings.skybox = null;

   string outPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation/all-skies-probe.txt"));
   Directory.CreateDirectory(Path.GetDirectoryName(outPath));
   File.WriteAllText(outPath, sb.ToString());
   Debug.Log(sb.ToString());
  }
 }
}
