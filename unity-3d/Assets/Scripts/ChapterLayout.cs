using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // One final pass for every decoration path, including props on secret islands.
 public static class ChapterLayout {
  public static bool IsTrap(Transform t)=>t.GetComponent<SpikeTrap>()||t.GetComponent<SawTrap>()||t.GetComponent<FloorBladeTrap>()||t.GetComponent<PendulumTrap>()||t.GetComponent<FireGeyser>()||t.GetComponent<CrusherPillar>()||t.GetComponent<DartTurret>()||t.GetComponent<RollingBoulder>()||t.GetComponent<FlameBrazier>()||t.GetComponent<FrostTotem>()||t.GetComponent<SerpentStatue>();
  // Authored flank coordinates can land on the sloped rim of an imported
  // island. Find real deck support before props consume the remaining space.
  public static void PlaceTurretsOnDeck(Transform world){
   Physics.SyncTransforms();
   foreach(var turret in world.GetComponentsInChildren<DartTurret>()){
    var t=turret.transform;var island=t.parent;
    if(!island||island.name!="Island")continue;
    var size=new Vector3(2f,.1f,2f);
    if(Supported(island,t,new Bounds(t.position,size),out _))continue;
    Vector3 original=t.localPosition;
    int slot=TrapArt.Reserved.FindIndex(s=>s.island==island&&Vector3.Distance(s.local,original)<.1f);
    float radius=slot>=0?TrapArt.Reserved[slot].radius:1.6f;
    var own=slot>=0?TrapArt.Reserved[slot]:default;
    if(slot>=0)TrapArt.Reserved.RemoveAt(slot);
    bool placed=false;
    // Search nearest first, leaving the central traversal lane open.
    for(int ring=1;ring<=16&&!placed;ring++){
     for(int x=-ring;x<=ring&&!placed;x++)for(int z=-ring;z<=ring&&!placed;z++){
      if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))!=ring)continue;
      var candidate=original+new Vector3(x*.5f,0,z*.5f);
      if(Mathf.Abs(candidate.x)<1.6f||!TrapArt.IsClear(island,candidate,radius))continue;
      var p=island.TransformPoint(candidate);
      if(!Supported(island,t,new Bounds(p,size),out float y))continue;
      t.position=new Vector3(p.x,y+.05f,p.z);
      TrapArt.Reserve(island,t.localPosition,radius);placed=true;
     }
    }
    if(!placed&&slot>=0)TrapArt.Reserved.Add(own);
   }
   Physics.SyncTransforms();
  }
  public static void GroundTraps(Transform world){
   Physics.SyncTransforms();
   foreach(var t in world.GetComponentsInChildren<Transform>()){
    if(!t||!IsTrap(t)||!t.parent||t.parent.name!="Island")continue;
    // The full moving assembly may hang overhead. Sample its mounting area,
    // not the animated renderer's lowest point.
    var footprint=new Bounds(t.position,new Vector3(2.0f,.1f,2.0f));
    if(Supported(t.parent,t,footprint,out float y)){
     t.position=new Vector3(t.position.x,y+.05f,t.position.z);
    }else if(RealmProps.TryDeckSurface(t.parent,t.localPosition.x,t.localPosition.z,t,out float cy)){
     float worldY=t.parent.TransformPoint(new Vector3(t.localPosition.x,cy,t.localPosition.z)).y;
     t.position=new Vector3(t.position.x,worldY+.05f,t.position.z);
    }
   }
   Physics.SyncTransforms();
  }
  public static bool IsProp(Transform t)=>t.name.StartsWith("Prop ")||t.name.StartsWith("Breakable")||t.name.StartsWith("Pushable");
  public static bool BoundsOf(Transform t,out Bounds bounds){
   bounds=new Bounds();bool found=false;
   foreach(var r in t.GetComponentsInChildren<Renderer>()){
    if(!r.enabled||r is ParticleSystemRenderer||r is TrailRenderer)continue;
    if(!found){bounds=r.bounds;found=true;}else bounds.Encapsulate(r.bounds);
   }
   return found;
  }
  public static bool Overlap(Bounds a,Bounds b){
   // Tiny leaf contacts are fine; solid visual intersections are not.
   a.Expand(-.08f);b.Expand(-.08f);return a.Intersects(b);
  }
  public static bool Supported(Transform island,Transform root,Bounds bounds,out float height){
   height=0;float min=float.PositiveInfinity,max=float.NegativeInfinity;
   Vector3[] points={bounds.center,bounds.center+Vector3.right*bounds.extents.x*.7f,bounds.center-Vector3.right*bounds.extents.x*.7f,bounds.center+Vector3.forward*bounds.extents.z*.7f,bounds.center-Vector3.forward*bounds.extents.z*.7f};
   foreach(var p in points){
    var local=island.InverseTransformPoint(p);
    if(!RealmProps.TryDeckSurface(island,local.x,local.z,root,out float y))return false;
    y=island.TransformPoint(new Vector3(local.x,y,local.z)).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);
   }
   height=(min+max)*.5f;return max-min<=.45f;
  }
  public static void Clean(Transform world){
   // Keep imported scenery visible; strict validation must never delete a whole decoration layer.
   Physics.SyncTransforms();
   foreach(var t in world.GetComponentsInChildren<Transform>()){
    if(!IsProp(t)||!BoundsOf(t,out var b))continue;
    var island=t.parent;
    if(!island||island.name!="Island"){
     island=null;float best=float.MaxValue;
     foreach(Transform candidate in world){
      if(candidate.name!="Island")continue;
      float d=(candidate.position-t.position).sqrMagnitude;
      if(d<best){best=d;island=candidate;}
     }
    }
    if(!island)continue;
    if(Supported(island,t,b,out float y)){
     float delta=y-b.min.y;
     if(Mathf.Abs(delta)<=1.6f)t.position+=Vector3.up*delta;
    }
   }
   Physics.SyncTransforms();
  }

 }
}
