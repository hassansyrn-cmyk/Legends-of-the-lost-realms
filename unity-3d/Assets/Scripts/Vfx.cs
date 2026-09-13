using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // One-shot spawner for the imported particle VFX packs. Prefabs were
 // relocated into Resources/VFX (GabrielAguiar BIRP = ga_*, Eric VFX =
 // eric_*, Mayker slash = mayker_*) so every effect is optional at build
 // time: if a prefab is missing, Play() silently returns null.
 public static class Vfx {
  static readonly Dictionary<string,GameObject> cache=new Dictionary<string,GameObject>();
  static GameObject Load(string key){
   if(cache.TryGetValue(key,out var prefab)&&prefab)return prefab;
   prefab=Resources.Load<GameObject>("VFX/"+key);
   cache[key]=prefab;return prefab;
  }
  public static GameObject Play(string key,Vector3 pos,Quaternion rotation=default,float scale=1f,Transform parent=null){
   var prefab=Load(key);if(!prefab)return null;
   Transform host=parent?parent:(RealmGame.I?RealmGame.I.World.transform:null);
   var go=Object.Instantiate(prefab,pos,rotation,host);
   go.transform.localScale=Vector3.one*scale;
   Repair(go);
   var kill=go.AddComponent<VfxKill>();
   kill.Life=LifeOf(go,scale);
   return go;
  }
  // Mother a looping weapon trail to the hero so it follows the draw/swing.
  public static GameObject Attach(string key,Transform parent,Vector3 localPos,Vector3 localScale){
   var prefab=Load(key);if(!prefab)return null;
   var go=Object.Instantiate(prefab,parent);
   go.transform.localPosition=localPos;
   go.transform.localScale=new Vector3(localScale.x,localScale.y,localScale.z);
   Repair(go);
   var kill=go.AddComponent<VfxKill>();
   kill.Life=LifeOf(go,1f);
   return go;
  }
  // Asset-store VFX shipped for other pipelines: (a) some velocity/force curves
  // mix modes ("Velocity curves must all be in the same mode" spam) and (b) some
  // shaders render opaque black on device. Normalise both so effects always
  // render: same-mode curves, and a Sprite material keyed to the original map.
  // Public so raw (non-VfxKill) instantiates like the rain volumes can use it.
  public static void Repair(GameObject go){
   foreach(var ps in go.GetComponentsInChildren<ParticleSystem>(true)){
    var vel=ps.velocityOverLifetime;
    if(vel.enabled){var c=vel.x;vel.y=c;vel.z=c;}
    var force=ps.forceOverLifetime;
    if(force.enabled){var f=force.x;force.y=f;force.z=f;}
   }
   foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
    var m=r.sharedMaterial;if(!m)continue;
    var sh=m.shader;string n=sh?sh.name:"";
    if(n.StartsWith("Sprites/")||n.StartsWith("Unlit/"))continue;
    Texture tex=m.HasProperty("_MainTex")?m.GetTexture("_MainTex"):(m.HasProperty("_BaseMap")?m.GetTexture("_BaseMap"):null);
    Color col=m.HasProperty("_Color")?m.GetColor("_Color"):(m.HasProperty("_BaseColor")?m.GetColor("_BaseColor"):Color.white);
    var nm=new Material(Shader.Find("Sprites/Default")){name=n+" -> sprite"};
    nm.mainTexture=tex;nm.color=col;
    r.material=nm;
   }
  }
  // Blade-slash effect matching the current element (0 ember / 1 frost / 2 gale).
  public static string Slash(int power){
   return power==0?"mayker_Slash Fire VFX":power==1?"mayker_Slash Water VFX":power==2?"mayker_Slash Eletric VFX":"mayker_Slash VFX";
  }
  static float LifeOf(GameObject go,float scale){
   float longest=0f;
   foreach(var ps in go.GetComponentsInChildren<ParticleSystem>(true)){
    var main=ps.main;float d=main.duration*Mathf.Max(ps.main.startLifetime.constantMax,ps.main.startLifetime.constantMin);
    if(ps.main.loop){d=2.5f;}
    if(d>longest)longest=d;
   }
   return Mathf.Max(1.4f,longest+1.2f)*scale;
  }
 }
 public sealed class VfxKill:MonoBehaviour {
  public float Life=3f;float age;
  void Update(){age+=Time.unscaledDeltaTime;if(age>=Life)Destroy(gameObject);}
 }
 // Code-built sprite puff burst on the Kenney smoke texture (no prefab).
 // Used for projectile impacts and other small soft bursts a shared VFX key
 // would be overkill for. Missing texture degrades to a no-op like Vfx.Play.
 public sealed class KenneyPuff:MonoBehaviour {
  static Mesh quad;static Texture2D smoke;
  Vector3 vel;float age,life,grow;Material mat;
  static Mesh Quad(){
   if(quad)return quad;
   var m=new Mesh{name="Puff quad"};
   m.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
   m.triangles=new[]{0,1,2,0,2,3};m.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
   m.RecalculateNormals();m.RecalculateBounds();quad=m;return m;
  }
  public static void Burst(Vector3 pos,Color color,int count=10,float scale=1f){
   if(!smoke)smoke=Resources.Load<Texture2D>("VFX/Textures/smoke_04");
   if(!smoke||!Camera.main)return;
   for(int i=0;i<count;i++){
    var go=new GameObject("Kenney puff");go.transform.SetParent(RealmGame.I?RealmGame.I.World.transform:null,false);go.transform.position=pos+Random.insideUnitSphere*.25f*scale;
    var p=go.AddComponent<KenneyPuff>();p.life=Random.Range(.45f,.8f);p.grow=Random.Range(1.2f,2.2f)*scale;
    p.vel=Vector3.up*Random.Range(.6f,1.8f)+Random.insideUnitSphere*Random.Range(.8f,2f)*scale;
    p.mat=new Material(Shader.Find("Sprites/Default"));p.mat.mainTexture=smoke;
    Color c=color;c.a=.85f;p.mat.color=c;
    go.AddComponent<MeshFilter>().sharedMesh=Quad();
    var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=p.mat;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;
    go.transform.localScale=Vector3.one*Random.Range(.35f,.6f)*scale;
    go.transform.rotation=Camera.main.transform.rotation;
   }
  }
  void Update(){
   age+=Time.deltaTime;if(age>=life){if(mat)Destroy(mat);Destroy(gameObject);return;}
   transform.position+=vel*Time.deltaTime;vel*=1f-Time.deltaTime*2.2f;
   transform.localScale+=Vector3.one*grow*Time.deltaTime;
   if(Camera.main)transform.rotation=Camera.main.transform.rotation;
   if(mat){var c=mat.color;c.a=.85f*(1f-age/life);mat.color=c;}
  }
 }
}