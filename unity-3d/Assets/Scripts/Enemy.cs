using UnityEngine;
namespace LostRealms {
 public class Enemy:MonoBehaviour {
  public int Kind;public bool Boss;public float Health,MaxHealth,Radius;public string DisplayName;public CharacterVisual Visual;
  Vector3 center,target,attackOrigin;Vector2 area;float timer,burnUntil,freezeUntil,burnTick;int phase=1,attackCount;enum State{Patrol,Notice,Windup,Attack,Recover,Dead}State state;GameObject warning;float baseY;
  Renderer[] skin;Material bodyMat;float flashUntil;Color flashColor;
  // Per-kind tuning so each enemy family reads and plays differently:
  // goblins dart in, demons pressure, frost runners slip wide, the elemental
  // and caster are slower but hit harder / shoot from range.
  static readonly float[] NoticeRange={8.5f,10.5f,8.5f,9f,10.5f,8.5f,7f,13f,12f,12.5f,13f,11f,9f,12f,11f};
  static readonly float[] ChaseSpeed={2.3f,2.05f,2.3f,2.5f,2.05f,2.3f,1.5f,1.15f,2.1f,2.3f,2.5f,2.9f,3.1f,1.2f,2.6f};
  static readonly int[] MeleeDamage={1,2,1,1,2,1,2,1,2,2,2,1,2,1,3};
  // Each family's vulnerable element (0 ember, 1 frost, 2 gale). Matching the
  // player's current element deals bonus damage and triggers that reaction.
  static readonly int[] Weakness={2,1,0,0,1,0,2,2,0,1,0,2,0,1,0};
  public int WeakElement=>Weakness[Mathf.Clamp(Kind,0,14)];
  // Kind 11 (Flyer) hovers: it ignores the ground clamp and bobs in the air.
  bool aerial;
  public void Configure(int kind,bool boss,Vector3 anchor,Vector2 island){Kind=kind;Boss=boss;center=anchor;area=island;baseY=transform.position.y;Radius=boss?1.1f:kind==14?.7f:.5f;MaxHealth=(boss?24+RealmGame.I.Realm*8:3+RealmGame.I.Level*.3f)*(kind==14?2f:1f);Health=MaxHealth;
   string[] roles={"Goblin","Demon","Goblin","Frost","Demon","Goblin","Elemental","Caster","Heartwood","Sunscar","Whiteout","Flyer","Bomber","Summoner","Elite"};string role=roles[Mathf.Clamp(kind,0,14)];DisplayName=boss?new[]{"HEARTWOOD COLOSSUS","SUNSCAR TITAN","WHITEOUT GUARDIAN"}[RealmGame.I.Realm]:role;
   Visual=CharacterVisual.Create(role,transform,boss?3.6f:kind==6?2.5f:kind==14?2.2f:1.65f,RealmGame.I.Accent);RealmGame.I.Enemies.Add(this);state=State.Patrol;timer=.5f;target=center;
    aerial=kind==11;if(aerial){transform.position+=Vector3.up*2.3f;baseY=transform.position.y;}
    skin=Visual?Visual.GetComponentsInChildren<Renderer>(true):null;
    if(skin!=null&&skin.Length>0)bodyMat=skin[0].material;
    gameObject.AddComponent<EnemyAura>().Setup(Weakness[Mathf.Clamp(kind,0,14)],boss);
    if(kind>=11&&Visual&&!Visual.animator)gameObject.AddComponent<EnemyIdleMotion>().Setup(0f);
    if(aerial)return;
    // Block Aster: a real (non-trigger) capsule so the CharacterController
    // cannot walk through the enemy. Damage stays manual (no contact damage).
    var body=gameObject.AddComponent<CapsuleCollider>();
    body.height=boss?3.6f:kind==6?2.5f:kind==14?2.2f:1.65f;body.radius=boss?1.15f:kind==14?.75f:.55f;body.center=Vector3.up*(body.height*.5f);
   }
  void Update(){var game=RealmGame.I;if(game.Screen!=GameScreen.Playing)return;if(state==State.Dead)return;float dt=Time.deltaTime;if(RealmGame.I.Elapsed<burnUntil&&RealmGame.I.Elapsed>=burnTick){burnTick=RealmGame.I.Elapsed+.7f;ApplyDamage(.5f);if(Health<=0)return;}   if(RealmGame.I.Elapsed<freezeUntil){Visual.Play("idle");return;}
   if(aerial)transform.position=new Vector3(transform.position.x,baseY+Mathf.Sin(RealmGame.I.Elapsed*2.4f+Kind)*.25f,transform.position.z);
    if(bodyMat!=null&&RealmGame.I.Elapsed>=flashUntil)bodyMat.SetColor("_EmissionColor",Color.black);
   if(Boss){int next=Health<MaxHealth*.33f?3:Health<MaxHealth*.67f?2:1;if(next>phase){phase=next;game.Tell(DisplayName+" / PHASE "+phase);HitSpark.Burst(transform.position+Vector3.up*1.5f,Vector3.up,game.Accent,20);}}
   timer-=dt;Vector3 delta=game.Player.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;bool sameHeight=Mathf.Abs(game.Player.transform.position.y-baseY)<3;
   switch(state){
    case State.Patrol:
     Visual.Play("walk");if(timer<=0){float x=Mathf.Sin(RealmGame.I.Elapsed*.7f)*area.x*.24f,z=Mathf.Cos(RealmGame.I.Elapsed*.5f)*area.y*.22f;target=center+new Vector3(x,.03f,z);timer=2;}
      MoveTo(target,.7f,dt);if(distance<(Boss?17:NoticeRange[Mathf.Clamp(Kind,0,14)])&&sameHeight){state=State.Notice;timer=.4f;game.Sound("enemy_warning");}break;
    case State.Notice:
      Visual.Play("walk");Face(game.Player.transform.position,dt);if(distance>(Boss?4:2.1f)&&Kind!=7&&Kind!=13)MoveTo(game.Player.transform.position,Boss?1.8f+phase*.2f:ChaseSpeed[Mathf.Clamp(Kind,0,14)],dt);
      if(timer<=0&&(distance<(Boss?8:(Kind==7||Kind==13)?10:2.8f))){state=State.Windup;timer=Boss?1.05f-.1f*phase:(Kind==13?.9f:.7f);target=game.Player.transform.position;target.y=baseY;attackOrigin=transform.position;attackCount++;
      warning=Art.Ring(Boss?target:transform.position+transform.forward*1.2f,Boss?2.2f+.3f*phase:1.15f,new Color(1,.18f,.13f),game.World.transform);Visual.Restart("attack");}
     if(distance>22){state=State.Patrol;timer=0;}break;
    case State.Windup:
     Visual.Play("attack");Glow(new Color(1,.2f,.13f),.51f);if(warning)warning.transform.localScale=Vector3.one*(1+Mathf.Sin(RealmGame.I.Elapsed*18)*.045f);if(timer<=0){ClearGlow();if(warning)Destroy(warning);state=State.Attack;timer=Boss?.45f:.25f;CommitAttack();}break;
    case State.Attack:
     if(timer<=0){state=State.Recover;timer=Boss?1.65f-.18f*phase:Kind==13?2.4f:1.1f;}break;
    case State.Recover:
     Visual.Play("idle");if(timer<=0){state=State.Notice;timer=.15f;}break;
   }
  }
  void CommitAttack(){var g=RealmGame.I;g.Sound(Boss?"boss":"enemy_dash");
   if(Boss){int attack=(attackCount+Kind)%3;
    if(attack==0){StrikeZone.Create(target,2.2f+phase*.3f,2,.05f);if(phase>=2)StrikeZone.Create(target+Vector3.right*3.5f,1.5f,1,.8f);if(phase==3)StrikeZone.Create(target-Vector3.right*3.5f,1.5f,1,1.2f);}
    else if(attack==1){for(int i=0;i<phase+1;i++){float angle=(i-phase*.5f)*13;Vector3 dir=Quaternion.Euler(0,angle,0)*(target-transform.position).normalized;EnemyBolt.Create(transform.position+Vector3.up*1.2f,dir,5.5f,2);}}
    else{Vector3 direction=(target-transform.position).normalized;Vector3 destination=Clamp(transform.position+direction*5);StrikeZone.Create(destination,2,2,.25f);transform.position=destination;}
   }
   else if(Kind==12){StrikeZone.Create(transform.position,2.7f,2,.05f);HitSpark.Burst(transform.position+Vector3.up*.6f,Vector3.up,new Color(1,.45f,.15f),26);ApplyDamage(Health);}
   else if(Kind==13){int alive=0;foreach(var e in g.Enemies)if(e&&!e.Boss&&e.Health>0)alive++;if(alive<16){var minion=new GameObject("Summoned minion");minion.transform.SetParent(transform.parent);minion.transform.position=transform.position+transform.forward*1.6f;minion.AddComponent<Enemy>().Configure(0,false,center,area);HitSpark.Burst(transform.position+Vector3.up*.7f,Vector3.up,new Color(.78f,.45f,1f),16);}}
   else if(Kind==7||Kind==1)EnemyBolt.Create(transform.position+Vector3.up*.9f,(g.Player.transform.position+Vector3.up*.8f-transform.position-Vector3.up*.9f).normalized,6,1);
   else{int dmg=MeleeDamage[Mathf.Clamp(Kind,0,14)];Vector3 d=g.Player.transform.position-transform.position;if(d.magnitude<3&&Vector3.Dot(transform.forward,d.normalized)>.05f){if(g.Player.Damage(dmg,transform.position))DamageTip.Show(g.Player.transform.position+Vector3.up*1.7f,"-"+dmg,new Color(1,.3f,.24f));}HitSpark.Burst(transform.position+transform.forward,transform.forward,new Color(1,.35f,.18f),8);}
   }
   void Face(Vector3 p,float dt){Vector3 d=p-transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),dt*8);}
  void MoveTo(Vector3 p,float speed,float dt){Face(p,dt);Vector3 goal=Clamp(p);var player=RealmGame.I.Player;if(player){Vector3 d=goal-player.transform.position;d.y=0;float minDist=Mathf.Max(Radius,.5f)+.34f;if(d.sqrMagnitude<minDist*minDist){d=d.sqrMagnitude>.0001f?d.normalized*minDist:Vector3.right*minDist;goal=player.transform.position+d;goal.y=baseY;goal=Clamp(goal);}}transform.position=Vector3.MoveTowards(transform.position,goal,speed*dt);}
  Vector3 Clamp(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,center.x-area.x*.5f+Radius+.2f,center.x+area.x*.5f-Radius-.2f),baseY,Mathf.Clamp(p.z,center.z-area.y*.5f+Radius+.2f,center.z+area.y*.5f-Radius-.2f));
  public void Stun(float seconds){
   if(Health<=0)return;
   state=State.Recover;timer=Mathf.Max(timer,seconds);ClearGlow();
   if(warning){Destroy(warning);warning=null;}
   Visual.Play("idle");
  }
  public void Hit(float damage,int power,bool elemental,Vector3 knockDir=default){
   if(Health<=0)return;
   bool weak=power>=0&&power<3&&Weakness[Mathf.Clamp(Kind,0,14)]==power;
   if(weak)elemental=true;
   if(Kind==5&&!elemental&&state!=State.Recover&&Vector3.Dot(transform.forward,(RealmGame.I.Player.transform.position-transform.position).normalized)>.5f){
    RealmGame.I.Tell("Shielded! Strike from behind or use a power.",1);
    HitSpark.Burst(transform.position+Vector3.up*.9f,-transform.forward,new Color(.9f,.9f,1f),8);
    return;
   }
   if(elemental){
    if(power==0){burnUntil=RealmGame.I.Elapsed+3;burnTick=RealmGame.I.Elapsed+.7f;}
    if(power==1)freezeUntil=RealmGame.I.Elapsed+(Boss?.6f:2.2f);
    if(power==2)transform.position=Clamp(transform.position+(transform.position-RealmGame.I.Player.transform.position).normalized*(Boss?1:2.5f));
   }
   // Physical knockback impulse
   Vector3 k=knockDir==default?(transform.position-RealmGame.I.Player.transform.position).normalized:knockDir.normalized;
   transform.position=Clamp(transform.position+k*(Boss?.35f:.75f));

   float real=damage*(Boss&&state==State.Recover?1.35f:1)*(weak?1.5f:1);
   ApplyDamage(real);
   RealmGame.I.Sound("impact");
   RealmGame.I.CameraRig.Shake=Boss?.22f:.12f;

   Color hitCol=power==0?new Color(1f,.45f,.1f):power==1?new Color(.2f,.85f,1f):new Color(.2f,1f,.55f);
   HitSpark.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),k,hitCol,Boss?16:10);
   if(weak){DamageTip.Show(transform.position+Vector3.up*(Boss?2.35f:1.3f),"WEAK  "+Mathf.Max(1,Mathf.RoundToInt(real)),hitCol);HitSpark.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),k,Color.white,10);}
   else DamageTip.Show(transform.position+Vector3.up*(Boss?2.2f:1.15f),"-"+Mathf.Max(1,Mathf.RoundToInt(real)),hitCol);
  }
  void Glow(Color color,float duration){flashColor=color;flashUntil=RealmGame.I.Elapsed+duration;if(bodyMat){bodyMat.EnableKeyword("_EMISSION");bodyMat.SetColor("_EmissionColor",color*1.6f);}}
  void ClearGlow(){if(bodyMat)bodyMat.SetColor("_EmissionColor",Color.black);}
  void ApplyDamage(float amount){
   Health=Mathf.Max(0,Health-amount);
   if(Health<=0){
    state=State.Dead;ClearGlow();if(warning)Destroy(warning);Visual.Restart("death");
    var collider=GetComponent<Collider>();if(collider)collider.enabled=false;
    RealmGame.I.Coins+=Boss?20:3;RealmGame.I.Sound("enemy_defeat");
    AshPuff.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),new Color(.36f,.3f,.27f),Boss?26:14,Boss?1.6f:1f,Boss);
    HitSpark.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),Vector3.up,RealmGame.I.Accent,Boss?28:16);
    if(Boss)RealmGame.I.Tell("Guardian restored. The realm gate is open.",5);
    gameObject.AddComponent<EnemyDeathFall>().Setup(Boss?1.35f:1.05f);
    Destroy(gameObject,2.5f);
   }
  }
  void OnDestroy(){if(RealmGame.I)RealmGame.I.Enemies.Remove(this);}
 }
 public class EnemyBolt:MonoBehaviour {
  Vector3 direction;float speed,life=5;int damage;
  public static void Create(Vector3 p,Vector3 dir,float speed,int damage){
   var go=Art.Shape("Enemy spell",PrimitiveType.Sphere,p,Vector3.one*.38f,new Color(1,.3f,.13f),RealmGame.I.World.transform);
   var bolt=go.AddComponent<EnemyBolt>();bolt.direction=dir;bolt.speed=speed;bolt.damage=damage;
}
  void Update(){
    var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;float step=speed*Time.deltaTime;
    if(Physics.Raycast(transform.position,direction,out var hit,step,~0,QueryTriggerInteraction.Ignore)){
      if(hit.collider.GetComponent<Hero>()){if(g.Player.Damage(damage,transform.position))DamageTip.Show(g.Player.transform.position+Vector3.up*1.7f,"-"+damage,new Color(1,.3f,.2f));}
     HitSpark.Burst(transform.position,-direction,new Color(1f,.4f,.15f),8);
     Destroy(gameObject);return;
    }
    transform.position+=direction*step;life-=Time.deltaTime;
     if(Vector3.Distance(transform.position,g.Player.transform.position+Vector3.up*.8f)<.7f){
      if(g.Player.Damage(damage,transform.position))DamageTip.Show(g.Player.transform.position+Vector3.up*1.7f,"-"+damage,new Color(1,.3f,.2f));
     HitSpark.Burst(transform.position,-direction,new Color(1f,.4f,.15f),8);
     Destroy(gameObject);
    }else if(life<=0)Destroy(gameObject);
   }
  }
  public class StrikeZone:MonoBehaviour {
   float radius,delay,age;int damage;GameObject ring;
   public static void Create(Vector3 p,float radius,int damage,float delay){
    var go=new GameObject("Telegraphed strike");go.transform.SetParent(RealmGame.I.World.transform);go.transform.position=p;
    var z=go.AddComponent<StrikeZone>();z.radius=radius;z.damage=damage;z.delay=delay;
    z.ring=Art.Ring(Vector3.up*.08f,radius,new Color(1,.15f,.1f),go.transform);
   }
   void Update(){
    var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;age+=Time.deltaTime;if(age<delay)return;
    Vector3 d=g.Player.transform.position-transform.position;
    if(new Vector2(d.x,d.z).magnitude<radius&&Mathf.Abs(d.y)<1.2f){if(g.Player.Damage(damage,transform.position))DamageTip.Show(g.Player.transform.position+Vector3.up*1.7f,"-"+damage,new Color(1,.3f,.2f));}
    HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.up,new Color(1,.3f,.15f),18);
    Destroy(gameObject);
   }
  }
  public class DamageTip:MonoBehaviour {
   float age,life=.85f;Color color;TextMesh mesh;
   public static void Show(Vector3 pos,string text,Color color){
    var go=new GameObject("DamageTip");go.transform.SetParent(RealmGame.I.World.transform,false);go.transform.position=pos;
    go.transform.localScale=Vector3.one*.06f;
    var tip=go.AddComponent<DamageTip>();tip.color=color;
    tip.mesh=go.AddComponent<TextMesh>();
    tip.mesh.text=text;tip.mesh.anchor=TextAnchor.MiddleCenter;tip.mesh.alignment=TextAlignment.Center;tip.mesh.fontSize=24;
    tip.mesh.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    tip.mesh.color=color;
    var renderer=go.GetComponent<MeshRenderer>();
    if(renderer){var m=new Material(Shader.Find("Sprites/Default"));m.color=color;renderer.sharedMaterial=m;}
   }
   void Update(){
    var g=RealmGame.I;if(g&&g.Screen!=GameScreen.Playing)return;
    age+=Time.deltaTime;transform.position+=Vector3.up*Time.deltaTime*1.15f;
    float a=Mathf.Clamp01(1f-age/life);
    if(mesh)mesh.color=new Color(color.r,color.g,color.b,a);
    if(age>=life)Destroy(gameObject);
   }
  }
 public class AshPuff:MonoBehaviour {
  Vector3 velocity;float age,duration;Material mat;
  public static void Burst(Vector3 pos,Color color,int count,float spread,bool boss){
   for(int i=0;i<count;i++){
    var go=GameObject.CreatePrimitive(PrimitiveType.Cube);var c=go.GetComponent<Collider>();if(c)Destroy(c);
    go.transform.position=pos;float s=boss?.09f:.055f;go.transform.localScale=new Vector3(s,s,s);go.transform.rotation=Random.rotation;
    var ash=go.AddComponent<AshPuff>();
    ash.velocity=(Vector3.up*Random.Range(.4f,1.5f)+Random.insideUnitSphere*spread).normalized*Random.Range(1.1f,3.2f);
    ash.duration=Random.Range(.6f,1.1f);
    ash.mat=new Material(Shader.Find("Sprites/Default"));ash.mat.color=color;
    var renderer=go.GetComponent<Renderer>();renderer.sharedMaterial=ash.mat;
   }
  }
  void Update(){
   age+=Time.deltaTime;velocity+=Vector3.down*6.5f*Time.deltaTime;
   transform.position+=velocity*Time.deltaTime;transform.Rotate(140f*Time.deltaTime,90f*Time.deltaTime,0);
   if(mat){var c=mat.color;c.a=Mathf.Clamp01(1f-age/duration);mat.color=c;}
   if(age>=duration){if(mat)Destroy(mat);Destroy(gameObject);}
  }
 }
 // Death: the dying animation plays (rigged enemies fall with their death
 // clip, static meshes tip over), then the body bursts to ashes and vanishes
 // in place. It never moves down through the platform.
 public class EnemyDeathFall:MonoBehaviour {
  float age,ashAt;bool ashed,tip;Transform visual;Quaternion rest;
  public void Setup(float wait){ashAt=wait;}
  void Start(){
   var e=GetComponent<Enemy>();
   if(e&&e.Visual){visual=e.Visual.transform;rest=visual.localRotation;tip=!e.Visual.animator;}
   if(!tip)ashAt+=.7f;
   var idle=GetComponent<EnemyIdleMotion>();if(idle)idle.enabled=false;
  }
  void Update(){
   var g=RealmGame.I;if(g&&g.Screen==GameScreen.Playing)age+=Time.deltaTime;
   if(!ashed){
    if(tip&&visual){float t=Mathf.Clamp01(age/.5f);visual.localRotation=rest*Quaternion.Euler(-80f*t*t,0f,0f);}
    if(age>=ashAt){ashed=true;AshPuff.Burst(transform.position+Vector3.up*.7f,new Color(.4f,.34f,.3f),34,1.5f,false);HitSpark.Burst(transform.position+Vector3.up*.7f,Vector3.up,RealmGame.I.Accent,12);foreach(var r in GetComponentsInChildren<Renderer>(true))r.enabled=false;}
   }else if(age>=ashAt+1f)Destroy(gameObject);
  }
 }
 // Three orbiting motes tinted to the family's weakness element: a readable,
 // cheap in-world cue that matches the HUD health-bar color.
 public class EnemyAura:MonoBehaviour {
  int element;float radius;Transform[] motes;Material mat;
  public void Setup(int elem,bool boss){
   element=Mathf.Clamp(elem,0,2);radius=boss?1.9f:.72f;
   mat=new Material(Shader.Find("Sprites/Default"));mat.color=RealmGame.ElementColors[element];
   motes=new Transform[3];
   for(int i=0;i<3;i++){
    var m=GameObject.CreatePrimitive(PrimitiveType.Cube);var c=m.GetComponent<Collider>();if(c)Destroy(c);
    m.transform.SetParent(transform,false);m.transform.localScale=Vector3.one*(boss?.16f:.075f);
    var renderer=m.GetComponent<Renderer>();renderer.sharedMaterial=mat;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
    motes[i]=m.transform;
   }
  }
  void OnDestroy(){if(mat)Destroy(mat);}
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing)return;
   float t=g.Elapsed*(element==1?.65f:element==2?1.7f:1.1f);
   for(int i=0;i<motes.Length;i++){if(!motes[i])continue;float a=t+i*2.0944f;motes[i].localPosition=new Vector3(Mathf.Cos(a)*radius,1f+Mathf.Sin(a*1.7f)*.34f,Mathf.Sin(a)*radius);motes[i].localRotation=Quaternion.Euler(t*80f+i*40f,t*120f,0);}
  }
 }
 // The new families are static showcase meshes (no skeleton), so the visual gets
 // a cheap procedural idle: a breathing bob and a slow sway around its facing.
 public class EnemyIdleMotion:MonoBehaviour {
  Transform visual;float phase,facing;
  public void Setup(float yaw){facing=yaw;}
  void Start(){var e=GetComponent<Enemy>();visual=e&&e.Visual?e.Visual.transform:null;phase=Random.value*6.283f;}
  void Update(){
   if(!visual)return;var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing)return;
   float t=g.Elapsed+phase;
   visual.localPosition=new Vector3(0,Mathf.Sin(t*1.6f)*.035f,0);
   visual.localRotation=Quaternion.Euler(Mathf.Sin(t*.9f)*1.5f,facing+Mathf.Sin(t*.6f)*6f,0);
  }
 }
 }
