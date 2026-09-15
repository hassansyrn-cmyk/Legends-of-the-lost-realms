using System;
using System.Collections;
using UnityEngine;

namespace LostRealms {
 public static class QualityUpgradeChecks {
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;
   g.LoadLevel(1);yield return new WaitForSeconds(.3f);
   check(g.Trial&&!g.Trial.Active&&!g.Trial.Completed,"Optional shrine starts inactive");
   g.Trial.EnemyDefeated();check(g.Trial.Count==0,"Inactive trials ignore combat");
   g.Player.Warp(g.Trial.ShrinePosition+Vector3.up*.05f);
   yield return new WaitForSeconds(1.2f);
   check(g.Trial.Active,"Standing at shrine activates optional trial");
   g.Player.Warp(g.World.Spawn);
   int gold=g.Coins,gems=g.Gems;
   for(int i=0;i<4;i++)g.Enemies[i].Hit(999,-1,false);
   check(g.Trial.Completed&&g.Trial.Count==4,"Four actual defeats complete courage trial");
   check(g.Coins==gold+12+g.Trial.GoldReward&&g.Gems==gems+2,"Trial pays chapter rewards exactly once");
   gold=g.Coins;gems=g.Gems;g.Trial.EnemyDefeated();g.Trial.Begin();
   check(g.Coins==gold&&g.Gems==gems,"Completed trial cannot be farmed again in same run");

   g.LoadLevel(2);yield return new WaitForSeconds(.3f);g.Trial.Begin();
   var echoes=UnityEngine.Object.FindObjectsByType<TrialEcho>(FindObjectsSortMode.None);
   check(echoes.Length==3,"Echo trial creates three collectibles");
   foreach(var echo in echoes){g.Player.Warp(echo.Origin-Vector3.up*.8f);yield return new WaitForSeconds(.12f);}
   check(g.Trial.Completed&&g.Trial.Count==3,"Collecting three distinct echoes completes exploration trial");

   g.LoadLevel(3);yield return new WaitForSeconds(.3f);g.Trial.Begin();var target=g.Enemies[0];target.Health=target.MaxHealth=100;
   target.Hit(0,target.WeakElement,true);check(g.Trial.Count==0,"Zero damage does not advance elemental trial");
   for(int i=0;i<12&&!g.Trial.Completed;i++)target.Hit(.5f,target.WeakElement,true);
   check(g.Trial.Completed,"Actual damaging weakness hits complete elemental trial");

   g.LoadLevel(4);yield return new WaitForSeconds(.2f);check(!g.Trial,"Boss chapters retain uncluttered guardian objectives");
   check(g.Audio.CurrentTrack=="boss_battle_theme","Guardian selects boss music");
   g.Audio.SetTrack("missing_optional_track");check(g.Audio.CurrentTrack=="boss_battle_theme","Missing music preserves the current track");
   g.Save.music=false;yield return null;check(g.Music.mute,"Music toggle mutes the active deck");g.Save.music=true;
   for(int i=0;i<30;i++){g.Sound("blade");g.Sound("impact");g.Sound("coin");}
   check(g.Audio.ActiveVoices<=12,"SFX voice bank remains bounded under burst load");

   var test=new GameObject("Curve repair test");test.transform.SetParent(g.World.transform,false);
   var ps=test.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
   var velocity=ps.velocityOverLifetime;velocity.x=new ParticleSystem.MinMaxCurve(-2,2);velocity.y=new ParticleSystem.MinMaxCurve(3);velocity.z=new ParticleSystem.MinMaxCurve(0);velocity.enabled=true;
   Vfx.Repair(test);velocity=ps.velocityOverLifetime;
   check(velocity.x.mode==velocity.y.mode&&velocity.x.mode==velocity.z.mode,"VFX axes share compatible curve modes");
   check(Mathf.Abs(velocity.x.Evaluate(.5f,0)+2)<.01f&&Mathf.Abs(velocity.y.Evaluate(.5f,0)-3)<.01f&&Mathf.Abs(velocity.z.Evaluate(.5f,0))<.01f,"VFX repair preserves independent axis velocities");
   UnityEngine.Object.Destroy(test);

   var warning=CombatTelegraph.Create(g.World.Spawn,2,1,g.World.transform).GetComponent<CombatTelegraph>();
   yield return new WaitForSeconds(.2f);float before=warning.Progress;g.Pause();yield return new WaitForSecondsRealtime(.2f);
   check(Mathf.Approximately(before,warning.Progress),"Combat countdown freezes while paused");g.Resume();UnityEngine.Object.Destroy(warning.gameObject);

   g.LoadLevel(1);yield return new WaitForSeconds(.3f);
   var moving=UnityEngine.Object.FindAnyObjectByType<MovingIsland>();check(moving,"Moving platform exists");
   g.Player.Warp(moving.transform.position+Vector3.up*.05f);yield return new WaitForSeconds(.3f);
   Vector3 relative=g.Player.transform.position-moving.transform.position;
   yield return new WaitForSeconds(.7f);Vector3 after=g.Player.transform.position-moving.transform.position;
   check(g.Player.Grounded&&new Vector2(after.x-relative.x,after.z-relative.z).magnitude<.15f,"Platform carries grounded hero without relative drift");
   g.Pause();Vector3 stopped=moving.transform.position;yield return new WaitForSecondsRealtime(.2f);
   check(Vector3.Distance(stopped,moving.transform.position)<.001f,"Moving platform freezes while paused");g.Resume();
   check(Mathf.Approximately(Time.fixedDeltaTime,1f/60f),"Quality pass preserves fixed physics timestep");
   g.LoadLevel(15);yield return new WaitForSeconds(.3f);
   var warden=g.Enemies.Find(e=>e&&e.Boss);var animator=warden.Visual.animator;
   check(animator&&animator.avatar&&animator.avatar.isHuman&&animator.avatar.isValid,"Warden has a valid humanoid animation avatar");
   warden.enabled=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;warden.Visual.Restart("attack_1");yield return new WaitForSeconds(.2f);
   var arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);Quaternion first=arm.localRotation;
   yield return new WaitForSeconds(.3f);
   check(Quaternion.Angle(first,arm.localRotation)>1f,"Warden attack actually animates its arm instead of holding rest pose");
  }
 }
}
