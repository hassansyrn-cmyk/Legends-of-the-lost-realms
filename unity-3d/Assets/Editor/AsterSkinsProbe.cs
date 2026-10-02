using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class AsterSkinsProbe {
  [MenuItem("Lost Realms/Probes/Aster Skins Probe")]
  public static void Run() {
   Debug.Log("--- Starting Aster Skins Probe ---");
   int passed = 0;

   // 1. Verify Catalog
   if (SkinCatalog.Count != 6) {
    throw new Exception($"Expected 6 skins, found {SkinCatalog.Count}");
   }
   passed++;
   Debug.Log("PASS: SkinCatalog has 6 skins.");

   // 2. Verify all prefabs, materials, and CharacterVisual instantiations
   var testRoot = new GameObject("SkinProbeRoot");
   try {
    for (int i = 0; i < SkinCatalog.Count; i++) {
     var def = SkinCatalog.Get(i);
     Debug.Log($"Testing Skin [{def.Id}]: {def.Name} ({def.PrefabName})");

     var prefab = Resources.Load<GameObject>("Characters/" + def.PrefabName);
     if (!prefab) throw new Exception($"Missing prefab for skin: {def.PrefabName}");

     var mat = Resources.Load<Material>("Materials/" + def.MaterialName);
     if (!mat) throw new Exception($"Missing material for skin: {def.MaterialName}");

     var visual = CharacterVisual.Create(def.PrefabName, testRoot.transform, 1.8f, Color.white);
     if (!visual) throw new Exception($"Failed to create CharacterVisual for {def.PrefabName}");
     if (visual.animator == null) throw new Exception($"CharacterVisual missing animator for {def.PrefabName}");

     // Check right hand bone
     var hand = visual.animator.GetBoneTransform(HumanBodyBones.RightHand);
     Debug.Log($"Skin {def.Name} RightHand bone: {(hand != null ? hand.name : "null")}");

     UnityEngine.Object.DestroyImmediate(visual.gameObject);
     passed++;
    }

    // 3. Test Hero Integration & Stats
    var heroGo = new GameObject("HeroProbe");
    heroGo.transform.SetParent(testRoot.transform);
    var hero = heroGo.AddComponent<Hero>();

    foreach (SkinId id in Enum.GetValues(typeof(SkinId))) {
     var def = SkinCatalog.Get(id);
     hero.ApplySkin(def, false);

     // Check speed multiplier
     if (Mathf.Abs(hero.MoveSpeedMultiplier - def.SpeedMult) > 0.001f) {
      throw new Exception($"Speed multiplier mismatch for {id}: got {hero.MoveSpeedMultiplier}, expected {def.SpeedMult}");
     }

     // Check MaxHealth
     int expectedHp = Mathf.Max(1, 5 + def.BonusHealth);
     if (hero.MaxHealth != expectedHp) {
      throw new Exception($"MaxHealth mismatch for {id}: got {hero.MaxHealth}, expected {expectedHp}");
     }

     passed++;
    }

    // 4. Test Economy & Purchase System
    var gameGo = new GameObject("GameProbe");
    gameGo.transform.SetParent(testRoot.transform);
    var game = gameGo.AddComponent<RealmGame>();
    game.Save = new Progress();
    game.Save.NormalizeSkins();

    if (!game.Save.skins.Contains(0)) throw new Exception("Default skin 0 not in save skins list!");
    if (game.Save.equippedSkin != 0) throw new Exception("Default equipped skin should be 0!");

    // Test insufficient funds
    game.Save.coins = 0;
    game.Save.gems = 0;
    bool buyFailed = game.BuySkin(SkinId.ShadowAssassin);
    if (buyFailed) throw new Exception("BuySkin should have failed with 0 coins/gems!");

    // Test sufficient funds
    var assassinDef = SkinCatalog.Get(SkinId.ShadowAssassin);
    game.Save.coins = assassinDef.CostCoins + 100;
    game.Save.gems = assassinDef.CostGems + 10;
    int prevCoins = game.Save.coins;
    int prevGems = game.Save.gems;

    bool buySuccess = game.BuySkin(SkinId.ShadowAssassin);
    if (!buySuccess) throw new Exception("BuySkin failed despite having enough funds!");
    if (game.Save.coins != prevCoins - assassinDef.CostCoins) throw new Exception("Coins not deducted properly!");
    if (game.Save.gems != prevGems - assassinDef.CostGems) throw new Exception("Gems not deducted properly!");
    if (!game.Save.skins.Contains((int)SkinId.ShadowAssassin)) throw new Exception("Skin not added to owned skins list!");
    if (game.Save.equippedSkin != (int)SkinId.ShadowAssassin) throw new Exception("Skin not equipped on purchase!");

    // Test equip other owned skin
    game.EquipSkin(SkinId.Wanderer);
    if (game.Save.equippedSkin != (int)SkinId.Wanderer) throw new Exception("Failed to equip owned Wanderer skin!");

    passed++;
    Debug.Log("PASS: Economy, purchasing, and equipping tests succeeded.");
   } finally {
    UnityEngine.Object.DestroyImmediate(testRoot);
   }

   Debug.Log($"SKINS_PROBE_PASSED: {passed} checks passed cleanly.");
   File.WriteAllText("Validation/skins-probe.txt", $"SKINS_PROBE_PASSED: {passed} checks passed.");
  }
 }
}
