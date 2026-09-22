using UnityEngine;
namespace LostRealms {
 // Segment tests prevent fast projectiles skipping Aster between rendered frames.
 public static class ProjectileSweep {
  public static bool Hits(Vector3 from,Vector3 to,Vector3 center,float radius,out float fraction){
   Vector3 segment=to-from;float length=segment.sqrMagnitude;
   fraction=length>1e-8f?Mathf.Clamp01(Vector3.Dot(center-from,segment)/length):0f;
   return (from+segment*fraction-center).sqrMagnitude<=radius*radius;
  }
 }
}
