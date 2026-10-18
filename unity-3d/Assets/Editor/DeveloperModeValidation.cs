using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LostRealms {
 public static class DeveloperModeValidation {
  static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
  public static void Run(){
   if(!RealmGame.Testing)throw new Exception("Use -menuVisualProbe to protect player saves.");
   var root=new GameObject("Developer mode checks");root.SetActive(false);
   try{
    var game=root.AddComponent<RealmGame>();
    var original=new Progress{coins=137,gems=19,equippedWeapon=1,weapons=new List<int>{1},skins=new List<int>{0}};
    game.Save=original;string snapshot=JsonUtility.ToJson(original);
    foreach(string password in new[]{null,"","0000","3258 "})Require(!game.TryEnableDeveloperMode(password)&&!game.DeveloperMode&&ReferenceEquals(game.Save,original),"Incorrect password activated developer mode");
    Require(game.TryEnableDeveloperMode("3258")&&game.DeveloperMode,"Correct password failed");
    Require(game.Save.unlocked==Campaign.Chapters,"All chapters must unlock for testing");
    Require(!ReferenceEquals(game.Save,original),"Test session must own a separate save");
    Require(game.Save.weapons.Count==WeaponCatalog.MaxId+1&&game.Save.skins.Count==SkinCatalog.Count,"Incomplete test collection");
    for(int id=0;id<=WeaponCatalog.MaxId;id++)Require(game.Save.weapons.Contains(id),"Missing weapon "+id);
    for(int id=0;id<SkinCatalog.Count;id++)Require(game.Save.skins.Contains(id),"Missing outfit "+id);
    var testingSave=game.Save;Require(game.TryEnableDeveloperMode("3258")&&ReferenceEquals(game.Save,testingSave),"Repeated activation reset the test session");
    game.Save.coins=0;game.Save.gems=0;game.Save.stars[0]=3;game.Save.equippedSkin=6;game.Save.equippedWeapon=WeaponCatalog.MaxId;
    Require(JsonUtility.ToJson(original)==snapshot,"Testing modified normal progress");
    game.Persist();game.DisableDeveloperMode();
    Require(!game.DeveloperMode&&ReferenceEquals(game.Save,original)&&JsonUtility.ToJson(game.Save)==snapshot,"Normal save was not restored exactly");
    game.DisableDeveloperMode();Require(ReferenceEquals(game.Save,original),"Repeated disable changed progress");
    Directory.CreateDirectory("Validation/DeveloperMode");
    File.WriteAllText("Validation/DeveloperMode/results.txt","DEVELOPER_MODE_CHECKS_PASSED\nWrong passwords rejected; correct password accepted; all "+(WeaponCatalog.MaxId+1)+" weapons and "+SkinCatalog.Count+" skins and all chapters unlocked; repeated activation/disable safe; separate test progress; exact normal-save restoration.\n");
    Debug.Log("DEVELOPER_MODE_CHECKS_PASSED");
   }finally{UnityEngine.Object.DestroyImmediate(root);}
   MenuVisualProbe.RunDeveloper();
  }
 }
}
