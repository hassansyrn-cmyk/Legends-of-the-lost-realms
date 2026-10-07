using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // One final pass for every decoration path, including props on secret islands.
 public static class ChapterLayout {
  public static bool IsTrap(Transform t)=>t.GetComponent<SpikeTrap>()||t.GetComponent<SawTrap>()||t.GetComponent<FloorBladeTrap>()||t.GetComponent<PendulumTrap>()||t.GetComponent<FireGeyser>()||t.GetComponent<CrusherPillar>()||t.GetComponent<DartTurret>()||t.GetComponent<RollingBoulder>()||t.GetComponent<WindVent>()||t.GetComponent<FlameBrazier>()||t.GetComponent<FrostTotem>()||t.GetComponent<SerpentStatue>();
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
   Vector3[] points={bounds.center,bounds.center+Vector3.right*bounds.extents.x*.85f,bounds.center-Vector3.right*bounds.extents.x*.85f,bounds.center+Vector3.forward*bounds.extents.z*.85f,bounds.center-Vector3.forward*bounds.extents.z*.85f,bounds.center+new Vector3(bounds.extents.x*.85f,0,bounds.extents.z*.85f),bounds.center+new Vector3(-bounds.extents.x*.85f,0,bounds.extents.z*.85f),bounds.center+new Vector3(bounds.extents.x*.85f,0,-bounds.extents.z*.85f),bounds.center-new Vector3(bounds.extents.x*.85f,0,bounds.extents.z*.85f)};
   foreach(var p in points){
    var local=island.InverseTransformPoint(p);
    if(!RealmProps.TryDeckSurface(island,local.x,local.z,root,out float y))return false;
    y=island.TransformPoint(new Vector3(local.x,y,local.z)).y;min=Mathf.Min(min,y);max=Mathf.Max(max,y);
   }
   height=max;return max-min<=.18f;
  }
  public static bool Soft(Transform t){string n=t.name.ToLowerInvariant();return n.Contains("grass")||n.Contains("flower")||n.Contains("bush")||n.Contains("pebbles")||n.Contains("skull");}
  public static bool Tree(Transform t){string n=t.name.ToLowerInvariant();return n.Contains("tree")||n.Contains("pine")||n.Contains("palm");}
  public static Transform IslandOf(Transform root,Transform world){
   for(var p=root.parent;p&&p!=world;p=p.parent)if(p.name=="Island")return p;
   Transform result=null;float best=float.MaxValue;
   foreach(Transform p in world){if(p.name!="Island")continue;float d=(p.position-root.position).sqrMagnitude;if(d<best){best=d;result=p;}}
   return result;
  }
  // Trees are supported by their trunks, not by their elevated canopy.
  public static Bounds Footprint(Transform root,Bounds visual){
   var bounds=visual;Bounds collision=default;bool found=false;
   foreach(var c in root.GetComponentsInChildren<Collider>())if(c.enabled&&!c.isTrigger){if(!found){collision=c.bounds;found=true;}else collision.Encapsulate(c.bounds);}
   if(Tree(root))return new Bounds(new Vector3(found?collision.center.x:visual.center.x,visual.min.y+.4f,found?collision.center.z:visual.center.z),new Vector3(found?Mathf.Max(.65f,collision.size.x):.65f,.8f,found?Mathf.Max(.65f,collision.size.z):.65f));
   if(found)bounds.Encapsulate(collision);return bounds;
  }
  static bool RootProp(Transform t){if(!IsProp(t))return false;for(var p=t.parent;p;p=p.parent)if(IsProp(p))return false;return true;}
  struct Placed {public Transform root;public Bounds visual,foot;}
  public static void SettleEnemies(Transform world){
   Physics.SyncTransforms();var g=RealmGame.I;if(!g)return;
   foreach(var enemy in g.Enemies){
    if(!enemy||!enemy.transform.IsChildOf(world))continue;
    var island=IslandOf(enemy.transform,world);if(!island)continue;
    Vector3 original=enemy.transform.position,best=original;float score=float.MaxValue;
    for(int x=-8;x<=8;x++)for(int z=-8;z<=8;z++){
     var candidate=original+new Vector3(x*.5f,0,z*.5f);var local=island.InverseTransformPoint(candidate);
     if(!TrapArt.IsClear(island,local,enemy.Boss?1.15f:.65f))continue;
     float diameter=enemy.Controller?enemy.Controller.radius*2f:1f;
     if(!Supported(island,enemy.transform,new Bounds(candidate,new Vector3(diameter,.1f,diameter)),out float y))continue;
     candidate.y=y+.03f;bool occupied=false;
     foreach(var other in g.Enemies)if(other&&other!=enemy){var delta=other.transform.position-candidate;delta.y=0;if(delta.magnitude<enemy.Radius+other.Radius+.35f&&Mathf.Abs(other.transform.position.y-candidate.y)<1f){occupied=true;break;}}
     if(occupied)continue;float cost=(candidate-original).sqrMagnitude;
     if(cost<score){score=cost;best=candidate;}
    }
    if(score<float.MaxValue)enemy.WarpBody(best);
    else Debug.LogWarning("ENEMY_SAFE_SPAWN_NOT_FOUND "+enemy.Kind+" at "+original);
   }
   Physics.SyncTransforms();
  }
  public static void Clean(Transform world){
   Physics.SyncTransforms();
   var props=new List<Transform>();foreach(var t in world.GetComponentsInChildren<Transform>())if(RootProp(t)&&BoundsOf(t,out _))props.Add(t);
   // Architecture gets its slots before rocks and small ground dressing.
   props.Sort((a,b)=>{int p=(Soft(a)?2:Tree(a)?1:0).CompareTo(Soft(b)?2:Tree(b)?1:0);if(p!=0)return p;BoundsOf(a,out var aa);BoundsOf(b,out var bb);return (bb.size.x*bb.size.z).CompareTo(aa.size.x*aa.size.z);});
   var accepted=new List<Placed>();int moved=0,omitted=0;
   foreach(var t in props){
    if(!t||!t.gameObject.activeInHierarchy)continue;var island=IslandOf(t,world);if(!island)continue;
    BoundsOf(t,out var original);var footprint=Footprint(t,original);var origin=t.position;bool found=false;
    for(int ring=0;ring<=14&&!found;ring++)for(int x=-ring;x<=ring&&!found;x++)for(int z=-ring;z<=ring&&!found;z++){
     if(Mathf.Max(Mathf.Abs(x),Mathf.Abs(z))!=ring)continue;
     Vector3 offset=new Vector3(x*.5f,0,z*.5f);var b=original;b.center+=offset;var foot=footprint;foot.center+=offset;
     var local=island.InverseTransformPoint(foot.center);
     // A continuous central lane leaves enough room for both combat and travel.
     if(!Soft(t)&&t.name!="Prop Gate_01"&&Mathf.Abs(local.x)<foot.extents.x+1.05f)continue;
     // Honor complete trap sweep reservations, not just the collider's current pose.
     if(!Soft(t)&&!TrapArt.IsClear(island,new Vector3(local.x,.05f,local.z),Mathf.Max(original.extents.x,original.extents.z)))continue;
     if(!Supported(island,t,foot,out float y))continue;
     offset.y=y-original.min.y;b.center+=Vector3.up*offset.y;foot.center+=Vector3.up*offset.y;
     if(!ClearInteractions(world,foot))continue;
     bool blocked=false;
     foreach(var other in accepted){
      // Ground cover may sit beneath leaves; solid trunks and walls never intersect.
      var a=Tree(t)||Soft(t)||Tree(other.root)||Soft(other.root)?foot:b;
      var c=Tree(t)||Soft(t)||Tree(other.root)||Soft(other.root)?other.foot:other.visual;
      a.Expand(.08f);if(Overlap(a,c)){blocked=true;break;}
     }
     if(blocked)continue;
     t.position=origin+offset;if(offset.sqrMagnitude>.0025f)moved++;
     if(!Soft(t))RealmProps.AddCollider(t.gameObject);
     accepted.Add(new Placed{root=t,visual=b,foot=foot});found=true;
    }
    if(!found){omitted++;t.gameObject.SetActive(false);Object.Destroy(t.gameObject);}
   }
   Physics.SyncTransforms();
   Debug.Log("CHAPTER_LAYOUT organized="+(props.Count-omitted)+" relocated="+moved+" unsupported_or_crowded_omitted="+omitted);
  }
  static bool ClearInteractions(Transform world,Bounds foot){
   var g=RealmGame.I;
   if(g){
    if(HorizontalDistance(foot,g.World.Spawn)<2.3f)return false;
    foreach(var enemy in g.Enemies)if(enemy&&HorizontalDistance(foot,enemy.transform.position)<enemy.Radius+.25f)return false;
   }
   foreach(var hit in Physics.OverlapBox(foot.center,foot.extents+new Vector3(.12f,.1f,.12f),Quaternion.identity,~0,QueryTriggerInteraction.Ignore)){
    if(!hit.transform.IsChildOf(world))continue;
    for(var p=hit.transform;p&&p!=world;p=p.parent)if(IsTrap(p)||p.GetComponent<RealmGate>()||p.GetComponent<CheckpointVisual>())return false;
   }
   return true;
  }
  static float HorizontalDistance(Bounds b,Vector3 p){p.y=b.center.y;return Vector3.Distance(p,b.ClosestPoint(p));}

 }
}
