using UnityEngine;
namespace LostRealms {
 // Third trap generation (Sep 2026 content pass): the four user-generated Tripo
 // models processed in Blender (decimated, grounded, textures exported as
 // Props/Traps/Textures/<name>_0 basecolor / _1 normal and bound explicitly).
 // The three elemental traps are projectile shooters themed to the realm pool:
 // brazier lobs fireballs, totem fires freezing frost shards, serpent spits
 // green poison at Aster. All follow the TrapsAndHazards conventions and skip
 // boss damage. TrapBolt is the shared projectile: emissive shard + Kenney
 // flare billboard + tinted puff trail, impact VFX key per element.
 public sealed class TrapBolt:MonoBehaviour {
  Vector3 vel;float damage,age,life,playerRadius,enemyRadius;int power;Color trail;string impactKey;Transform flare;bool froze;
  public static TrapBolt Fire(Vector3 pos,Vector3 velocity,float damage,int power,Color color,string impactKey,float scale=1f,float boltLife=3f){
   var go=new GameObject("Trap bolt");
   go.transform.SetParent(RealmGame.I?RealmGame.I.World.transform:null,false);
   go.transform.position=pos;
   var core=go.AddComponent<MeshFilter>();core.sharedMesh=Resources.GetBuiltinResource<Mesh>("Cube.fbx")?Resources.GetBuiltinResource<Mesh>("Cube.fbx"):null;
   if(!core.sharedMesh){var prim=GameObject.CreatePrimitive(PrimitiveType.Cube);core.sharedMesh=prim.GetComponent<MeshFilter>().sharedMesh;Object.Destroy(prim);}
   go.transform.localScale=Vector3.one*.16f*scale;
   var mr=go.AddComponent<MeshRenderer>();
   var m=new Material(Shader.Find("Standard")){name="Trap bolt core"};
   m.color=color;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.6f);
   mr.sharedMaterial=m;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   // billboard flare child on the Kenney flare sprite
   var flareGo=new GameObject("flare");flareGo.transform.SetParent(go.transform,false);
   var fq=flareGo.AddComponent<MeshFilter>();
   var quad=new Mesh{vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)},triangles=new[]{0,1,2,0,2,3},uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)}};
   quad.RecalculateNormals();quad.RecalculateBounds();fq.sharedMesh=quad;
   var fr=flareGo.AddComponent<MeshRenderer>();
   var fm=new Material(Shader.Find("Sprites/Default"));fm.mainTexture=Resources.Load<Texture2D>("VFX/Textures/flare_01");fm.color=new Color(color.r,color.g,color.b,.9f);
   fr.sharedMaterial=fm;fr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   flareGo.transform.localScale=Vector3.one*2.6f*scale;
   var b=go.AddComponent<TrapBolt>();
   b.vel=velocity;b.damage=damage;b.power=power;b.trail=color;b.impactKey=impactKey;b.life=boltLife;b.flare=flareGo.transform;
   return b;
  }
  void Update(){
   var g=RealmGame.I;
   if(!g||g.Screen!=GameScreen.Playing){Destroy(gameObject);return;}
   age+=Time.deltaTime;
   if(age>life){Impact(false);return;}
   vel.y-=4f*Time.deltaTime;                       // slight droop, keeps lobs arcing
   transform.position+=vel*Time.deltaTime;
   transform.rotation=Quaternion.Euler(age*320f,age*410f,0);
   if(flare&&Camera.main)flare.rotation=Camera.main.transform.rotation;
   if(Random.value<.35f)KenneyPuff.Burst(transform.position,trail,1,.4f);
   var p=g.Player;
   if(p&&Vector3.Distance(transform.position,p.transform.position+Vector3.up*.9f)<.6f){
    if(p.Damage(Mathf.Max(1,Mathf.RoundToInt(damage)),transform.position)){
     HitSpark.Burst(transform.position,-vel.normalized,new Color(1f,.3f,.2f),8);
     if(power==1)p.ApplyForce(vel.normalized*-2f);  // frost shards stagger slightly
    }
    Impact(true);return;
   }
   // Player-only: stray bolts never kill enemies the player never fought â€”
   // that read as creatures "dying by themselves" when you walked past.
   if(Physics.Raycast(transform.position,vel.normalized,vel.magnitude*Time.deltaTime+.25f,~0,QueryTriggerInteraction.Ignore))Impact(true);
  }
  void Impact(bool hit){
   if(hit){Vfx.Play(impactKey,transform.position,Quaternion.identity,.7f);g_Sound();}
   KenneyPuff.Burst(transform.position,trail,7,.8f);
   Destroy(gameObject);
  }
  void g_Sound(){var g=RealmGame.I;if(g)g.Sound("impact");}
 }

 public sealed class FlameBrazier:MonoBehaviour {
  float timer;Renderer bowl;Color accent;
  public static FlameBrazier Place(Transform parent,Vector3 localPos,Color accent,Color stone){
   var go=new GameObject("Flame brazier");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.LoadTextured("Trap_Brazier",go.transform);
   Renderer bowlRenderer;
   if(model){bowlRenderer=model.GetComponentInChildren<Renderer>();}
   else{
    Art.Shape("BrazierBowl",PrimitiveType.Cylinder,new Vector3(0,.55f,0),new Vector3(1.6f,.5f,1.6f),new Color(.2f,.16f,.15f),go.transform);
    bowlRenderer=Art.Shape("BrazierCore",PrimitiveType.Cylinder,new Vector3(0,1f,0),new Vector3(.7f,.3f,.7f),new Color(1f,.4f,.1f),go.transform).GetComponent<Renderer>();
   }
   var b=go.AddComponent<FlameBrazier>();b.bowl=bowlRenderer;b.accent=accent;b.timer=Random.value*2.2f;
   var col=go.AddComponent<CapsuleCollider>();
   col.radius=.85f;col.height=1.2f;col.center=new Vector3(0,.6f,0);
   TrapArt.Reserve(parent,localPos,1.6f);
   return b;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   if(timer<2.6f){
    if(bowl)bowl.material.SetColor("_EmissionColor",accent*(.25f+Mathf.Sin(Time.time*6f)*.12f));
    return;
   }
   timer=0;
   // Lob an arcing fireball at Aster (impact ring shows where it lands).
   Vector3 mouth=transform.position+Vector3.up*1.1f;
   Vector3 target=g.Player.transform.position+Vector3.up*.4f;
   CombatTelegraph.Create(target,1.1f,.9f,g.World.transform);
   float T=.9f,g0=4f;
   Vector3 vel=(target-mouth)/T+Vector3.up*(.5f*g0*T);
   TrapBolt.Fire(mouth,vel,1,0,new Color(1f,.5f,.15f),"ga_vfx_Explosion_02",1.1f,2.5f);
   g.TrapSound("ember_cast",transform.position,4f,18f,1f);
   KenneyPuff.Burst(mouth,new Color(1f,.5f,.12f),6,.9f);
  }
 }

 public sealed class FrostTotem:MonoBehaviour {
  float timer;Renderer[] runes;Color accent;
  public static FrostTotem Place(Transform parent,Vector3 localPos,Color accent,Color stone){
   var go=new GameObject("Frost totem");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.LoadTextured("Trap_Totem",go.transform);
   Renderer[] runeSet;
   if(model){
    var rs=model.GetComponentsInChildren<Renderer>();
    runeSet=rs.Length>0?new[]{rs[0]}:null;
   }else{
    runeSet=new[]{Art.Shape("TotemPillar",PrimitiveType.Cylinder,new Vector3(0,1.2f,0),new Vector3(.9f,2.4f,.9f),new Color(.62f,.78f,.9f),go.transform).GetComponent<Renderer>()};
   }
   var t=go.AddComponent<FrostTotem>();t.runes=runeSet;t.accent=accent;t.timer=Random.value*2f;
   var col=go.AddComponent<BoxCollider>();
   col.size=new Vector3(1.1f,2.4f,1.1f);col.center=new Vector3(0,1.2f,0);
   TrapArt.Reserve(parent,localPos,1.5f);
   return t;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   if(timer<2.4f){
    if(runes!=null)foreach(var r in runes)if(r)r.material.SetColor("_EmissionColor",new Color(.3f,.6f,.9f)*(.2f+Mathf.Sin(Time.time*4f)*.1f));
    return;
   }
   timer=0;
   // Burst of three frost shards; power 1 freezes enemies on hit.
   Vector3 mouth=transform.position+Vector3.up*1.6f;
   Vector3 aim=(g.Player.transform.position+Vector3.up*.9f-mouth).normalized;
   for(int i=0;i<3;i++){
    Vector3 spread=Quaternion.Euler(0,(i-1)*7f,0)*aim;
    TrapBolt.Fire(mouth+spread*.3f,spread*11f,1,1,new Color(.6f,.9f,1.2f),"ga_vfx_Electricity_01",.85f,2.2f);
   }
   g.TrapSound("frost_cast",transform.position,4f,18f,1f);
   KenneyPuff.Burst(mouth,new Color(.85f,.95f,1f),8,.9f);
  }
 }

 public sealed class SerpentStatue:MonoBehaviour {
  float timer;Renderer mouthR;Color accent;Color poison=new Color(.45f,1f,.35f);
  public static SerpentStatue Place(Transform parent,Vector3 localPos,Color accent,Color stone){
   var go=new GameObject("Serpent statue");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.LoadTextured("Trap_Serpent",go.transform);
   if(!model){
    Art.Shape("SerpentPedestal",PrimitiveType.Cube,new Vector3(0,.5f,0),new Vector3(1.5f,1f,1.5f),stone,go.transform);
    Art.Shape("SerpentBody",PrimitiveType.Capsule,new Vector3(0,1.4f,0),new Vector3(.8f,.8f,.8f),new Color(.85f,.72f,.5f),go.transform);
   }
   var s=go.AddComponent<SerpentStatue>();s.accent=accent;s.timer=Random.value*1.8f;
   var col=go.AddComponent<BoxCollider>();
   col.size=new Vector3(1.25f,2.2f,1.25f);col.center=new Vector3(0,1.1f,0);
   TrapArt.Reserve(parent,localPos,1.4f);
   return s;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   // Aim the whole statue (translation-only islands: rotating in world space
   // rides along fine â€” the statue never leaves its island-local spot).
   Vector3 to=g.Player.transform.position+Vector3.up*1.1f-transform.position;to.y=0;
   if(to.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(to.normalized,Vector3.up),Time.deltaTime*2.2f);
   if(timer<2.4f)return;
   timer=0;
   // Spit a green poison bolt from the mouth.
   Vector3 mouth=transform.position+transform.forward*.55f+Vector3.up*1.7f;
   Vector3 aim=(g.Player.transform.position+Vector3.up*.85f-mouth).normalized;
   TrapBolt.Fire(mouth+aim*.3f,aim*12f,1,-1,poison,"ga_vfx_Heal_01",1f,2.4f);
   g.TrapSound("gale_cast",transform.position,4f,16f,.9f);
   HitSpark.Burst(mouth,aim,poison,10);
   CombatTelegraph.Create(g.Player.transform.position,1f,.5f,g.World.transform);
  }
 }

}
