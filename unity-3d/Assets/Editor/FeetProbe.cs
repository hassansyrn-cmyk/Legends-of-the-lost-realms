using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LostRealms {
 public static class FeetProbe {
  [MenuItem("Lost Realms/Probes/Feet Probe")]
  public static void Run() {
   Debug.Log("=== FEET PROBE START ===");
   var report = new System.Collections.Generic.List<string>();
   var idle = Resources.Load<AnimationClip>("Animations/Aster/idle");
   if (!idle) throw new Exception("Missing idle.anim");

   string[] characters = { "Aster", "Aster_Assassin", "Aster_Knight", "Aster_KnightArmored", "Aster_Viking", "Aster_DesertWarrior" };

   foreach (var name in characters) {
    var prefab = Resources.Load<GameObject>("Characters/" + name);
    if (!prefab) {
     Debug.LogError($"Missing prefab: {name}");
     continue;
    }

    var go = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
    var anim = go.GetComponentInChildren<Animator>();

    // 1. Measure bind pose
    var renderers = go.GetComponentsInChildren<Renderer>();
    Bounds bBind = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++) bBind.Encapsulate(renderers[i].bounds);

    // 2. Play idle clip with PlayableGraph (exact runtime mechanism)
    if (anim) {
     var graph = UnityEngine.Playables.PlayableGraph.Create();
     var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "Anim", anim);
     var clipPlayable = UnityEngine.Animations.AnimationClipPlayable.Create(graph, idle);
     output.SetSourcePlayable(clipPlayable);
     graph.Evaluate(0.1f);
     graph.Destroy();
    }

    // 3. Measure animated pose
    Bounds bAnim = renderers[0].bounds;
    for (int i = 1; i < renderers.Length; i++) bAnim.Encapsulate(renderers[i].bounds);

    Transform leftFoot = anim ? anim.GetBoneTransform(HumanBodyBones.LeftFoot) : null;
    Transform rightFoot = anim ? anim.GetBoneTransform(HumanBodyBones.RightFoot) : null;
    Transform leftToes = anim ? anim.GetBoneTransform(HumanBodyBones.LeftToes) : null;
    Transform rightToes = anim ? anim.GetBoneTransform(HumanBodyBones.RightToes) : null;

    string logLine = $"[FEET_CHECK] {name}: bindMinY={bBind.min.y:F4}, animMinY={bAnim.min.y:F4}, LFoot.y={(leftFoot ? leftFoot.position.y : -99):F4}, RFoot.y={(rightFoot ? rightFoot.position.y : -99):F4}, LToes.y={(leftToes ? leftToes.position.y : -99):F4}";
    Debug.Log(logLine);
    report.Add(logLine);

    UnityEngine.Object.DestroyImmediate(go);
   }

   try {
    string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Temp/opencode"));
    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    File.WriteAllLines(Path.Combine(dir, "feet_probe_output.txt"), report);
   } catch (Exception ex) {
    Debug.LogWarning("Failed to write report file: " + ex.Message);
   }
   Debug.Log("=== FEET PROBE COMPLETE ===");
  }
 }
}
