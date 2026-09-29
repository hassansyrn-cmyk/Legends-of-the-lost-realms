using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace LostRealms {
 public static class DesertPlaytestChecks {
  public static IEnumerator Run(Action<bool,string> check,Action<Vector2> move){
   var g=RealmGame.I;
   foreach(int stage in new[]{5,6}){
    g.LoadLevel(stage);yield return null;
    var w=g.World;
    foreach(var checkpoint in w.GetComponentsInChildren<RealmCheckpoint>())
     foreach(var prop in w.GetComponentsInChildren<Transform>())
      if(ChapterLayout.IsProp(prop)&&InteractionClearance.PropBounds(prop,out var b))
       check(!InteractionClearance.Near(b,checkpoint.transform.position,1.5f),"Chapter "+stage+" checkpoint clear of "+prop.name);
    foreach(var pad in w.GetComponentsInChildren<BouncePad>())
     foreach(var prop in w.GetComponentsInChildren<Transform>())
      if(ChapterLayout.IsProp(prop)&&InteractionClearance.PropBounds(prop,out var b))
       check(!InteractionClearance.Near(b,pad.transform.position,1.7f),"Chapter "+stage+" bounce pad clear of "+prop.name);
    if(stage==6){
     var island=InteractionClearance.RouteIsland(w,4);
     var spikes=island.GetComponentsInChildren<SpikeTrap>();
     check(spikes.Length>0,"Chapter 6 spikes moved to next island");
     foreach(var spike in spikes)check(ChapterLayout.Supported(island,spike.transform,new Bounds(spike.transform.position,new Vector3(2.5f,.1f,2.5f)),out _),"Relocated spike footprint supported");
     check(!island.Find("Prop House_01"),"Floating house behind spikes removed");
    }
    if(stage==5){
     foreach(var enemy in g.Enemies)if(enemy)enemy.enabled=false;
     var gate=w.transform.Find("Realm gate");g.CameraRig.Yaw=0;
     g.Player.Warp(gate.position+new Vector3(0,.1f,-7));
     yield return new WaitForSeconds(.5f);move(Vector2.up);
     yield return new WaitForSeconds(1.5f);move(Vector2.zero);
     check(g.Player.transform.position.z>gate.position.z-2,"Chapter 5 gate approach walkable without jumping");
    }
   }
   // Isolate projectile physics above the level; retain the real hero collider and damage path.
   var hero=g.Player;hero.enabled=false;hero.Warp(new Vector3(100,30,100));
   var turret=DartTurret.Place(g.World.transform,new Vector3(100,30,95),Color.yellow,Color.gray,DartTurret.TurretKind.Lion);
   var flags=BindingFlags.NonPublic|BindingFlags.Instance;
   var timer=typeof(DartTurret).GetField("timer",flags);timer.SetValue(turret,-1000f);
   var fire=typeof(DartTurret).GetMethod("Fire",flags);
   foreach(float height in new[]{.25f,.85f,1.55f}){
    yield return new WaitForSeconds(1.3f);hero.Health=hero.MaxHealth;
    var target=hero.transform.position+Vector3.up*height;
    var origin=target+Vector3.back*3;
    fire.Invoke(turret,new object[]{origin,Vector3.forward,13f});
    yield return new WaitForSeconds(.5f);
    check(hero.Health==hero.MaxHealth-1,"Lion arrow damages body at height "+height);
   }
   yield return new WaitForSeconds(1.3f);hero.Health=hero.MaxHealth;
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=hero.transform.position+new Vector3(0,.9f,-1.5f);wall.transform.localScale=new Vector3(2,3,.3f);Physics.SyncTransforms();
   fire.Invoke(turret,new object[]{hero.transform.position+new Vector3(0,.85f,-3),Vector3.forward,13f});
   yield return new WaitForSeconds(.5f);check(hero.Health==hero.MaxHealth,"Wall blocks arrow damage");
   UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(turret.gameObject);hero.enabled=true;
  }
 }
}
