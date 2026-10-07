using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
namespace LostRealms {
 public static class StaffShotChecks {
  static Enemy Target(Vector3 p){
   var go=new GameObject("Staff target");go.transform.SetParent(RealmGame.I.World.transform);go.transform.position=p;
   var e=go.AddComponent<Enemy>();e.Configure(0,false,p,new Vector2(10,10));e.Health=e.MaxHealth=100;e.enabled=false;return e;
  }
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;g.LoadLevel(1);yield return null;
   float originalMaximumDelta=Time.maximumDeltaTime;Time.maximumDeltaTime=.05f;
   foreach(var e in g.Enemies.ToArray())if(e)UnityEngine.Object.Destroy(e.gameObject);g.Enemies.Clear();yield return null;
   g.Player.Warp(new Vector3(100,30,100));g.Player.enabled=false;g.Player.transform.rotation=Quaternion.identity;
   var attack=typeof(Hero).GetMethod("Attack",BindingFlags.Instance|BindingFlags.NonPublic);
   var ready=typeof(Hero).GetField("attackReady",BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(var id in new[]{WeaponId.EmberTorch,WeaponId.SageStaff,WeaponId.WardenPike}){
    g.EquipWeapon(id);ready.SetValue(g.Player,0f);
    Vector3 origin=g.Player.transform.position;
    var first=Target(origin+Vector3.forward*8f);var second=Target(origin+new Vector3(1.15f,0,8f));
    Physics.SyncTransforms();float energy=g.Player.Energy;
    attack.Invoke(g.Player,new object[]{true});
    check(first.Health==100,"Staff charge does not also deal immediate melee damage");
    check(!UnityEngine.Object.FindAnyObjectByType<StaffShot>(),id+" waits for its attack motion before launch");
    float remaining=(float)ready.GetValue(g.Player)-g.Elapsed;
    float releaseTime=g.Elapsed+remaining*.85f;
    yield return new WaitForSeconds(remaining*.5f);
    check(!UnityEngine.Object.FindAnyObjectByType<StaffShot>(),id+" has no projectile halfway through the swing");
    yield return new WaitForSeconds(Mathf.Max(0,releaseTime-g.Elapsed));yield return null;
    var shot=UnityEngine.Object.FindAnyObjectByType<StaffShot>();
    check(g.Elapsed<(float)ready.GetValue(g.Player),id+" releases before the recovery motion ends");
    check(shot&&shot.Element==WeaponCatalog.ChargedShot(id),id+" charged attack launches its assigned magic");
    check(g.Player.Energy==energy,"Staff charge uses existing attack cost, not spell energy");
    g.Screen=GameScreen.Paused;var pausedPosition=shot.transform.position;
    yield return new WaitForSeconds(.15f);check(shot.transform.position==pausedPosition,"Staff projectile freezes during pause");g.Screen=GameScreen.Playing;
    yield return new WaitForSeconds(.8f);
    check(first.Health<100,"Staff shot damages its forward target");
    check(second.Health<100,id==WeaponId.EmberTorch?"Fire splash damages a nearby foe":"Thunder arcs to a nearby foe");
    check(!shot,"Staff shot ends on impact");
    UnityEngine.Object.Destroy(first.gameObject);UnityEngine.Object.Destroy(second.gameObject);yield return null;
   }
   var blocked=Target(g.Player.transform.position+Vector3.forward*5f);
   var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=g.Player.transform.position+new Vector3(0,1,2);wall.transform.localScale=new Vector3(3,3,.4f);Physics.SyncTransforms();
   foreach(int element in new[]{0,2}){
    StaffShot.Fire(g.Player.transform.position+Vector3.up,Vector3.forward,5,element);yield return new WaitForSeconds(.6f);
    check(blocked.Health==100,"Solid scenery blocks staff element "+element);
   }
   UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(blocked.gameObject);yield return null;
   foreach(var id in new[]{WeaponId.PureScythe,WeaponId.OrnateCurvedBlade}){
    g.EquipWeapon(id);ready.SetValue(g.Player,0f);var victim=Target(g.Player.transform.position+Vector3.forward*8);
    attack.Invoke(g.Player,new object[]{true});
    check(victim.Health==100,"Slash charge replaces immediate melee damage");
    check(!UnityEngine.Object.FindAnyObjectByType<StaffShot>(),id+" delays its slash volley until the motion ends");
    yield return new WaitForSeconds(Mathf.Max(0,(float)ready.GetValue(g.Player)-g.Elapsed)*.85f);yield return null;
    var shots=UnityEngine.Object.FindObjectsByType<StaffShot>(FindObjectsSortMode.None);
    check(shots.Length==(id==WeaponId.OrnateCurvedBlade?2:1),id+" emits the correct slash count");
    yield return new WaitForSeconds(.08f);Capture(shots,id);
    yield return new WaitForSeconds(.75f);check(victim.Health<100,id+" traveling slashes damage a target");
    UnityEngine.Object.Destroy(victim.gameObject);yield return new WaitForSeconds(.4f);
   }
   g.EquipWeapon(WeaponId.WardenPike);ready.SetValue(g.Player,0f);attack.Invoke(g.Player,new object[]{true});yield return new WaitForSeconds(Mathf.Max(0,(float)ready.GetValue(g.Player)-g.Elapsed)+.08f);Capture(UnityEngine.Object.FindObjectsByType<StaffShot>(FindObjectsSortMode.None),WeaponId.WardenPike);
   yield return new WaitForSeconds(1f);
   var miss=StaffShot.Fire(g.Player.transform.position+Vector3.up,Vector3.back,5,0);yield return new WaitForSeconds(1.6f);check(!miss,"Missed staff fireball expires at maximum range");
   g.EquipWeapon(WeaponId.SageStaff);ready.SetValue(g.Player,0f);attack.Invoke(g.Player,new object[]{false});check(!UnityEngine.Object.FindAnyObjectByType<StaffShot>(),"Short staff attack remains melee");
   check(WeaponCatalog.ChargedShot(WeaponId.MoonChakram)==-1&&WeaponCatalog.ChargedShot(WeaponId.HuntsmanSpear)==-1,"Chakram and other physical polearms retain their charge behavior");
   yield return new WaitForSeconds(1f);
   g.EquipWeapon(WeaponId.WardenPike);ready.SetValue(g.Player,0f);attack.Invoke(g.Player,new object[]{true});
   g.EquipWeapon(WeaponId.Axe);yield return new WaitForSeconds(1.5f);
   check(!UnityEngine.Object.FindAnyObjectByType<StaffShot>(),"Switching weapons cancels an unreleased charged shot");
   yield return NewWeaponPoseChecks.Run(check);
   g.Player.enabled=true;Time.maximumDeltaTime=originalMaximumDelta;
  }
  static void Capture(StaffShot[] shots,WeaponId id){
   if(shots.Length==0)return;
   var renderers=new System.Collections.Generic.List<Renderer>();var layers=new System.Collections.Generic.List<int>();Vector3 center=Vector3.zero;
   foreach(var shot in shots){center+=shot.transform.position;foreach(var r in shot.GetComponentsInChildren<Renderer>()){renderers.Add(r);layers.Add(r.gameObject.layer);r.gameObject.layer=30;}}
   center/=shots.Length;var go=new GameObject("Charge preview");var camera=go.AddComponent<Camera>();camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.065f);camera.fieldOfView=45;
   camera.transform.position=center+new Vector3(2,2,3.5f);camera.transform.LookAt(center);
   var rt=new RenderTexture(700,500,24);var previous=RenderTexture.active;camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
   var image=new Texture2D(700,500,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,700,500),0,0);image.Apply();System.IO.File.WriteAllBytes("Validation/charge-"+id+".png",image.EncodeToPNG());
   RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(go);
   for(int i=0;i<renderers.Count;i++)renderers[i].gameObject.layer=layers[i];
  }
 }
}
