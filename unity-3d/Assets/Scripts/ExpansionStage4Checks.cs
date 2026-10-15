using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;
namespace LostRealms {
 public static class ExpansionStage4Checks {
  static readonly BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
  static void Defeat(Enemy e){if(!e||e.Health<=0)return;e.Hit(100000,0,false,Vector3.zero,true);if(e.Health>0)e.Hit(100000,0,false,Vector3.zero,true);}
  static Bounds PosedBounds(Enemy enemy){
   var skin=enemy.Visual.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh,false);var vertices=mesh.vertices;
   var bounds=new Bounds(skin.transform.TransformPoint(vertices[0]),Vector3.zero);
   foreach(var vertex in vertices)bounds.Encapsulate(skin.transform.TransformPoint(vertex));UnityEngine.Object.Destroy(mesh);return bounds;
  }
  static void Capture(Vector3 position,string name,float size=10){
   string folder=RuntimeEvidence.Folder(4);Directory.CreateDirectory(folder);var obj=new GameObject("Stormveil review camera");var cam=obj.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=size;cam.farClipPlane=150;cam.transform.position=position+new Vector3(10,14,-13);cam.transform.LookAt(position+Vector3.up*.5f);var rt=new RenderTexture(1100,800,24);cam.targetTexture=rt;var old=RenderTexture.active;cam.Render();RenderTexture.active=rt;var t=new Texture2D(1100,800,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,1100,800),0,0);t.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),t.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(t);UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(obj);
  }
  public static IEnumerator Run(Action<bool,string> check,Action<Vector2> move,Action jump){
   var g=RealmGame.I;string original=JsonUtility.ToJson(g.Save);var reviewedKinds=new System.Collections.Generic.HashSet<int>();
   var old=new Progress{stars=new int[15],best=new float[15],unlocked=15,coins=999,gems=42,equippedWeapon=47,healthRank=2};old.stars[0]=3;old.stars[14]=1;old.best[14]=123;old.relics.Add(7);old.achievements.Add("clear_15");Campaign.Normalize(old);Codex.Normalize(old);
   check(old.stars.Length==Campaign.Chapters&&old.best.Length==Campaign.Chapters&&old.unlocked==16&&old.stars[0]==3&&old.best[14]==123,"Completed 15-chapter save opens Stormveil without losing stars or times");check(old.coins==999&&old.gems==42&&old.equippedWeapon==47&&old.healthRank==2&&old.relics.Contains(7)&&old.achievements.Contains("clear_15"),"Stormveil migration preserves inventory, currencies, mastery and old rewards");
   Campaign.Normalize(old);check(old.unlocked==16&&old.stars[19]==0,"Campaign migration is idempotent and new chapters start uncleared");check(RealmGame.Titles.Length==Campaign.Chapters&&RealmGame.Realms.Length==6&&RealmGame.Accents.Length==6&&Codex.Kinds==32&&Codex.Entries.Length==32,"Campaign, realm and bestiary registries expand together");
   g.Save=new Progress{healthRank=3,unlocked=20};Campaign.Normalize(g.Save);
   foreach(string role in new[]{"StormStalker","StormScribe","TempestRegent"}){
    var prefab=Resources.Load<GameObject>("Characters/"+role);check(prefab&&Resources.Load<Texture2D>("Characters/Textures/"+role+"_basecolor"),"Textured Stormveil character asset exists: "+role);check(prefab.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh.isReadable,"Animated grounding mesh remains readable in a player build: "+role);
    foreach(string state in new[]{"idle","walk","attack","gethit","death","cast"}){var c=Resources.Load<AnimationClip>("Animations/"+role+"_"+state);check(c&&c.length>.2f,"Usable Stormveil animation: "+role+" / "+state);}
   }
   for(int chapter=16;chapter<=20;chapter++){
    g.LoadLevel(chapter);yield return new WaitForSeconds(.5f);check(g.Realm==4&&g.Storm&&g.World.IsBoss==(chapter==20),"New chapter maps to Stormveil: "+chapter);check(g.Player.Grounded&&g.World.Route.Count>=9,"Stormveil starts grounded with an authored route: "+chapter);check(!g.GateOpen(),"Conductor progression seals chapter "+chapter);
    foreach(var e in g.Enemies){check(e.Kind>=0&&e.Kind<Codex.Kinds&&e.Visual&&!e.Visual.UsesFallback&&(e.Visual.animator||e.GetComponent<SkeletonLimbMotion>()||e.GetComponent<SpiderLimbMotion>()),"New enemy uses its own animated model: "+e.Kind);check(e.Grounded,"Stormveil enemy starts on its deck: "+e.Kind+" position="+e.transform.position+" vertical="+e.VerticalVelocity);e.enabled=false;}
    foreach(var c in g.Storm.Circuits){check(c.Relays.Count==Mathf.Min(4,(chapter>=18?3:2)+(c.Index==0?0:1))&&!c.Completed,"Relay circuit authored: "+chapter+" / "+c.Index);check(!c.Turn(0,c.CorrectFor(0)),"Alive guards block conductor turning");}
    foreach(var enemy in g.Enemies)if(enemy.Kind>=26&&reviewedKinds.Add(enemy.Kind)){
     foreach(string pose in new[]{"idle","walk","attack","cast","gethit","death"}){
      enemy.Visual.Restart(pose);yield return new WaitForSeconds(.3f);var bounds=PosedBounds(enemy);float height=enemy.Boss?3.6f:enemy.Kind==27?2.25f:1.8f;
      check(bounds.size.y>height*.15f&&bounds.size.y<height*1.8f&&Vector3.Distance(bounds.center,enemy.transform.position)<height*1.8f&&Mathf.Abs(bounds.min.y-enemy.transform.position.y)<.25f,"Rendered mesh remains at its body and deck: "+enemy.Kind+" / "+pose+" "+bounds);
     }
     enemy.Visual.Restart("idle");yield return new WaitForSeconds(.3f);
    }
    Capture(g.Storm.Circuits[0].transform.position,"chapter-"+chapter+"-conductors");
    var stalker=g.Enemies.Find(e=>e&&e.Kind==26);var scribe=g.Enemies.Find(e=>e&&e.Kind==27);
    if(chapter==16){
     foreach(var e in new[]{stalker,scribe})if(e){g.Player.Warp(e.transform.position+Vector3.back*3);if(e.Kind==27)typeof(Enemy).GetField("timer",Private).SetValue(e,2f);e.enabled=true;var before=e.transform.position;yield return new WaitForSeconds(1.5f);check((e.transform.position-before).sqrMagnitude>.02f,"New family pursues or kites using its collision body: "+e.Kind);e.enabled=false;var bones=e.Visual.GetComponentsInChildren<Transform>();var rotations=new Quaternion[bones.Length];for(int i=0;i<bones.Length;i++)rotations[i]=bones[i].localRotation;e.Visual.Restart("walk");yield return new WaitForSeconds(.3f);float change=0;for(int i=0;i<bones.Length;i++)change+=Quaternion.Angle(rotations[i],bones[i].localRotation);check(change>1f,"Walk clips animate the actual Stormveil rig: "+e.Kind);Capture(e.transform.position,"enemy-"+e.Kind,3.2f);}
     yield return ExplorationChecks.Cross(g.World.Exploration.Crossings[0],check,move,jump,"Stormveil revised route");
    }
    foreach(var e in g.Enemies)if(!e.Boss)Defeat(e);yield return new WaitForSeconds(.2f);
    foreach(var key in g.World.Exploration.Finds)if(key.Key){g.Player.Warp(key.transform.position+Vector3.back*1.1f+Vector3.up*.05f);yield return new WaitForSeconds(1.1f);check(key.Claimed,"Gate key recovered before conductor completion");}
    foreach(var c in g.Storm.Circuits){
     for(int i=0;i<c.Relays.Count;i++)check(c.Turn(i,(c.CorrectFor(i)+1)%3),"Clear relay accepts an alternative orientation");yield return new WaitForSeconds(1f);check(!c.Completed,"Broken lightning links cannot complete a circuit");
     // Test real proximity controls, including stepping off before a second turn.
     for(int round=0;round<c.Rounds;round++){for(int i=0;i<c.Relays.Count;i++){g.Player.Warp(c.PadPosition(i,c.CorrectFor(i)));yield return new WaitForSeconds(.8f);g.Player.Warp(c.transform.position+Vector3.forward*2);yield return new WaitForSeconds(.08f);}
     yield return new WaitForSeconds(1.3f);}
     check(c.Completed,"Standing on pads redirects and completes circuit "+chapter+" / "+c.Index);check(!c.Turn(0,0),"Completed circuits are latched for checkpoint retries");
    }
    var winds=g.World.GetComponentsInChildren<StormWindWindow>();if(chapter==18){check(winds.Length>=3,"Causeway chapter has authored wind-window crossings");var w=winds[0];g.Screen=GameScreen.Paused;Time.timeScale=0;float cycle=w.Cycle;yield return new WaitForSecondsRealtime(.25f);check(Mathf.Abs(w.Cycle-cycle)<.001f,"Pause freezes wind timing and conductor interactions");Time.timeScale=1;g.Screen=GameScreen.Playing;w.PhaseOffset=4.2f-g.Elapsed;g.Player.Warp(w.transform.position+Vector3.up*.04f);var before=g.Player.transform.position;yield return new WaitForSeconds(.3f);check(w.Gusting&&Vector3.Distance(g.Player.transform.position,before)>.04f,"Exposed wind windows physically push Aster");Capture(w.transform.position,"wind-crossing");}
    var boss=g.Enemies.Find(e=>e&&e.Boss&&e.Health>0);
    if(boss){
     check(!g.GateOpen()&&g.Storm.ArenaRods.Count==4,"Guardian and arena conductors remain after approach circuits");boss.enabled=true;g.Player.Warp(boss.transform.position+Vector3.back*7);yield return new WaitForSeconds(.3f);var before=boss.transform.position;yield return new WaitForSeconds(1f);check((boss.transform.position-before).sqrMagnitude>.1f&&boss.Grounded,"Regent pursues Aster while grounded: delta="+(boss.transform.position-before)+" vertical="+boss.VerticalVelocity);
     for(int pattern=0;pattern<4;pattern++){typeof(Enemy).GetField("attackCount",Private).SetValue(boss,pattern);typeof(Enemy).GetField("state",Private).SetValue(boss,Enum.Parse(typeof(Enemy).GetNestedType("State",BindingFlags.NonPublic),"Notice"));typeof(Enemy).GetField("timer",Private).SetValue(boss,0f);typeof(Enemy).GetField("stormNext",Private).SetValue(boss,0f);g.Player.Warp(boss.transform.position+boss.transform.forward*(pattern==0?3f:5f));yield return new WaitForSeconds(.12f);check(boss.Telegraphing,"Regent warns before pattern "+pattern);g.Player.Warp(g.World.Route[g.World.Route.Count-1]+Vector3.left*10f);yield return new WaitForSeconds(3.3f);check(boss.Grounded&&Mathf.Abs(boss.transform.position.y-g.World.Route[g.World.Route.Count-1].y)<.15f,"Regent pattern stays on the platform: "+pattern);}
     check(boss.StormPatternMask==15,"Regent exposes all four distinct attack patterns");check(g.Storm.GroundArena(0)&&!g.Storm.GroundArena(1),"Grounded arena rods interrupt and share a recharge cooldown");check(!boss.Telegraphing,"Grounding clears the guardian's pending warning");Capture(boss.transform.position,"tempest-regent",5f);Defeat(boss);yield return new WaitForSeconds(.3f);
    }
    check(g.GateOpen(),"Chapter gate requires linked circuits and guardian victory: "+chapter);g.Finish();check(g.Save.stars[chapter-1]>=1&&g.Save.unlocked<=Campaign.Chapters,"New chapter rewards persist within expanded campaign bounds");
   }
   // A normal enemy uses the existing gravity and cannot hover off the island.
   g.LoadLevel(16);yield return new WaitForSeconds(.3f);var falling=g.Enemies[0];falling.enabled=false;var start=falling.transform.position;falling.Controller.enabled=false;falling.transform.position+=Vector3.right*250;falling.Controller.enabled=true;falling.enabled=true;yield return new WaitForSeconds(1f);check(!falling||falling.transform.position.y<start.y-2f,"Stormveil enemies fall outside the island footprint");
   g.Save=JsonUtility.FromJson<Progress>(original);Campaign.Normalize(g.Save);move(Vector2.zero);g.LoadLevel(1);yield return new WaitForSeconds(.2f);
  }
 }
}
