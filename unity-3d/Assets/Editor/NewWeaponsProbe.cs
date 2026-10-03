using System;
using UnityEditor;
using UnityEngine;

namespace LostRealms {
 public static class NewWeaponsProbe {
  public static void Run() {
   int passed = 0;
   void Check(bool cond, string msg) {
    if (!cond) {
     Debug.LogError("NEW_WEAPONS_PROBE_FAILED: " + msg);
     EditorApplication.Exit(1);
     throw new Exception("Check failed: " + msg);
    }
    passed++;
   }

   Debug.Log("--- Starting NewWeaponsProbe ---");

   // 1. Check definitions and resources
   var fg = WeaponCatalog.Get(WeaponId.FantasyGreatsword);
   Check(fg.Name == "FANTASY GREATSWORD", "FantasyGreatsword name correct");
   Check(Mathf.Approximately(fg.Damage, 1.65f), "FantasyGreatsword damage is 1.65");
   Check(Mathf.Approximately(fg.Reach, 1.38f), "FantasyGreatsword reach is 1.38");
   Check(WeaponCatalog.Affinity(fg) == -1, "FantasyGreatsword affinity is physical (-1)");
   Check(WeaponCatalog.HasSpecialHold(fg.Id), "FantasyGreatsword has special hold attack");

   var fs = WeaponCatalog.Get(WeaponId.FierySword);
   Check(fs.Name == "INFERNO BLADE", "FierySword name correct");
   Check(Mathf.Approximately(fs.Damage, 1.45f), "FierySword damage is 1.45");
   Check(Mathf.Approximately(fs.Tempo, 1.08f), "FierySword tempo is 1.08");
   Check(WeaponCatalog.Affinity(fs) == 0, "FierySword affinity is Ember (0)");
   Check(WeaponCatalog.HasSpecialHold(fs.Id), "FierySword has special hold attack");

   var oc = WeaponCatalog.Get(WeaponId.OrnateCurvedBlade);
   Check(oc.Name == "ORNATE CURVED BLADE", "OrnateCurvedBlade name correct");
   Check(Mathf.Approximately(oc.Damage, 1.24f), "OrnateCurvedBlade damage is 1.24");
   Check(Mathf.Approximately(oc.Tempo, 1.32f), "OrnateCurvedBlade tempo is 1.32");
   Check(WeaponCatalog.Affinity(oc) == 2, "OrnateCurvedBlade affinity is Gale (2)");
   Check(WeaponCatalog.HasSpecialHold(oc.Id), "OrnateCurvedBlade has special hold attack");

   // 2. Load models from Resources
   var fgModel = Resources.Load<GameObject>("Weapons/FantasyGreatsword");
   Check(fgModel != null, "FantasyGreatsword FBX loads from Resources");
   var fsModel = Resources.Load<GameObject>("Weapons/FierySword");
   Check(fsModel != null, "FierySword FBX loads from Resources");
   var ocModel = Resources.Load<GameObject>("Weapons/OrnateCurvedBlade");
   Check(ocModel != null, "OrnateCurvedBlade FBX loads from Resources");

   // 3. Load textures
   var fgTex = Resources.Load<Texture2D>("Weapons/Textures/FantasyGreatsword_basecolor");
   Check(fgTex != null, "FantasyGreatsword texture loads from Resources");
   var fsTex = Resources.Load<Texture2D>("Weapons/Textures/FierySword_basecolor");
   Check(fsTex != null, "FierySword texture loads from Resources");
   var ocTex = Resources.Load<Texture2D>("Weapons/Textures/OrnateCurvedBlade_basecolor");
   Check(ocTex != null, "OrnateCurvedBlade texture loads from Resources");

   // 4. Load Icons
   var fgIcon = Resources.Load<Texture2D>("Weapons/Icons/FantasyGreatsword");
   Check(fgIcon != null, "FantasyGreatsword icon loads from Resources");
   var fsIcon = Resources.Load<Texture2D>("Weapons/Icons/FierySword");
   Check(fsIcon != null, "FierySword icon loads from Resources");
   var ocIcon = Resources.Load<Texture2D>("Weapons/Icons/OrnateCurvedBlade");
   Check(ocIcon != null, "OrnateCurvedBlade icon loads from Resources");

   // 5. Test CreateModel instantiation and materials
   var host = new GameObject("TestHost");
   try {
    var go1 = WeaponCatalog.CreateModel(WeaponId.FantasyGreatsword, host.transform);
    Check(go1 != null, "FantasyGreatsword model created");
    var r1 = go1.GetComponentInChildren<Renderer>();
    Check(r1 != null && r1.sharedMaterial != null && r1.sharedMaterial.mainTexture != null, "FantasyGreatsword has bound textured material");

    var go2 = WeaponCatalog.CreateModel(WeaponId.FierySword, host.transform);
    Check(go2 != null, "FierySword model created");
    var r2 = go2.GetComponentInChildren<Renderer>();
    Check(r2 != null && r2.sharedMaterial != null && r2.sharedMaterial.mainTexture != null, "FierySword has bound textured material");

    var go3 = WeaponCatalog.CreateModel(WeaponId.OrnateCurvedBlade, host.transform);
    Check(go3 != null, "OrnateCurvedBlade model created");
    var r3 = go3.GetComponentInChildren<Renderer>();
    Check(r3 != null && r3.sharedMaterial != null && r3.sharedMaterial.mainTexture != null, "OrnateCurvedBlade has bound textured material");
   } finally {
    RealmGame.I = null;
    UnityEngine.Object.DestroyImmediate(host);
   }

   // 6. Test Save Normalization with weapon IDs 41, 42, 43
   var p = new Progress();
   p.weapons.Add(41);
   p.weapons.Add(42);
   p.weapons.Add(43);
   p.equippedWeapon = 42;
   p.NormalizeWeapons();
   Check(p.weapons.Contains(41) && p.weapons.Contains(42) && p.weapons.Contains(43), "NormalizeWeapons preserves new weapon IDs 41-43");
   Check(p.equippedWeapon == 42, "NormalizeWeapons preserves equipped weapon 42");

   // 7. Test DiscoveryDrop covers range
   var pEmpty = new Progress();
   var d41 = WeaponCatalog.DiscoveryDrop(41, pEmpty);
   var d42 = WeaponCatalog.DiscoveryDrop(42, pEmpty);
   var d43 = WeaponCatalog.DiscoveryDrop(43, pEmpty);
   Check((int)d41 == 41 && (int)d42 == 42 && (int)d43 == 43, "DiscoveryDrop maps rolls 41-43");

   // 8. Test Hold Attack components and instantiations
   var dummyWorld = new GameObject("DummyWorld");
   var dummyGame = new GameObject("DummyGame").AddComponent<RealmGame>();
   dummyGame.World = dummyWorld.AddComponent<RealmWorld>();
   var rigGo = new GameObject("CameraRig");
   dummyGame.CameraRig = rigGo.AddComponent<FollowCamera>();
   RealmGame.I = dummyGame;
   try {
    var eq = EarthQuakeSlam.Create(Vector3.zero, Vector3.forward, 5f);
    Check(eq != null, "EarthQuakeSlam instantiates cleanly");

    var inf = InfernoWave.Fire(Vector3.up, Vector3.forward, 5f);
    Check(inf != null, "InfernoWave fires and instantiates cleanly");

    var gv = GaleVortex.Fire(Vector3.up, Vector3.forward, 5f);
    Check(gv != null, "GaleVortex fires and instantiates cleanly");
   } finally {
    RealmGame.I = null;
    UnityEngine.Object.DestroyImmediate(rigGo);
    UnityEngine.Object.DestroyImmediate(dummyGame.gameObject);
    UnityEngine.Object.DestroyImmediate(dummyWorld);
   }

   Debug.Log($"NEW_WEAPONS_PROBE_PASSED: {passed} checks passed.");
   EditorApplication.Exit(0);
  }
 }
}
