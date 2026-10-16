using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public static class InteractionClearance {
  public static Transform RouteIsland(RealmWorld world,int index){
   Transform best=null;float distance=float.MaxValue;
   foreach(Transform t in world.transform){if(t.name!="Island")continue;float d=(t.position-world.Route[index]).sqrMagnitude;if(d<distance){distance=d;best=t;}}
   return best;
  }
  public static void MoveDesertSpikes(RealmWorld world,int stage){
   if(stage!=6||world.Route.Count<5)return;
   var source=RouteIsland(world,3);var next=RouteIsland(world,4);if(!source||!next)return;
   foreach(var spike in source.GetComponentsInChildren<SpikeTrap>()){
    foreach(var offset in new[]{new Vector3(0,0,2f),new Vector3(-1.5f,0,2f),new Vector3(1.5f,0,2f)}){
     var point=world.Route[4]+offset;
     if(!ChapterLayout.Supported(next,spike.transform,new Bounds(point,new Vector3(2.5f,.1f,2.5f)),out float y))continue;
     Vector3 local=next.InverseTransformPoint(new Vector3(point.x,y+.07f,point.z));
     if(!TrapArt.IsClear(next,local,1.4f))continue;
     Vector3 old=spike.transform.localPosition;
     TrapArt.Reserved.RemoveAll(s=>s.island==source&&Vector3.Distance(s.local,old)<.6f);
     spike.transform.SetParent(next,true);spike.transform.localPosition=local;TrapArt.Reserve(next,local,1.4f);break;
    }
   }
  }
  public static bool PropBounds(Transform t,out Bounds b){
   if(!ChapterLayout.BoundsOf(t,out b))return false;
   foreach(var c in t.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger)b.Encapsulate(c.bounds);
   return true;
  }
  public static bool IsNearTrap(RealmWorld world,Vector3 point,float radius=1.85f){
    if(!world)return false;
    float rsq=radius*radius;
    foreach(var s in world.GetComponentsInChildren<SpikeTrap>())
     if((new Vector2(s.transform.position.x-point.x,s.transform.position.z-point.z)).sqrMagnitude<rsq&&Mathf.Abs(s.transform.position.y-point.y)<1.8f)return true;
    foreach(var b in world.GetComponentsInChildren<FloorBladeTrap>())
     if((new Vector2(b.transform.position.x-point.x,b.transform.position.z-point.z)).sqrMagnitude<rsq&&Mathf.Abs(b.transform.position.y-point.y)<1.8f)return true;
    foreach(var p in world.GetComponentsInChildren<BouncePad>())
     if((new Vector2(p.transform.position.x-point.x,p.transform.position.z-point.z)).sqrMagnitude<rsq&&Mathf.Abs(p.transform.position.y-point.y)<1.8f)return true;
    foreach(var w in world.GetComponentsInChildren<SawTrap>())
     if((new Vector2(w.transform.position.x-point.x,w.transform.position.z-point.z)).sqrMagnitude<rsq&&Mathf.Abs(w.transform.position.y-point.y)<1.8f)return true;
    return false;
   }
   public static bool Near(Bounds b,Vector3 p,float radius){
   if(b.max.y<p.y-.7f||b.min.y>p.y+2.5f)return false;
   float x=Mathf.Clamp(p.x,b.min.x,b.max.x),z=Mathf.Clamp(p.z,b.min.z,b.max.z);
   return (new Vector2(x-p.x,z-p.z)).sqrMagnitude<radius*radius;
  }
  public static void Clear(RealmWorld world,int stage){
   Physics.SyncTransforms();var spaces=new List<Vector3>();var radii=new List<float>();
   foreach(var checkpoint in world.GetComponentsInChildren<RealmCheckpoint>()){
    spaces.Add(checkpoint.transform.position);radii.Add(1.5f);
    spaces.Add(checkpoint.SpawnPoint);radii.Add(.8f);
   }
   foreach(var pad in world.GetComponentsInChildren<BouncePad>()){spaces.Add(pad.transform.position);radii.Add(2.2f);}
    foreach(var spike in world.GetComponentsInChildren<SpikeTrap>()){spaces.Add(spike.transform.position);radii.Add(2.3f);}
    foreach(var blade in world.GetComponentsInChildren<FloorBladeTrap>()){spaces.Add(blade.transform.position);radii.Add(2.3f);}
    foreach(var saw in world.GetComponentsInChildren<SawTrap>()){spaces.Add(saw.transform.position);radii.Add(2.0f);}
   var gate=world.transform.Find("Realm gate");
   var next=stage==6?RouteIsland(world,4):null;
   foreach(var prop in world.GetComponentsInChildren<Transform>()){
    if(!prop||!ChapterLayout.IsProp(prop)||!PropBounds(prop,out var b))continue;
    bool remove=next&&prop.parent==next&&prop.name=="Prop House_01";
    for(int i=0;i<spaces.Count&&!remove;i++)remove=Near(b,spaces[i],radii[i]);
    if(gate&&!remove){
     // Keep a continuous walking approach, including oversized collider corners.
     for(float z=-8f;z<=-1f&&!remove;z+=.5f)remove=Near(b,gate.position+Vector3.forward*z,1.15f);
    }
    if(remove)Object.DestroyImmediate(prop.gameObject);
   }
   Physics.SyncTransforms();
  }
 }
}
