using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace LostRealms {
 public static class ChapterPhysicsProbe {
  public static Transform IslandOf(Transform root,RealmWorld world){
   var t=root;while(t&&t!=world.transform){if(t.name=="Island")return t;t=t.parent;}
   Transform best=null;float distance=float.MaxValue;
   foreach(Transform island in world.transform){if(island.name!="Island")continue;float d=(island.position-root.position).sqrMagnitude;if(d<distance){distance=d;best=island;}}
   return best;
  }
  public static bool Decorative(Transform t)=>ChapterLayout.Soft(t);
  public static Bounds Footprint(Transform t,Bounds visual){
   bool found=false;var result=visual;
   foreach(var c in t.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger){if(!found){result=c.bounds;found=true;}else result.Encapsulate(c.bounds);}
   if(t.name.Contains("Tree")||t.name.Contains("tree")||t.name.Contains("Pine")||t.name.Contains("Palm"))return new Bounds(visual.center,new Vector3(.6f,visual.size.y,.6f));
   return result;
  }
  public static void Run(){
   if(!Application.isBatchMode)throw new InvalidOperationException("Use a separate Unity test process.");
   bool baseline=Array.IndexOf(Environment.GetCommandLineArgs(),"-layoutBaseline")>=0;
   var log=new StringBuilder();int failures=0,props=0,overlaps=0,floating=0,missingCollision=0,blocked=0,trapBlocked=0;
   var cameraObject=new GameObject("Layout inspection camera");cameraObject.tag="MainCamera";cameraObject.AddComponent<Camera>();
   bool visuals=Array.IndexOf(Environment.GetCommandLineArgs(),"-layoutVisual")>=0;
   GameObject key=null;
   if(visuals){key=new GameObject("Layout review sun");var light=key.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;key.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=new Color(.55f,.55f,.55f);}
   var gameObject=new GameObject("Layout inspection game");var game=gameObject.AddComponent<RealmGame>();RealmGame.I=game;game.Save=new Progress();
   for(int chapter=1;chapter<=15;chapter++){
    game.Level=chapter;game.Realm=chapter<=4?0:chapter<=7?1:chapter<=10?2:3;game.Enemies.Clear();TrapArt.Reserved.Clear();
    var worldObject=new GameObject("Layout inspection chapter "+chapter);var world=worldObject.AddComponent<RealmWorld>();game.World=world;
    int start=failures,chapterProps=0;
    try{
     world.Build(chapter,game.Realm);Physics.SyncTransforms();
     var seen=new List<Transform>();var bounds=new List<Bounds>();
     foreach(var t in world.GetComponentsInChildren<Transform>()){
      if(!ChapterLayout.IsProp(t)||!t.gameObject.activeInHierarchy||!ChapterLayout.BoundsOf(t,out var b))continue;
      bool child=false;var p=t.parent;while(p&&p!=world.transform){if(ChapterLayout.IsProp(p)){child=true;break;}p=p.parent;}if(child)continue;
      props++;chapterProps++;var island=IslandOf(t,world);var footprint=baseline?Footprint(t,b):ChapterLayout.Footprint(t,b);
      if(!island||!ChapterLayout.Supported(island,t,footprint,out float y)||Mathf.Abs(b.min.y-y)>.18f){
       failures++;floating++;log.AppendLine($"FLOAT chapter={chapter} {t.name} pos={t.position} bounds={b}");
      }
      bool collider=false;foreach(var c in t.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger)collider=true;
      if(!collider&&!Decorative(t)){failures++;missingCollision++;log.AppendLine($"NO_COLLISION chapter={chapter} {t.name}");}
      if(!baseline&&island&&!Decorative(t)){
       var local=island.InverseTransformPoint(footprint.center);
       if(!TrapArt.IsClear(island,new Vector3(local.x,.05f,local.z),Mathf.Max(b.extents.x,b.extents.z))){failures++;trapBlocked++;log.AppendLine($"TRAP_SWEEP_BLOCK chapter={chapter} {t.name}");}
      }
      for(int i=0;i<seen.Count;i++){
       bool trunks=!baseline&&(ChapterLayout.Tree(t)||ChapterLayout.Tree(seen[i])||Decorative(t)||Decorative(seen[i]));
       var aa=trunks?ChapterLayout.Footprint(t,b):b;var bb=trunks?ChapterLayout.Footprint(seen[i],bounds[i]):bounds[i];
       if(ChapterLayout.Overlap(aa,bb)){failures++;overlaps++;log.AppendLine($"OVERLAP chapter={chapter} {t.name} / {seen[i].name}");}
      }
      seen.Add(t);bounds.Add(b);
      if(island&&t.name!="Prop Gate_01"&&collider&&!Decorative(t)){
       var local=island.InverseTransformPoint(footprint.center);
       if(Mathf.Abs(local.x)<footprint.extents.x+1.0f){failures++;blocked++;log.AppendLine($"LANE_BLOCK chapter={chapter} {t.name}");}
      }
     }
     foreach(var enemy in game.Enemies){
      var island=IslandOf(enemy.transform,world);var local=island?island.InverseTransformPoint(enemy.transform.position):Vector3.zero;
      if(!island||!RealmProps.TryDeckSurface(island,local.x,local.z,enemy.transform,out float y)||Mathf.Abs(enemy.transform.position.y-island.TransformPoint(new Vector3(local.x,y,local.z)).y)>.25f){failures++;log.AppendLine($"ENEMY_UNGROUNDED chapter={chapter} kind={enemy.Kind} at={enemy.transform.position}");}
     }
     if(visuals)RenderChapter(world,cameraObject.GetComponent<Camera>(),chapter);
     log.AppendLine($"CHAPTER {chapter:D2} props={chapterProps} enemies={game.Enemies.Count} failures={failures-start}");
    }catch(Exception e){failures++;log.AppendLine(e.ToString());}
    finally{UnityEngine.Object.DestroyImmediate(worldObject);game.Enemies.Clear();}
   }
   UnityEngine.Object.DestroyImmediate(gameObject);UnityEngine.Object.DestroyImmediate(cameraObject);if(key)UnityEngine.Object.DestroyImmediate(key);RealmGame.I=null;
   log.AppendLine($"SUMMARY chapters=15 props={props} overlaps={overlaps} unsupported_or_floating={floating} missing_collision={missingCollision} blocked_lanes={blocked} blocked_trap_sweeps={trapBlocked} failures={failures}");
   Directory.CreateDirectory("Validation");File.WriteAllText("Validation/chapter-physics-"+(baseline?"baseline":"layout")+".txt",log.ToString());
   Debug.Log(log.ToString());
   if(!baseline&&failures==0&&Array.IndexOf(Environment.GetCommandLineArgs(),"-layoutThenPhysics")>=0){QualityValidation.Run();return;}
   EditorApplication.Exit(baseline||failures==0?0:1);
  }
  static void RenderChapter(RealmWorld world,Camera camera,int chapter){
   Directory.CreateDirectory("Validation/ChapterPhysics");camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.09f,.15f,.2f);camera.fieldOfView=42;camera.farClipPlane=100;camera.nearClipPlane=.2f;
   int[] views={0,world.Route.Count/2,world.Route.Count-1};
   for(int i=0;i<views.Length;i++){
    var focus=world.Route[views[i]];camera.transform.position=focus+new Vector3(9,13,-15);camera.transform.LookAt(focus);
    var target=new RenderTexture(900,600,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
    var picture=new Texture2D(900,600,TextureFormat.RGB24,false);picture.ReadPixels(new Rect(0,0,900,600),0,0);picture.Apply();
    File.WriteAllBytes($"Validation/ChapterPhysics/chapter-{chapter:D2}-{i}.png",picture.EncodeToPNG());
    RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(picture);UnityEngine.Object.DestroyImmediate(target);
   }
  }
 }
}
