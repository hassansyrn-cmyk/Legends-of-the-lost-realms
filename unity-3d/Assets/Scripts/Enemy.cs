using UnityEngine;
namespace LostRealms {
 public partial class Enemy:MonoBehaviour {
  public int Kind;public bool Boss;public float Health,MaxHealth,Radius;public string DisplayName;public CharacterVisual Visual;
  Vector3 center,target,attackOrigin;Vector2 area;float timer,burnUntil,freezeUntil,burnTick;int phase=1,attackCount;float bossAddAt;enum State{Patrol,Notice,Windup,Attack,Recover,Dead}State state;GameObject warning;float baseY;
  Renderer[] skin;Material bodyMat;float flashUntil;Color flashColor;
  // Per-kind tuning so each enemy family reads and plays differently:
  // goblins dart in, demons pressure, frost runners slip wide, the elemental
  // and caster are slower but hit harder / shoot from range.
    static readonly float[] NoticeRange={8.5f,10.5f,8.5f,9f,10.5f,8.5f,7f,13f,12f,12.5f,13f,11f,9f,12f,11f,10.5f,10f,12f,9f,10f,10.5f,12.5f};
    static readonly float[] ChaseSpeed={2.3f,2.05f,2.3f,2.5f,2.05f,2.3f,1.5f,1.15f,2.1f,2.3f,2.5f,2.9f,3.1f,1.2f,2.6f,2.2f,2.7f,2.4f,3.2f,2.1f,2.3f,2.3f};
    static readonly int[] MeleeDamage={1,2,1,1,2,1,2,1,2,2,2,1,2,1,3,2,2,2,1,2,3,3};
   // Each family's vulnerable element (0 ember, 1 frost, 2 gale). Matching the
   // player's current element deals bonus damage and triggers that reaction.
    static readonly int[] Weakness={2,1,0,0,1,0,2,2,0,1,0,2,0,1,0,1,0,1,1,0,2,2};
   public int WeakElement=>Weakness[Mathf.Clamp(Kind,0,21)];
  // Kind 11 (Flyer) hovers: it ignores the ground clamp and bobs in the air.
  bool aerial;
  // Elite affixes (non-boss, stage>=3): 1 Shielded, 2 Burning, 4 Swift, 8 Vampiric, 16 Armored.
  public int Affix;float shield;float speedMul=1f;float comboNext;int comboLeft;bool Ranged=>Kind==7||Kind==13||Kind==17;
   public int BossPhase=>phase;
  public void Configure(int kind,bool boss,Vector3 anchor,Vector2 island){Kind=kind;Boss=boss;center=anchor;area=island;baseY=transform.position.y;Radius=boss?1.1f:kind>=14?.7f:.5f;MaxHealth=(boss?24+RealmGame.I.Realm*8:3+RealmGame.I.Level*.3f)*(kind==14?2.7f:1f);Health=MaxHealth;
   if(boss)bossAddAt=RealmGame.I.Elapsed+11f;
    string[] roles={"Goblin","Demon","Goblin","Frost","Demon","Goblin","Elemental","Caster","Heartwood","Sunscar","Whiteout","Flyer","Bomber","Summoner","Elite","Skeleton","BriarGoblin","EmberDemon","Spider","Footman","DogKnight","DogKnight"};string role=roles[Mathf.Clamp(kind,0,21)];DisplayName=boss?new[]{"HEARTWOOD COLOSSUS","SUNSCAR TITAN","WHITEOUT GUARDIAN","EMBERFALL WARDEN"}[RealmGame.I.Realm]:role;
   if(boss&&RealmGame.I.Realm==3)role="LavaBoss";
   Visual=CharacterVisual.Create(role,transform,boss?3.6f:kind==6?2.5f:kind==14?2.2f:kind==15?1.7f:kind==16?1.35f:kind==17?1.85f:kind==18?0.6f:kind==19?1.75f:kind==20?1.4f:1.65f,RealmGame.I.Accent);RealmGame.I.Enemies.Add(this);state=State.Patrol;timer=.5f;target=center;
   if(IsLavaBoss)LavaBossWeapon.Attach(Visual);
    aerial=kind==11;if(aerial){transform.position+=Vector3.up*2.3f;baseY=transform.position.y;}
    skin=Visual?Visual.GetComponentsInChildren<Renderer>(true):null;
    if(skin!=null&&skin.Length>0)bodyMat=skin[0].material;
    gameObject.AddComponent<EnemyAura>().Setup(Weakness[Mathf.Clamp(kind,0,21)],boss);
    if(!boss&&RealmGame.I!=null&&RealmGame.I.Level>=3){
     float chance=Mathf.Clamp01((RealmGame.I.Level-2)*.06f);
     if(Random.value<chance){
      int pick=Random.Range(0,5);Affix=pick==0?1:pick==1?2:pick==2?4:pick==3?8:16;
      if((Affix&1)!=0)shield=MaxHealth*.35f;
      if((Affix&4)!=0)speedMul=1.28f;
      Color ac=Affix==1?new Color(.4f,.9f,1f):Affix==2?new Color(1,.5f,.15f):Affix==4?new Color(1,.9f,.3f):Affix==8?new Color(.75f,.4f,1f):new Color(.9f,.9f,.95f);
      Art.Ring(new Vector3(0,.06f,0),Radius+.55f,ac,transform);
     }
    }
    // Restyled families (Goblin/Demon/Frost/Elemental/Caster + the bosses) are
    // static showcase meshes too — everything without a real Animator and
    // without the primitive fallback (which bobs itself) gets the idle motion.
    if(Visual&&!Visual.animator&&!Visual.UsesFallback)gameObject.AddComponent<EnemyIdleMotion>().Setup(0f);
     if(kind==15&&Visual)gameObject.AddComponent<SkeletonLimbMotion>().Setup(Visual);
     if(kind==16&&Visual)gameObject.AddComponent<SkeletonLimbMotion>().Setup(Visual,"upper_arm.L","upper_arm.R",null,null,null,null);
     if(kind==17&&Visual)gameObject.AddComponent<SkeletonLimbMotion>().Setup(Visual,"upper_arm.L","upper_arm.R","wing_upper.L","wing_lower.L","wing_upper.R","wing_lower.R");
     if(kind==18&&Visual)gameObject.AddComponent<SpiderLimbMotion>().Setup(Visual);
    if(aerial)return;
    // Block Aster: a real (non-trigger) capsule so the CharacterController
    // cannot walk through the enemy. Damage stays manual (no contact damage).
    var body=gameObject.AddComponent<CapsuleCollider>();
    body.height=boss?3.6f:kind==6?2.5f:kind==14?2.2f:kind==15?1.7f:kind==16?1.35f:kind==17?1.85f:kind==18?0.6f:kind==19?1.75f:kind==20?1.4f:1.65f;body.radius=boss?1.15f:kind==18?.45f:kind>=14?.75f:.55f;body.center=Vector3.up*(body.height*.5f);
   }
  void Update(){var game=RealmGame.I;if(game.Screen!=GameScreen.Playing)return;if(state==State.Dead)return;float dt=Time.deltaTime;if(RealmGame.I.Elapsed<burnUntil&&RealmGame.I.Elapsed>=burnTick){burnTick=RealmGame.I.Elapsed+.7f;ApplyDamage(.5f);if(Health<=0)return;}   if(RealmGame.I.Elapsed<freezeUntil){Visual.Play("idle");return;}
   if(aerial)transform.position=new Vector3(transform.position.x,baseY+Mathf.Sin(RealmGame.I.Elapsed*1.2f+Kind)*.25f,transform.position.z);
    if(bodyMat!=null&&RealmGame.I.Elapsed>=flashUntil)bodyMat.SetColor("_EmissionColor",Color.black);
   if(Boss){int next=Health<MaxHealth*.33f?3:Health<MaxHealth*.67f?2:1;if(next>phase){phase=next;game.Tell(DisplayName+" / PHASE "+phase);HitSpark.Burst(transform.position+Vector3.up*1.5f,Vector3.up,game.Accent,20);Vfx.Play("ga_vfx_Shockwave_01",transform.position+Vector3.up*1.5f,Quaternion.identity,1.7f);}}
    if(Boss&&phase>=2&&RealmGame.I.Elapsed>=bossAddAt){bossAddAt=RealmGame.I.Elapsed+15f;SummonHeralds();}
   if(IsLavaBoss){UpdateLavaBoss(dt);return;}
   timer-=dt;Vector3 delta=game.Player.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;bool sameHeight=Mathf.Abs(game.Player.transform.position.y-baseY)<3;
   switch(state){
    case State.Patrol:
     Visual.Play("walk");if(timer<=0){float x=Mathf.Sin(RealmGame.I.Elapsed*.7f)*area.x*.24f,z=Mathf.Cos(RealmGame.I.Elapsed*.5f)*area.y*.22f;target=center+new Vector3(x,.03f,z);timer=2;}
      MoveTo(target,.7f,dt);if(distance<(Boss?17:NoticeRange[Mathf.Clamp(Kind,0,21)])&&sameHeight){state=State.Notice;timer=.4f;game.Sound("enemy_warning");
       // Aggro sharing: nearby patrolling kin join the fight.
       foreach(var other in game.Enemies){if(!other||other==this||other.Boss||other.Health<=0)continue;if(other.state==State.Patrol&&Vector3.Distance(other.transform.position,transform.position)<8f){other.state=State.Notice;other.timer=.55f;}}}break;
    case State.Notice:
      Visual.Play("walk");Face(game.Player.transform.position,dt);
      float chase=Boss?1.8f+phase*.2f:ChaseSpeed[Mathf.Clamp(Kind,0,21)]*speedMul;
      if(Ranged){
       // Ranged families keep their distance instead of closing in.
       if(distance<3.4f){Vector3 away=transform.position-game.Player.transform.position;away.y=0;if(away.sqrMagnitude<.01f)away=-transform.forward;transform.position=Vector3.MoveTowards(transform.position,Clamp(transform.position+away.normalized*2f),chase*dt);}
       else if(distance>3.6f)MoveTo(game.Player.transform.position,chase,dt);
      }else if(distance>(Boss?4:2.1f))MoveTo(game.Player.transform.position,chase,dt);
      if(timer<=0&&(distance<(Boss?8:(Kind==7||Kind==13)?10:2.8f))){state=State.Windup;timer=Boss?1.05f-.1f*phase:(Kind==13?.9f:.7f);target=game.Player.transform.position;target.y=baseY;attackOrigin=transform.position;attackCount++;
      warning=CombatTelegraph.Create(Boss?target:transform.position+transform.forward*1.2f,Boss?2.2f+.3f*phase:1.15f,timer,game.World.transform);Visual.Restart("attack");}
     if(distance>22){state=State.Patrol;timer=0;}break;
    case State.Windup:
     Visual.Play("attack");Glow(new Color(1,.2f,.13f),.51f);if(timer<=0){ClearGlow();if(warning)Destroy(warning);state=State.Attack;timer=Boss?.45f:.25f;CommitAttack();}break;
    case State.Attack:
     if(timer<=0){state=State.Recover;timer=Boss?1.65f-.18f*phase:Kind==13?2.4f:1.1f;}break;
     case State.Recover:
      Visual.Play("idle");if(timer<=0){state=State.Notice;timer=.15f;}break;
   }
   // Elite (kind 14) telegraphed follow-up strikes.
   if(comboLeft>0&&RealmGame.I.Elapsed>=comboNext){
    comboLeft--;comboNext=RealmGame.I.Elapsed+.34f;
    int dmg=MeleeDamage[Mathf.Clamp(Kind,0,21)];Vector3 dc=game.Player.transform.position-transform.position;
    if(dc.magnitude<3f&&Vector3.Dot(transform.forward,dc.normalized)>.0f){if(game.Player.Damage(dmg,transform.position))DamageTip.Show(game.Player.transform.position+Vector3.up*1.7f,"-"+dmg,new Color(1,.3f,.24f));}
    Visual.Restart("attack");HitSpark.Burst(transform.position+transform.forward,transform.forward,new Color(1,.35f,.18f),8);
   }
  }
  void CommitAttack(){var g=RealmGame.I;g.Sound(Boss?"boss":"enemy_dash");
   if(Boss){int attack=(attackCount+Kind)%3;
    if(attack==0){StrikeZone.Create(target,2.2f+phase*.3f,2,.05f);if(phase>=2)StrikeZone.Create(target+Vector3.right*3.5f,1.5f,1,.8f);if(phase==3)StrikeZone.Create(target-Vector3.right*3.5f,1.5f,1,1.2f);}
    else if(attack==1){if(phase==3){RealmGame.I.Tell(RealmGame.I.Realm==0?"THORN WHEEL":RealmGame.I.Realm==1?"CINDER NOVA":RealmGame.I.Realm==2?"TEMPEST BURST":"EMBERFALL FLOOD",1.5f);for(int i=0;i<12;i++){float angle=i*30f;EnemyBolt.Create(transform.position+Vector3.up*1.5f,Quaternion.Euler(0,angle,0)*Vector3.forward,6f,2,true);}}else{for(int i=0;i<phase+1;i++){float angle=(i-phase*.5f)*13;Vector3 dir=Quaternion.Euler(0,angle,0)*(target-transform.position).normalized;EnemyBolt.Create(transform.position+Vector3.up*1.2f,dir,5.5f,2,true);}}}
    else{Vector3 direction=(target-transform.position).normalized;Vector3 destination=Clamp(transform.position+direction*5);StrikeZone.Create(destination,2,2,.25f);transform.position=destination;}
   }
   else if(Kind==12){StrikeZone.Create(transform.position,2.7f,2,.05f);HitSpark.Burst(transform.position+Vector3.up*.6f,Vector3.up,new Color(1,.45f,.15f),26);ApplyDamage(Health);}
   else if(Kind==13){int alive=0;foreach(var e in g.Enemies)if(e&&!e.Boss&&e.Health>0)alive++;if(alive<16){var minion=new GameObject("Summoned minion");minion.transform.SetParent(transform.parent);minion.transform.position=transform.position+transform.forward*1.6f;minion.AddComponent<Enemy>().Configure(RealmGame.I.Level>=7?16:0,false,center,area);HitSpark.Burst(transform.position+Vector3.up*.7f,Vector3.up,new Color(.78f,.45f,1f),16);Vfx.Play("ga_vfx_Implosion_01",transform.position+Vector3.up*.8f,Quaternion.identity,.9f);}}
   else if(Kind==17)EnemyBolt.Create(transform.position+Vector3.up*1.1f,(g.Player.transform.position+Vector3.up*.8f-transform.position-Vector3.up*1.1f).normalized,6.5f,2,false,17);
    else if(Kind==7||Kind==1)EnemyBolt.Create(transform.position+Vector3.up*.9f,(g.Player.transform.position+Vector3.up*.8f-transform.position-Vector3.up*.9f).normalized,6,1);
   else{int dmg=MeleeDamage[Mathf.Clamp(Kind,0,21)];if((Affix&2)!=0)dmg+=1;Vector3 d=g.Player.transform.position-transform.position;if(d.magnitude<3&&Vector3.Dot(transform.forward,d.normalized)>.05f){if(g.Player.Damage(dmg,transform.position)){DamageTip.Show(g.Player.transform.position+Vector3.up*1.7f,"-"+dmg,new Color(1,.3f,.24f));if((Affix&8)!=0){Health=Mathf.Min(MaxHealth,Health+.8f);HitSpark.Burst(transform.position+Vector3.up*.9f,Vector3.up,new Color(.75f,.4f,1f),6);}}}HitSpark.Burst(transform.position+transform.forward,transform.forward,new Color(1,.35f,.18f),8);if(Kind==14){comboLeft=2;comboNext=RealmGame.I.Elapsed+.34f;}}
   }
   // Phase-2 boss mechanic: each realm guardian calls its own minions into the
   // arena every ~15s (capped so the fight never becomes an add-spam). Realm
   // theming: Verdant summons briar goblins, Burning summons ember demons, the
   // Frozen guardian summons frost demons.
   void SummonHeralds(){
    int alive=0;foreach(var e in RealmGame.I.Enemies)if(e&&!e.Boss&&e.Health>0&&Vector3.Distance(e.transform.position,transform.position)<16f)alive++;
    if(alive>=4){bossAddAt=RealmGame.I.Elapsed+3f;return;}
    int addKind=RealmGame.I.Realm==0?16:RealmGame.I.Realm==1?17:RealmGame.I.Realm==2?3:18;
    var add=new GameObject("Herald of "+DisplayName);
    add.transform.SetParent(transform.parent);
    add.transform.position=Clamp(transform.position+(Quaternion.Euler(0,Random.Range(0,360),0)*Vector3.forward*2f)+Vector3.up*.1f);
    add.AddComponent<Enemy>().Configure(addKind,false,center,area);
    HitSpark.Burst(add.transform.position+Vector3.up*.7f,Vector3.up,RealmGame.I.Accent,16);
    Vfx.Play("ga_vfx_Implosion_01",add.transform.position+Vector3.up*.9f,Quaternion.identity,1f);
    Vfx.Play("ga_vfx_Portal_02",add.transform.position+Vector3.up*1.2f,Quaternion.identity,1f);
    RealmGame.I.Tell(RealmGame.I.Realm==0?"The colossus calls its children…":RealmGame.I.Realm==1?"The titan's flame kindles…":RealmGame.I.Realm==2?"The guardian stirs the tempest…":"The warden's embers breathe…",2.5f);
   }
   void Face(Vector3 p,float dt){Vector3 d=p-transform.position;d.y=0;if(d.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(d),dt*8);}
   void MoveTo(Vector3 p,float speed,float dt){Face(p,dt);Vector3 goal=Clamp(p);var player=RealmGame.I.Player;
    if(!Boss&&player){Vector3 axis=player.transform.position-transform.position;axis.y=0;if(axis.sqrMagnitude>.01f){Vector3 perp=Vector3.Cross(axis.normalized,Vector3.up);goal+=perp*Mathf.Sin(Kind*2.3f+RealmGame.I.Elapsed*.5f)*.85f;}}
    if(player){Vector3 d=goal-player.transform.position;d.y=0;float minDist=Mathf.Max(Radius,.5f)+.34f;if(d.sqrMagnitude<minDist*minDist){d=d.sqrMagnitude>.0001f?d.normalized*minDist:Vector3.right*minDist;goal=player.transform.position+d;goal.y=baseY;goal=Clamp(goal);}}transform.position=Vector3.MoveTowards(transform.position,goal,speed*dt);}
  Vector3 Clamp(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,center.x-area.x*.5f+Radius+.2f,center.x+area.x*.5f-Radius-.2f),baseY,Mathf.Clamp(p.z,center.z-area.y*.5f+Radius+.2f,center.z+area.y*.5f-Radius-.2f));
  public void Stun(float seconds){
   if(Health<=0)return;
   if(IsLavaBoss){lavaRoaring=false;Visual.transform.localPosition=Vector3.zero;}
   state=State.Recover;timer=Mathf.Max(timer,seconds);comboLeft=0;ClearGlow();
   if(warning){Destroy(warning);warning=null;}
   Visual.Play("idle");
  }
  public void Hit(float damage,int power,bool elemental,Vector3 knockDir=default){
   if(Health<=0)return;
   RealmGame.I?.ComboHit();
   bool weak=power>=0&&power<3&&Weakness[Mathf.Clamp(Kind,0,21)]==power;
   if(weak)elemental=true;
   if(Kind==5&&!elemental&&state!=State.Recover&&Vector3.Dot(transform.forward,(RealmGame.I.Player.transform.position-transform.position).normalized)>.5f){
    RealmGame.I.Tell("Shielded! Strike from behind or use a power.",1);
    HitSpark.Burst(transform.position+Vector3.up*.9f,-transform.forward,new Color(.9f,.9f,1f),8);
    return;
   }
   if(elemental){
    if(power==0){burnUntil=RealmGame.I.Elapsed+3;burnTick=RealmGame.I.Elapsed+.7f;}
    if(power==1){freezeUntil=RealmGame.I.Elapsed+(Boss?.6f:2.2f);if(IsLavaBoss)Stun(.6f);if(warning){var countdown=warning.GetComponent<CombatTelegraph>();if(countdown)countdown.HoldUntil(freezeUntil);}}
    if(power==2)transform.position=Clamp(transform.position+(transform.position-RealmGame.I.Player.transform.position).normalized*(Boss?1:2.5f));
   }
   // Physical knockback impulse
   Vector3 k=knockDir==default?(transform.position-RealmGame.I.Player.transform.position).normalized:knockDir.normalized;
   transform.position=Clamp(transform.position+k*(Boss?.35f:.75f));

   float real=damage*(Boss&&state==State.Recover?1.35f:1)*(weak?1.5f:1);
   if((Affix&16)!=0)real*=.75f;
   if(shield>0f){float absorbed=Mathf.Min(shield,real);shield-=absorbed;real-=absorbed;HitSpark.Burst(transform.position+Vector3.up*.9f,-k,new Color(.5f,.95f,1f),8);if(shield<=0f){Vfx.Play("ga_vfx_Implosion_01",transform.position+Vector3.up*1f,Quaternion.identity,.8f);RealmGame.I.Tell("Guard broken!",1);}}
   if(real>0&&weak&&RealmGame.I.Trial)RealmGame.I.Trial.WeaknessHit();
   ApplyDamage(real);
   RealmGame.I.Sound("impact");
   RealmGame.I.CameraRig.Shake=Boss?.22f:.12f;

   Color hitCol=power==0?new Color(1f,.45f,.1f):power==1?new Color(.2f,.85f,1f):new Color(.2f,1f,.55f);
   HitSpark.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),k,hitCol,Boss?16:10);
   Vfx.Play(elemental?"ga_vfx_Explosion_01":"ga_vfx_Impact_01",transform.position+Vector3.up*(Boss?1.7f:.9f),Quaternion.identity,Boss?1.15f:.6f);
   if(weak){DamageTip.Show(transform.position+Vector3.up*(Boss?2.35f:1.3f),"WEAK  "+Mathf.Max(1,Mathf.RoundToInt(real)),hitCol);HitSpark.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),k,Color.white,10);Vfx.Play("ga_vfx_Electricity_01",transform.position+Vector3.up*1.2f,Quaternion.identity,.8f);}
   else DamageTip.Show(transform.position+Vector3.up*(Boss?2.2f:1.15f),"-"+Mathf.Max(1,Mathf.RoundToInt(real)),hitCol);
  }
  void Glow(Color color,float duration){flashColor=color;flashUntil=RealmGame.I.Elapsed+duration;if(bodyMat){bodyMat.EnableKeyword("_EMISSION");bodyMat.SetColor("_EmissionColor",color*1.6f);}}
  void ClearGlow(){if(bodyMat)bodyMat.SetColor("_EmissionColor",Color.black);}
  void ApplyDamage(float amount){
   Health=Mathf.Max(0,Health-amount);
   if(Health<=0){
    state=State.Dead;comboLeft=0;if(IsLavaBoss)Visual.transform.localPosition=Vector3.zero;if(RealmGame.I.Trial)RealmGame.I.Trial.EnemyDefeated();ClearGlow();if(warning)Destroy(warning);Visual.Restart("death");
    var collider=GetComponent<Collider>();if(collider)collider.enabled=false;
    RealmGame.I.Coins+=Boss?20:3;RealmGame.I.Sound("enemy_defeat");
    // Loot gems: bosses shower, the Elite mini-boss pays well, affixed elites
    // drop a couple, regular enemies rarely drop one so gems become a real
    // stream that funds the Sanctuary's gem-only tracks.
    int loot=Boss?6:(Kind==14?4:(Affix!=0?2:(Random.value<.28f?1:0)));
    if(loot>0)RealmPickup.Drop(transform.position+Vector3.up*.5f,loot);
    AshPuff.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),new Color(.36f,.3f,.27f),Boss?26:14,Boss?1.6f:1f,Boss);
    HitSpark.Burst(transform.position+Vector3.up*(Boss?1.8f:.9f),Vector3.up,RealmGame.I.Accent,Boss?28:16);
    ImpactMarks.Place(transform.position,.9f,Boss?new Color(.4f,.32f,.32f):new Color(.34f,.29f,.26f));
    if(Boss)Vfx.Play("ga_vfx_Explosion_01",transform.position+Vector3.up*1.4f,Quaternion.identity,1.7f);if(Boss)Vfx.Play("ga_vfx_MeteorRain_01",transform.position+Vector3.up*2f,Quaternion.identity,1.2f);
    if(Boss)RealmGame.I.Tell("Guardian restored. The realm gate is open.",5);
    float deathWait=IsLavaBoss?Mathf.Max(1.35f,Visual.ClipLength("death")):Boss?1.35f:1.05f;
    gameObject.AddComponent<EnemyDeathFall>().Setup(deathWait);
    if(!IsLavaBoss)Destroy(gameObject,2.5f);
   }
  }
  void OnDestroy(){if(RealmGame.I)RealmGame.I.Enemies.Remove(this);}
 }
  public class EnemyBolt:MonoBehaviour {
   Vector3 direction;float speed,life=5;int damage;Transform core,flare;Color tint;
   static Mesh shardMesh,quadMesh;
   static readonly System.Collections.Generic.Dictionary<string,Material> boltMats=new System.Collections.Generic.Dictionary<string,Material>();
   static Mesh Shard(){
    if(shardMesh)return shardMesh;
    var m=new Mesh{name="Bolt shard"};
    m.vertices=new[]{new Vector3(0,.5f,0),new Vector3(0,-.5f,0),new Vector3(.22f,0,0),new Vector3(-.22f,0,0),new Vector3(0,0,.22f),new Vector3(0,0,-.22f)};
    m.triangles=new[]{0,2,4, 0,4,3, 0,3,5, 0,5,2, 1,4,2, 1,3,4, 1,5,3, 1,2,5};
    m.RecalculateNormals();m.RecalculateBounds();shardMesh=m;return m;
   }
   static Mesh Quad(){
    if(quadMesh)return quadMesh;
    var m=new Mesh{name="Bolt quad"};
    m.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
    m.triangles=new[]{0,1,2,0,2,3};m.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
    m.RecalculateNormals();m.RecalculateBounds();quadMesh=m;return m;
   }
   static Material BoltGlow(Color c){
    string key=ColorUtility.ToHtmlStringRGB(c);
    if(boltMats.TryGetValue(key,out var m))return m;
    m=new Material(Shader.Find("Standard")){name="Bolt glow "+key};
    m.SetColor("_Color",c);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*1.8f);
    m.SetFloat("_Metallic",0f);m.SetFloat("_Glossiness",.4f);boltMats[key]=m;return m;
   }
   // kind selects the warhead: 17 = ember-demon fire shard, boss = large void
   // shard, anything else = the standard ember bolt. Missing sprite textures
   // degrade gracefully to a bare glowing shard.
   public static void Create(Vector3 p,Vector3 dir,float speed,int damage,bool boss=false,int kind=1){
    var go=new GameObject(boss?"Boss spell":"Enemy spell");go.transform.SetParent(RealmGame.I.World.transform,false);go.transform.position=p;
    Color color=kind==17?new Color(1,.14f,.28f):boss?new Color(.66f,.32f,1f):new Color(1,.38f,.12f);
    float s=(boss?1.7f:1f)*(kind==17?1.2f:1f);
    var bolt=go.AddComponent<EnemyBolt>();bolt.direction=dir;bolt.speed=speed;bolt.damage=damage;bolt.tint=color;
    var coreGo=new GameObject("Bolt core");coreGo.transform.SetParent(go.transform,false);
    coreGo.AddComponent<MeshFilter>().sharedMesh=Shard();
    var cr=coreGo.AddComponent<MeshRenderer>();cr.sharedMaterial=BoltGlow(color);cr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;cr.receiveShadows=false;
    coreGo.transform.localScale=Vector3.one*.34f*s;bolt.core=coreGo.transform;
    var flareTex=Resources.Load<Texture2D>("VFX/Textures/flare_01");
    if(flareTex){
     var fl=new GameObject("Bolt flare");fl.transform.SetParent(go.transform,false);
     fl.AddComponent<MeshFilter>().sharedMesh=Quad();
     var mr=fl.AddComponent<MeshRenderer>();var fm=new Material(Shader.Find("Sprites/Default"));fm.mainTexture=flareTex;fm.color=color;mr.sharedMaterial=fm;
     mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;
     fl.transform.localScale=Vector3.one*.95f*s;bolt.flare=fl.transform;
    }
    var smoke=Resources.Load<Texture2D>("VFX/Textures/smoke_04");
    if(smoke){
     var tr=new GameObject("Bolt trail");tr.transform.SetParent(go.transform,false);
     var ps=tr.AddComponent<ParticleSystem>();
     var main=ps.main;main.playOnAwake=false;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
     main.loop=false;main.duration=10;main.startLifetime=.45f;main.startSpeed=0f;main.startSize=.4f*s;main.maxParticles=24;main.startColor=color;main.simulationSpace=ParticleSystemSimulationSpace.World;
     var em=ps.emission;em.rateOverTime=28;
     var sh=ps.shape;sh.enabled=false;
     var pr=tr.GetComponent<ParticleSystemRenderer>();var tm=new Material(Shader.Find("Sprites/Default"));tm.mainTexture=smoke;tm.color=color;pr.material=tm;
     ps.Play();
    }
   }
   void Update(){
    var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;float step=speed*Time.deltaTime;
    if(core)core.Rotate(320*Time.deltaTime,410*Time.deltaTime,0);
    if(flare&&Camera.main)flare.rotation=Camera.main.transform.rotation;
    if(Physics.Raycast(transform.position,direction,out var hit,step,~0,QueryTriggerInteraction.Ignore)){
      if(hit.collider.GetComponent<Hero>()){if(g.Player.Damage(damage,transform.position))DamageTip.Show(g.Player.transform.position+Vector3.up*1.7f,"-"+damage,new Color(1,.3f,.2f));}
     BurstFX();Destroy(gameObject);return;
    }
    transform.position+=direction*step;life-=Time.deltaTime;
     if(Vector3.Distance(transform.position,g.Player.transform.position+Vector3.up*.8f)<.7f){
      if(g.Player.Damage(damage,transform.position))DamageTip.Show(g.Player.transform.position+Vector3.up*1.7f,"-"+damage,new Color(1,.3f,.2f));
     BurstFX();Destroy(gameObject);
    }else if(life<=0)Destroy(gameObject);
   }
   void BurstFX(){
    HitSpark.Burst(transform.position,-direction,new Color(1f,.4f,.15f),8);
    Vfx.Play("ga_vfx_Impact_01",transform.position,Quaternion.identity,.55f);
    KenneyPuff.Burst(transform.position,tint,10,.8f);
   }
  }
  public class StrikeZone:MonoBehaviour {
   float radius,delay,age;int damage;GameObject ring;
   public static void Create(Vector3 p,float radius,int damage,float delay){
    var go=new GameObject("Telegraphed strike");go.transform.SetParent(RealmGame.I.World.transform);go.transform.position=p;
    var z=go.AddComponent<StrikeZone>();z.radius=radius;z.damage=damage;z.delay=delay;
    z.ring=CombatTelegraph.Create(p,radius,delay,go.transform);
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
float t=g.Elapsed*(element==1?.5f:element==2?1f:.8f);
    for(int i=0;i<motes.Length;i++){if(!motes[i])continue;float a=t+i*2.0944f;motes[i].localPosition=new Vector3(Mathf.Cos(a)*radius,1f+Mathf.Sin(a*1.1f)*.34f,Mathf.Sin(a)*radius);motes[i].localRotation=Quaternion.Euler(t*40f+i*20f,t*60f,0);}
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
visual.localPosition=new Vector3(0,Mathf.Sin(t*1.1f)*.02f,0);
    visual.localRotation=Quaternion.Euler(Mathf.Sin(t*.7f)*1f,facing+Mathf.Sin(t*.45f)*3f,0);
   }
  }
  // Kind 15's mesh carries a real armature (15 bones, no Animator and no clips),
  // so its limbs are posed procedurally: walk swing, idle breath, overhead
  // strike. Each bone is driven in world space about the enemy's lateral axis,
  // relative to the rest pose captured at spawn — bone-local axes never matter,
  // so Blender roll is irrelevant. Death is untouched: with no Animator,
  // EnemyDeathFall still tips the whole visual over.
  public class SkeletonLimbMotion:MonoBehaviour {
   CharacterVisual visual;Enemy enemy;
   Transform[] db;Quaternion[] drest;float[] dcur;
   int iHips=-1,iSpine=-1,iChest=-1,iHead=-1,iUaL=-1,iUaR=-1,iFaL=-1,iFaR=-1,iThL=-1,iThR=-1,iShL=-1,iShR=-1;
   int iWuL=-1,iWuR=-1,iWlL=-1,iWlR=-1;
   string fUaL="upperarm.L",fUaR="upperarm.R",fWuL=null,fWlL=null,fWuR=null,fWlR=null;
   public void Setup(CharacterVisual v){visual=v;}
   // Rigged families reuse this driver with their own bone names (upper_arm.*
   // instead of upperarm.*, plus optional wing chains for the Ember Demon).
   public void Setup(CharacterVisual v,string uaL,string uaR,string wul,string wll,string wur,string wlr){
    visual=v;fUaL=uaL;fUaR=uaR;fWuL=wul;fWlL=wll;fWuR=wur;fWlR=wlr;
   }
   public static Transform FindDeep(Transform t,string n){
    if(!t)return null;if(t.name==n)return t;
    for(int i=0;i<t.childCount;i++){var f=FindDeep(t.GetChild(i),n);if(f)return f;}
    return null;
   }
   void AddBone(string n,System.Collections.Generic.List<Transform> found,System.Collections.Generic.List<Quaternion> rests,out int idx){
    var b=visual?FindDeep(visual.transform,n):null;
    if(!b){idx=-1;return;}
    idx=found.Count;found.Add(b);rests.Add(b.localRotation);
   }
   void Start(){
    enemy=GetComponent<Enemy>();
    if(!visual)return;
    var found=new System.Collections.Generic.List<Transform>();
    var rests=new System.Collections.Generic.List<Quaternion>();
    AddBone("hips",found,rests,out iHips);AddBone("spine",found,rests,out iSpine);
    AddBone("chest",found,rests,out iChest);AddBone("head",found,rests,out iHead);
    AddBone(fUaL,found,rests,out iUaL);AddBone(fUaR,found,rests,out iUaR);
    AddBone("forearm.L",found,rests,out iFaL);AddBone("forearm.R",found,rests,out iFaR);
    AddBone("thigh.L",found,rests,out iThL);AddBone("thigh.R",found,rests,out iThR);
    AddBone("shin.L",found,rests,out iShL);AddBone("shin.R",found,rests,out iShR);
    if(fWuL!=null)AddBone(fWuL,found,rests,out iWuL);if(fWuR!=null)AddBone(fWuR,found,rests,out iWuR);
    if(fWlL!=null)AddBone(fWlL,found,rests,out iWlL);if(fWlR!=null)AddBone(fWlR,found,rests,out iWlR);
    db=found.ToArray();drest=rests.ToArray();dcur=new float[db.Length];
   }
   void Update(){
    if(db==null||visual==null)return;var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing)return;
    if(enemy&&enemy.Health<=0)return;
    float dt=Time.deltaTime,t=g.Elapsed;
    string st=visual.CurrentState;
    float amp=st=="walk"?1f:0.12f;
    float w=t*7f;
    Vector3 lat=transform.right;
    bool attacking=st=="attack_1";
    float thLT=0.42f*Mathf.Sin(w)*amp,thRT=-0.42f*Mathf.Sin(w)*amp;
    float bendL=(0.15f+0.35f*Mathf.Max(0f,Mathf.Sin(w+2.2f)))*amp;
    float bendR=(0.15f+0.35f*Mathf.Max(0f,Mathf.Sin(w+Mathf.PI+2.2f)))*amp;
    float uaLT=-thLT*0.7f,uaRT=attacking?-2.4f:-thRT*0.7f;
    float faLT=uaLT-0.3f+0.1f*Mathf.Sin(w-0.7f)*amp;
    float faRT=attacking?-2.9f:uaRT-0.3f+0.1f*Mathf.Sin(w+Mathf.PI-0.7f)*amp;
    Apply(iHips,0.06f*Mathf.Sin(w)*amp,Vector3.up,dt);
    Apply(iSpine,-0.06f-0.02f*Mathf.Sin(t*1.7f),lat,dt);
    Apply(iChest,-0.03f*Mathf.Sin(t*1.7f+0.5f),lat,dt);
    Apply(iHead,0.05f*Mathf.Sin(t*1.7f+1f),lat,dt);
    Apply(iThL,thLT,lat,dt);Apply(iShL,thLT+bendL,lat,dt);
    Apply(iThR,thRT,lat,dt);Apply(iShR,thRT+bendR,lat,dt);
    Apply(iUaL,uaLT,lat,dt);Apply(iFaL,faLT,lat,dt);
    Apply(iUaR,uaRT,lat,dt);Apply(iFaR,faRT,lat,dt);
    if(iWuL>=0||iWuR>=0){
     Vector3 fwd=transform.forward;
     float flap=Mathf.Sin(t*3.4f+1f)*0.5f*amp+(attacking?0.55f:0.12f);
     Apply(iWuL,flap,fwd,dt);Apply(iWlL,flap*1.35f+0.15f,fwd,dt);
     Apply(iWuR,-flap,fwd,dt);Apply(iWlR,-flap*1.35f-0.15f,fwd,dt);
    }
   }
   void Apply(int i,float target,Vector3 axis,float dt){
    if(i<0||i>=db.Length)return;
    float c=dcur[i]+(target-dcur[i])*Mathf.Min(1f,dt*12f);
    dcur[i]=c;
    var b=db[i];
    Quaternion parentWorld=b.parent?b.parent.rotation:transform.rotation;
    b.rotation=Quaternion.AngleAxis(c*Mathf.Rad2Deg,axis)*(parentWorld*drest[i]);
   }
  }
  // The spider pack ships only a rig (35 bones), no animation takes. Its 8 legs
  // are split into 4 mirrored Box chains under Dummy02; drive them procedurally
  // with a metachronal travel wave (rear→front, alternating sides) — the same
  // no-Animator approach as kinds 16/17, so idle flutter, walk scuttle and the
  // attack lunge all read from the shared CurrentState, and death tip-over is
  // untouched.
  public class SpiderLimbMotion:MonoBehaviour {
   CharacterVisual visual;Enemy enemy;
   string[,] legs=new string[4,3]{{"Box09","Box10","Box11"},{"Box20","Box18","Box19"},{"Box25","Box22","Box23"},{"Box26","Box21","Box24"}};
   string[,] mir=new string[4,3]{{"Box31","Box28","Box29"},{"Box35","Box32","Box36"},{"Box37","Box33","Box34"},{"Box38","Box27","Box30"}};
   Transform[] lb;Quaternion[] lrest;float[] lcur;
   Transform hub;Vector3 hubRest;
   public void Setup(CharacterVisual v){visual=v;}
   void Start(){
    enemy=GetComponent<Enemy>();
    if(!visual)return;
    lb=new Transform[24];lrest=new Quaternion[24];lcur=new float[24];
    int k=0;
    for(int s=0;s<4;s++){
     for(int j=0;j<3;j++){AddBone(ref k,legs[s,j]);AddBone(ref k,mir[s,j]);}
    }
    if(k<24)System.Array.Resize(ref lb,k);
    hub=SkeletonLimbMotion.FindDeep(visual.transform,"Dummy02");
    if(hub)hubRest=hub.localPosition;else hubRest=Vector3.zero;
   }
   void AddBone(ref int k,string n){
    var b=visual?SkeletonLimbMotion.FindDeep(visual.transform,n):null;
    if(!b)return;
    lb[k]=b;lrest[k]=b.localRotation;k++;
   }
   void Update(){
    if(lb==null||visual==null)return;var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing)return;
    if(enemy&&enemy.Health<=0)return;
    float t=g.Elapsed;
    string st=visual.CurrentState;
    bool attacking=st=="attack_1";
    float amp=st=="walk"||attacking?1f:0.22f;
    // Rear-to-front metachronal wave: each successive leg lags the one behind,
    // and mirrored sides alternate so it reads as a true scuttle.
    float wave=(attacking?5.2f:6.4f)*t;
    for(int s=0;s<4;s++){
     float ph=-(s*(Mathf.PI/2f))+(attacking?Mathf.PI*0.35f:0f);
     for(int m=0;m<3;m++){
      int i=s*3+m;
      if(lb[i]==null)continue;
      float delay=(m==0?0f:m==1?.22f:.4f);
      float stride=(m==0?0.75f:m==1?.4f:-0.2f)*(attacking?1.35f:Mathf.Min(1f,0.4f+Mathf.Abs(Mathf.Sin(t*1.2f))*0.6f));
      float ang=Mathf.Sin(wave+ph+delay)*stride*amp+m/3f*0.06f*Mathf.Sin(t*1.7f+ph);
      Apply(i,ang*Mathf.Rad2Deg,transform.right,Time.deltaTime);
      // Tip bones trail with the same wave plus a soft landing curl.
      if(m==2)Apply(i,ang*Mathf.Rad2Deg*0.5f-0.08f,Vector3.up,Time.deltaTime);
     }
    }
    if(hub)hub.localPosition=Vector3.Lerp(hub.localPosition,hubRest+Vector3.up*(Mathf.Sin(t*2.1f)*0.02f*amp),Mathf.Min(1f,Time.deltaTime*8f));
   }
   void Apply(int i,float target,Vector3 axis,float dt){
    if(i<0||i>=lb.Length||!lb[i])return;
    float c=lcur[i]+(target-lcur[i])*Mathf.Min(1f,dt*10f);
    lcur[i]=c;
    var b=lb[i];
    Quaternion parentWorld=b.parent?b.parent.rotation:transform.rotation;
    b.rotation=Quaternion.AngleAxis(c,axis)*(parentWorld*lrest[i]);
   }
  }
  }
