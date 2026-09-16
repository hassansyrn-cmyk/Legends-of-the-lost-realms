using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class AudioProbe {
  public static void Run() {
   var sb = new StringBuilder();
   sb.AppendLine("=== AUDIO ASSETS VALIDATION ===");
   string[] keys = new[] {
    "sfx_step",
    "sfx_jump",
    "sfx_double_jump",
    "sfx_player_dash",
    "sfx_blade",
    "sfx_impact",
    "sfx_ember_cast",
    "sfx_frost_cast",
    "sfx_gale_cast",
    "sfx_coin",
    "sfx_gem",
    "sfx_weapon_pickup",
    "sfx_boss_heartwood_roar",
    "sfx_boss_sunscar_roar",
    "sfx_boss_whiteout_roar",
    "sfx_boss_lavaboss_roar"
   };

   int passed = 0;
   foreach (var k in keys) {
    var clip = Resources.Load<AudioClip>("Audio/" + k);
    if (clip) {
     sb.AppendLine($"[PASS] {k} => length={clip.length:F2}s, channels={clip.channels}, freq={clip.frequency}Hz");
     passed++;
    } else {
     sb.AppendLine($"[FAIL] {k} => NOT FOUND IN RESOURCES!");
    }
   }
   sb.AppendLine($"Summary: {passed}/{keys.Length} passed.");

   string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Validation"));
   Directory.CreateDirectory(outDir);
   File.WriteAllText(Path.Combine(outDir, "audio-probe.txt"), sb.ToString());
   Debug.Log("AUDIO_PROBE_COMPLETE: " + passed + "/" + keys.Length);
  }
 }
}
