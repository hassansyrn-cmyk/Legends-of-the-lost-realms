using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 // One bestiary page per enemy Kind (0..21). Weakness comes from Enemy.WeaknessOf so the
 // codex can never drift from the real combat table.
 public sealed class BestiaryEntry {
  public string Name,Habitat,Behavior;
  public BestiaryEntry(string name,string habitat,string behavior){Name=name;Habitat=habitat;Behavior=behavior;}
 }

 public sealed class Achievement {
  public string Id,Title,Description;public int Target,RewardGold,RewardGems;public Func<Progress,int> Current;
  public Achievement(string id,string title,string description,int target,int gold,int gems,Func<Progress,int> current){Id=id;Title=title;Description=description;Target=target;RewardGold=gold;RewardGems=gems;Current=current;}
  public int Value(Progress s)=>Mathf.Min(Target,Current(s));
  public bool Done(Progress s)=>Current(s)>=Target;
 }

 public static class Codex {
  public const int Kinds=22;
  public static bool Dirty;
  public static readonly string[] ElementNames={"Ember","Frost","Gale"};

  public static readonly BestiaryEntry[] Entries={
   new BestiaryEntry("Goblin Raider","Verdant Kingdom","Scrappy melee raider that rushes the moment it spots you."),
   new BestiaryEntry("Imp","Burning Dunes","Quick demon that darts in for bites. Light armor, low health."),
   new BestiaryEntry("Goblin Scout","Verdant Kingdom","Patrols its island and strafes while waiting for an opening."),
   new BestiaryEntry("Frostling","Frozen Peaks","Chilled brawler. Stay mobile so its swings land on empty snow."),
   new BestiaryEntry("Cinder Demon","Emberfall","Heavy hitter wreathed in embers. Telegraphs every strike."),
   new BestiaryEntry("Shield Goblin","Verdant Kingdom","Blocks frontal blows. Strike from behind, use a power, or break its guard with a heavy weapon."),
   new BestiaryEntry("Elemental","All realms","Towering conjured construct with punishing melee."),
   new BestiaryEntry("Hex Caster","Verdant Kingdom","Ranged spellcaster. Close the gap or dodge the bolt sideways."),
   new BestiaryEntry("Heartwood Colossus","Guardian of the Verdant Kingdom","Golem guardian: combos, ground slam and a phase-three charge. Stagger it with heavy hits."),
   new BestiaryEntry("Sunscar Titan","Guardian of the Burning Dunes","Golem guardian that summons allies and slams the ground."),
   new BestiaryEntry("Whiteout Guardian","Guardian of the Frozen Peaks","Golem guardian that grows faster each phase."),
   new BestiaryEntry("Wisp Flyer","Sky islands","Hovers above the ground. Jump attacks and ranged powers reach it."),
   new BestiaryEntry("Bomber","Burning Dunes","Fragile and explosive. Kill it at range or step out of the blast."),
   new BestiaryEntry("Summoner","Frozen Peaks","Keeps its distance and calls reinforcements. Priority target."),
   new BestiaryEntry("Elite Brute","Late chapters","Much tougher than common foes and hits hard. Fight it with dodges, not trades."),
   new BestiaryEntry("Skeleton","Ruins","Brittle fighter that attacks in steady rhythm."),
   new BestiaryEntry("Briar Goblin","Verdant Kingdom","Thorny goblin that favors close, fast pokes."),
   new BestiaryEntry("Winged Demon","Emberfall","Ranged flyer that spits fire. Keep moving across the island."),
   new BestiaryEntry("Spider","Ruins","Tiny and quick. Easy to miss, so aim at your feet."),
   new BestiaryEntry("Footman","Frozen Peaks","Disciplined soldier with solid damage and steady pressure."),
   new BestiaryEntry("Dog Knight","Emberfall","Armored knight that punishes wild swinging."),
   new BestiaryEntry("Emberfall Warden","Guardian of Emberfall","Final guardian wielding a juggernaut blade. Learn its telegraphs.")
  };

  public static bool IsBoss(int kind)=>kind==8||kind==9||kind==10||kind==21;
  public static string Weakness(int kind)=>ElementNames[Mathf.Clamp(Enemy.WeaknessOf(kind),0,2)];

  public static void Normalize(Progress s){
   if(s.bestiary==null||s.bestiary.Length!=Kinds){var old=s.bestiary;s.bestiary=new int[Kinds];if(old!=null)for(int i=0;i<old.Length&&i<Kinds;i++)s.bestiary[i]=Mathf.Max(0,old[i]);}
   if(s.achievements==null)s.achievements=new List<string>();
   for(int i=s.achievements.Count-1;i>=0;i--)if(string.IsNullOrEmpty(s.achievements[i])||s.achievements.IndexOf(s.achievements[i])!=i)s.achievements.RemoveAt(i);
   s.bossKills=Mathf.Max(0,s.bossKills);s.lifetimeParries=Mathf.Max(0,s.lifetimeParries);s.lifetimeDodges=Mathf.Max(0,s.lifetimeDodges);s.bestCombo=Mathf.Max(0,s.bestCombo);
  }

  // Returns true the first time this kind is discovered.
  public static bool Record(Progress s,int kind,bool boss){
   Normalize(s);
   kind=Mathf.Clamp(kind,0,Kinds-1);
   bool first=s.bestiary[kind]==0;
   s.bestiary[kind]=Mathf.Min(99999,s.bestiary[kind]+1);
   if(boss||IsBoss(kind))s.bossKills++;
   Dirty=true;
   return first;
  }

  public static int Discovered(Progress s){if(s.bestiary==null)return 0;int n=0;for(int i=0;i<s.bestiary.Length;i++)if(s.bestiary[i]>0)n++;return n;}
  public static int TotalKills(Progress s){if(s.bestiary==null)return 0;long n=0;for(int i=0;i<s.bestiary.Length;i++)n+=s.bestiary[i];return (int)Mathf.Min(int.MaxValue,n);}
  static int Stars(Progress s){int n=0;if(s.stars!=null)foreach(int v in s.stars)n+=Mathf.Max(0,v);return n;}
  static int Cleared(Progress s){int n=0;if(s.stars!=null)foreach(int v in s.stars)if(v>0)n++;return n;}
  static int Upgrades(Progress s)=>s.healthRank+s.powerRank+s.arsenalRank+s.aetherRank+s.moxieRank+s.tempoRank+s.windRank;

  public static readonly Achievement[] All={
   new Achievement("kill_1","First Blood","Defeat your first enemy.",1,20,0,TotalKills),
   new Achievement("kill_100","Hunter","Defeat 100 enemies.",100,60,0,TotalKills),
   new Achievement("kill_500","Realm Slayer","Defeat 500 enemies.",500,200,2,TotalKills),
   new Achievement("boss_1","Guardian Breaker","Defeat a realm guardian.",1,80,1,s=>s.bossKills),
   new Achievement("boss_4","Crown of Guardians","Defeat 4 realm guardians.",4,250,3,s=>s.bossKills),
   new Achievement("clear_1","Into the Mist","Clear your first chapter.",1,25,0,Cleared),
   new Achievement("clear_5","Pathfinder","Clear 5 chapters.",5,75,0,Cleared),
   new Achievement("clear_10","Peak Climber","Clear 10 chapters.",10,150,1,Cleared),
   new Achievement("clear_15","Heart of Realms","Clear all 15 chapters.",15,400,5,Cleared),
   new Achievement("stars_15","Starlit","Earn 15 stars.",15,60,0,Stars),
   new Achievement("stars_30","Constellation","Earn 30 stars.",30,150,1,Stars),
   new Achievement("stars_45","Perfectionist","Earn every star (45).",45,350,4,Stars),
   new Achievement("weapons_5","Armed","Collect 5 weapons.",5,50,0,s=>s.weapons==null?0:s.weapons.Count),
   new Achievement("weapons_15","Armory","Collect 15 weapons.",15,150,1,s=>s.weapons==null?0:s.weapons.Count),
   new Achievement("weapons_30","Arsenal Master","Collect 30 weapons.",30,300,3,s=>s.weapons==null?0:s.weapons.Count),
   new Achievement("skins_3","Fresh Look","Own 3 outfits.",3,50,0,s=>s.skins==null?0:s.skins.Count),
   new Achievement("skins_6","Wardrobe Complete","Own every outfit.",6,200,2,s=>s.skins==null?0:s.skins.Count),
   new Achievement("codex_6","Field Notes","Discover 6 bestiary entries.",6,50,0,Discovered),
   new Achievement("codex_12","Naturalist","Discover 12 bestiary entries.",12,120,1,Discovered),
   new Achievement("codex_22","Complete Bestiary","Discover every bestiary entry.",22,300,3,Discovered),
   new Achievement("parry_10","Steel Wall","Parry 10 attacks.",10,50,0,s=>s.lifetimeParries),
   new Achievement("parry_50","Riposte Artist","Parry 50 attacks.",50,150,1,s=>s.lifetimeParries),
   new Achievement("dodge_10","Light Feet","Perfect-dodge 10 attacks.",10,50,0,s=>s.lifetimeDodges),
   new Achievement("dodge_50","Untouchable","Perfect-dodge 50 attacks.",50,150,1,s=>s.lifetimeDodges),
   new Achievement("combo_10","Rhythm","Land a 10-hit combo.",10,40,0,s=>s.bestCombo),
   new Achievement("combo_25","Whirlwind","Land a 25-hit combo.",25,120,1,s=>s.bestCombo),
   new Achievement("upgrade_5","Sanctuary Apprentice","Buy 5 upgrade ranks.",5,60,0,Upgrades),
   new Achievement("upgrade_21","Sanctuary Master","Max every upgrade track.",21,300,3,Upgrades)
  };

  public static int UnlockedCount(Progress s){int n=0;if(s.achievements!=null)foreach(var a in All)if(s.achievements.Contains(a.Id))n++;return n;}

  // Unlocks every newly satisfied achievement, pays its reward, and returns the new ones.
  public static List<Achievement> Unlock(Progress s){
   Normalize(s);
   List<Achievement> fresh=null;
   foreach(var a in All){
    if(s.achievements.Contains(a.Id)||!a.Done(s))continue;
    s.achievements.Add(a.Id);s.coins+=a.RewardGold;s.gems+=a.RewardGems;
    (fresh??=new List<Achievement>()).Add(a);Dirty=true;
   }
   return fresh??new List<Achievement>();
  }
 }

 // Lightweight watcher: unlocks achievements, toasts them and flushes dirty codex data to disk.
 public sealed class CodexTracker:MonoBehaviour {
  float nextCheck,nextFlush;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot(){var go=new GameObject("Codex tracker");DontDestroyOnLoad(go);go.AddComponent<CodexTracker>();}
  void Update(){
   var g=RealmGame.I;if(g==null||g.Save==null)return;
   float now=Time.unscaledTime;
   if(now>=nextCheck){
    nextCheck=now+1.5f;
    var fresh=Codex.Unlock(g.Save);
    if(fresh.Count>0){
     var a=fresh[0];string reward=(a.RewardGold>0?"+"+a.RewardGold+" Gold":"")+(a.RewardGems>0?"  +"+a.RewardGems+" Gems":"");
     g.Tell("Achievement unlocked: "+a.Title+"   "+reward+(fresh.Count>1?"   (+"+(fresh.Count-1)+" more)":""),4.5f);
     g.Sound("upgrade");
    }
   }
   if(Codex.Dirty&&now>=nextFlush){nextFlush=now+5f;Codex.Dirty=false;g.Persist();}
  }
  void OnApplicationPause(bool paused){if(paused&&RealmGame.I!=null&&Codex.Dirty){Codex.Dirty=false;RealmGame.I.Persist();}}
 }
}
