using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace LostRealms {
 public static class ExpansionStage2Checks {
  static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
  static void Defeat(Enemy e){if(!e||e.Health<=0)return;e.Hit(100000f,0,false,Vector3.zero,true);if(e.Health>0)e.Hit(100000f,0,false,Vector3.zero,true);}
  static void Capture(Transform target,string name){
   string folder=RuntimeEvidence.Folder(2);Directory.CreateDirectory(folder);
   var go=new GameObject("Stage 2 review camera");var cam=go.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=7;cam.farClipPlane=140;cam.transform.position=target.position+new Vector3(8,13,-12);cam.transform.LookAt(target.position+Vector3.up*.5f);
   var rt=new RenderTexture(1100,800,24);cam.targetTexture=rt;var old=RenderTexture.active;cam.Render();RenderTexture.active=rt;var image=new Texture2D(1100,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1100,800),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(image);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);
  }
  static void ResetSignature(Enemy e){typeof(Enemy).GetField("state",Private).SetValue(e,Enum.Parse(typeof(Enemy).GetField("state",Private).FieldType,"Notice"));typeof(Enemy).GetField("timer",Private).SetValue(e,0f);typeof(Enemy).GetField("signatureReady",Private).SetValue(e,0f);}
  public static IEnumerator Run(Action<bool,string> check){
   var g=RealmGame.I;
   for(int chapter=1;chapter<=15;chapter++){
    g.LoadLevel(chapter);yield return new WaitForSeconds(.3f);g.Player.Health=10000;var c=g.Adventure;
    check(c&&c.Nodes.Count==c.Definition.Route.Length,"Stage 2 chapter has its authored objective set "+chapter);
    foreach(var foe in g.Enemies)if(foe)foe.enabled=false;
    g.Coins=g.CoinsTotal;g.Gems=g.GemsTotal;g.Kills=g.KillsTotal;check(!g.GateOpen(),"Collection cannot bypass chapter objectives "+chapter);
    foreach(var node in c.Nodes){
     check(ChapterLayout.Supported(node.transform.parent,node.transform,new Bounds(node.transform.position,new Vector3(.8f,.1f,.8f)),out _),"Objective rests on visible ground "+chapter+"/"+node.Order);
     check(RealmTrials.ShrineClear(node.transform.position,g.World.transform,.9f),"Objective has clear interaction space "+chapter+"/"+node.Order);
    }
    if(c.Definition.Ordered){var last=c.Nodes[c.Nodes.Count-1];foreach(var e in last.Guards)Defeat(e);g.Player.Power=Mathf.Max(0,last.Element);c.Complete(last);check(!last.Completed,"Chapter sequence rejects last objective first "+chapter);}
    foreach(var node in c.Nodes){
     if(node.Element>=0){g.Player.Power=(node.Element+1)%3;c.Complete(node);check(!node.Completed,"Elemental objective rejects wrong power "+chapter+"/"+node.Order);}
     foreach(var foe in node.Guards)Defeat(foe);g.Player.Power=Mathf.Max(0,node.Element);g.Player.Warp(node.transform.position+Vector3.up*.05f);
     yield return new WaitForSeconds(.25f);g.Pause();yield return new WaitForSecondsRealtime(.4f);check(!node.Completed,"Objective channel pauses correctly "+chapter+"/"+node.Order);g.Resume();
     if(c.Definition.Kind==AdventureKind.Ritual){yield return new WaitForSeconds(.8f);check(!node.Completed,"Ritual requires sustained channel "+chapter);}
     yield return new WaitForSeconds(node.DwellTime+.45f);
     if(c.Definition.Kind==AdventureKind.Ambush){
      check(node.WaveStarted&&!node.Completed&&node.Guards.Exists(e=>e&&e.Health>0),"Outpost triggers a living reinforcement wave "+chapter+"/"+node.Order);
      foreach(var foe in node.Guards)if(foe&&foe.Health>0){foe.enabled=false;check(foe.Grounded,"Ambush reinforcement is grounded "+chapter+"/"+node.Order);Defeat(foe);}
      g.Player.Warp(node.transform.position+Vector3.up*.05f);yield return new WaitForSeconds(1.4f);
     }
     check(node.Completed,"Proximity gameplay restores objective "+chapter+"/"+node.Order);
    }
    if(c.Definition.Kind==AdventureKind.Guardian){check(!c.Ready&&!g.GateOpen(),"Approach seals cannot bypass living guardian "+chapter);foreach(var e in g.Enemies)if(e&&e.Boss)Defeat(e);yield return null;}
    check(c.Ready&&g.GateOpen(),"Authored chapter objectives unlock gate "+chapter);
    check(ChapterLayout.Supported(c.Branch,null,new Bounds(c.SecretPosition,new Vector3(.8f,.1f,.8f)),out _),"Optional secret has visible ground support "+chapter);
    if(c.Definition.EliteKind>=0){check(c.OptionalElite&&c.OptionalElite.NamedSignature==c.Definition.Signature&&c.OptionalElite.Visual&&!c.OptionalElite.Visual.UsesFallback,"Named elite uses existing animated model and unique signature "+chapter);Defeat(c.OptionalElite);yield return new WaitForSeconds(2.2f);}
    int coins=g.Coins,gems=g.Gems;g.Player.Warp(c.SecretPosition+Vector3.up*.05f);yield return new WaitForSeconds(1.4f);
    check(c.BranchVisited&&c.RewardClaimed&&g.Coins>=coins+c.GoldReward&&g.Gems>=gems+c.GemReward,"Optional secret grants its promised reward even after corpse cleanup "+chapter);
    coins=g.Coins;gems=g.Gems;yield return new WaitForSeconds(.5f);check(g.Coins==coins&&g.Gems==gems,"Chapter secret reward pays once "+chapter);
    Capture(c.Nodes[0].transform,"chapter-"+chapter+"-objective");Capture(c.Branch,"chapter-"+chapter+"-secret");
    int count=c.CompletedCount;g.Defeat();yield return new WaitForSecondsRealtime(.15f);g.Respawn();g.Resume();yield return new WaitForSeconds(.2f);check(c.CompletedCount==count&&c.RewardClaimed,"Checkpoint preserves chapter and secret progress "+chapter);
    g.LoadLevel(chapter);yield return null;check(g.Adventure.CompletedCount==0&&!g.Adventure.RewardClaimed,"Restart resets objectives and secrets "+chapter);
   }
   foreach(int chapter in new[]{2,3,6,9,12,14}){
    g.LoadLevel(chapter);yield return new WaitForSeconds(.4f);var e=g.Adventure.OptionalElite;foreach(var foe in g.Enemies)if(foe!=e)foe.enabled=false;g.Player.Health=10000;
    Vector3 player=ExpansionChapter.ClearSpot(g.World,g.Adventure.Branch,e.transform.position+Vector3.back*2.7f);g.Player.Warp(player+Vector3.up*.05f);yield return new WaitForSeconds(.2f);ResetSignature(e);yield return new WaitForSeconds(.25f);
    check(e.SignaturePending,"Named elite visibly telegraphs its signature "+chapter);Capture(e.transform,"chapter-"+chapter+"-signature");int attacks=e.SignatureAttacks;
    g.Pause();yield return new WaitForSecondsRealtime(1.3f);check(e.SignatureAttacks==attacks&&e.SignaturePending,"Named attack does not advance while paused "+chapter);g.Resume();yield return new WaitForSeconds(1.2f);
    check(e.SignatureAttacks==attacks+1&&e.Grounded,"Named signature executes without floating "+chapter);
    ResetSignature(e);yield return new WaitForSeconds(.2f);check(e.SignaturePending,"Named signature can be telegraphed again "+chapter);e.Stun(.8f);yield return new WaitForSeconds(1.3f);check(!e.SignaturePending&&e.SignatureAttacks==attacks+1,"Stunning elite cancels its warned attack "+chapter);
    ResetSignature(e);yield return new WaitForSeconds(.2f);Defeat(e);yield return new WaitForSeconds(.1f);check(!e.SignaturePending,"Death cancels pending named attack "+chapter);
   }
  }
 }
}