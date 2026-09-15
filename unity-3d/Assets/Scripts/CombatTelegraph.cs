using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
 // A shared annulus mesh replaces dozens of individual warning cubes.
 public sealed class CombatTelegraph:MonoBehaviour {
  static Mesh ringMesh;static Material material;
  Transform sweep;MeshRenderer rimRenderer,sweepRenderer;MaterialPropertyBlock properties;
  float duration,age,radius,holdUntil;Color color;
  public void HoldUntil(float gameTime){holdUntil=Mathf.Max(holdUntil,gameTime);}
  public float Progress=>Mathf.Clamp01(age/duration);
  static Mesh RingMesh(){
   if(ringMesh)return ringMesh;
   const int steps=64;var vertices=new Vector3[(steps+1)*2];var triangles=new int[steps*6];
   for(int i=0;i<=steps;i++){
    float a=i*Mathf.PI*2f/steps;var direction=new Vector3(Mathf.Sin(a),0,Mathf.Cos(a));
    vertices[i*2]=direction*.92f;vertices[i*2+1]=direction;
    if(i==steps)continue;int v=i*2,t=i*6;
    triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;
    triangles[t+3]=v+1;triangles[t+4]=v+2;triangles[t+5]=v+3;
   }
   ringMesh=new Mesh{name="Shared warning annulus"};ringMesh.vertices=vertices;ringMesh.triangles=triangles;ringMesh.RecalculateNormals();ringMesh.RecalculateBounds();return ringMesh;
  }
  public static MeshRenderer Ring(Transform parent,float size,Color tint,string name="Sigil"){
   if(!material)material=new Material(Shader.Find("Sprites/Default")){name="Shared ground sigil"};
   var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localScale=Vector3.one*size;
   go.AddComponent<MeshFilter>().sharedMesh=RingMesh();var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
   var block=new MaterialPropertyBlock();block.SetColor("_Color",tint);r.SetPropertyBlock(block);return r;
  }
  public static GameObject Create(Vector3 position,float size,float seconds,Transform parent){
   var go=new GameObject("Combat countdown");go.transform.SetParent(parent,false);go.transform.position=position+Vector3.up*.16f;
   var tell=go.AddComponent<CombatTelegraph>();tell.radius=size;tell.duration=Mathf.Max(.02f,seconds);tell.color=new Color(1f,.25f,.12f,.9f);tell.properties=new MaterialPropertyBlock();
   tell.rimRenderer=Ring(go.transform,size,tell.color,"Danger boundary");
   tell.sweepRenderer=Ring(go.transform,size*.08f,tell.color,"Countdown");tell.sweep=tell.sweepRenderer.transform;return go;
  }
  void Update(){
   var g=RealmGame.I;if(g&&(g.Screen!=GameScreen.Playing||g.Elapsed<holdUntil))return;
   age+=Time.deltaTime;float t=Progress;
   sweep.localScale=Vector3.one*radius*Mathf.Lerp(.08f,1f,t);
   color.a=.55f+.35f*t;properties.SetColor("_Color",color);rimRenderer.SetPropertyBlock(properties);sweepRenderer.SetPropertyBlock(properties);
  }
 }
}
