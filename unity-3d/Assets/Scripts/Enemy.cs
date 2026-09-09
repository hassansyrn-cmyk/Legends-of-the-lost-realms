using UnityEngine;
namespace LostRealms {
 public class Enemy:MonoBehaviour {
  public int Kind;public bool Boss;public float Health,MaxHealth,Radius;public string DisplayName;public CharacterVisual Visual;
  Vector3 center,target,attackOrigin;Vector2 area;float timer,burnUntil,freezeUntil,burnTick;int phase=1,attackCount;enum State{Patrol,Notice,Windup,Attack,Recover,Dead}State state;GameObject warning;float baseY;
  public void Configure(int kind,bool boss,Vector3 anchor,Vector2 island){Kind=kind;Boss=boss;center=anchor;area=island;baseY=transform.position.y;Radius=boss?1.1f:.5f;MaxHealth=boss?24+RealmGame.I.Realm*8:3+RealmGame.I.Level*.3f;Health=MaxHealth;
   string[] roles={"Goblin","Demon","Goblin","Frost","Demon","Goblin","Elemental","Caster","Heartwood","Sunscar","Whiteout"};string role=roles[Mathf.Clamp(kind,0,10)];DisplayName=boss?new[]{"HEARTWOOD COLOSSUS","SUNSCAR TITAN","WHITEOUT GUARDIAN"}[RealmGame.I.Realm]:role;
   Visual=CharacterVisual.Create(role,transform,boss?3.6f:kind==6?2.5f:1.65f,RealmGame.I.Accent);RealmGame.I.Enemies.Add(this);state=State.Patrol;timer=.5f;target=center;
  }
  void Update(){var game=RealmGame.I;if(game.Screen!=GameScreen.Playing)return;if(state==State.Dead)return;float dt=Time.deltaTime;if(RealmGame.I.Elapsed<burnUntil&&RealmGame.I.Elapsed>=burnTick){burnTick=RealmGame.I.Elapsed+.7f;ApplyDamage(.5f);if(Health<=0)return;}if(RealmGame.I.Elapsed<freezeUntil){Visual.Play("idle");return;}
   if(Boss){int next=Health<MaxHealth*.33f?3:Health<MaxHealth*.67f?2:1;if(next>phase){phase=next;game.Tell(DisplayName+" / PHASE "+phase);Pulse.Create(transform.position+Vector3.up*.1f,4,game.Accent,.6f);}}
   timer-=dt;Vector3 delta=game.Player.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;bool sameHeight=Mathf.Abs(game.Player.transform.position.y-baseY)<3;
   switch(state){
    case State.Patrol:
     Visual.Play("walk");if(timer<=0){float x=Mathf.Sin(RealmGame.I.Elapsed*.7f)*area.x*.24f,z=Mathf.Cos(RealmGame.I.Elapsed*.5f)*area.y*.22f;target=center+new Vector3(x,.03f,z);timer=2;}
     MoveTo(target,.7f,dt);if(distance<(Boss?17:8)&&sameHeight){state=State.Notice;timer=.4f;game.Sound("enemy_warning");}break;
    case State.Notice:
     Visual.Play("walk");Face(game.Player.transform.position,dt);if(distance>(Boss?4:2.1f)&&Kind!=7)MoveTo(game.Player.transform.position,Boss?1.8f+phase*.2f:1.8f+(Kind%3)*.35f,dt);
     if(timer<=0&&(distance<(Boss?8:Kind==7?10:2.8f))){state=State.Windup;timer=Boss?1.05f-.1f*phase:.7f;target=game.Player.transform.position;target.y=baseY;attackOrigin=transform.position;attackCount++;
      warning=Art.Ring(Boss?target:transform.position+transform.forward*1.2f,Boss?2.2f+.3f*phase:1.15f,new Color(1,.18f,.13f),game.World.transform);Visual.Restart("attack");}
     if(distance>22){state=State.Patrol;timer=0;}break;
    case State.Windup:
     Visual.Play("attack");if(warning)warning.transform.localScale=Vector3.one*(1+Mathf.Sin(RealmGame.I.Elapsed*18)*.045f);if(timer<=0){if(warning)Destroy(warning);state=State.Attack;timer=Boss?.45f:.25f;CommitAttack();}break;
    case State.Attack:
     // Damage happens only once on commitment, never from passive body contact.
     if(timer<=0){state=State.Recover;timer=Boss?1.65f-.18f*phase:1.1f;}break;
    case State.Recover:
     Visual.Play("idle");if(timer<=0){state=State.Notice;timer=.15f;}break;
   }
  }
  void CommitAttack(){var g=RealmGame.I;g.Sound(Boss?"boss":"enemy_dash");if(Boss){int attack=(attackCount+Kind)%3;
    if(attack==0){StrikeZone.Create(target,2.2f+phase*.3f,2,.05f);if(phase>=2)StrikeZone.Create(target+Vector3.right*3.5f,1.5f,1,.8f);if(phase==3)StrikeZone.Create(target-Vector3.right*3.5f,1.5f,1,1.2f);}
    else if(attack==1){for(int i=0;i<phase+1;i++){float angle=(i-phase*.5f)*13;Vector3 dir=Quaternion.Euler(0,angle,0)*(target-transform.position).normalized;EnemyBolt.Create(transform.position+Vector3.up*1.2f,dir,5.5f,2);}}
    else{Vector3 direction=(target-transform.position).normalized;Vector3 destination=Clamp(transform.position+direction*5);StrikeZone.Create(destination,2,2,.25f);transform.position=destination;}
   }else if(Kind==7||Kind==1){EnemyBolt.Create(transform.position+Vector3.up*.9f,(g.Player.transform.position+Vector3.up*.8f-transform.position-Vector3.up*.9f).normalized,6,1);}
   else{Vector3 d=g.Player.transform.position-transform.position;if(d.magnitude<3&&Vector3.Dot(transform.forward,d.normalized)>.05f)g.Player.Damage(Kind==6?2:1,transform.position);Pulse.Create(transform.position+transform.forward,1.2f,new Color(1,.35f,.18f),.2f);}
  }
  void Face(Vector3 p,float dt){Vector3 d=p-transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),dt*8);}
  void MoveTo(Vector3 p,float speed,float dt){Face(p,dt);Vector3 goal=Clamp(p);transform.position=Vector3.MoveTowards(transform.position,goal,speed*dt);}
  Vector3 Clamp(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,center.x-area.x*.5f+Radius+.2f,center.x+area.x*.5f-Radius-.2f),baseY,Mathf.Clamp(p.z,center.z-area.y*.5f+Radius+.2f,center.z+area.y*.5f-Radius-.2f));
  public void Hit(float damage,int power,bool elemental){if(Health<=0)return;if(Kind==5&&!elemental&&state!=State.Recover&&Vector3.Dot(transform.forward,(RealmGame.I.Player.transform.position-transform.position).normalized)>.5f){RealmGame.I.Tell("Shielded! Strike from behind or use a power.",1);return;}
   if(elemental){if(power==0){burnUntil=RealmGame.I.Elapsed+3;burnTick=RealmGame.I.Elapsed+.7f;}if(power==1)freezeUntil=RealmGame.I.Elapsed+(Boss?.6f:2.2f);if(power==2)transform.position=Clamp(transform.position+(transform.position-RealmGame.I.Player.transform.position).normalized*(Boss?1:2.5f));}
   ApplyDamage(damage*(Boss&&state==State.Recover?1.35f:1));RealmGame.I.Sound("impact");Pulse.Create(transform.position+Vector3.up, .8f,RealmGame.I.Accent,.18f);
  }
  void ApplyDamage(float amount){Health=Mathf.Max(0,Health-amount);if(Health<=0){state=State.Dead;if(warning)Destroy(warning);Visual.Restart("death");RealmGame.I.Coins+=Boss?20:3;RealmGame.I.Sound("enemy_defeat");if(Boss)RealmGame.I.Tell("Guardian restored. The realm gate is open.",5);Destroy(gameObject,2.5f);}}
  void OnDestroy(){if(RealmGame.I)RealmGame.I.Enemies.Remove(this);}
 }
 public class EnemyBolt:MonoBehaviour {Vector3 direction;float speed,life=5;int damage;public static void Create(Vector3 p,Vector3 dir,float speed,int damage){var go=Art.Shape("Enemy spell",PrimitiveType.Sphere,p,Vector3.one*.35f,new Color(1,.3f,.13f),RealmGame.I.World.transform);var bolt=go.AddComponent<EnemyBolt>();bolt.direction=dir;bolt.speed=speed;bolt.damage=damage;}
  void Update(){var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;float step=speed*Time.deltaTime;if(Physics.Raycast(transform.position,direction,out var hit,step,~0,QueryTriggerInteraction.Ignore)){if(hit.collider.GetComponent<Hero>())g.Player.Damage(damage,transform.position);Destroy(gameObject);return;}transform.position+=direction*step;life-=Time.deltaTime;if(Vector3.Distance(transform.position,g.Player.transform.position+Vector3.up*.8f)<.7f){g.Player.Damage(damage,transform.position);Destroy(gameObject);}else if(life<=0)Destroy(gameObject);}}
 public class StrikeZone:MonoBehaviour {float radius,delay,age;int damage;GameObject ring;public static void Create(Vector3 p,float radius,int damage,float delay){var go=new GameObject("Telegraphed strike");go.transform.SetParent(RealmGame.I.World.transform);go.transform.position=p;var z=go.AddComponent<StrikeZone>();z.radius=radius;z.damage=damage;z.delay=delay;z.ring=Art.Ring(Vector3.up*.08f,radius,new Color(1,.15f,.1f),go.transform);}
  void Update(){var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;age+=Time.deltaTime;if(age<delay)return;Vector3 d=g.Player.transform.position-transform.position;if(new Vector2(d.x,d.z).magnitude<radius&&Mathf.Abs(d.y)<1.2f)g.Player.Damage(damage,transform.position);Pulse.Create(transform.position+Vector3.up*.1f,radius,new Color(1,.3f,.15f),.4f);Destroy(gameObject);}}
}



