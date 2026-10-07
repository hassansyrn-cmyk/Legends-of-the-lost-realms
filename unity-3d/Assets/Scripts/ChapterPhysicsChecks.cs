using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public static class ChapterPhysicsChecks {
  static Enemy Foe(RealmGame game,int kind,Vector3 point,bool boss=false){
   var go=new GameObject("Physics regression foe "+kind);go.transform.SetParent(game.World.transform);go.transform.position=point;
   var foe=go.AddComponent<Enemy>();foe.Configure(kind,boss,point,new Vector2(20,20));foe.Stun(60);return foe;
  }
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;
   for(int chapter=1;chapter<=15;chapter++){
    g.LoadLevel(chapter);foreach(var e in g.Enemies)e.Stun(60);
    yield return new WaitForSeconds(.4f);
    check(g.Player.Grounded,"Aster grounded at chapter "+chapter+" start");
    foreach(var e in g.Enemies.ToArray()){
     if(!e||e.Health<=0||!e.Grounded){
      Debug.Log("ENEMY_PHYSICS_DIAGNOSTIC chapter="+chapter+" kind="+(e?e.Kind:-1)+" alive="+(e?e.Health:0)+" pos="+(e?e.transform.position:Vector3.zero)+" vertical="+(e?e.VerticalVelocity:0));
      if(e)foreach(var h in Physics.RaycastAll(e.transform.position+Vector3.up*.2f,Vector3.down,4f,~0,QueryTriggerInteraction.Ignore))Debug.Log("ENEMY_BELOW "+h.collider.name+" point="+h.point+" normal="+h.normal);
      if(e){Debug.Log("ENEMY_CONTACT "+e.LastContact+" flags="+e.Controller.collisionFlags+" bounds="+e.Controller.bounds);foreach(var c in Physics.OverlapSphere(e.transform.position+Vector3.up*.3f,.8f,~0,QueryTriggerInteraction.Ignore))Debug.Log("ENEMY_NEAR "+c.name+" enabled="+c.enabled+" trigger="+c.isTrigger+" bounds="+c.bounds);}
     }
     check(e&&e.Health>0&&e.Grounded,"Enemy family "+e.Kind+" has real ground and collision in chapter "+chapter);
    }
    check(!g.Trial||RealmTrials.ShrineClear(g.Trial.ShrinePosition,g.World.transform),"Optional shrine remains accessible in chapter "+chapter);
   }
   g.LoadLevel(1);yield return new WaitForSeconds(.2f);
   var deck=new GameObject("Island");deck.transform.SetParent(g.World.transform);deck.transform.position=new Vector3(1000,30,0);
   Art.Shape("Visible physics deck",PrimitiveType.Cube,Vector3.down*.25f,new Vector3(30,.5f,30),Color.gray,deck.transform,true);
   var families=new List<Enemy>();
   for(int kind=0;kind<=21;kind++)families.Add(Foe(g,kind,deck.transform.position+new Vector3((kind%6-2.5f)*3f,.05f,(kind/6-1.5f)*3f)));
   Physics.SyncTransforms();yield return new WaitForSeconds(.4f);
   foreach(var e in families)check(e.Grounded&&e.Controller&&e.Controller.enabled,"All families use a grounded collision capsule: "+e.Kind);
   foreach(var e in families)UnityEngine.Object.Destroy(e.gameObject);yield return null;
   var enemy=Foe(g,0,deck.transform.position+Vector3.up*.05f);Physics.SyncTransforms();yield return new WaitForSeconds(.3f);
   var wall=Art.Shape("Solid collision regression",PrimitiveType.Cube,new Vector3(1.5f,2,0),new Vector3(.3f,4,4),Color.gray,deck.transform,true);
   Physics.SyncTransforms();enemy.PushBack(Vector3.right,8f,.4f);yield return new WaitForSeconds(.5f);
   check(enemy.transform.position.x<1001f&&enemy.Grounded,"Enemy knockback cannot pass through a solid building wall");
   UnityEngine.Object.Destroy(wall);yield return null;
   enemy.WarpBody(deck.transform.position+Vector3.up*10);enemy.Stun(60);Physics.SyncTransforms();yield return new WaitForSeconds(.2f);
   check(enemy.Controller.enabled&&enemy.VerticalVelocity<0,"Falling enemies retain collision and accelerate under gravity");
   yield return new WaitForSeconds(1.3f);check(enemy&&enemy.Grounded&&Mathf.Abs(enemy.transform.position.y-30)<.08f,"Falling enemy lands without hovering or sinking");
   var corpse=Foe(g,0,deck.transform.position+new Vector3(3,6,0));corpse.Hit(999,-1,false);float corpseY=corpse.transform.position.y;
   yield return new WaitForSeconds(.3f);check(corpse&&corpse.transform.position.y<corpseY-.3f,"Enemy killed in midair continues falling");UnityEngine.Object.Destroy(corpse.gameObject);
   var ferry=deck.AddComponent<MovingIsland>();ferry.Origin=deck.transform.position;ferry.Offset=new Vector3(1.5f,.7f,0);ferry.Speed=1.1f;ferry.Style=IslandMotionStyle.VerticalElevator;
   enemy.WarpBody(deck.transform.position+Vector3.up*.03f);yield return new WaitForSeconds(.7f);
   check(enemy.Grounded&&Mathf.Abs(enemy.transform.position.y-deck.transform.position.y)<.08f,"Enemy rides a moving deck without floating");
   g.Pause();yield return new WaitForFixedUpdate();Vector3 paused=enemy.transform.position;
   yield return new WaitForSeconds(.25f);check(Vector3.Distance(paused,enemy.transform.position)<.01f,"Enemy physics freezes during pause");g.Resume();
   ferry.enabled=false;enemy.WarpBody(deck.transform.position+new Vector3(14.2f,.03f,0));enemy.Stun(60);yield return new WaitForSeconds(.15f);
   int kills=g.Kills;enemy.PushBack(Vector3.right,4f,.25f);yield return new WaitForSeconds(.6f);
   check(enemy&&enemy.transform.position.y<deck.transform.position.y-1f,"Enemy pushed beyond the island edge falls instead of clamping or hovering");
   yield return new WaitForSeconds(1.2f);check(g.Kills==kills+1,"Falling enemy awards exactly one defeat");
   UnityEngine.Object.Destroy(deck);
   g.LoadLevel(4);yield return new WaitForSeconds(.3f);var boss=g.Enemies.Find(e=>e&&e.Boss);check(boss,"First guardian exists");
   float health=boss.Health;foreach(int power in new[]{0,1,2})boss.HitByTrap(999,power,true,Vector3.right);
   check(boss.Health==health,"Every trap damage mode respects guardian immunity");
   yield return new WaitForSeconds(30);check(boss&&boss.Health==health,"First guardian survives active arena traps before Aster arrives");
   boss.Hit(1,-1,false);check(boss.Health<health,"Guardian still receives Aster's combat damage");
  }
 }
}
