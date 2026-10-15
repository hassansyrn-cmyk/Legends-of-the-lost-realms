using System;
using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public partial class Progress {
  public int[] masteryChoices=new int[4];public List<int> relics=new List<int>();public int relicA=-1,relicB=-1,wayfinderQuest,cryptRoom;public bool cryptCleared;
  public void NormalizeBuild(){
   if(masteryChoices==null||masteryChoices.Length!=4){var old=masteryChoices;masteryChoices=new int[4];if(old!=null)Array.Copy(old,masteryChoices,Math.Min(4,old.Length));}
   for(int i=0;i<4;i++)masteryChoices[i]=Mathf.Clamp(masteryChoices[i],0,2);
   if(relics==null)relics=new List<int>();for(int i=relics.Count-1;i>=0;i--)if(relics[i]<0||relics[i]>7||relics.IndexOf(relics[i])!=i)relics.RemoveAt(i);
   if(stars!=null&&stars.Length>0&&stars[0]>0&&!relics.Contains(0))relics.Add(0);
   if(!relics.Contains(relicA))relicA=-1;if(!relics.Contains(relicB)||relicB==relicA)relicB=-1;
   wayfinderQuest=Mathf.Clamp(wayfinderQuest,0,5);cryptRoom=Mathf.Clamp(cryptRoom,0,5);if(cryptCleared)cryptRoom=5;
  }
 }
 public static class BuildProgression {
  public static readonly string[] Paths={"BLADE","HEAVY","POLEARM","UNARMED"};
  public static readonly string[,] Perks={{"Duelist","Flow"},{"Rupture","Resolve"},{"Precision","Conduit"},{"Pressure","Meditation"}};
  public static readonly string[,] Descriptions={{"Ripostes deal +25% damage.","Third-combo swings restore 8 Energy."},{"Charged melee hits stagger non-guardians for 0.45s.","Perfect defense restores 1 HP; 8s cooldown."},{"Outer-reach melee hits deal +20% damage.","Spells cost 3 less Energy while using a polearm."},{"Third-combo strikes deal +25% damage.","Perfect defense restores 12 extra Energy."}};
  public static readonly string[] RelicNames={"Dawn Sigil","Ember Seal","Duelist Knot","Skyglass","Wardstone","Siphon Shard","Frost Emblem","Wayfinder Heart"};
  public static readonly string[] RelicEffects={"Perfect parry restores 8 extra Energy.","Charged melee hits inflict a 3s burn.","Ripostes deal +15% damage.","Airborne spells after a double jump cost 5 less Energy.","Perfect dodge restores 1 HP; 8s cooldown.","Defeating a foe restores 6 Energy.","Matching-weakness melee hits deal +10% damage.","Restore and ritual channels finish 25% faster."};
  public static readonly string[] RelicSources={"Finish Mosslight Trail.","Defeat the Waterfall Sentinel in chapter 2.","Defeat the Rootbound Duelist in chapter 3.","Defeat the Burial Chamber Warden in chapter 6.","Defeat the Crystal Sentinel in chapter 9.","Defeat the Rampart Hexer in chapter 12.","Clear the Oathkeeper's Crypt.","Complete Lyra's Wayfinder quest."};
  public static bool MasteryUnlocked(Progress p)=>p.stars!=null&&p.stars.Length>1&&p.stars[1]>0;
  public static int Path(WeaponDefinition w){string style=WeaponCatalog.AttackStyle(w);return style=="chop"?1:style=="spear"?2:style=="unarmed"?3:0;}
  public static bool Perk(Progress p,int path,int choice,WeaponDefinition w)=>Path(w)==path&&p.masteryChoices[path]==choice;
  public static bool Has(Progress p,int id)=>p.relicA==id||p.relicB==id;
  public static bool Choose(Progress p,int path,int choice){
   if(path<0||path>3||choice<1||choice>2||!MasteryUnlocked(p))return false;
   int current=p.masteryChoices[path];if(current==choice)return true;int cost=current==0?0:25;if(p.coins<cost)return false;p.coins-=cost;p.masteryChoices[path]=choice;return true;
  }
  public static bool Equip(Progress p,int id,int slot){if(slot<0||slot>1||id<-1||id>7||id>=0&&!p.relics.Contains(id))return false;if(id>=0&&(slot==0?p.relicB:p.relicA)==id)return false;if(slot==0)p.relicA=id;else p.relicB=id;return true;}
  public static bool Unlock(RealmGame g,int id){if(id<0||id>7||g.Save.relics.Contains(id))return false;g.Save.relics.Add(id);g.Persist();g.Tell("RELIC DISCOVERED: "+RelicNames[id]+". Equip it in Sanctuary / Mastery & Relics.",5f);g.Sound("upgrade");return true;}
  public static float DamageMultiplier(Progress p,WeaponDefinition w,bool riposte,int combo,bool outer,bool weakness){float factor=1f;if(riposte&&Perk(p,0,1,w))factor*=1.25f;if(riposte&&Has(p,2))factor*=1.15f;if(combo==3&&Perk(p,3,1,w))factor*=1.25f;if(outer&&Perk(p,2,1,w))factor*=1.2f;if(weakness&&Has(p,6))factor*=1.1f;return Mathf.Min(1.6f,factor);}
  public static float SpellCost(Progress p,WeaponDefinition w,float baseline,bool doubleJump){float cost=baseline;if(Perk(p,2,2,w))cost-=3f;if(doubleJump&&Has(p,3))cost-=5f;return Mathf.Max(7f,cost);}
  public static float ChannelMultiplier(Progress p)=>Has(p,7)?.75f:1f;
  public static void OnSecret(RealmGame g,int chapter){int relic=chapter==2?1:chapter==3?2:chapter==6?3:chapter==9?4:chapter==12?5:-1;if(relic>=0)Unlock(g,relic);if(chapter==2&&g.Save.wayfinderQuest==1){g.Save.wayfinderQuest=2;g.Persist();g.Tell("Lyra: The lost map names the Temple of Keys. Recover both keys in chapter 6.",6f);}}
  public static void OnObjective(RealmGame g,int chapter,int completed){if(chapter==6&&completed==2&&g.Save.wayfinderQuest==2){g.Save.wayfinderQuest=g.Save.cryptCleared?4:3;g.Persist();g.Tell("Lyra: The cipher reveals the Oathkeeper's Crypt. Find its entrance in chapter 3's optional clearing.",6f);}}
  public static string QuestObjective(Progress p)=>p.wayfinderQuest==0?"Archivist Lyra needs help tracing a vanished wayfinder. Accept her commission.":p.wayfinderQuest==1?"Recover the lost map: defeat the Waterfall Sentinel in chapter 2.":p.wayfinderQuest==2?"Decode the map: recover both temple keys in chapter 6.":p.wayfinderQuest==3?"Honor the oath: clear the Oathkeeper's Crypt off chapter 3.":p.wayfinderQuest==4?"Return to Lyra here to receive the Wayfinder Heart and 200 Gold.":"The wayfinder's trail is restored. Lyra's commission is complete.";
  public static bool QuestAction(RealmGame g){if(g.Save.wayfinderQuest==0){g.Save.wayfinderQuest=1;g.Persist();return true;}if(g.Save.wayfinderQuest!=4)return false;g.Save.wayfinderQuest=5;g.Save.coins+=200;Unlock(g,7);g.Persist();return true;}
 }
 public sealed class HeroBuildEffects:MonoBehaviour {
  float nextResolve,nextWard;
  public void PerfectDefense(bool parry){var g=RealmGame.I;var h=g.Player;var p=g.Save;int energy=(parry&&BuildProgression.Has(p,0)?8:0)+(BuildProgression.Perk(p,3,2,g.CurrentWeapon)?12:0);h.Energy=Mathf.Min(100,h.Energy+energy);
   bool heal=false;if(BuildProgression.Perk(p,1,2,g.CurrentWeapon)&&g.Elapsed>=nextResolve){nextResolve=g.Elapsed+8f;heal=true;}if(!parry&&BuildProgression.Has(p,4)&&g.Elapsed>=nextWard){nextWard=g.Elapsed+8f;heal=true;}
   if(heal&&h.Health<h.MaxHealth){h.Health=Mathf.Min(h.MaxHealth,h.Health+1);DamageTip.Show(h.transform.position+Vector3.up*1.8f,"+1 HP",new Color(.5f,1f,.75f));}
  }
 }
}