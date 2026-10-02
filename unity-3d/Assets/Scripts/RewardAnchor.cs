using UnityEngine;
namespace LostRealms {
 // Rewards follow their supporting ferry instead of bobbing at an old world position.
 public sealed class RewardAnchor {
  Transform support;Vector3 local,position;
  public Vector3 Position=>support?support.TransformPoint(local):position;
  public void Bind(Transform owner,Vector3 origin){
   position=origin;support=null;float highest=float.NegativeInfinity;
   Physics.SyncTransforms();
   foreach(var hit in Physics.RaycastAll(origin+Vector3.up*4f,Vector3.down,8f,~0,QueryTriggerInteraction.Ignore)){
    if(hit.normal.y<.55f||hit.point.y<=highest||hit.transform==owner||hit.transform.IsChildOf(owner))continue;
    if(hit.collider.GetComponentInParent<Enemy>()||hit.collider.GetComponentInParent<Hero>())continue;
    highest=hit.point.y;var ferry=hit.collider.GetComponentInParent<MovingIsland>();
    support=ferry?ferry.transform:null;
   }
   if(support)local=support.InverseTransformPoint(origin);
  }
 }
}
