using UnityEngine;
namespace LostRealms {
 public sealed class StaffShot:MonoBehaviour {
  Vector3 direction;float damage,distance;int element;Material material;
  public int Element=>element;
  WeaponId weapon;bool Slash=>WeaponCatalog.ChargedSlash(weapon);
  public WeaponId Weapon=>weapon;
  public static StaffShot Fire(Vector3 origin,Vector3 forward,float damage,int element,WeaponId weapon=WeaponId.AstersBlade){
   var g=RealmGame.I;if(!g||!g.World)return null;
   var go=new GameObject(element==0?"Staff fireball":element==1?"Staff frost bolt":"Staff thunder bolt");go.transform.SetParent(g.World.transform,false);go.transform.position=origin;
   var shot=go.AddComponent<StaffShot>();shot.direction=forward.normalized;shot.damage=damage;shot.element=element;shot.weapon=weapon;
   if(shot.Slash||weapon==WeaponId.WardenPike){
    go.name=weapon==WeaponId.OrnateCurvedBlade?"Gale crosscut":weapon==WeaponId.VoidReaper?"Void Reaper crescent":weapon==WeaponId.PureScythe?"Reaper crescent":weapon==WeaponId.BrassFangs?"Fang claw slash":"Warden storm lance";
    shot.material=ChargedWeaponArt.Build(go.transform,forward,weapon);
    g.Sound(shot.Slash?"sword_slash":"gale_cast");return shot;
   }
   Color color=element==0?new Color(1f,.3f,.04f):new Color(.45f,.65f,1f);
   var core=GameObject.CreatePrimitive(PrimitiveType.Sphere);core.name="Magic core";core.transform.SetParent(go.transform,false);core.transform.localScale=Vector3.one*(element==0?.24f:.13f);
   var collider=core.GetComponent<Collider>();collider.enabled=false;Destroy(collider);
   shot.material=new Material(Shader.Find("Sprites/Default"));shot.material.color=color;
   var renderer=core.GetComponent<Renderer>();renderer.sharedMaterial=shot.material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   var trail=go.AddComponent<TrailRenderer>();trail.sharedMaterial=shot.material;trail.time=element==0?.18f:.09f;trail.startWidth=element==0?.22f:.12f;trail.endWidth=0;trail.minVertexDistance=.08f;
   Vfx.Play(element==0?"eric_FX_Fireball":"ga_vfx_Electricity_01",origin,Quaternion.LookRotation(forward),.35f,go.transform);
   g.Sound(element==0?"ember_cast":element==1?"frost_cast":"gale_cast");return shot;
  }
  void Update(){
   var g=RealmGame.I;if(!g||!g.Player){Destroy(gameObject);return;}if(g.Screen!=GameScreen.Playing)return;
   Vector3 from=transform.position;float range=Slash?12f:16f;float step=Mathf.Min((Slash?15f:element==0?13f:23f)*Time.deltaTime,range-distance);Vector3 to=from+direction*step;
   float stop=step;bool blocked=false;
   foreach(var hit in Physics.RaycastAll(from,direction,step,~0,QueryTriggerInteraction.Ignore)){
    if(hit.collider.GetComponentInParent<Hero>()||hit.collider.GetComponentInParent<Enemy>())continue;
    if(hit.distance<stop){stop=hit.distance;blocked=true;}
   }
   to=from+direction*stop;Enemy target=null;float nearest=2f;
   foreach(var enemy in g.Enemies){
    if(!enemy||enemy.Health<=0)continue;
    if(ProjectileSweep.Hits(from,to,enemy.transform.position+Vector3.up*.85f,enemy.Radius+(weapon==WeaponId.PureScythe||weapon==WeaponId.OrnateCurvedBlade||weapon==WeaponId.VoidReaper?.65f:.18f),out float fraction)&&fraction<nearest&&Clear(from,enemy.transform.position+Vector3.up*.85f)){
     nearest=fraction;target=enemy;
    }
   }
   transform.position=target?Vector3.Lerp(from,to,nearest):to;distance+=step;
   if(target){Impact(target);return;}if(blocked){Impact(null);return;}if(distance>=range)Destroy(gameObject);
  }
  static bool Clear(Vector3 from,Vector3 to){
   foreach(var hit in Physics.RaycastAll(from,(to-from).normalized,Vector3.Distance(from,to),~0,QueryTriggerInteraction.Ignore))
    if(!hit.collider.GetComponentInParent<Hero>()&&!hit.collider.GetComponentInParent<Enemy>())return false;
   return true;
  }
  void Impact(Enemy target){
   var g=RealmGame.I;Vector3 point=transform.position;
   bool gale=weapon==WeaponId.OrnateCurvedBlade;
   Vfx.Play(gale?"ga_vfx_Hyperdrive_01":Slash?"eric_FX_Purple_Hit_02":weapon==WeaponId.WardenPike?"ga_vfx_Lightning_02":element==0?"ga_vfx_Explosion_02":"ga_vfx_Electricity_01",point,Quaternion.identity,Slash?.4f:element==0?.65f:.55f);
   if(target)target.Hit(damage,gale?2:Slash?-1:element,gale||!Slash,direction);
   if(target&&!Slash){
    Enemy arc=null;float closest=3.5f;
    foreach(var other in g.Enemies.ToArray()){
     if(!other||other==target||other.Health<=0)continue;
     Vector3 center=other.transform.position+Vector3.up*.85f;float d=Vector3.Distance(center,point);
     if(!Clear(point,center))continue;
     if(element==0&&d<1.6f)other.Hit(damage*.5f,0,true,direction);
     else if(element==2&&d<closest){closest=d;arc=other;}
    }
    if(arc){
     Vector3 end=arc.transform.position+Vector3.up*.85f;
     arc.Hit(damage*.55f,2,true,(end-point).normalized);
     var link=new GameObject("Thunder arc");link.transform.SetParent(g.World.transform,false);
     var line=link.AddComponent<LineRenderer>();line.sharedMaterial=material;line.positionCount=7;line.startWidth=.08f;line.endWidth=.025f;
     for(int i=0;i<7;i++)line.SetPosition(i,Vector3.Lerp(point,end,i/6f)+(i==0||i==6?Vector3.zero:Vector3.up*(i%2==0?.16f:-.16f)));
     // Transfer material ownership so the short-lived arc stays visible.
     link.AddComponent<StaffArcCleanup>().Material=material;material=null;Destroy(link,.18f);
    }
   }
   Destroy(gameObject);
  }
  void OnDestroy(){if(material)Destroy(material);}
 }
 public sealed class StaffArcCleanup:MonoBehaviour {public Material Material;void OnDestroy(){if(Material)Destroy(Material);}}
}
