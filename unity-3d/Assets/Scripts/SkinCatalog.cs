using System;
using UnityEngine;

namespace LostRealms {
 public enum SkinId {
  Wanderer = 0,
  ShadowAssassin = 1,
  RoyalKnight = 2,
  ArmoredJuggernaut = 3,
  ValhallaViking = 4
 }

 public readonly struct SkinDefinition {
  public readonly SkinId Id;
  public readonly string Name;
  public readonly string Title;
  public readonly string PrefabName;
  public readonly string MaterialName;
  public readonly int CostCoins;
  public readonly int CostGems;
  public readonly int BonusHealth;
  public readonly float SpeedMult;
  public readonly float DamageMult;
  public readonly float FinisherDamageMult;
  public readonly float DamageTakenMult;
  public readonly int BonusAirDashes;
  public readonly float EnergyRegenMult;
  public readonly float BonusParryWindow;
  public readonly bool InnateHyperArmor;
  public readonly string StatSummary;
  public readonly string Description;

  public SkinDefinition(SkinId id, string name, string title, string prefabName, string materialName,
   int costCoins, int costGems, int bonusHealth, float speedMult, float damageMult,
   float finisherDamageMult, float damageTakenMult, int bonusAirDashes, float energyRegenMult,
   float bonusParryWindow, bool innateHyperArmor, string statSummary, string description) {
   Id = id;
   Name = name;
   Title = title;
   PrefabName = prefabName;
   MaterialName = materialName;
   CostCoins = costCoins;
   CostGems = costGems;
   BonusHealth = bonusHealth;
   SpeedMult = speedMult;
   DamageMult = damageMult;
   FinisherDamageMult = finisherDamageMult;
   DamageTakenMult = damageTakenMult;
   BonusAirDashes = bonusAirDashes;
   EnergyRegenMult = energyRegenMult;
   BonusParryWindow = bonusParryWindow;
   InnateHyperArmor = innateHyperArmor;
   StatSummary = statSummary;
   Description = description;
  }
 }

 public static class SkinCatalog {
  public static readonly SkinDefinition[] Definitions = {
   new SkinDefinition(
    SkinId.Wanderer,
    "Classic Wanderer",
    "THE PATHFINDER",
    "Aster",
    "Aster",
    0, 0,
    bonusHealth: 0,
    speedMult: 1.0f,
    damageMult: 1.0f,
    finisherDamageMult: 1.0f,
    damageTakenMult: 1.0f,
    bonusAirDashes: 0,
    energyRegenMult: 1.0f,
    bonusParryWindow: 0f,
    innateHyperArmor: false,
    "Balanced baseline  •  Versatile movement & steady resilience",
    "Aster's original adventuring garb. Balanced, versatile, and battle-tested across all four realms."
   ),
   new SkinDefinition(
    SkinId.ShadowAssassin,
    "Shadow Assassin",
    "THE NIGHTBLADE",
    "Aster_Assassin",
    "Aster_Assassin",
    400, 8,
    bonusHealth: -1,
    speedMult: 1.18f,
    damageMult: 1.0f,
    finisherDamageMult: 1.25f,
    damageTakenMult: 1.0f,
    bonusAirDashes: 1,
    energyRegenMult: 1.1f,
    bonusParryWindow: 0f,
    innateHyperArmor: false,
    "+18% Move Speed  •  +1 Air Dash  •  +25% Finisher DMG  •  -1 Max HP",
    "Shroud yourself in twilight. Lethal agility with unmatched mobility, trading resilience for deadly swiftness."
   ),
   new SkinDefinition(
    SkinId.RoyalKnight,
    "Royal Knight",
    "THE CITADEL SENTINEL",
    "Aster_Knight",
    "Aster_Knight",
    550, 0,
    bonusHealth: 2,
    speedMult: 1.0f,
    damageMult: 1.12f,
    finisherDamageMult: 1.0f,
    damageTakenMult: 0.95f,
    bonusAirDashes: 0,
    energyRegenMult: 1.0f,
    bonusParryWindow: 0.12f,
    innateHyperArmor: false,
    "+2 Max HP  •  +12% Melee DMG  •  +0.12s Parry Window  •  -5% DMG Taken",
    "Gilded plate of the Sunken Citadel. Inspires resolute combat with enhanced vitality and wider parry reflexes."
   ),
   new SkinDefinition(
    SkinId.ArmoredJuggernaut,
    "Armored Juggernaut",
    "THE IRON BULWARK",
    "Aster_KnightArmored",
    "Aster_KnightArmored",
    0, 22,
    bonusHealth: 4,
    speedMult: 0.90f,
    damageMult: 1.08f,
    finisherDamageMult: 1.0f,
    damageTakenMult: 0.78f,
    bonusAirDashes: 0,
    energyRegenMult: 0.95f,
    bonusParryWindow: 0.05f,
    innateHyperArmor: true,
    "+4 Max HP  •  -22% DMG Taken  •  Unstoppable Hyper-Armor  •  -10% Speed",
    "Heavy fortified bulwark forged in volcanic steel. Absorbs punishing strikes with unyielding hyper-armor."
   ),
   new SkinDefinition(
    SkinId.ValhallaViking,
    "Valhalla Viking",
    "THE FROST REAVER",
    "Aster_Viking",
    "Aster_Viking",
    650, 12,
    bonusHealth: 1,
    speedMult: 1.04f,
    damageMult: 1.22f,
    finisherDamageMult: 1.35f,
    damageTakenMult: 1.0f,
    bonusAirDashes: 0,
    energyRegenMult: 1.25f,
    bonusParryWindow: 0f,
    innateHyperArmor: false,
    "+22% Melee DMG  •  +35% Finisher DMG  •  +25% Aether Regen  •  +1 HP",
    "Fierce northland raider imbued with primal frost fury. Unleashes savage blows and channels rapid aether regen."
   )
  };

  public static SkinDefinition Get(int rawId) {
   if (rawId < 0 || rawId >= Definitions.Length) return Definitions[0];
   return Definitions[rawId];
  }

  public static SkinDefinition Get(SkinId id) => Get((int)id);

  public static int Count => Definitions.Length;
 }
}
