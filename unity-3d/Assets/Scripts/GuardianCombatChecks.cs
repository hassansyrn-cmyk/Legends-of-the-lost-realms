using System;
using System.Collections;
using System.Reflection;
using System.IO;
using UnityEngine;
namespace LostRealms {
 public static class GuardianCombatChecks {
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Set(Enemy boss,string field,object value)=>typeof(Enemy).GetField(field,Private).SetValue(boss,value);
  static void Reset(Enemy boss){Set(boss,"state",Enum.Parse(typeof(Enemy).GetField("state",Private).FieldType,"Notice"));Set(boss,"timer",0f);Set(boss,"poseUntil",0f);Set(boss,"desertChargeUntil",0f);Set(boss,"desertChaseTime",0f);Set(boss,"desertNextAttack",RealmGame.I.Elapsed+3f);}
  static void Capture(Enemy boss,string name){
   var obj=new GameObject("Guardian gameplay review");var cam=obj.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=8;cam.farClipPlane=100;cam.transform.position=boss.transform.position+new Vector3(8,9,9);cam.transform.LookAt(boss.transform.position+Vector3.up*1.7f);
   var rt=new RenderTexture(900,700,24);cam.targetTexture=rt;var old=RenderTexture.active;cam.Render();RenderTexture.active=rt;var texture=new Texture2D(900,700,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,900,700),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Validation/NewEpic/Guardian-gameplay-"+name+".png"),texture.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(texture);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(obj);
  }
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;g.LoadLevel(7);yield return new WaitForSeconds(.4f);var boss=g.Enemies.Find(e=>e&&e.IsDesertLavaGuardian);
   foreach(var foe in g.Enemies)if(foe!=boss)foe.enabled=false;g.Player.enabled=false;g.Player.Health=10000;Vector3 start=boss.transform.position;
   var island=ChapterLayout.IslandOf(boss.transform,g.World.transform);
   foreach(var direction in new[]{Vector3.back,Vector3.right,Vector3.left}){
    boss.WarpBody(start);Reset(boss);Vector3 point=start+direction*5.5f;var local=island.InverseTransformPoint(point);
    check(RealmProps.TryDeckSurface(island,local.x,local.z,null,out float y),"Guardian approach test has actual arena deck "+direction);
    point.y=island.TransformPoint(new Vector3(local.x,y,local.z)).y+.05f;g.Player.Warp(point);float before=Vector3.Distance(start,point);
    yield return new WaitForSeconds(.7f);Capture(boss,"chase-"+direction.x+"-"+direction.z);yield return new WaitForSeconds(1.8f);
    check(Vector3.Distance(boss.transform.position,point)<before-.8f,"Guardian closes toward Aster from arena approach "+direction);
    check(boss.Grounded,"Guardian chase stays physically grounded "+direction);
   }
   for(int pattern=0;pattern<5;pattern++){
    boss.WarpBody(start);Reset(boss);Vector3 playerPoint=start+Vector3.back*5.5f;var playerLocal=island.InverseTransformPoint(playerPoint);if(RealmProps.TryDeckSurface(island,playerLocal.x,playerLocal.z,null,out float playerDeck))playerPoint.y=island.TransformPoint(new Vector3(playerLocal.x,playerDeck,playerLocal.z)).y+.05f;g.Player.Warp(playerPoint);g.Player.Health=10000;float travel=boss.GuardianChargeTravel;int count=boss.GuardianPatternCounts[pattern];
    typeof(Enemy).GetMethod("BeginDesertPattern",Private).Invoke(boss,new object[]{pattern});
    yield return new WaitForSeconds(.4f);Capture(boss,"pattern-"+pattern);
    if(pattern==4){for(int sample=0;sample<5;sample++){yield return new WaitForSeconds(.46f);Debug.Log("GUARDIAN_CHARGE_SAMPLE action="+boss.GuardianAction+" pos="+boss.transform.position+" start="+start+" travel="+boss.GuardianChargeTravel+" contact="+(boss.LastContact?boss.LastContact.name:"none")+" center="+typeof(Enemy).GetField("center",Private).GetValue(boss)+" area="+typeof(Enemy).GetField("area",Private).GetValue(boss));}}else yield return new WaitForSeconds(2.3f);
    check(boss.GuardianPatternCounts[pattern]>count,"Guardian executes distinct attack pattern "+pattern);
    if(pattern==4){check(boss.GuardianChargeTravel>travel+1f,"Telegraphed guardian charge advances its collision body: "+(boss.GuardianChargeTravel-travel));check(boss.LastContact!=g.Player.Controller,"Guardian charge stops before climbing Aster's capsule");}
    check(boss.Grounded,"Guardian attack pattern "+pattern+" remains grounded");
   }
   g.Player.enabled=true;g.Player.Health=g.Player.MaxHealth;
  }
 }
}
