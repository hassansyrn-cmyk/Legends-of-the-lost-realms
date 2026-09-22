using System;
using System.Collections;
using UnityEngine;
namespace LostRealms {
 public static class ArsenalChecks {
  public static IEnumerator Run(Action<bool,string> check){
   var legacy=JsonUtility.FromJson<Progress>("{\"equippedWeapon\":5}");legacy.NormalizeWeapons();
   check(legacy.weapons.Count==1&&legacy.weapons[0]==5,"Arsenal migrates an old equipped weapon");
   var invalid=JsonUtility.FromJson<Progress>("{\"equippedWeapon\":16,\"weapons\":[4,4,-2,999,33]}");invalid.NormalizeWeapons();
   check(invalid.weapons.Count==3&&invalid.weapons.Contains(16),"Arsenal removes duplicate and invalid saved ids");
   var g=RealmGame.I;g.LoadLevel(1);yield return new WaitForSeconds(.3f);g.Pause();
   g.Save.weapons.Clear();g.EquipWeapon(WeaponId.Axe);g.EquipWeapon(WeaponId.Axe);yield return null;
   check(g.Save.weapons.Count==1&&g.Player.GetComponentsInChildren<EquippedWeapon>().Length==1,"Duplicate pickups retain one inventory entry and one equipped model");
   g.EquipWeapon(WeaponId.HuntsmanSpear);yield return null;
   check(g.Screen==GameScreen.Paused&&g.Save.weapons.Count==2&&g.CurrentWeapon.Id==WeaponId.HuntsmanSpear,"Mid-run arsenal swap preserves pause and both collected weapons");
   var roundtrip=JsonUtility.FromJson<Progress>(JsonUtility.ToJson(g.Save));roundtrip.NormalizeWeapons();
   check(roundtrip.weapons.Count==2&&roundtrip.equippedWeapon==33,"Arsenal collection and equipped selection round-trip through save JSON");
   g.UnequipWeapon();yield return null;
   check(g.Save.weapons.Count==2&&g.Save.equippedWeapon==-1&&!g.Player.GetComponentInChildren<EquippedWeapon>(),"Unequip removes model but retains collected weapons");
   for(int id=0;id<=40;id++){
    var def=WeaponCatalog.Get(id);string name=def.Resource.Substring(def.Resource.LastIndexOf('/')+1);
    check(Resources.Load<Texture2D>("Weapons/Icons/"+name),"Arsenal icon resolves for "+def.Name);
   }
   foreach(string style in new[]{"slash","chop","spear","unarmed","charged"})for(int v=1;v<=3;v++)
    check(g.Audio.GetClip("sfx_attack_"+style+"_"+v),"Generated attack sound resolves: "+style+v);
   foreach(string kind in new[]{"warning","crush","dart","boulder","wind","fire","frost","poison","blade","spikes","saw_loop","fan_loop"})
    check(g.Audio.GetClip("sfx_trap_"+kind),"Generated trap sound resolves: "+kind);
   check(ProjectileSweep.Hits(Vector3.left*3,Vector3.right*3,Vector3.zero,.6f,out float fraction)&&Mathf.Abs(fraction-.5f)<.001f,"Fast projectile sweep catches a player between frame endpoints");
   check(!ProjectileSweep.Hits(Vector3.left*3+Vector3.up*2,Vector3.right*3+Vector3.up*2,Vector3.zero,.6f,out fraction),"Projectile sweep preserves vertical misses");
   Vector3 origin=g.Player.transform.position+Vector3.up*5;
   var bolt=TrapBolt.Fire(origin,Vector3.forward*10,1,0,Color.red,"ga_vfx_Explosion_02");
   yield return new WaitForSecondsRealtime(.2f);
   check(bolt&&Vector3.Distance(bolt.transform.position,origin)<.001f,"Trap bolt survives pause without moving");
   g.Resume();yield return new WaitForSeconds(.08f);
   check(bolt&&Vector3.Distance(bolt.transform.position,origin)>.1f,"Trap bolt resumes flight after pause");
   if(bolt)UnityEngine.Object.Destroy(bolt.gameObject);
   g.EquipWeapon(WeaponId.Axe);
  }
 }
}
