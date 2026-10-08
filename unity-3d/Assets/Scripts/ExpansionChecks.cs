using System;
using System.Collections;
using System.IO;
using UnityEngine;
namespace LostRealms {
 public static class ExpansionChecks {
  static void Defeat(Enemy e){if(!e||e.Health<=0)return;e.Hit(100000f,0,false,Vector3.zero,true);if(e.Health>0)e.Hit(100000f,0,false,Vector3.zero,true);}
  static void Capture(Transform target,string name){
   string folder=Path.Combine(Application.dataPath,"../Validation/ExpansionStage1");Directory.CreateDirectory(folder);
   var go=new GameObject("Expansion review camera");var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=8;cam.farClipPlane=140;cam.transform.position=target.position+new Vector3(8,13,-12);cam.transform.LookAt(target.position);
   var rt=new RenderTexture(1100,800,24);cam.targetTexture=rt;var old=RenderTexture.active;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1100,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,800),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);
  }
  static IEnumerator Cross(Action<Vector2> move,Action jump,Vector3 direction){
   var g=RealmGame.I;Vector3 input=Quaternion.Euler(0,-g.CameraRig.Yaw,0)*direction;move(new Vector2(input.x,input.z));yield return new WaitForSeconds(.15f);jump();yield return new WaitForSeconds(.4f);jump();yield return new WaitForSeconds(1f);move(Vector2.zero);yield return new WaitForSeconds(.6f);
  }
  public static IEnumerator Run(Action<bool,string> check,Action<Vector2> move,Action jump){
   var g=RealmGame.I;
   g.LoadLevel(1);yield return new WaitForSeconds(.2f);
   check(!g.Adventure,"Stage one leaves the other chapters' objectives unchanged");
   bool epic=false;foreach(var drop in UnityEngine.Object.FindObjectsByType<WeaponDrop>(FindObjectsSortMode.None))if(WeaponCatalog.IsEpic(drop.Id))epic=true;
   check(!epic,"Normal campaign has no temporary first-chapter Epic pickup");
   foreach(int chapter in new[]{2,6,9}){
    g.LoadLevel(chapter);yield return new WaitForSeconds(.3f);var c=g.Adventure;g.Player.Health=10000;
    check(c&&c.Nodes.Count==(chapter==9?3:2),"Authored objectives present in chapter "+chapter);
    foreach(var foe in g.Enemies)foe.enabled=false;
    Vector3 takeoff=default;var takeoffIsland=ExpansionChapter.Nearest(g.World,g.World.Route[4]);bool launchFound=false;
    foreach(float x in new[]{-2.7f,-2f,-3.4f,-1.5f})foreach(float z in new[]{0f,-2.5f,2.5f,-1.5f,1.5f,-3f,3f}){
     Vector3 p=g.World.Route[4]+new Vector3(x,0,z);if(!ChapterLayout.Supported(takeoffIsland,null,new Bounds(p,new Vector3(.6f,.1f,.6f)),out float y))continue;p.y=y+.05f;if(!RealmTrials.ShrineClear(p,g.World.transform,.6f))continue;takeoff=p;launchFound=true;break;
    }
    check(launchFound,"Detour has a real takeoff surface "+chapter);
    g.Player.Warp(takeoff);yield return new WaitForSeconds(.3f);check(!c.BranchVisited,"Standing near a detour does not falsely record participation "+chapter);yield return Cross(move,jump,Vector3.left);
    check(g.Player.Grounded&&Vector3.Distance(g.Player.transform.position,c.Branch.position)<5f,"Aster reaches optional clearing using normal double jump "+chapter);
    yield return Cross(move,jump,Vector3.right);
    check(g.Player.Grounded&&Vector3.Distance(g.Player.transform.position,g.World.Route[4])<5.5f,"Aster can return from optional clearing using normal movement "+chapter);
    g.Coins=g.CoinsTotal;g.Gems=g.GemsTotal;g.Kills=g.KillsTotal;
    check(!g.GateOpen(),"Collecting everything cannot bypass authored objective "+chapter);
    g.Finish();check(g.Screen==GameScreen.Playing,"Sealed gate rejects finish in prototype "+chapter);
    check(c.Branch&&!c.Branch.GetComponent<MovingIsland>(),"Optional elite clearing is stable in chapter "+chapter);
    check(c.OptionalElite&&!c.OptionalElite.Boss&&c.OptionalElite.Visual,"Optional encounter retains an animated enemy in chapter "+chapter);
    check(ChapterLayout.Supported(c.Branch,null,new Bounds(c.OptionalElite.transform.position,new Vector3(1f,.1f,1f)),out _),"Optional elite has real visible deck support in chapter "+chapter);
    Capture(c.Branch,"chapter-"+chapter+"-optional");
    foreach(var node in c.Nodes){
     check(ChapterLayout.Supported(node.transform.parent,node.transform,new Bounds(node.transform.position,new Vector3(.8f,.1f,.8f)),out _),"Objective has a grounded footprint "+chapter+"/"+node.Order);
     check(RealmTrials.ShrineClear(node.transform.position,g.World.transform,.9f),"Objective activation space is free of props and traps "+chapter+"/"+node.Order);
    }
    var first=c.Nodes[0];g.Player.Warp(first.transform.position+Vector3.left*.65f+Vector3.up*.05f);yield return new WaitForSeconds(.3f);
    if(chapter!=9){check(first.Guards.Count>0,"Restoration has an authored guard encounter "+chapter);yield return new WaitForSeconds(1f);check(!first.Completed,"Living guards prevent restoration "+chapter);}
    else {g.Player.Power=1;yield return new WaitForSeconds(1f);check(!first.Completed,"Wrong element cannot activate crystal");g.Player.Power=0;g.Player.Warp(c.Nodes[1].transform.position+Vector3.left*.65f+Vector3.up*.05f);yield return new WaitForSeconds(1f);check(!c.Nodes[1].Completed,"Crystal order cannot be bypassed");}
    foreach(var node in c.Nodes){
     foreach(var guard in node.Guards)Defeat(guard);
     g.Player.Power=node.Element>=0?node.Element:0;g.Player.Warp(node.transform.position+Vector3.left*.65f+Vector3.up*.05f);yield return new WaitForSeconds(.35f);
     g.Pause();yield return new WaitForSecondsRealtime(1f);check(!node.Completed,"Objective dwell freezes during pause "+chapter+"/"+node.Order);g.Resume();
     yield return new WaitForSeconds(1.5f);check(node.Completed,"Actual proximity interaction completes objective "+chapter+"/"+node.Order);
     Capture(node.transform,"chapter-"+chapter+"-node-"+node.Order);
    }
    check(c.Ready&&g.GateOpen(),"Authored objective opens prototype gate "+chapter);
    int completed=c.CompletedCount;g.Defeat();yield return new WaitForSecondsRealtime(.2f);g.Respawn();g.Resume();yield return new WaitForSeconds(.2f);check(c.CompletedCount==completed,"Checkpoint retry preserves objective progress "+chapter);
    // Optional reward cannot be obtained merely by reaching the clearing.
    g.Player.Warp(c.Branch.position+Vector3.up*.06f);yield return new WaitForSeconds(.6f);check(c.BranchVisited&&!c.RewardClaimed,"Optional clearing records visit without paying before victory "+chapter);
    int gold=g.Coins,gems=g.Gems;Defeat(c.OptionalElite);yield return new WaitForSeconds(.6f);check(c.RewardClaimed&&g.Coins>=gold+60+chapter*5&&g.Gems>=gems+4,"Optional elite pays the promised bonus "+chapter);
    gold=g.Coins;gems=g.Gems;yield return new WaitForSeconds(1f);check(g.Coins==gold&&g.Gems==gems,"Optional reward cannot pay twice "+chapter);
    if(chapter==6){
     Transform portal=null;foreach(var t in c.Nodes[0].transform.parent.GetComponentsInChildren<Transform>())if(t.name=="Adventure RETURN PASSAGE")portal=t;
     check(portal,"Temple shortcut has a real entry");g.Player.Warp(portal.position+Vector3.up*.05f);foreach(var foe in g.Enemies)Defeat(foe);yield return new WaitForSeconds(1.8f);
     check(Vector3.Distance(g.Player.transform.position,c.Nodes[1].transform.position)<6f,"Temple return passage physically moves Aster to the other clearing");check(g.Player.Grounded,"Return passage lands Aster on real deck");
     Vector3 arrival=g.Player.transform.position;yield return new WaitForSeconds(5f);check(Vector3.Distance(arrival,g.Player.transform.position)<.2f,"Standing at arrival cannot repeatedly bounce through the shortcut");
    }
    float active=g.Pacing.Current.activeSeconds;g.Pause();yield return new WaitForSecondsRealtime(.5f);check(g.Pacing.Current.activeSeconds<active+.05f&&g.Pacing.Current.pausedSeconds>=.5f,"Pacing separates paused and active play in chapter "+chapter);g.Resume();
    check(g.Pacing.Current.deaths==1,"Pacing records checkpoint retry death once "+chapter);
    g.Finish();check(g.Screen==GameScreen.Complete,"Prototype can be completed with its objectives "+chapter);check(g.Pacing.Last.outcome=="completed"&&g.Pacing.Last.optionalCompleted&&g.Pacing.Last.objectives==c.Nodes.Count,"Completed run captures optional and objective participation "+chapter);
    g.LoadLevel(chapter);yield return null;check(g.Adventure.CompletedCount==0&&!g.Adventure.RewardClaimed,"Full restart resets chapter objective and optional reward state "+chapter);
   }
  }
 }
}
