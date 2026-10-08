using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class ExpansionStage1Tools {
  [MenuItem("Lost Realms/Expansion/Play Chapter 2 — Whispering Falls")] static void Forest()=>Play(2);
  [MenuItem("Lost Realms/Expansion/Play Chapter 6 — Temple of Keys")] static void Desert()=>Play(6);
  [MenuItem("Lost Realms/Expansion/Play Chapter 9 — Crystal Hollow")] static void Snow()=>Play(9);
  static void Play(int chapter){if(!EditorApplication.isPlaying||!RealmGame.I){Debug.Log("Enter Play mode first, then select the expansion chapter.");return;}RealmGame.I.LoadLevel(chapter);}
  [MenuItem("Lost Realms/Expansion/Test Weapon/Duskblade")] static void Dusk()=>Equip(WeaponId.Duskblade);
  [MenuItem("Lost Realms/Expansion/Test Weapon/Soulreaper")] static void Reaper()=>Equip(WeaponId.Soulreaper);
  static void Equip(WeaponId weapon){if(EditorApplication.isPlaying&&RealmGame.I&&RealmGame.I.Player)RealmGame.I.EquipWeapon(weapon);else Debug.Log("Start a chapter first to test the weapon.");}
  [MenuItem("Lost Realms/Expansion/Export Pacing Summary")]
  static void Export(){
   string source=Path.Combine(Application.persistentDataPath,"campaign-pacing.json");if(!File.Exists(source)){Debug.Log("No human pacing attempts recorded yet. Finish or leave a chapter first.");return;}
   try{
    var history=JsonUtility.FromJson<CampaignPacing.History>(File.ReadAllText(source));if(history==null||history.runs==null)throw new Exception("Invalid pacing history");
    string folder=Path.Combine(Application.dataPath,"../Validation/ExpansionStage1");Directory.CreateDirectory(folder);
    var details=new StringBuilder("startedUtc,chapter,outcome,activeSeconds,pausedSeconds,retrySeconds,startWeapon,endWeapon,deaths,kills,initialEnemies,livingEnemies,healthRank,arsenalRank,powerRank,objectives,trialCompleted,optionalVisited,optionalCompleted\n");
    foreach(var r in history.runs)details.AppendLine(FormattableString.Invariant($"{r.startedUtc},{r.chapter},{r.outcome},{r.activeSeconds:F2},{r.pausedSeconds:F2},{r.retrySeconds:F2},{r.startWeapon},{r.endWeapon},{r.deaths},{r.kills},{r.initialEnemies},{r.livingEnemies},{r.healthRank},{r.arsenalRank},{r.powerRank},{r.objectives},{r.trialCompleted},{r.optionalVisited},{r.optionalCompleted}"));
    var summary=new StringBuilder("chapter,completedAttempts,medianActiveMinutes,minActiveMinutes,maxActiveMinutes,meanDeaths,optionalClearRate\n");
    foreach(var group in history.runs.Where(r=>r.outcome=="completed").GroupBy(r=>r.chapter).OrderBy(g=>g.Key)){
     var times=group.Select(r=>r.activeSeconds/60f).OrderBy(x=>x).ToArray();float median=(times[(times.Length-1)/2]+times[times.Length/2])*.5f;
     summary.AppendLine(FormattableString.Invariant($"{group.Key},{times.Length},{median:F2},{times.First():F2},{times.Last():F2},{group.Average(r=>r.deaths):F2},{group.Count(r=>r.optionalCompleted)/(float)times.Length:F2}"));
    }
    File.WriteAllText(Path.Combine(folder,"pacing-attempts.csv"),details.ToString());File.WriteAllText(Path.Combine(folder,"pacing-summary.csv"),summary.ToString());Debug.Log("Local pacing CSVs exported to "+folder+". Use the attempt rows to separate equipment and upgrade cohorts.");
   }catch(Exception e){Debug.LogWarning("Could not export pacing history: "+e.Message);}
  }
 }
}
