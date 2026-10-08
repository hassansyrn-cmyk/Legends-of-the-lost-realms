using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace LostRealms {
 // Only the visual follows the animated soles. The existing collision body
 // supplies gravity, platform movement, knockback and abyss deaths unchanged.
 [DefaultExecutionOrder(100)]
 public sealed class EnemyGroundPose:MonoBehaviour {
  SkinnedMeshRenderer skin;Transform actor;CharacterVisual visual;Vector3[] vertices;Mesh posedMesh;readonly List<Vector3> posedVertices=new List<Vector3>();int[] samples;
  public IReadOnlyList<int> SoleVertices=>samples;
  public void Setup(Transform root,SkinnedMeshRenderer renderer){
   actor=root;skin=renderer;skin.quality=SkinQuality.Bone4;visual=GetComponent<CharacterVisual>();var mesh=skin.sharedMesh;vertices=mesh.vertices;posedMesh=new Mesh{name="Enemy sole pose"};
   // AccuRig can weight boots entirely to the calf, leaving the Foot and Toe
   // bones without mesh weights. Select the actual lowest sole geometry.
   var rest=vertices.Select(p=>skin.transform.TransformPoint(p)).ToArray();float centerX=skin.bounds.center.x;
   var ordered=Enumerable.Range(0,vertices.Length).OrderBy(i=>rest[i].y).ToArray();
   samples=ordered.Where(i=>rest[i].x<centerX).Take(128).Concat(ordered.Where(i=>rest[i].x>=centerX).Take(128)).Distinct().ToArray();
   if(samples.Length==0)Debug.LogError("No sole vertices for "+skin.name);
  }
  void LateUpdate(){SnapPose();}
  public void SnapPose(){
   if(!skin||!actor||samples==null||samples.Length==0||!RealmGame.I||RealmGame.I.Screen!=GameScreen.Playing)return;
   skin.BakeMesh(posedMesh,false);posedMesh.GetVertices(posedVertices);
   float floor=float.PositiveInfinity;
   // The large rock body can extend below its soles during a low attack or
   // crossfade. Keep the entire body above the collision deck in those poses.
   if(visual&&(visual.CurrentState=="death"||visual.name.Contains("StoneBrute"))){foreach(var p in posedVertices)floor=Mathf.Min(floor,skin.transform.TransformPoint(p).y);}
   else foreach(int i in samples)floor=Mathf.Min(floor,skin.transform.TransformPoint(posedVertices[i]).y);
   transform.position+=Vector3.up*(actor.position.y-floor+.005f);
  }
  void OnDestroy(){if(posedMesh){if(Application.isPlaying)Destroy(posedMesh);else DestroyImmediate(posedMesh);}}
 }
}
