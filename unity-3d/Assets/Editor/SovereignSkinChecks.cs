using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 public static class SovereignSkinChecks {
  sealed class Rolls:System.Random {
   readonly Queue<int> values;
   public Rolls(params int[] rolls){values=new Queue<int>(rolls);}
   public override int Next(int min,int max){int value=values.Count>0?values.Dequeue():min;if(value<min||value>=max)throw new Exception("Invalid test roll");return value;}
  }
  static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
  public static void Run(){
   Require(SkinCatalog.Count==7,"Expected seven skins");
   var oldSave=JsonUtility.FromJson<Progress>("{\"equippedSkin\":5,\"skins\":[0,1,2,3,4,5]}");oldSave.NormalizeSkins();
   Require(oldSave.equippedSkin==5&&oldSave.skins.Count==6&&!oldSave.skins.Contains(6),"Existing save changed or premium granted for free");
   var owned=new Progress{equippedSkin=6,skins=new List<int>{0,6,6,-1,99}};owned.NormalizeSkins();
   owned=JsonUtility.FromJson<Progress>(JsonUtility.ToJson(owned));owned.NormalizeSkins();
   Require(owned.equippedSkin==6&&owned.skins.Count==2&&owned.skins.Contains(6),"Premium ownership failed to persist");
   var counts=new int[7];int resources=0,weapons=0;
   // Exhaust the equally likely category, premium ticket and standard outfit rolls.
   for(int category=0;category<100;category++)for(int ticket=0;ticket<30;ticket++)for(int outfit=0;outfit<5;outfit++){
    var reward=ChestSystem.Open(ChestType.Epic,new Progress(),category>=85?new Rolls(category,ticket,outfit):new Rolls(category));
    if(reward.Type==ChestRewardType.Skin)counts[(int)reward.Skin]++;
    else if(reward.Type==ChestRewardType.Resources)resources++;else weapons++;
   }
   Require(counts[6]==75,"Premium must be exactly 0.5% per Epic Chest");
   for(int id=1;id<=5;id++)Require(counts[id]==435,"Existing outfit must be exactly 2.9% per Epic Chest");
   Require(resources==2250&&weapons==10500,"Non-outfit drop rates changed");
   for(int category=0;category<100;category++)foreach(var type in new[]{ChestType.Normal,ChestType.Golden})
    Require(ChestSystem.Open(type,new Progress(),new Rolls(category)).Type!=ChestRewardType.Skin,"Outfit leaked to lower chest tier");
   var save=new Progress();var first=ChestSystem.Open(ChestType.Epic,save,new Rolls(85,0));
   Require(first.Skin==SkinId.AetherSovereign&&first.Rarity==RewardRarity.Legendary&&!first.IsDuplicate,"Premium reward/reveal mismatch");
   int gold=save.coins,gems=save.gems;var duplicate=ChestSystem.Open(ChestType.Epic,save,new Rolls(85,0));
   Require(duplicate.IsDuplicate&&save.coins==gold+600&&save.gems==gems+8&&save.skins.Count==2,"Duplicate compensation failed");
   Debug.Log("SOVEREIGN_ECONOMY_CHECKS_PASSED: 15000 exhaustive Epic outcomes; 0.5% legendary, 2.9% each epic; migration, persistence, tier exclusions and duplicates");
  }
 }
}
