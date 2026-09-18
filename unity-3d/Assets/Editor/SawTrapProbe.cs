using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class SawTrapProbe {
  [InitializeOnLoadMethod]
  [MenuItem("Lost Realms/Diagnose Saw Trap")]
  public static void Diagnose() {
   var sb = new StringBuilder();
   sb.AppendLine("=== SAW TRAP PROBE DIAGNOSTIC ===");

   var root = new GameObject("TestRoot");
   Vector3 start = new Vector3(-2.6f, 0, 0);
   Vector3 end = new Vector3(2.6f, 0, 0);
   var sawTrap = SawTrap.Place(root.transform, start, end, Color.red);

   float length = Vector3.Distance(start, end);
   float halfRail = length * 0.5f;
   float bladeRadius = 0.925f;
   float safetyMargin = 0.15f;

   var tr = sawTrap.transform.Find("RunicTrack");
   sb.AppendLine($"Track Object: {(tr != null ? tr.name : "null")}");
   sb.AppendLine($"Rail Total Length: {length:F2}m (Span: [{-halfRail:F2}, {+halfRail:F2}])");

   var carriage = sawTrap.transform.Find("SawCarriage");
   sb.AppendLine($"Carriage: {(carriage != null ? carriage.name : "null")}");

   var bladeSpin = carriage ? carriage.Find("BladeSpinRoot") : null;
   sb.AppendLine($"BladeSpinRoot: {(bladeSpin != null ? bladeSpin.name : "null")}");

   var sawMesh = bladeSpin ? bladeSpin.Find("RunicSawMesh") : null;
   sb.AppendLine($"RunicSawMesh: {(sawMesh != null ? sawMesh.name : "null")}");

   var trackCol = sawTrap.GetComponent<BoxCollider>();
   sb.AppendLine($"Track BoxCollider: {(trackCol != null ? $"size={trackCol.size}, center={trackCol.center}" : "null")}");

   // Verify limits
   var field = typeof(SawTrap).GetField("halfLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
   float halfLength = field != null ? (float)field.GetValue(sawTrap) : -1f;
   sb.AppendLine($"SawTrap.halfLength: {halfLength:F3}m");

   float expectedHalfLength = Mathf.Max(0.2f, halfRail - bladeRadius - safetyMargin);
   sb.AppendLine($"Expected halfLength: {expectedHalfLength:F3}m");

   bool halfLengthMatches = Mathf.Abs(halfLength - expectedHalfLength) < 0.001f;
   sb.AppendLine($"CHECK halfLength matches formula: {halfLengthMatches}");

   float maxBladeReach = halfLength + bladeRadius;
   float marginAtEnd = halfRail - maxBladeReach;
   sb.AppendLine($"Max blade reach at maxPos (+{halfLength:F3}m): {maxBladeReach:F3}m");
   sb.AppendLine($"Margin at rail end (+{halfRail:F3}m): {marginAtEnd:F3}m (should be >= {safetyMargin:F2}m)");
   bool insideRail = marginAtEnd >= (safetyMargin - 0.001f);
   sb.AppendLine($"CHECK saw blade completely inside rail at max: {insideRail}");

   float minBladeReach = -halfLength - bladeRadius;
   float marginAtStart = minBladeReach - (-halfRail);
   sb.AppendLine($"Min blade reach at minPos (-{halfLength:F3}m): {minBladeReach:F3}m");
   sb.AppendLine($"Margin at rail start ({-halfRail:F3}m): {marginAtStart:F3}m (should be >= {safetyMargin:F2}m)");
   bool insideRailMin = marginAtStart >= (safetyMargin - 0.001f);
   sb.AppendLine($"CHECK saw blade completely inside rail at min: {insideRailMin}");

   bool strokeWider = (halfLength * 2f) > 3.0f;
   sb.AppendLine($"Total travel stroke: {halfLength * 2f:F2}m (stroke > 3.0m: {strokeWider})");

   Object.DestroyImmediate(root);

   string outPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, "../Validation/saw-trap-probe.txt"));
   System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));
   System.IO.File.WriteAllText(outPath, sb.ToString());
   Debug.Log(sb.ToString());
  }
 }
}
