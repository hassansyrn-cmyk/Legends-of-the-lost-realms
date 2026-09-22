using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LostRealms {
 [DefaultExecutionOrder(20)] public class Hero:MonoBehaviour {
  public CharacterController Controller;public int MaxHealth,Health,Power;public float Energy=100;public bool Grounded=>Controller&&Controller.isGrounded; public CharacterVisual Visual;
  public int windUsed;
  const float MaxMoveSpeed=4.8f,GroundResponse=18f,AirResponse=8f,StopResponse=22f;
  Vector3 velocity;Vector3 moveWish;float vertical,jumpGrace,dashUntil,dashReady,dodgeVisualUntil,hitUntil,immuneUntil,attackReady,comboUntil,chargeStart,attackBufferUntil,spellUntil;int jumps,combo,airDashes;bool charging,attackBufferCharged,spellCharging,wasGrounded,plunging;float jumpBuffer,spellChargeStart;string attackState="attack_1",hitState="hit",parryState="charged",dodgeVisualState="dodge";
  float parryUntil,parryReady,counterUntil,pullUntil,stepTimer;Vector3 pullPoint;bool dodgeRewarded,stepAlt;
  Vector3 platformDisplacement;float hyperArmorUntil,ledgeLostAt;
  public void CarryByPlatform(Vector3 delta){platformDisplacement+=delta;}
  // Heavy weapons (greataxes/hammers) grant hyper-armor during the active
  // swing: damage still lands but the flinch and knockback are shrugged off.
  public bool HyperArmor=>RealmGame.I!=null&&RealmGame.I.Elapsed<hyperArmorUntil;
  public bool CounterReady=>RealmGame.I!=null&&RealmGame.I.Elapsed<counterUntil;
  public float HorizontalSpeed{get{var horizontal=velocity;horizontal.y=0;return horizontal.magnitude;}}
  // Read by FollowCamera for FOV kick (dash/attack) without exposing internals.
  public bool Dashing=>RealmGame.I!=null&&RealmGame.I.Elapsed<dashUntil;
  public bool Attacking=>RealmGame.I!=null&&RealmGame.I.Elapsed<attackReady-.06f;
  public bool Pulling=>RealmGame.I!=null&&RealmGame.I.Elapsed<pullUntil;
  public bool Plunging=>plunging;
  void Awake(){
   Controller=gameObject.AddComponent<CharacterController>();Controller.height=1.75f;Controller.radius=.32f;Controller.center=Vector3.up*.9f;Controller.stepOffset=.35f;Controller.slopeLimit=48;Controller.skinWidth=.08f;Controller.minMoveDistance=.001f;
   MaxHealth=5+RealmGame.I.Save.healthRank;Health=MaxHealth;
   Visual=CharacterVisual.Create("Aster",transform,1.8f,new Color(.25f,.55f,.57f));
  }
  void Start(){if(RealmGame.I&&RealmGame.I.Save.equippedWeapon>=0)EquippedWeapon.Equip(this,(WeaponId)RealmGame.I.Save.equippedWeapon);}
  void Update(){
   var g=RealmGame.I;
   if(!g||!g.CameraRig||!Visual||!Controller)return;
   if(g.Screen!=GameScreen.Playing){
    if(g.Screen==GameScreen.Defeated)Visual.Play("death");else Visual.Play("idle");
    return;
   }
   // Input shaping only: camera-facing wish is computed here for rotation
   // and animation, while ALL CharacterController physics runs in FixedUpdate
   // so input timing can never make the capsule stutter or jitter.
   Vector3 forward=Quaternion.Euler(0,g.CameraRig.Yaw,0)*Vector3.forward;
   Vector3 right=Quaternion.Euler(0,g.CameraRig.Yaw,0)*Vector3.right;
   moveWish=forward*g.MoveInput.y+right*g.MoveInput.x;
   if(moveWish.sqrMagnitude>1f)moveWish.Normalize();
   float sdt=Mathf.Min(Time.deltaTime,1f/30f);

   // Smooth turning with subtle banking (yaw on root, lean on visual only
   // so the CharacterController capsule never tilts and jitters).
   if(moveWish.sqrMagnitude>.02f){
    Vector3 flatWish=moveWish;flatWish.y=0;
    if(flatWish.sqrMagnitude>.001f){
     Quaternion targetRot=Quaternion.LookRotation(flatWish.normalized);
     float turnAngle=Vector3.SignedAngle(transform.forward,flatWish.normalized,Vector3.up);
     Quaternion bank=Quaternion.Euler(0,0,Mathf.Clamp(-turnAngle*0.25f,-8f,8f));
     transform.rotation=Quaternion.Slerp(transform.rotation,targetRot,sdt*12f);
     if(Visual)Visual.transform.localRotation=Quaternion.Slerp(Visual.transform.localRotation,bank,sdt*10f);
    }
   }else if(Visual&&Visual.transform.localRotation!=Quaternion.identity){
    Visual.transform.localRotation=Quaternion.Slerp(Visual.transform.localRotation,Quaternion.identity,sdt*10f);
   }

   // Attack inputs (taps chain via a short queue so swings link smoothly)
   if(g.AttackPressed){
    if(!Grounded&&!plunging&&transform.position.y>-6f){
     StartPlunge();
    }else{
     charging=true;chargeStart=RealmGame.I.Elapsed;
    }
   }
   if(charging&&g.AttackReleased){Attack(RealmGame.I.Elapsed-chargeStart>=.38f);charging=false;}
   if(charging&&RealmGame.I.Elapsed-chargeStart>=.75f){Attack(true);charging=false;}
   if(attackBufferUntil>0&&RealmGame.I.Elapsed>=attackReady&&RealmGame.I.Elapsed>=dodgeVisualUntil&&RealmGame.I.Elapsed>=hitUntil){attackBufferUntil=0;Attack(attackBufferCharged);}
   if(g.CastPressed)Cast();
   if(g.ParryPressed)Parry();
   // Spell: tap = bolt, hold (>=.45s) = lobbed AoE. Release or a 1.15s cap fires.
   if(g.SpellPressed){spellCharging=true;spellChargeStart=RealmGame.I.Elapsed;}
   if(spellCharging&&g.SpellReleased){if(RealmGame.I.Elapsed-spellChargeStart>=.45f)LobSpell();else CastSpell();spellCharging=false;}
   if(spellCharging&&RealmGame.I.Elapsed-spellChargeStart>=1.15f){LobSpell();spellCharging=false;}
   if(g.GrapplePressed)TryGrapple();
   if(transform.position.y<-12){Health=0;g.DamageTaken++;g.Defeat();}

   // Character Animation state selection with natural speeds
   if(RealmGame.I.Elapsed<dodgeVisualUntil){
    Visual.Play(dodgeVisualState);
   }else if(RealmGame.I.Elapsed<hitUntil){
    Visual.Play(hitState);
   }else if(RealmGame.I.Elapsed<parryUntil){
    Visual.Play(parryState);
   }else if(plunging){
    Visual.Play("charged");
   }else if(RealmGame.I.Elapsed<attackReady-.06f){
    Visual.Play(attackState);
   }else if(!Grounded){
    Visual.Play("jump");
   }else if(moveWish.sqrMagnitude>.01f||HorizontalSpeed>.2f){
    float speedRatio=Mathf.Clamp01(HorizontalSpeed/MaxMoveSpeed);
    Visual.PlayWalk(speedRatio);
   }else{
    Visual.Play("idle");
   }
  }

  void FixedUpdate(){
   var g=RealmGame.I;
   if(!g||!Controller||g.Screen!=GameScreen.Playing){platformDisplacement=Vector3.zero;return;}
   // Button edges are consumed here so a single press can never fire twice
   // when a frame happens to contain multiple physics steps.
   bool jumpPressed=g.JumpPressed;if(jumpPressed)g.JumpPressed=false;
   bool dashPressed=g.DashPressed;if(dashPressed)g.DashPressed=false;
   // Physics always advances on the constant fixed timestep: frame-rate
   // spikes no longer move Aster farther (the old dt clamp) or make the
   // controller skip collision layers.
   float dt=Time.fixedDeltaTime;
   Vector3 wish=moveWish;

   // Grounding and Jump Grace (Coyote time)
   if(Grounded){jumps=0;airDashes=1+Mathf.Min(3,RealmGame.I.Save.moxieRank);jumpGrace=.14f;if(vertical<0)vertical=-3f;}else jumpGrace-=dt;
   jumpBuffer=jumpPressed?.13f:Mathf.Max(0,jumpBuffer-dt);
   if(jumpBuffer>0&&(jumps<2||jumpGrace>0)){
    jumpBuffer=0;
    vertical=8.4f;jumps=jumpGrace>0?1:jumps+1;jumpGrace=0;
    // Platform dismount inertia: inherit the carrying island's horizontal
    // velocity (damped + capped) so hops off ferries/elevators carry momentum
    // instead of braking hard in mid-air.
    Vector3 platformVelocity=platformDisplacement/Mathf.Max(dt,1e-5f);platformVelocity.y=0;
    if(platformVelocity.sqrMagnitude>.04f)velocity+=Vector3.ClampMagnitude(platformVelocity*.85f,4.5f);
    g.Sound(jumps==1?"jump":"double_jump");
    if(jumps==2){
     Color elemColor=Power==0?new Color(1f,.45f,.1f):Power==1?new Color(.2f,.85f,1f):new Color(.2f,1f,.55f);
     HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.down,elemColor,8);
    }
    Visual.Restart("jump");
   }else if(jumpBuffer>0&&jumps==0&&ledgeLostAt>0f&&RealmGame.I.Elapsed-ledgeLostAt<=.15f){
    // Ledge forgiveness hop: narrowly missed an island edge just after coyote
    // time expired — a slightly weaker rescue jump that keeps the double jump.
    jumpBuffer=0;jumps=1;vertical=6.9f;ledgeLostAt=0f;
    g.Sound("jump");Visual.Restart("jump");
    HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.down,new Color(.85f,1f,.9f),6);
   }
   // Variable jump height removed: a tap-vs-hold trim shortened jumps on
   // touch devices (JumpHeld is never true for a tap), breaking island gaps.

   // Dash (one extra allowed per airtime)
   bool canDodgeCancel=Grounded&&RealmGame.I.Elapsed<attackReady&&RealmGame.I.Elapsed>=attackReady-.35f;
   if(dashPressed&&(RealmGame.I.Elapsed>=dashReady||canDodgeCancel)&&(Grounded||airDashes>0)){
    if(!Grounded)airDashes--;
    if(canDodgeCancel){dashReady=0;attackReady=RealmGame.I.Elapsed;charging=false;plunging=false;}
    dashUntil=RealmGame.I.Elapsed+.22f;dodgeVisualUntil=RealmGame.I.Elapsed+.63f;dashReady=RealmGame.I.Elapsed+.9f;
    dodgeRewarded=false;
    immuneUntil=Mathf.Max(immuneUntil,dashUntil+.1f);
    attackReady=RealmGame.I.Elapsed;charging=false;
    if(wish.sqrMagnitude<.1f)wish=transform.forward;
    wish.y=0;wish.Normalize();
    velocity=wish*9.5f;velocity.y=0;g.Sound("player_dash");
    dodgeVisualState=Grounded?"roll":"dodge";
    Visual.Restart(dodgeVisualState);
   Vfx.Play("ga_vfx_Hyperdrive_01",transform.position+Vector3.up*.9f,Quaternion.LookRotation(transform.forward),.7f);
    HitSpark.Burst(transform.position+Vector3.up*.8f,-wish,new Color(1f,.9f,.7f),8);
   }

   float fall=vertical;
   // Grapple pull overrides steering until arrival (or until grounded).
   if(RealmGame.I.Elapsed<pullUntil){
    Vector3 to=pullPoint-transform.position;
    if(to.magnitude<1.3f||Grounded){pullUntil=0;velocity*=.4f;vertical=Mathf.Max(vertical,0f);}
    else{Vector3 d=to.normalized;velocity=d*Mathf.Min(15f,to.magnitude*4.5f);velocity.y=0;vertical=Mathf.Max(vertical,d.y*9f);}
   }else if(RealmGame.I.Elapsed>=dashUntil){
    Vector3 desiredVelocity=wish.sqrMagnitude>.0001f?wish.normalized*(MaxMoveSpeed*Mathf.Clamp01(wish.magnitude)):Vector3.zero;
    float response=desiredVelocity.sqrMagnitude>.001f?(Grounded?GroundResponse:AirResponse):StopResponse;
    velocity=Vector3.MoveTowards(velocity,desiredVelocity,response*dt);
    velocity.y=0;
    // Dash leftover (or knockback) must not linger above run speed and
    // slingshot Aster across small islands.
    if(velocity.sqrMagnitude>MaxMoveSpeed*MaxMoveSpeed)velocity=Vector3.MoveTowards(velocity,desiredVelocity,StopResponse*2f*dt);
   }else{
    velocity.y=0;
   }

   // Vertical physics with Jump Apex Float. Exactly one Move per fixed step:
   // sub-stepping added extra capsule collision resolutions that jittered.
   float grav=Mathf.Abs(vertical)<2.5f?11f:23f;
   vertical-=grav*dt;
   vertical=Mathf.Max(vertical,-20f);
   CollisionFlags contacts=Controller.Move((velocity+Vector3.up*vertical)*dt+platformDisplacement);
   if((contacts&CollisionFlags.Above)!=0&&vertical>0f)vertical=0f;
   platformDisplacement=Vector3.zero;
   bool nowGrounded=Grounded;
   // Ledge-loss bookkeeping for the forgiveness hop (consumed by the jump
   // block next step): stamp the moment Aster walks off an edge.
   if(nowGrounded)ledgeLostAt=0f;else if(wasGrounded)ledgeLostAt=RealmGame.I.Elapsed;
   if(nowGrounded&&plunging){
    LandPlunge();
   }
   if(nowGrounded&&!wasGrounded){
    if(fall<-11f){g.Sound("land_hard");KenneyPuff.Burst(transform.position+Vector3.up*.05f,new Color(.62f,.56f,.46f),12,1f);}
    else if(fall<-4.5f){g.Sound("land_soft");if(fall<-7f)KenneyPuff.Burst(transform.position+Vector3.up*.05f,new Color(.62f,.56f,.46f),10,.8f);}
   }
   if(nowGrounded&&HorizontalSpeed>1.2f){
    stepTimer-=dt;
     if(stepTimer<=0f){stepTimer=HorizontalSpeed>3.5f?.32f:.42f;stepAlt=!stepAlt;g.Sound(stepAlt?"step":"step2");}
   }else{stepTimer=.15f;}
   wasGrounded=nowGrounded;
   Energy=Mathf.Min(100,Energy+dt*(12f+4f*RealmGame.I.Save.aetherRank));
  }

  public void Warp(Vector3 p){platformDisplacement=Vector3.zero;Controller.enabled=false;transform.position=p;Controller.enabled=true;vertical=0;velocity=Vector3.zero;jumpBuffer=0;charging=false;plunging=false;dashUntil=0;attackBufferUntil=0;immuneUntil=RealmGame.I.Elapsed+1.2f;parryUntil=0;parryReady=0;counterUntil=0;RealmGame.I.CameraRig.Snap();}
  public void Push(Vector3 p){Controller.enabled=false;transform.position=p;Controller.enabled=true;}
  public void Bounce(float upwardVelocity,Vector3 horizontalImpulse=default){vertical=upwardVelocity;if(horizontalImpulse.sqrMagnitude>.01f)velocity=horizontalImpulse;jumps=1;airDashes=1+Mathf.Min(3,RealmGame.I.Save.moxieRank);jumpGrace=0;jumpBuffer=0;Visual.Restart("jump");}
  public void Boost(Vector3 impulse){velocity=Vector3.ClampMagnitude(velocity+impulse,14f);airDashes=Mathf.Max(airDashes,1);}
  // Continuous external force (wind vents, geysers): small per-step impulses,
  // no clamp and no air-dash refresh — Boost is for one-shot launches.
  public void ApplyForce(Vector3 impulse){velocity+=impulse;}
  public bool Damage(int damage,Vector3 source){
   plunging=false;
   if(Health<=0)return false;
   if(RealmGame.I.Elapsed<parryUntil){ParrySuccess(source);return false;}
   if(RealmGame.I.Elapsed<immuneUntil){if(RealmGame.I.Elapsed<dashUntil)PerfectDodge(source);return false;}
Health=Mathf.Max(0,Health-damage);RealmGame.I.DamageTaken+=damage;immuneUntil=RealmGame.I.Elapsed+1;
    // Hyper-armor: heavy-weapon swings absorb the hit without flinching or
    // losing ground — the swing commits. Lethal hits always break through.
    bool armored=HyperArmor&&Health>0;
    if(!armored){
     hitUntil=RealmGame.I.Elapsed+.5f;
     Vector3 knock=transform.position-source;knock.y=0;if(knock.sqrMagnitude<.01f)knock=-transform.forward;knock.y=0;velocity=knock.normalized*4.5f;RealmGame.I.Sound("hurt");RealmGame.I.CameraRig.Shake=.18f;RealmGame.I.CameraRig.Kick(knock.normalized,.4f);
     Visual.Restart(Health<=0&&!(RealmGame.I.Save.windRank>windUsed)?"death":(hitState=HitVariant()));
    }else{
     DamageTip.Show(transform.position+Vector3.up*1.9f,"UNSTOPPABLE",new Color(1f,.82f,.35f));
    }
    HitSpark.Burst(transform.position+Vector3.up*.8f,(transform.position-source).normalized,new Color(1f,.2f,.2f),8);
    if(Health<=0&&RealmGame.I.Save.windRank>windUsed){windUsed++;Health=Mathf.Max(3,Mathf.RoundToInt(MaxHealth*.45f));Energy=Mathf.Max(Energy,60);immuneUntil=RealmGame.I.Elapsed+2.2f;RealmGame.I.Tell("SECOND WIND — the realm spirit holds you up.",3);RealmGame.I.Sound("respawn");Vfx.Play("ga_vfx_Heal_02",transform.position+Vector3.up*.9f,Quaternion.identity,1f);}
    else if(Health<=0)RealmGame.I.Defeat();
    return true;
  }

  void Attack(bool charged){
   // Never cut a dodge roll or hit flinch mid-clip: queue instead so the
   // swing starts exactly as the current visual blends out.
   if(RealmGame.I.Elapsed<attackReady||RealmGame.I.Elapsed<dodgeVisualUntil||RealmGame.I.Elapsed<hitUntil){
    attackBufferUntil=RealmGame.I.Elapsed+.3f;attackBufferCharged=charged;return;
   }
   var g=RealmGame.I;var weapon=g.CurrentWeapon;
   string style=WeaponCatalog.AttackStyle(weapon);
   int affinity=WeaponCatalog.Affinity(weapon);
   combo=RealmGame.I.Elapsed<comboUntil?(combo%3)+1:1;
   float baseSpeed=charged?2.55f:combo==2?3.15f:combo==3?2.9f:3.05f;
   float playSpeed=baseSpeed*Mathf.Max(.4f,weapon.Tempo);
   attackState=charged?"charged":"attack_"+Mathf.Clamp(combo,1,3);
   float clipLen=Visual?Visual.ClipLength(attackState,style):0f;
   float attackDuration=clipLen>0f?clipLen/playSpeed:(charged?.84f:(combo==2?.78f:(combo==3?.64f:.66f)))/weapon.Tempo;
   // Archetype perks by weapon family (Sep 2026 combat pass):
   //  heavy (greataxe/hammer grips, Damage>=1.4) = hyper-armor through the swing,
   //  pole (spear/halberd/staff) = tipper sweet-spot, flurry (fists/dagger) =
   //  faster chains + backstab crits, chakram = returning-disc specialist.
   bool heavy=style=="chop"&&weapon.Damage>=1.4f;
   bool pole=style=="spear";
   bool flurry=style=="unarmed"||weapon.Id==WeaponId.RiftDagger;
   if(flurry)attackDuration*=.85f;
   hyperArmorUntil=heavy?RealmGame.I.Elapsed+attackDuration*.8f:0f;
   attackReady=RealmGame.I.Elapsed+attackDuration;
   comboUntil=attackReady+.35f;
    bool isDashStrike=RealmGame.I.Elapsed<dashUntil;
    dashUntil=0;

    // Small facing assist keeps nearby targets usable with a phone stick.
    // Partial turn only: an instant snap reads as a teleport pop.
    Enemy closest=null;float nearest=3.5f;foreach(var foe in g.Enemies){if(!foe||foe.Health<=0)continue;Vector3 d=foe.transform.position-transform.position;d.y=0;if(d.magnitude<nearest&&Vector3.Dot(transform.forward,d.normalized)>.35f){closest=foe;nearest=d.magnitude;}}
    if(closest){Vector3 aim=closest.transform.position-transform.position;aim.y=0;if(aim.sqrMagnitude>.01f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(aim.normalized),.65f);}
    // Forward attack step blended with current momentum (never a teleport
    // pop), softened in air so jump arcs survive the swing.
    Vector3 lungeDir=transform.forward;lungeDir.y=0;
    if(lungeDir.sqrMagnitude<.01f)lungeDir=Vector3.forward;
    lungeDir.Normalize();
    float lungeStep=(isDashStrike?4.2f:charged?3f:2f)*(Grounded?1f:.5f);
    velocity=Vector3.ClampMagnitude(velocity*.55f+lungeDir*lungeStep*.45f,MaxMoveSpeed);velocity.y=0;

     Visual.PlayAttack(combo,charged,weapon.Tempo/(flurry?.85f:1f),style);
     // Armed swings ring steel; bare fists thud — never swords unarmed.
     g.Sound("attack_"+(charged?"charged":style)+"_"+((combo-1)%3+1));
    if(isDashStrike){
     Vfx.Play("ga_vfx_Hyperdrive_01",transform.position+Vector3.up*.9f,Quaternion.LookRotation(transform.forward),.8f);
     DamageTip.Show(transform.position+Vector3.up*1.6f,"DASH STRIKE!",new Color(1f,.95f,.4f));
    }
    if(combo==3&&!charged){
     g.CameraRig.Shake=.2f;
     HitSpark.Burst(transform.position+Vector3.up*1f,transform.forward,new Color(1f,.85f,.3f),16);
     DamageTip.Show(transform.position+Vector3.up*1.8f,"FINISHER!",new Color(1f,.85f,.2f));
    }

    float reach=(charged?3.4f:2.7f)*weapon.Reach;
    float damage=(charged?3.5f:combo==3?2.5f:1.5f)*weapon.Damage;
    if(isDashStrike){damage*=1.35f;reach*=1.2f;}
    damage*=1f+g.Save.arsenalRank*.08f;
    if(RealmGame.I.Elapsed<counterUntil){
     damage*=2.2f+RealmGame.I.Save.tempoRank*.25f;
     counterUntil=0;
     g.HitStop(.16f,.05f);
     g.CameraRig.Shake=.35f;
     g.CameraRig.Kick(transform.forward,.5f);
     g.Sound("impact");
     HitSpark.Burst(transform.position+Vector3.up*1.2f,transform.forward,new Color(1f,.92f,.25f),32);
     Vfx.Play("ga_vfx_Hyperdrive_01",transform.position+Vector3.up*.9f,Quaternion.LookRotation(transform.forward),1.2f);
     DamageTip.Show(transform.position+Vector3.up*2.2f,"CRITICAL RIPOSTE!",new Color(1f,.92f,.25f));
    }
    // Moon Chakram's charged throw: a returning disc that hits on both passes.
    if(charged&&weapon.Id==WeaponId.MoonChakram)ChakramProjectile.Throw(transform.position+Vector3.up*1f,transform.forward,damage,Power);

// Dynamic 3D curved slash arc ribbon + imported particle slash
    SlashArc.Create(transform.position+Vector3.up*.85f,transform.forward,combo,charged,Power);
    Vfx.Play(Vfx.Slash(Power),transform.position+Vector3.up*.85f+transform.forward*.45f,Quaternion.LookRotation(transform.forward),charged?1.25f:.95f);

   foreach(var e in g.Enemies.ToArray()){
    if(!e||e.Health<=0)continue;
    Vector3 delta=e.transform.position-transform.position;delta.y=0;
    if(delta.magnitude<reach+e.Radius&&Vector3.Dot(transform.forward,delta.normalized)>-.2f){
     float dmg=damage+(g.Save.powerRank*.18f);
     if(affinity>=0&&affinity==e.WeakElement)dmg*=1.15f;
     // Spear tipper: strikes landing in the outer ~40% of reach hit harder,
     // close-in pokes are weaker — rewards spacing.
     if(pole)dmg*=delta.magnitude>reach*.62f?1.3f:.85f;
     // Dagger/fist backstab: striking a foe from behind is a critical.
     if(flurry&&Vector3.Dot(e.transform.forward,transform.forward)>.45f){
      dmg*=1.6f;
      DamageTip.Show(e.transform.position+Vector3.up*(e.Boss?2.4f:1.6f),"BACKSTAB!",new Color(.6f,1f,.55f));
     }
     e.Hit(dmg,Power,false,transform.forward,heavy);
    }
   }
   // The blade also smashes breakable crates/barrels in reach.
   for(int i=BreakableCrate.All.Count-1;i>=0;i--){
    var c=BreakableCrate.All[i];if(!c)continue;
    Vector3 d=c.transform.position-transform.position;d.y=0;
    if(d.magnitude<reach+.6f&&Vector3.Dot(transform.forward,d.normalized)>-.2f)c.Break();
   }
  }

  void StartPlunge(){
   var g=RealmGame.I;
   plunging=true;charging=false;
   vertical=-20f;
   velocity=Vector3.ClampMagnitude(velocity*.2f,1.5f);
   attackState="charged";
   attackReady=g.Elapsed+1.5f;
   g.Sound(WeaponCatalog.AttackStyle(g.CurrentWeapon)=="unarmed"?"punch":"sword_slash");
   Vfx.Play(Vfx.Slash(Power),transform.position+Vector3.up*.9f,Quaternion.Euler(90,0,0),1.1f);
   DamageTip.Show(transform.position+Vector3.up*1.7f,"AIR PLUNGE!",new Color(1f,.85f,.3f));
  }
  void LandPlunge(){
   plunging=false;
   var g=RealmGame.I;
   attackReady=g.Elapsed+.25f;
   g.CameraRig.Shake=.42f;
   g.CameraRig.Kick(Vector3.down,.6f);
   g.Sound("land_hard");g.Sound("impact");
   Color elemColor=Power==0?new Color(1f,.45f,.1f):Power==1?new Color(.2f,.85f,1f):new Color(.2f,1f,.55f);
   Vfx.Play("ga_vfx_Shockwave_01",transform.position+Vector3.up*.15f,Quaternion.identity,1.6f);
   Vfx.Play("ga_vfx_Explosion_01",transform.position+Vector3.up*.25f,Quaternion.identity,1.1f);
   HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.up,elemColor,28);
   ImpactMarks.Place(transform.position,1.3f,elemColor);
   DamageTip.Show(transform.position+Vector3.up*1.8f,"EARTH SHATTER!",elemColor);
   var weapon=g.CurrentWeapon;
   float pReach=4.8f*weapon.Reach;
   float pDmg=3.8f*weapon.Damage*(1f+g.Save.arsenalRank*.08f);
   foreach(var foe in g.Enemies.ToArray()){
    if(!foe||foe.Health<=0)continue;
    Vector3 toFoe=foe.transform.position-transform.position;toFoe.y=0;
    if(toFoe.magnitude<pReach+foe.Radius){
     foe.Hit(pDmg,Power,true,toFoe.normalized);
     foe.Stun(.8f);
    }
   }
   for(int i=BreakableCrate.All.Count-1;i>=0;i--){
    var c=BreakableCrate.All[i];if(!c)continue;
    Vector3 d=c.transform.position-transform.position;d.y=0;
    if(d.magnitude<pReach)c.Break();
   }
   if(Power==2){
    foreach(var pickup in FindObjectsByType<RealmPickup>(FindObjectsSortMode.None)){
     if(pickup&&Vector3.Distance(transform.position,pickup.transform.position)<14f){
      pickup.transform.position=Vector3.MoveTowards(pickup.transform.position,transform.position+Vector3.up*.8f,18f*Time.deltaTime);
     }
    }
   }
  }

   // Timing-based guard: a short window that negates one incoming hit, stuns the
   // attacker and opens a counter. Armed Aster raises a real block; unarmed
   // Aster braces in the charged pose until a dedicated animation is baked.
   string HitVariant(){int r=Random.Range(0,5);return r==0?"hit":r==1?"hit_2":r==2?"hit_3":r==3?"hit_4":"hit_5";}
   string ParryState(){return WeaponCatalog.AttackStyle(RealmGame.I.CurrentWeapon)=="unarmed"?"charged":"block";}
   void Parry(){
   float now=RealmGame.I.Elapsed;if(now<parryReady||!Grounded)return;
   parryUntil=now+.2f;parryReady=now+.55f;
   RealmGame.I.Sound("player_dash");
   Visual.Restart(ParryState());
   HitSpark.Burst(transform.position+Vector3.up*.9f+transform.forward*.4f,transform.forward,new Color(.8f,.92f,1f),6);
  }
  void ParrySuccess(Vector3 source){
   var g=RealmGame.I;parryUntil=0;parryReady=g.Elapsed+.55f;
   g.HitStop(.14f,.08f);g.CameraRig.Shake=.3f;g.Sound("impact");g.Haptic();
   Energy=Mathf.Min(100,Energy+20+RealmGame.I.Save.tempoRank*5);counterUntil=g.Elapsed+1.6f;
   HitSpark.Burst(transform.position+Vector3.up*.95f,-transform.forward,new Color(1f,.96f,.72f),28);
   DamageTip.Show(transform.position+Vector3.up*1.95f,"PARRY!",new Color(1f,.94f,.6f));
   var foe=NearestEnemy(source,3.2f);if(foe)foe.Stun(1.1f);
   Visual.Restart(ParryState());
   Vfx.Play("ga_vfx_Shield_01",transform.position+Vector3.up*1f,Quaternion.identity,.9f);
  }
  // A dodge that actually shrugs off a hit during its active i-frame window
  // rewards the player with energy and a counter opening.
  void PerfectDodge(Vector3 source){
   if(dodgeRewarded)return;dodgeRewarded=true;
   var g=RealmGame.I;g.HitStop(.1f,.1f);g.CameraRig.Shake=.2f;g.Sound("player_dash");g.Haptic();
   Energy=Mathf.Min(100,Energy+30);counterUntil=g.Elapsed+1.4f;
   HitSpark.Burst(transform.position+Vector3.up*.85f,-transform.forward,new Color(.7f,.96f,1f),24);
   DamageTip.Show(transform.position+Vector3.up*1.95f,"PERFECT DODGE",new Color(.65f,.95f,1f));
  }
  Enemy NearestEnemy(Vector3 source,float range){
   Enemy best=null;float closest=range;
   foreach(var foe in RealmGame.I.Enemies){if(!foe||foe.Health<=0)continue;float d=Vector3.Distance(foe.transform.position,source);if(d<closest){closest=d;best=foe;}}
   return best;
  }

  // Spellbolt skill (F key / SPELL touch button): a fast elemental projectile
  // with soft homing toward the enemy ahead. Shares the Power element (Q) and
  // scales with powerRank like every other elemental hit, so weaknesses and
  // reactions apply automatically through Enemy.Hit.
  void CastSpell(){
   var g=RealmGame.I;
   if(g.Elapsed<spellUntil)return;
   const float cost=15f;
   if(Energy<cost){g.Sound("power_fail");g.Tell("Wait for your energy to recharge.");spellUntil=g.Elapsed+.3f;return;}
   Energy-=cost;spellUntil=g.Elapsed+.8f;
   g.Sound(Power==0?"ember_cast":Power==1?"frost_cast":"gale_cast");
   Visual.Restart("cast");
   Vector3 dir=transform.forward;dir.y=0;if(dir.sqrMagnitude<.01f)dir=Vector3.forward;dir.Normalize();
   Vector3 origin=transform.position+Vector3.up*1.1f+dir*.5f;
   Vfx.Play("ga_vfx_MuzzleFlash_01",origin,Quaternion.LookRotation(dir),.8f);
   SpellBolt.Create(origin,dir,Power);
  }
  // Charged spell: a lobbed arc that detonates in an area on landing.
  void LobSpell(){
   var g=RealmGame.I;
   if(g.Elapsed<spellUntil)return;
   const float cost=20f;
   if(Energy<cost){g.Sound("power_fail");g.Tell("Wait for your energy to recharge.");spellUntil=g.Elapsed+.3f;return;}
   Energy-=cost;spellUntil=g.Elapsed+.95f;
   g.Sound(Power==0?"ember_cast":Power==1?"frost_cast":"gale_cast");
   Visual.Restart("cast");
   Vector3 dir=transform.forward;dir.y=0;if(dir.sqrMagnitude<.01f)dir=Vector3.forward;dir.Normalize();
   Vector3 origin=transform.position+Vector3.up*1.1f+dir*.5f;
   Vfx.Play("ga_vfx_MuzzleFlash_01",origin,Quaternion.LookRotation(dir),1f);
   SpellBolt.Create(origin,dir,Power,true);
  }
  // Grapple: hooks the next route island ahead and pulls Aster toward it.
  void TryGrapple(){
   var g=RealmGame.I;if(g==null||g.World==null||g.World.Route==null)return;
   Vector3 p=transform.position;Vector3 best=Vector3.zero;float bestScore=float.MaxValue;bool found=false;
   foreach(var node in g.World.Route){
    float dz=node.z-p.z;if(dz<1.5f)continue;float d=Vector3.Distance(node,p);if(d>20f)continue;
    if(d<bestScore){bestScore=d;best=node;found=true;}
   }
   if(!found)return;
   pullPoint=best+Vector3.up*1.4f;pullUntil=g.Elapsed+.5f;
   velocity=Vector3.zero;vertical=Mathf.Max(vertical,4f);
   g.Sound("player_dash");
   Vfx.Play("ga_vfx_Lightning_02",transform.position+Vector3.up*1f,Quaternion.identity,.7f);
   HitSpark.Burst(pullPoint,p-pullPoint,new Color(.7f,.95f,1f),10);
  }
  void Cast(){
   var g=RealmGame.I;float cost=28-g.Save.powerRank*2;
   if(Energy<cost){g.Sound("power_fail");g.Tell("Wait for your energy to recharge.");return;}
   Energy-=cost;
   g.Sound(Power==0?"ember_cast":Power==1?"frost_cast":"gale_cast");

   // Cast elemental burst
   Color elemColor=Power==0?new Color(1f,.38f,.1f):Power==1?new Color(.22f,.85f,1f):new Color(.3f,1f,.62f);
   SlashArc.Create(transform.position+Vector3.up*.5f,transform.forward,0,true,Power);
   HitSpark.Burst(transform.position+Vector3.up*.8f,Vector3.up,elemColor,16);
   Vfx.Play(Power==0?"ga_vfx_Flames_01":Power==1?"ga_vfx_Explosion_02":"ga_vfx_Tornado_01",transform.position+Vector3.up*.9f,Quaternion.identity,Power==2?1.3f:.9f);

   foreach(var e in g.Enemies.ToArray())
    if(e&&e.Health>0&&Vector3.Distance(transform.position,e.transform.position)<6.5f)
     e.Hit(2.5f+g.Save.powerRank*.5f,Power,true,(e.transform.position-transform.position).normalized);

   if(Power==2){
    foreach(var p in FindObjectsByType<EnemyBolt>())
     if(Vector3.Distance(transform.position,p.transform.position)<7.5f)Destroy(p.gameObject);
     vertical=Mathf.Max(vertical,6f);
   }
  }
  // CharacterController shoves dynamic props (pushable blocks) on contact.
  void OnControllerColliderHit(ControllerColliderHit hit){
   var rb=hit.rigidbody;if(!rb||rb.isKinematic||!rb.GetComponent<PushableBlock>()||hit.moveDirection.y<-.3f)return;
   Vector3 push=hit.moveDirection;push.y=0;
   Vector3 horizontal=rb.linearVelocity;horizontal.y=0;
   rb.AddForce(Vector3.ClampMagnitude(push*2.6f-horizontal,.35f),ForceMode.VelocityChange);
  }
 }

  // Hero spell bolt: fast elemental projectile with soft homing. Its look is
  // built from in-project assets — a tinted shard + Kenney flare core, a smoke
  // trail, and one attached element shell (eric fireball / mayker water /
  // mayker electric projectiles). Impacts reuse the shared puff + sparks.
  public class SpellBolt:MonoBehaviour {
   Vector3 direction;float speed=14f,life=2.2f,damage,coreBase=.3f,phase;int power,rank;Transform core,flare;
   bool lob;float vy,detonateY;
   static int spawnCount;
   static Mesh shardMesh,quadMesh;
   static readonly System.Collections.Generic.Dictionary<string,Material> spellMats=new System.Collections.Generic.Dictionary<string,Material>();
   static Mesh Shard(){
    if(shardMesh)return shardMesh;
    var m=new Mesh{name="Spell shard"};
    m.vertices=new[]{new Vector3(0,.5f,0),new Vector3(0,-.5f,0),new Vector3(.22f,0,0),new Vector3(-.22f,0,0),new Vector3(0,0,.22f),new Vector3(0,0,-.22f)};
    m.triangles=new[]{0,2,4, 0,4,3, 0,3,5, 0,5,2, 1,4,2, 1,3,4, 1,5,3, 1,2,5};
    m.RecalculateNormals();m.RecalculateBounds();shardMesh=m;return m;
   }
   static Mesh Quad(){
    if(quadMesh)return quadMesh;
    var m=new Mesh{name="Spell quad"};
    m.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};
    m.triangles=new[]{0,1,2,0,2,3};m.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
    m.RecalculateNormals();m.RecalculateBounds();quadMesh=m;return m;
   }
   static Material SpellGlow(Color c){
    string key=ColorUtility.ToHtmlStringRGB(c);
    if(spellMats.TryGetValue(key,out var m))return m;
    m=new Material(Shader.Find("Standard")){name="Spell glow "+key};
    m.SetColor("_Color",c);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*1.8f);
    m.SetFloat("_Metallic",0f);m.SetFloat("_Glossiness",.4f);spellMats[key]=m;return m;
   }
   static Color ElemColor(int power,int rank=0){
    Color c=power==0?new Color(1,.45f,.12f):power==1?new Color(.25f,.85f,1f):new Color(.35f,1f,.65f);
    return Color.Lerp(c,Color.white,Mathf.Clamp01(rank*.14f));
   }
   static string ShellFor(int power)=>power==0?"eric_FX_Fireball":power==1?"mayker_Slash Projectile VFX Water":"mayker_Slash Projectile VFX Eletric";
   // (ShellFor retained for reference; the Eric pack ships URP shaders which
   // render magenta in this BIRP project, so bolts are code-built only.)
   public static void Create(Vector3 p,Vector3 dir,int power,bool lob=false){
    var g=RealmGame.I;
    var go=new GameObject(lob?"Hero lob":"Hero spell");go.transform.SetParent(g.World.transform,false);go.transform.position=p;
    int rank=g.Save.powerRank;
    Color color=ElemColor(power,rank);
    var bolt=go.AddComponent<SpellBolt>();bolt.direction=dir;bolt.power=power;bolt.rank=rank;bolt.lob=lob;
    if(lob){bolt.speed=8.5f;bolt.life=3f;bolt.vy=5.6f;bolt.detonateY=p.y-.2f;}
    bolt.damage=2.2f+rank*.45f+(lob?.8f:0f);
    bolt.phase=(spawnCount%100)*0.0628f;spawnCount++;
    bolt.coreBase=.3f*(1f+.18f*rank);
    var coreGo=new GameObject("Spell core");coreGo.transform.SetParent(go.transform,false);
    coreGo.AddComponent<MeshFilter>().sharedMesh=Shard();
    var cr=coreGo.AddComponent<MeshRenderer>();cr.sharedMaterial=SpellGlow(color);cr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;cr.receiveShadows=false;
    coreGo.transform.localScale=Vector3.one*bolt.coreBase;bolt.core=coreGo.transform;
    var flareTex=Resources.Load<Texture2D>("VFX/Textures/flare_01");
    if(flareTex){
     var fl=new GameObject("Spell flare");fl.transform.SetParent(go.transform,false);
     fl.AddComponent<MeshFilter>().sharedMesh=Quad();
     var mr=fl.AddComponent<MeshRenderer>();var fm=new Material(Shader.Find("Sprites/Default"));fm.mainTexture=flareTex;fm.color=color;mr.sharedMaterial=fm;
     mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;
     fl.transform.localScale=Vector3.one*.8f*(1f+.2f*rank);bolt.flare=fl.transform;
    }
    var smoke=Resources.Load<Texture2D>("VFX/Textures/smoke_04");
    if(smoke){
     var tr=new GameObject("Spell trail");tr.transform.SetParent(go.transform,false);
     var ps=tr.AddComponent<ParticleSystem>();
     var main=ps.main;main.playOnAwake=false;ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
     main.loop=false;main.duration=10;main.startLifetime=.45f;main.startSpeed=0f;main.startSize=.35f+.08f*rank;main.maxParticles=24;main.startColor=color;main.simulationSpace=ParticleSystemSimulationSpace.World;
     var em=ps.emission;em.rateOverTime=28+10*rank;
     var sh=ps.shape;sh.enabled=false;
     var pr=tr.GetComponent<ParticleSystemRenderer>();var tm=new Material(Shader.Find("Sprites/Default"));tm.mainTexture=smoke;tm.color=color;pr.material=tm;
     ps.Play();
    }
   }
   void Update(){
    var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;
    Enemy best=null;float bestScore=.93f;
    foreach(var e in g.Enemies){
     if(!e||e.Health<=0)continue;Vector3 to=e.transform.position+Vector3.up*.8f-transform.position;
     float dist=to.magnitude;if(dist>8)continue;
     float dot=Vector3.Dot(direction,to.normalized);if(dot>bestScore){bestScore=dot;best=e;}
    }
    if(best){Vector3 want=(best.transform.position+Vector3.up*.8f-transform.position).normalized;direction=Vector3.Slerp(direction,want,Mathf.Min(1f,Time.deltaTime*2f)).normalized;}
    float step=speed*Time.deltaTime;
    Vector3 perp=Vector3.Cross(direction,Vector3.up);if(perp.sqrMagnitude<.01f)perp=Vector3.right;perp.Normalize();
    float wob=Mathf.Sin(g.Elapsed*9f+phase)*.12f;
    if(core){core.Rotate(320*Time.deltaTime,410*Time.deltaTime,0);core.localPosition=perp*wob;core.localScale=Vector3.one*coreBase*(1f+.18f*Mathf.Sin(g.Elapsed*14f+phase));}
    if(flare){flare.localPosition=perp*wob;if(Camera.main)flare.rotation=Camera.main.transform.rotation;}
    life-=Time.deltaTime;
    if(lob){
     vy-=20f*Time.deltaTime;
     transform.position+=(direction*speed+Vector3.up*vy)*Time.deltaTime;
     foreach(var e in g.Enemies.ToArray()){
      if(!e||e.Health<=0)continue;Vector3 d=e.transform.position-transform.position;
      if(new Vector2(d.x,d.z).magnitude<e.Radius+.6f&&Mathf.Abs(d.y)<1.8f){Detonate();return;}
     }
     if(vy<0f&&transform.position.y<detonateY){Detonate();return;}
    }else{
     transform.position+=direction*step;
     foreach(var e in g.Enemies.ToArray()){
      if(!e||e.Health<=0)continue;Vector3 d=e.transform.position-transform.position;
      if(new Vector2(d.x,d.z).magnitude<e.Radius+.38f&&Mathf.Abs(d.y)<1.6f){
       e.Hit(damage,power,true,direction);
       HitSpark.Burst(transform.position,direction,new Color(1f,.9f,.6f),8);
       KenneyPuff.Burst(transform.position,ElemColor(power,rank),10+4*rank,.8f+.35f*rank);
       ImpactMarks.Place(transform.position,1f,ElemColor(power,rank));
       Destroy(gameObject);return;
      }
     }
    }
    life-=Time.deltaTime;
    if(life<=0){if(lob)Detonate();else Destroy(gameObject);}
   }
   void Detonate(){
    var g=RealmGame.I;
    for(int i=0;i<g.Enemies.Count;i++){var e=g.Enemies[i];if(!e||e.Health<=0)continue;if(Vector3.Distance(e.transform.position,transform.position)<2.8f)e.Hit(damage,power,true,(e.transform.position-transform.position).normalized);}
    HitSpark.Burst(transform.position,Vector3.up,new Color(1f,.9f,.6f),18);
    Vfx.Play("ga_vfx_Explosion_01",transform.position,Quaternion.identity,1.1f);
    KenneyPuff.Burst(transform.position,ElemColor(power,rank),18,1.4f);
    ImpactMarks.Place(transform.position,2f,ElemColor(power,rank));
    Destroy(gameObject);
   }
  }
  // Moon Chakram's charged throw: an outward disc that returns to Aster,
  // damaging on both passes. Ricochets read as a real weapon identity rather
  // than another melee arc.
  public class ChakramProjectile:MonoBehaviour {
   Vector3 dir;float damage,age,outTime=.45f;int power;bool returning;Transform disc,player;
   readonly System.Collections.Generic.Dictionary<Enemy,float> lastHit=new System.Collections.Generic.Dictionary<Enemy,float>();
   public static void Throw(Vector3 p,Vector3 forward,float dmg,int pow){
    var g=RealmGame.I;if(g==null||g.World==null)return;
    var go=new GameObject("Moon chakram");go.transform.SetParent(g.World.transform,false);go.transform.position=p;
    var c=go.AddComponent<ChakramProjectile>();c.dir=forward;c.damage=dmg;c.power=pow;c.player=g.Player?g.Player.transform:null;
    Color color=pow==0?new Color(1,.5f,.15f):pow==1?new Color(.3f,.9f,1f):new Color(.45f,1f,.7f);
    var dgo=GameObject.CreatePrimitive(PrimitiveType.Cylinder);dgo.name="Chakram disc";
    var col=dgo.GetComponent<Collider>();if(col)Destroy(col);
    dgo.transform.SetParent(go.transform,false);
    dgo.transform.localScale=new Vector3(.55f,.05f,.55f);
    var mr=dgo.GetComponent<MeshRenderer>();
    var m=new Material(Shader.Find("Standard")){name="Chakram glow"};m.SetColor("_Color",color);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*1.6f);m.SetFloat("_Metallic",.2f);m.SetFloat("_Glossiness",.5f);
    mr.sharedMaterial=m;mr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;mr.receiveShadows=false;
    c.disc=dgo.transform;
   }
   void Update(){
    var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;
    age+=Time.deltaTime;
    if(!returning){transform.position+=dir*15f*Time.deltaTime;if(age>=outTime)returning=true;}
    else{
     if(player){Vector3 to=player.position+Vector3.up*1f-transform.position;float d=to.magnitude;
      if(d<1.1f){Destroy(gameObject);return;}
      transform.position+=to.normalized*18f*Time.deltaTime;}
    }
    if(disc)disc.Rotate(0,900f*Time.deltaTime,0);
    foreach(var e in g.Enemies.ToArray()){
     if(!e||e.Health<=0)continue;Vector3 d=e.transform.position-transform.position;
     if(new Vector2(d.x,d.z).magnitude<e.Radius+.55f&&Mathf.Abs(d.y)<1.8f){
      float last;lastHit.TryGetValue(e,out last);
      if(Time.time-last<.25f)continue;
      lastHit[e]=Time.time;
      e.Hit(damage,power,true,(e.transform.position-transform.position).normalized);
      HitSpark.Burst(transform.position,d.normalized,new Color(1f,.95f,.7f),8);
      KenneyPuff.Burst(transform.position,new Color(1f,.9f,.6f),6,.6f);
     }
    }
    if(age>3.5f)Destroy(gameObject);
   }
  }
  public class CharacterVisual:MonoBehaviour {
  public Animator animator;PlayableGraph graph;AnimationMixerPlayable mixer;AnimationClipPlayable[] playable=new AnimationClipPlayable[2];AnimationClip[] clips;string current="";float blend;int slot;bool hasGraph;Transform fallbackBody;
  // Extra per-role clips (boss movesets): attack_2/victory/gethit/dizzy loaded
  // from Animations/<role>_<state> for rigged enemy roles with authored extras.
  public readonly System.Collections.Generic.Dictionary<string,AnimationClip> extraClips=new System.Collections.Generic.Dictionary<string,AnimationClip>();
  public bool UsesFallback=>fallbackBody!=null;
  // Per-weapon attack style variants (chop/spear/unarmed × combo 1-3), baked by
  // AsterPhase1 alongside the base set. Entries stay null when a style file is
  // missing and playback falls back to the slash clips, so a partial bake set
  // can never break attacks.
  AnimationClip[] chopClips=new AnimationClip[3],spearClips=new AnimationClip[3],unarmedClips=new AnimationClip[3];
  public string CurrentState=>current;
  bool lavaBoss;
  static readonly string[] Names={"idle","walk","run","attack_1","attack_2","attack_3","charged","jump","double_jump","dodge","hit","death","block","hit_2","hit_3","cast","roll","hit_4","hit_5"};

  public static CharacterVisual Create(string role,Transform parent,float height,Color color){
   var holder=new GameObject(role+" visual");holder.transform.SetParent(parent,false);var v=holder.AddComponent<CharacterVisual>();
   v.lavaBoss=role=="LavaBoss";
   GameObject modelRef=null;
   var prefab=Resources.Load<GameObject>("Characters/"+role);
   if(prefab){
    var model=Instantiate(prefab,holder.transform);
    model.transform.localPosition=Vector3.zero;
    modelRef=model;
    if(role=="Aster"){
     var asterMaterial=Resources.Load<Material>("Materials/Aster");
     if(!asterMaterial)throw new System.Exception("Missing runtime Aster material");
     foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=asterMaterial;
    }else{
     var skin=CharacterSkin(role);
     if(skin)foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=skin;
    }
    v.animator=model.GetComponentInChildren<Animator>();
    if(v.animator){
     v.animator.applyRootMotion=false;
     v.animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
    }
    // The supplied Aster mesh already includes a sword; avoid attaching a second weapon.
    if(role=="Aster"&&model.GetComponentsInChildren<SkinnedMeshRenderer>().Length==0){
     Transform rightHand=null;
     foreach(var t in model.GetComponentsInChildren<Transform>()){
      if(t.name.Equals("hand_R",System.StringComparison.OrdinalIgnoreCase)||t.name.EndsWith("hand_r",System.StringComparison.OrdinalIgnoreCase)||t.name.EndsWith("RightHand",System.StringComparison.OrdinalIgnoreCase)){
       rightHand=t;break;
      }
     }
     if(rightHand!=null)HeroWeapon.Attach(rightHand);
    }
   }else{
    v.fallbackBody=Art.Shape("Body",PrimitiveType.Capsule,Vector3.up*.9f,new Vector3(.65f,.65f,.5f),color,holder.transform).transform;
    Art.Shape("Head",PrimitiveType.Sphere,Vector3.up*1.65f,Vector3.one*.45f,new Color(.85f,.71f,.52f),holder.transform);
    Art.Shape("Blade",PrimitiveType.Cube,new Vector3(.5f,.9f,.28f),new Vector3(.1f,.9f,.1f),Color.white,holder.transform);
    holder.transform.localScale=Vector3.one*height/1.8f;
   }

   v.clips=new AnimationClip[Names.Length];
   // The supplied Mixamo Aster clips are generated into Resources/Animations/Aster.
   // Other characters retain the existing shared fallback set.
    for(int i=0;i<Names.Length;i++){
     if(role=="Aster")v.clips[i]=Resources.Load<AnimationClip>("Animations/"+role+"/"+Names[i]);
     else{
      string legacy=Names[i].StartsWith("attack_")?"attack":Names[i]=="charged"?"attack":Names[i]=="run"?"walk":Names[i];
      v.clips[i]=Resources.Load<AnimationClip>("Animations/"+role+"/"+Names[i])
        ??Resources.Load<AnimationClip>("Animations/"+role+"/"+legacy)
        ??(role=="Footman"?Resources.Load<AnimationClip>("Animations/Elite/"+legacy):null)
        ??Resources.Load<AnimationClip>("Animations/"+role+"_"+Names[i])
        ??Resources.Load<AnimationClip>("Animations/"+role+"_"+legacy)
        ??Resources.Load<AnimationClip>("Animations/Shared/"+legacy);
     }
    }
   if(role=="Aster"&&System.Array.Exists(v.clips,c=>c==null))throw new System.Exception("Aster Phase 1 animation set is incomplete");
   if(role=="Aster")for(int s=0;s<3;s++){
    v.chopClips[s]=Resources.Load<AnimationClip>("Animations/Aster/chop_"+(s+1));
    v.spearClips[s]=Resources.Load<AnimationClip>("Animations/Aster/spear_"+(s+1));
    v.unarmedClips[s]=Resources.Load<AnimationClip>("Animations/Aster/unarmed_"+(s+1));
   }
   if(role!="Aster")foreach(var st in new[]{"attack_2","victory","gethit","dizzy","run_fast"}){
    var extra=Resources.Load<AnimationClip>("Animations/"+role+"_"+st);
    if(extra)v.extraClips[st]=extra;
   }
   if(role!="Aster"){var runFast=Resources.Load<AnimationClip>("Animations/"+role+"_run");if(runFast)v.extraClips["run_fast"]=runFast;}
   // Removed dynamic Animator addition. Prefabs should contain their own Animators if they are animated.
   // Normalize any FBX model to the requested role height and ground it on its
   // real bounds — pack FBX ship arbitrary scales and pivot offsets (the golem
   // imported at 473 units with feet 1.3 below origin). Aster is exempt: its
   // prefab scale is baked by AsterPhase1.
    if(modelRef&&role!="Aster"){
     var renderers=modelRef.GetComponentsInChildren<Renderer>(true);
     if(renderers.Length>0){
      Bounds nb=renderers[0].bounds;
      for(int i=1;i<renderers.Length;i++)nb.Encapsulate(renderers[i].bounds);
      if(nb.size.y>.01f){
       float s=height/nb.size.y;
       modelRef.transform.localScale=Vector3.one*s;
       // Re-measure AFTER scaling, then shift in WORLD space so the mesh is
       // centered and grounded on the holder. Renderer.bounds is world
       // space — never fold it into localPosition: far down-route that
       // teleports the visible body back toward the world origin, and the
       // displaced mesh then swings wide whenever the enemy turns
       // ("flying enemies").
       nb=renderers[0].bounds;
       for(int i=1;i<renderers.Length;i++)nb.Encapsulate(renderers[i].bounds);
       Vector3 anchor=new Vector3(nb.center.x,nb.min.y,nb.center.z);
       modelRef.transform.position-=anchor-modelRef.transform.parent.position;
      }
     }
    }

   if(v.animator&&System.Array.Exists(v.clips,c=>c!=null)){
    v.graph=PlayableGraph.Create(role+" motion");v.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
    v.mixer=AnimationMixerPlayable.Create(v.graph,2);
    var output=AnimationPlayableOutput.Create(v.graph,"Character",v.animator);
    output.SetSourcePlayable(v.mixer);
    v.hasGraph=true;v.graph.Play();
   }
   return v;
  }

  // New enemy families ship as decimated showcase meshes with a basecolor baked
  // to Resources/Characters/Textures. Bind it at runtime; roles without a texture
  // (the original prefab families) are left untouched.
  static readonly System.Collections.Generic.Dictionary<string,Material> skins=new System.Collections.Generic.Dictionary<string,Material>();
  static Material CharacterSkin(string role){
   if(skins.TryGetValue(role,out var cached))return cached;
   var texture=Resources.Load<Texture2D>("Characters/Textures/"+role+"_basecolor");
   if(!texture){skins[role]=null;return null;}
   var mat=new Material(Shader.Find("Standard")){name=role,color=Color.white};
   mat.mainTexture=texture;mat.SetFloat("_Metallic",.05f);mat.SetFloat("_Glossiness",.26f);
   skins[role]=mat;return mat;
  }

  public void Restart(string state){current="";Play(state);}
  // Manual playback-speed override for controller-driven movement (boss charge).
  public void SetCurrentSpeed(float s){if(hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(s);}
  public void PlayTimed(string state,float duration){
   Restart(state);
   if(hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(playable[slot].GetAnimationClip().length/Mathf.Max(.1f,duration));
  }
  public void PlayBossAction(string state,float duration){
   var clip=Resources.Load<AnimationClip>("Animations/LavaBoss/"+state);
   current="";PlayClip(clip,state);
   if(clip&&hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(clip.length/Mathf.Max(.1f,duration));
  }
  public void PlayAttack(int combo,bool charged,float tempo=1f,string style="slash"){
   current="";string state=charged?"charged":"attack_"+Mathf.Clamp(combo,1,3);
   int ci=System.Array.IndexOf(Names,state);
   AnimationClip clip=ci>=0&&clips!=null&&ci<clips.Length?clips[ci]:null;
   AnimationClip styled=StyleClip(state,style,combo);
   PlayClip(styled?styled:clip,state);
   float baseSpeed=charged?2.55f:combo==2?3.15f:combo==3?2.9f:3.05f;
   if(hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(baseSpeed*Mathf.Max(.4f,tempo));
  }
  AnimationClip StyleClip(string state,string style,int combo){
   if(!state.StartsWith("attack_"))return null;
   AnimationClip[] set=style=="chop"?chopClips:style=="spear"?spearClips:style=="unarmed"?unarmedClips:null;
   if(set!=null&&combo>=1&&combo<=3&&set[combo-1])return set[combo-1];
   return null;
  }
  public float ClipLength(string state){int i=System.Array.IndexOf(Names,state);if(i>=0&&clips!=null&&i<clips.Length&&clips[i]!=null)return clips[i].length;return 0f;}
  public float ClipLength(string state,string style){
   if(state.StartsWith("attack_")){
    int combo=state.Length>7?(state[7]-'0'):1;
    AnimationClip styled=StyleClip(state,style,combo);
    if(styled)return styled.length;
   }
   return ClipLength(state);
  }
  public void PlayWalk(float speedRatio=1f){
   // Hysteresis keeps a thumbstick near the walk/run threshold from restarting
   // clips every frame and making the character appear to twitch.
   string locomotion=speedRatio>.72f||(current=="run"&&speedRatio>.58f)?"run":"walk";Play(locomotion);
   if(hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(locomotion=="run"?Mathf.Lerp(.76f,1.02f,Mathf.InverseLerp(.68f,1f,speedRatio)):Mathf.Lerp(.62f,.9f,Mathf.InverseLerp(.08f,.68f,speedRatio)));
  }
  public void Play(string state){
   if(state=="attack")state="attack_1";
   if(current==state)return;
   int i=System.Array.IndexOf(Names,state);AnimationClip clip=i>=0?clips[i]:null;
   if(extraClips.TryGetValue(state,out var extra))clip=extra;
   if(lavaBoss&&!clip)clip=Resources.Load<AnimationClip>("Animations/LavaBoss/"+state);
   PlayClip(clip,state);
  }
  void PlayClip(AnimationClip clip,string state){
   bool stride=(current=="walk"||current=="run")&&(state=="walk"||state=="run");
   double phase=0;
   if(stride&&hasGraph&&playable[slot].IsValid()){var oldClip=playable[slot].GetAnimationClip();if(oldClip&&oldClip.length>0)phase=(playable[slot].GetTime()/oldClip.length)%1.0;}
   current=state;if(!hasGraph)return;
   if(!clip)clip=clips[0]?clips[0]:clips[1];if(!clip)return;
   slot=1-slot;
   if(playable[slot].IsValid()){mixer.DisconnectInput(slot);graph.DestroyPlayable(playable[slot]);}
   playable[slot]=AnimationClipPlayable.Create(graph,clip);
   playable[slot].SetApplyFootIK(false);
   if(state=="jump")playable[slot].SetSpeed(1.9f);
   if(state=="double_jump")playable[slot].SetSpeed(3.55f);
   if(state=="dodge"||state=="roll")playable[slot].SetSpeed(3.35f);
   if(state=="run_fast")playable[slot].SetSpeed(1.7f);
   if(state=="victory"||state=="gethit"||state=="dizzy")playable[slot].SetSpeed(1.15f);
   if(state=="hit"||state.StartsWith("hit_"))playable[slot].SetSpeed(clip.length/.5f);
   playable[slot].SetTime(stride?phase*clip.length:0);
   graph.Connect(playable[slot],0,mixer,slot);blend=0;mixer.SetInputWeight(slot,0f);mixer.SetInputWeight(1-slot,1f);
  }

  void Update(){
   if(hasGraph){
    float speed=RealmGame.I.Screen==GameScreen.Playing?1:0;
    graph.GetRootPlayable(0).SetSpeed(speed);
    float blendRate=current=="dodge"||current=="roll"||current=="hit"||current=="hit_2"||current=="hit_3"||current=="hit_4"||current=="hit_5"||current=="block"?16f:current=="double_jump"?15f:current.StartsWith("attack_")||current=="charged"||current=="cast"?12f:9f;
    blend=Mathf.MoveTowards(blend,1,Time.deltaTime*blendRate);
    mixer.SetInputWeight(slot,blend);mixer.SetInputWeight(1-slot,1-blend);
   }else if(fallbackBody){
    float bob=current=="walk"?Mathf.Sin(RealmGame.I.Elapsed*11)*.07f:Mathf.Sin(RealmGame.I.Elapsed*2)*.015f;
    fallbackBody.localPosition=Vector3.up*(.9f+bob);
   }
  }
  void OnDestroy(){if(hasGraph&&graph.IsValid())graph.Destroy();}
 }

 public class HeroWeapon:MonoBehaviour {
  public static HeroWeapon Instance;
  Renderer runeRenderer;Material runeMat;int lastPower=-1;
  public static void Attach(Transform hand){
   var go=new GameObject("HeroBlade");
   go.transform.SetParent(hand,false);
   Vector3 inherited=hand.lossyScale;
   go.transform.localScale=new Vector3(1f/Mathf.Max(.0001f,Mathf.Abs(inherited.x)),1f/Mathf.Max(.0001f,Mathf.Abs(inherited.y)),1f/Mathf.Max(.0001f,Mathf.Abs(inherited.z)));
   go.transform.localPosition=new Vector3(0.04f,0.06f,-0.02f);
   go.transform.localRotation=Quaternion.Euler(15f,95f,-85f);
   Instance=go.AddComponent<HeroWeapon>();
   Instance.Build();
  }
  void Build(){
   // Hilt and pommel
   Art.Shape("Grip",PrimitiveType.Cylinder,new Vector3(0,-.09f,0),new Vector3(.032f,.13f,.032f),new Color(.16f,.13f,.11f),transform);
   Art.Shape("Pommel",PrimitiveType.Sphere,new Vector3(0,-.24f,0),Vector3.one*.065f,new Color(.85f,.68f,.25f),transform);
   // Winged crossguard
   Art.Shape("Guard",PrimitiveType.Cube,new Vector3(0,.03f,0),new Vector3(.24f,.045f,.06f),new Color(.85f,.68f,.25f),transform);
   Art.Shape("GuardWingL",PrimitiveType.Cube,new Vector3(-.13f,.06f,0),new Vector3(.05f,.08f,.04f),new Color(.85f,.68f,.25f),transform);
   Art.Shape("GuardWingR",PrimitiveType.Cube,new Vector3(.13f,.06f,0),new Vector3(.05f,.08f,.04f),new Color(.85f,.68f,.25f),transform);
   // Blade steel
   Art.Shape("BladeCore",PrimitiveType.Cube,new Vector3(0,.60f,0),new Vector3(.065f,1.08f,.018f),new Color(.88f,.93f,.98f),transform);
   var tip=Art.Shape("BladeTip",PrimitiveType.Cube,new Vector3(0,1.17f,0),new Vector3(.046f,.046f,.016f),new Color(.88f,.93f,.98f),transform);
   tip.transform.localRotation=Quaternion.Euler(0,0,45f);
   // Glowing runic core channel
   var rune=Art.Shape("RuneCore",PrimitiveType.Cube,new Vector3(0,.56f,0),new Vector3(.024f,.94f,.024f),Color.white,transform);
   runeRenderer=rune.GetComponent<Renderer>();
   UpdatePower(RealmGame.I&&RealmGame.I.Player?RealmGame.I.Player.Power:0);
  }
  void Update(){
   if(RealmGame.I&&RealmGame.I.Player&&RealmGame.I.Player.Power!=lastPower)
    UpdatePower(RealmGame.I.Player.Power);
  }
  public void UpdatePower(int power){
   lastPower=power;if(!runeRenderer)return;
   Color c=power==0?new Color(1f,.45f,.08f):power==1?new Color(.15f,.88f,1f):new Color(.15f,1f,.55f);
   if(!runeMat){
    runeMat=new Material(Shader.Find("Standard"));
    runeMat.EnableKeyword("_EMISSION");
   }
   runeMat.color=c;
   runeMat.SetColor("_EmissionColor",c*2.5f);
   runeRenderer.sharedMaterial=runeMat;
  }
 }

 public class SlashArc:MonoBehaviour {
  float age,duration=.22f;Vector3 initialScale;Material mat;
  public static void Create(Vector3 origin,Vector3 forward,int combo,bool charged,int power){
   var go=new GameObject("SlashArc");
   go.transform.position=origin+forward*.35f;

   Quaternion rot=Quaternion.LookRotation(forward);
   if(charged)rot=Quaternion.LookRotation(forward)*Quaternion.Euler(0,0,0);
   else if(combo==1)rot=Quaternion.LookRotation(forward)*Quaternion.Euler(15f,-20f,15f);
   else if(combo==2)rot=Quaternion.LookRotation(forward)*Quaternion.Euler(-25f,30f,-40f);
   else rot=Quaternion.LookRotation(forward)*Quaternion.Euler(0,0,85f);
   go.transform.rotation=rot;

   Color elem=power==0?new Color(1f,.45f,.1f):power==1?new Color(.2f,.85f,1f):new Color(.2f,1f,.55f);
   if(charged)elem=Color.Lerp(elem,Color.white,.35f);

   var mf=go.AddComponent<MeshFilter>();
   var mr=go.AddComponent<MeshRenderer>();
   var arc=go.AddComponent<SlashArc>();
   arc.duration=charged?.30f:.22f;

   int segments=charged?36:24;
   float startAngle=charged?-140f:-70f;
   float endAngle=charged?140f:70f;
   float innerRadius=charged?.6f:.4f;
   float outerRadius=charged?3.4f:2.4f;

   Mesh mesh=new Mesh();
   Vector3[] verts=new Vector3[(segments+1)*2];
   int[] tris=new int[segments*6];
   Color[] colors=new Color[verts.Length];
   Vector2[] uvs=new Vector2[verts.Length];

   for(int i=0;i<=segments;i++){
    float t=(float)i/segments;
    float deg=Mathf.Lerp(startAngle,endAngle,t);
    float rad=deg*Mathf.Deg2Rad;
    Vector3 dir=new Vector3(Mathf.Sin(rad),0,Mathf.Cos(rad));
    verts[i*2]=dir*innerRadius;
    verts[i*2+1]=dir*outerRadius;
    float a=Mathf.Sin(t*Mathf.PI);
    colors[i*2]=new Color(elem.r*.7f,elem.g*.7f,elem.b*.7f,a*.25f);
    colors[i*2+1]=new Color(elem.r*1.5f,elem.g*1.5f,elem.b*1.5f,a);
    uvs[i*2]=new Vector2(t,0);
    uvs[i*2+1]=new Vector2(t,1);
    if(i<segments){
     int v=i*2,tri=i*6;
     tris[tri]=v;tris[tri+1]=v+1;tris[tri+2]=v+2;
     tris[tri+3]=v+2;tris[tri+4]=v+1;tris[tri+5]=v+3;
    }
   }
   mesh.vertices=verts;mesh.triangles=tris;mesh.colors=colors;mesh.uv=uvs;
   mesh.RecalculateNormals();
   mf.sharedMesh=mesh;

   arc.mat=new Material(Shader.Find("Sprites/Default"));
   mr.sharedMaterial=arc.mat;
   go.transform.localScale=Vector3.one*.85f;
   arc.initialScale=Vector3.one*.85f;

   HitSpark.Burst(origin+forward*1.4f,forward,elem,charged?10:5);
  }

  void Update(){
   if(RealmGame.I.Screen!=GameScreen.Playing)return;
   age+=Time.deltaTime;
   float p=age/duration;
   transform.localScale=initialScale*(1f+p*.45f);
   if(mat){
    Color c=mat.color;
    c.a=Mathf.Clamp01(1f-p*1.2f);
    mat.color=c;
   }
   if(age>=duration){
    if(mat)Destroy(mat);
    Destroy(gameObject);
   }
  }
 }

 public class HitSpark:MonoBehaviour {
  Vector3 velocity;float age,duration=.2f;Material mat;
  public static void Burst(Vector3 pos,Vector3 dir,Color color,int count=8){
   for(int i=0;i<count;i++){
    var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
    var col=go.GetComponent<Collider>();if(col)Destroy(col);
    go.transform.position=pos;
    go.transform.localScale=new Vector3(.04f,.04f,.18f);
    Vector3 spread=dir+Random.insideUnitSphere*.85f;
    go.transform.rotation=Quaternion.LookRotation(spread);
    var spark=go.AddComponent<HitSpark>();
    spark.velocity=spread.normalized*Random.Range(5f,11f);
    spark.duration=Random.Range(.14f,.25f);
    var r=go.GetComponent<Renderer>();
    spark.mat=new Material(Shader.Find("Sprites/Default"));
    spark.mat.color=Color.Lerp(color,Color.white,.45f);
    r.sharedMaterial=spark.mat;
   }
  }
  void Update(){
   age+=Time.deltaTime;
   transform.position+=velocity*Time.deltaTime;
   transform.localScale*=(1f-Time.deltaTime*3.2f);
   if(age>=duration){
    if(mat)Destroy(mat);
    Destroy(gameObject);
   }
  }
 }

  public class FollowCamera:MonoBehaviour {
   readonly RaycastHit[] obstructionHits=new RaycastHit[32];
   public Transform Target;public float Yaw,Shake,ZoomBias;Vector3 currentFocus;Vector3 velocity;Vector3 kick;float groundY;float currentDist=8.3f;float distVelocity;bool initialized;float currentFov=58f,fovVelocity;
   public void Snap(){initialized=false;velocity=Vector3.zero;currentDist=8.3f;ZoomBias=0f;currentFov=58f;kick=Vector3.zero;}
   public void Kick(Vector3 dir,float amount){if(RealmGame.I&&!RealmGame.I.Save.shake)return;if(dir.sqrMagnitude>.001f)kick+=dir.normalized*amount;}
   void LateUpdate(){
    if(!Target)return;ZoomBias=Mathf.MoveTowards(ZoomBias,0f,Time.deltaTime*1.4f);kick=Vector3.MoveTowards(kick,Vector3.zero,Time.deltaTime*1.6f);Vector3 targetFocus=Target.position+Vector3.up*1.35f;
   if(!initialized){currentFocus=targetFocus;groundY=Target.position.y;initialized=true;}
   else{var hero=Target.GetComponent<Hero>();if(hero&&hero.Grounded)groundY=Mathf.Lerp(groundY,Target.position.y,Time.deltaTime*14f);targetFocus.y=groundY+1.35f+Mathf.Clamp(Target.position.y-groundY,0,1.5f)*.18f;currentFocus=Vector3.Lerp(currentFocus,targetFocus,1-Mathf.Exp(-Time.deltaTime*10));}
   Vector3 dir=Quaternion.Euler(14f,Yaw,0)*new Vector3(0,0.45f,-1f).normalized;float targetDist=8.0f;
   int mask=1<<0;
   int count=Physics.SphereCastNonAlloc(currentFocus,.28f,dir,obstructionHits,targetDist,mask,QueryTriggerInteraction.Ignore);
   for(int i=0;i<count;i++){
    var hit=obstructionHits[i];var c=hit.collider;
    if(!c||c.transform.IsChildOf(Target)||c.GetComponentInParent<Enemy>()||c.GetComponentInParent<PushableBlock>())continue;
    targetDist=Mathf.Min(targetDist,Mathf.Max(2f,hit.distance-.25f));
   }
   currentDist=Mathf.SmoothDamp(currentDist,targetDist+ZoomBias,ref distVelocity,0.12f);Vector3 desired=currentFocus+dir*currentDist;
   bool shakeEnabled=!RealmGame.I||RealmGame.I.Save.shake;
   transform.position=Vector3.SmoothDamp(transform.position,desired,ref velocity,0.08f)+(shakeEnabled?kick:Vector3.zero);transform.LookAt(currentFocus+Vector3.up*0.2f);
   if(Shake>0){Shake-=Time.deltaTime;if(RealmGame.I==null||RealmGame.I.Save.shake)transform.position+=Random.insideUnitSphere*0.06f;}
   float targetFov=58f;var motion=Target.GetComponent<Hero>();
   if(motion){if(motion.Pulling)targetFov+=10f;else if(motion.Dashing)targetFov+=7.5f;else if(motion.Attacking)targetFov+=2.5f;}
   currentFov=Mathf.SmoothDamp(currentFov,targetFov,ref fovVelocity,.14f);
   var cam=GetComponent<Camera>();if(cam)cam.fieldOfView=currentFov;
  }
 }

 public class Pulse:MonoBehaviour {
  float age,duration,radius;Color color;
  public static void Create(Vector3 at,float radius,Color color,float duration){
   var root=Art.Ring(at,.5f,color,RealmGame.I.World.transform);
   var p=root.AddComponent<Pulse>();p.duration=duration;p.radius=radius;p.color=color;
  }
  void Update(){
   if(RealmGame.I.Screen!=GameScreen.Playing)return;
   age+=Time.deltaTime;transform.localScale=Vector3.one*Mathf.Lerp(.15f,radius*2,age/duration);
   if(age>=duration)Destroy(gameObject);
  }
 }
}
