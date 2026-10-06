using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 public enum ChestType {
  Normal = 0,
  Golden = 1,
  Epic = 2
 }

 public enum RewardRarity {
  Common = 0,
  Rare = 1,
  Epic = 2
 }

 public enum ChestRewardType {
  Resources = 0,
  Weapon = 1,
  Skin = 2
 }

 public sealed class ChestReward {
  public ChestRewardType Type;
  public RewardRarity Rarity;
  public int Gold;
  public int Gems;
  public WeaponId Weapon;
  public SkinId Skin;
  public bool IsDuplicate;
  public string Title;
  public string Subtitle;

  public static ChestReward MakeResources(int gold, int gems, RewardRarity rarity, string title = null) {
   return new ChestReward {
    Type = ChestRewardType.Resources,
    Rarity = rarity,
    Gold = gold,
    Gems = gems,
    Title = title ?? (gems > 0 ? $"{gold} Gold  +  {gems} Gems" : $"{gold} Gold"),
    Subtitle = "Treasure Cache"
   };
  }

  public static ChestReward MakeWeapon(WeaponId weapon, bool isDuplicate, int dupGold, int dupGems, RewardRarity rarity) {
   var def = WeaponCatalog.Get(weapon);
   return new ChestReward {
    Type = ChestRewardType.Weapon,
    Rarity = rarity,
    Weapon = weapon,
    IsDuplicate = isDuplicate,
    Gold = isDuplicate ? dupGold : 0,
    Gems = isDuplicate ? dupGems : 0,
    Title = def.Name,
    Subtitle = isDuplicate ? $"Duplicate Refund: +{dupGold} Gold" + (dupGems > 0 ? $" +{dupGems} Gems" : "") : def.Summary
   };
  }

  public static ChestReward MakeSkin(SkinId skin, bool isDuplicate, int dupGold, int dupGems) {
   var def = SkinCatalog.Get((int)skin);
   return new ChestReward {
    Type = ChestRewardType.Skin,
    Rarity = RewardRarity.Epic,
    Skin = skin,
    IsDuplicate = isDuplicate,
    Gold = isDuplicate ? dupGold : 0,
    Gems = isDuplicate ? dupGems : 0,
    Title = def.Name + " (" + def.Title + ")",
    Subtitle = isDuplicate ? $"Duplicate Refund: +{dupGold} Gold + {dupGems} Gems" : def.StatSummary
   };
  }
 }

 public static class ChestSystem {
  // Costs
  public const int NormalGoldCost = 150;
  public const int GoldenGoldCost = 600;
  public const int GoldenGemCost = 10;
  public const int EpicGemCost = 35;

  // Curated list of in-level world drops
  public static readonly WeaponId[] WorldDrops = {
   WeaponId.LongSword,
   WeaponId.Axe,
   WeaponId.CurvedSword,
   WeaponId.AxeIron
  };

  // Special-hold weapons (12 weapons)
  public static readonly WeaponId[] RareHoldWeapons = {
   WeaponId.PureScythe,
   WeaponId.WardenPike,
   WeaponId.BrassFangs,
   WeaponId.EmberTorch,
   WeaponId.SageStaff,
   WeaponId.FantasyGreatsword,
   WeaponId.FierySword,
   WeaponId.OrnateCurvedBlade,
   WeaponId.AstralStaff,
   WeaponId.GlacierMaul,
   WeaponId.VoidReaper,
   WeaponId.FrostHalberd,
   WeaponId.VerdantFang
  };

  // Unlockable skins (5 skins, skin 0 Wanderer is starter)
  public static readonly SkinId[] UnlockableSkins = {
   SkinId.ShadowAssassin,
   SkinId.RoyalKnight,
   SkinId.ArmoredJuggernaut,
   SkinId.ValhallaViking,
   SkinId.DesertWarrior
  };

  static List<WeaponId> standardWeaponsCache;

  public static List<WeaponId> GetStandardWeapons() {
   if (standardWeaponsCache != null) return standardWeaponsCache;
   standardWeaponsCache = new List<WeaponId>();
   for (int i = 1; i <= WeaponCatalog.MaxId; i++) {
    var wid = (WeaponId)i;
    if (!WeaponCatalog.HasSpecialHold(wid)) {
     standardWeaponsCache.Add(wid);
    }
   }
   return standardWeaponsCache;
  }

  public static bool CanOpen(ChestType type, Progress save, bool useGemsForGolden = false) {
   if (save == null) return false;
   switch (type) {
    case ChestType.Normal:
     return save.coins >= NormalGoldCost;
    case ChestType.Golden:
     return useGemsForGolden ? save.gems >= GoldenGemCost : save.coins >= GoldenGoldCost;
    case ChestType.Epic:
     return save.gems >= EpicGemCost;
    default:
     return false;
   }
  }

  public static void DeductCost(ChestType type, Progress save, bool useGemsForGolden = false) {
   if (save == null) return;
   switch (type) {
    case ChestType.Normal:
     save.coins = Mathf.Max(0, save.coins - NormalGoldCost);
     break;
    case ChestType.Golden:
     if (useGemsForGolden) save.gems = Mathf.Max(0, save.gems - GoldenGemCost);
     else save.coins = Mathf.Max(0, save.coins - GoldenGoldCost);
     break;
    case ChestType.Epic:
     save.gems = Mathf.Max(0, save.gems - EpicGemCost);
     break;
   }
  }

  public static ChestReward Open(ChestType type, Progress save, System.Random rng = null) {
   if (rng == null) rng = new System.Random();
   if (save == null) save = new Progress();
   save.NormalizeWeapons();
   save.NormalizeSkins();

   int roll = rng.Next(0, 100);

   if (type == ChestType.Normal) {
    // Normal Chest: 65% Resources, 35% Standard Weapon
    if (roll < 65) {
     int gold = rng.Next(50, 151);
     int gems = rng.Next(0, 100) < 25 ? rng.Next(1, 3) : 0;
     save.coins += gold;
     save.gems += gems;
     return ChestReward.MakeResources(gold, gems, RewardRarity.Common);
    } else {
     var std = GetStandardWeapons();
     var wid = std[rng.Next(0, std.Count)];
     bool has = save.weapons != null && save.weapons.Contains((int)wid);
     if (has) {
      int dupGold = 100, dupGems = 1;
      save.coins += dupGold;
      save.gems += dupGems;
      return ChestReward.MakeWeapon(wid, true, dupGold, dupGems, RewardRarity.Common);
     } else {
      save.weapons.Add((int)wid);
      return ChestReward.MakeWeapon(wid, false, 0, 0, RewardRarity.Common);
     }
    }
   } else if (type == ChestType.Golden) {
    // Golden Chest: 30% Resources, 50% Standard Weapon, 20% Rare Special-Hold Weapon
    if (roll < 30) {
     int gold = rng.Next(200, 451);
     int gems = rng.Next(3, 7);
     save.coins += gold;
     save.gems += gems;
     return ChestReward.MakeResources(gold, gems, RewardRarity.Rare, $"{gold} Gold  +  {gems} Gems");
    } else if (roll < 80) {
     var std = GetStandardWeapons();
     var wid = std[rng.Next(0, std.Count)];
     bool has = save.weapons != null && save.weapons.Contains((int)wid);
     if (has) {
      int dupGold = 150, dupGems = 2;
      save.coins += dupGold;
      save.gems += dupGems;
      return ChestReward.MakeWeapon(wid, true, dupGold, dupGems, RewardRarity.Common);
     } else {
      save.weapons.Add((int)wid);
      return ChestReward.MakeWeapon(wid, false, 0, 0, RewardRarity.Common);
     }
    } else {
     // Rare / Epic Special-Hold Weapon
     var wid = RareHoldWeapons[rng.Next(0, RareHoldWeapons.Length)];
     bool isEpic = wid == WeaponId.VerdantFang;
     RewardRarity rar = isEpic ? RewardRarity.Epic : RewardRarity.Rare;
     bool has = save.weapons != null && save.weapons.Contains((int)wid);
     if (has) {
      int dupGold = isEpic ? 600 : 350, dupGems = isEpic ? 8 : 4;
      save.coins += dupGold;
      save.gems += dupGems;
      return ChestReward.MakeWeapon(wid, true, dupGold, dupGems, rar);
     } else {
      save.weapons.Add((int)wid);
      return ChestReward.MakeWeapon(wid, false, 0, 0, rar);
     }
    }
   } else {
    // Epic Chest: 15% Resources, 35% Standard Weapon, 35% Rare Special-Hold Weapon, 15% Epic Character Skin
    if (roll < 15) {
     int gold = rng.Next(500, 1001);
     int gems = rng.Next(8, 16);
     save.coins += gold;
     save.gems += gems;
     return ChestReward.MakeResources(gold, gems, RewardRarity.Epic, $"{gold} Gold  +  {gems} Gems");
    } else if (roll < 50) {
     var std = GetStandardWeapons();
     var wid = std[rng.Next(0, std.Count)];
     bool has = save.weapons != null && save.weapons.Contains((int)wid);
     if (has) {
      int dupGold = 200, dupGems = 3;
      save.coins += dupGold;
      save.gems += dupGems;
      return ChestReward.MakeWeapon(wid, true, dupGold, dupGems, RewardRarity.Common);
     } else {
      save.weapons.Add((int)wid);
      return ChestReward.MakeWeapon(wid, false, 0, 0, RewardRarity.Common);
     }
    } else if (roll < 85) {
     var wid = RareHoldWeapons[rng.Next(0, RareHoldWeapons.Length)];
     bool isEpic = wid == WeaponId.VerdantFang;
     RewardRarity rar = isEpic ? RewardRarity.Epic : RewardRarity.Rare;
     bool has = save.weapons != null && save.weapons.Contains((int)wid);
     if (has) {
      int dupGold = isEpic ? 700 : 450, dupGems = isEpic ? 10 : 5;
      save.coins += dupGold;
      save.gems += dupGems;
      return ChestReward.MakeWeapon(wid, true, dupGold, dupGems, rar);
     } else {
      save.weapons.Add((int)wid);
      return ChestReward.MakeWeapon(wid, false, 0, 0, rar);
     }
    } else {
     // Epic Character Skin
     var sid = UnlockableSkins[rng.Next(0, UnlockableSkins.Length)];
     bool has = save.skins != null && save.skins.Contains((int)sid);
     if (has) {
      int dupGold = 600, dupGems = 8;
      save.coins += dupGold;
      save.gems += dupGems;
      return ChestReward.MakeSkin(sid, true, dupGold, dupGems);
     } else {
      save.skins.Add((int)sid);
      return ChestReward.MakeSkin(sid, false, 0, 0);
     }
    }
   }
  }
 }
}
