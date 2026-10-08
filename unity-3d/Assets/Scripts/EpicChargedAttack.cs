using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public sealed class EpicChargedAttack:MonoBehaviour {
  public WeaponId Weapon {get;private set;}
  Vector3 direction;float damage,age,travel,pulse;Material material;
  readonly HashSet<Enemy> struck=new HashSet<Enemy>();
  readonly List<LineRenderer> ribbons=new List<LineRenderer>();
  public static EpicChargedAttack Fire(Vector3 origin,Vector3 forward,float damage,WeaponId weapon){
   var g=RealmGame.I;if(!g||!g.World)return null;
   var go=new GameObject(weapon==WeaponId.Duskblade?"Duskblade star lance":"Soulreaper soul vortex");go.transform.SetParent(g.World.transform,false);
   var effect=go.AddComponent<EpicChargedAttack>();effect.Weapon=weapon;effect.damage=damage;effect.direction=forward.normalized;
   float placement=weapon==WeaponId.Soulreaper?3.5f:0;
   foreach(var hit in Physics.RaycastAll(origin,effect.direction,placement,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.GetComponentInParent<Hero>()&&!hit.collider.GetComponentInParent<Enemy>())placement=Mathf.Min(placement,Mathf.Max(0,hit.distance-.3f));
   go.transform.position=origin+effect.direction*placement;
   go.transform.rotation=Quaternion.LookRotation(effect.direction);
   effect.material=new Material(Shader.Find("Sprites/Default")){name="Epic charge light"};
   for(int i=0;i<(weapon==WeaponId.Duskblade?3:4);i++){
    var line=new GameObject("Epic light filament "+i).AddComponent<LineRenderer>();line.transform.SetParent(go.transform,false);line.useWorldSpace=false;line.sharedMaterial=effect.material;line.positionCount=weapon==WeaponId.Duskblade?5:49;
    line.widthCurve=new AnimationCurve(new Keyframe(0,0),new Keyframe(.5f,i==0?.12f:.045f),new Keyframe(1,0));
    line.startColor=line.endColor=weapon==WeaponId.Duskblade?(i==0?new Color(1,1,.8f):new Color(1,.65f,.12f)):(i%2==0?new Color(.3f,1f,.85f):new Color(.8f,.25f,1f));effect.ribbons.Add(line);
   }
   effect.Draw();g.Sound(weapon==WeaponId.Duskblade?"gale_cast":"frost_cast");return effect;
  }
  void Draw(){
   for(int k=0;k<ribbons.Count;k++){
    var line=ribbons[k];
    for(int i=0;i<line.positionCount;i++){
     if(Weapon==WeaponId.Duskblade){float z=(i-2)*.55f;float width=i==2?.24f:0;line.SetPosition(i,new Vector3(k==1?width:0,k==2?width:0,z));}
     else{float t=i/48f,a=t*Mathf.PI*4+age*(k%2==0?4:-4)+k*Mathf.PI*.5f;float radius=1.65f*Mathf.Sin(t*Mathf.PI);line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,(t-.5f)*1.8f,Mathf.Sin(a)*radius));}
    }
   }
  }
  void Update(){
   var g=RealmGame.I;if(!g||!g.Player){Destroy(gameObject);return;}if(g.Screen!=GameScreen.Playing)return;age+=Time.deltaTime;Draw();
   if(Weapon==WeaponId.Duskblade){
    Vector3 from=transform.position;float step=Mathf.Min(20f*Time.deltaTime,16f-travel);bool blocked=false;
    foreach(var hit in Physics.RaycastAll(from,direction,step,~0,QueryTriggerInteraction.Ignore))if(!hit.collider.GetComponentInParent<Hero>()&&!hit.collider.GetComponentInParent<Enemy>()&&hit.distance<=step){step=hit.distance;blocked=true;}
    Vector3 to=from+direction*step;
    foreach(var enemy in g.Enemies.ToArray())if(enemy&&enemy.Health>0&&!struck.Contains(enemy)&&ProjectileSweep.Hits(from,to,enemy.transform.position+Vector3.up*.9f,enemy.Radius+.3f,out _)){
     struck.Add(enemy);enemy.Hit(damage,-1,false,direction);Vfx.Play("ga_vfx_Electricity_01",enemy.transform.position+Vector3.up,Quaternion.identity,.3f);
    }
    transform.position=to;travel+=step;if(blocked||travel>=16f)Destroy(gameObject);
   }else{
    if(age>=pulse){pulse=age+.4f;
     foreach(var enemy in g.Enemies.ToArray())if(enemy&&enemy.Health>0){Vector3 delta=transform.position-(enemy.transform.position+Vector3.up*1.05f);if(delta.magnitude>2.4f||!Clear(enemy.transform.position+Vector3.up*1.05f))continue;
      enemy.Hit(damage*.22f,-1,false,delta.normalized);if(!enemy.Boss)enemy.PushBack(delta,.35f,.3f);
     }
    }
    if(age>=2.1f)Destroy(gameObject);
   }
  }
  bool Clear(Vector3 point){foreach(var hit in Physics.RaycastAll(transform.position,(point-transform.position).normalized,Vector3.Distance(point,transform.position),~0,QueryTriggerInteraction.Ignore))if(!hit.collider.GetComponentInParent<Hero>()&&!hit.collider.GetComponentInParent<Enemy>())return false;return true;}
  void OnDestroy(){if(material)Destroy(material);}
 }
}
