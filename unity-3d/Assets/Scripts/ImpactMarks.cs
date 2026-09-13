using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // Ground scorch/dirt marks left by heavy impacts, spell detonations and
 // deaths. Capped ring buffer so decals never accumulate on mobile.
 public static class ImpactMarks {
  static readonly List<ImpactMark> live=new List<ImpactMark>();
  public static void Place(Vector3 pos,float size,Color tint,int max=14){
   var g=RealmGame.I;if(g==null||g.World==null)return;
   var tex=Resources.Load<Texture2D>("VFX/Textures/scorch_01");if(!tex)return;
   var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.name="Impact mark";
   var col=quad.GetComponent<Collider>();if(col)Object.Destroy(col);
   quad.transform.SetParent(g.World.transform,false);
   quad.transform.position=pos+Vector3.up*.03f;
   quad.transform.rotation=Quaternion.Euler(90f,0f,Random.Range(0f,360f));
   quad.transform.localScale=Vector3.one*size;
   var mr=quad.GetComponent<MeshRenderer>();
   var m=new Material(Shader.Find("Sprites/Default")){name="Impact mark"};
   m.mainTexture=tex;m.color=new Color(tint.r,tint.g,tint.b,.5f);m.renderQueue=3000;
   mr.sharedMaterial=m;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;
   var mark=quad.AddComponent<ImpactMark>();mark.mat=m;live.Add(mark);
   while(live.Count>max){var old=live[0];live.RemoveAt(0);if(old)Object.Destroy(old.gameObject);}
  }
 }
 public sealed class ImpactMark:MonoBehaviour {
  public Material mat;float age;const float Life=7f;
  void Update(){
   age+=Time.deltaTime;
   if(mat){var c=mat.color;c.a=.5f*(1f-age/Life);mat.color=c;}
   if(age>=Life){if(mat)Destroy(mat);Destroy(gameObject);}
  }
 }
}
