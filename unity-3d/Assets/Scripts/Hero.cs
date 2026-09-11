using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LostRealms {
 [DefaultExecutionOrder(20)] public class Hero:MonoBehaviour {
  public CharacterController Controller;public int MaxHealth,Health,Power;public float Energy=100;public bool Grounded=>Controller&&Controller.isGrounded; public CharacterVisual Visual;
  const float MaxMoveSpeed=4.8f,GroundResponse=18f,AirResponse=8f,StopResponse=22f;
  Vector3 velocity;Vector3 moveWish;float vertical,jumpGrace,dashUntil,dashReady,dodgeVisualUntil,hitUntil,immuneUntil,attackReady,comboUntil,chargeStart,attackBufferUntil;int jumps,combo;bool charging,attackBufferCharged;float jumpBuffer;string attackState="attack_1";
  float parryUntil,parryReady,counterUntil;bool dodgeRewarded;
  public bool CounterReady=>RealmGame.I!=null&&RealmGame.I.Elapsed<counterUntil;
  public float HorizontalSpeed{get{var horizontal=velocity;horizontal.y=0;return horizontal.magnitude;}}
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
   if(g.AttackPressed){charging=true;chargeStart=RealmGame.I.Elapsed;}
   if(charging&&g.AttackReleased){Attack(RealmGame.I.Elapsed-chargeStart>=.38f);charging=false;}
   if(charging&&RealmGame.I.Elapsed-chargeStart>=.75f){Attack(true);charging=false;}
   if(attackBufferUntil>0&&RealmGame.I.Elapsed>=attackReady&&RealmGame.I.Elapsed>=dodgeVisualUntil&&RealmGame.I.Elapsed>=hitUntil){attackBufferUntil=0;Attack(attackBufferCharged);}
   if(g.CastPressed)Cast();
   if(g.ParryPressed)Parry();
   if(transform.position.y<-12){Health=0;g.DamageTaken++;g.Defeat();}

   // Character Animation state selection with natural speeds
   if(RealmGame.I.Elapsed<dodgeVisualUntil){
    Visual.Play("dodge");
   }else if(RealmGame.I.Elapsed<hitUntil){
    Visual.Play("hit");
   }else if(RealmGame.I.Elapsed<parryUntil){
    Visual.Play("charged");
   }else if(RealmGame.I.Elapsed<attackReady-.06f){
    Visual.Play(attackState);
   }else if(!Grounded){
    Visual.Play("jump");
   }else if(moveWish.sqrMagnitude>.01f){
    float speedRatio=Mathf.Clamp01(HorizontalSpeed/MaxMoveSpeed);
    Visual.PlayWalk(speedRatio);
   }else{
    Visual.Play("idle");
   }
  }

  void FixedUpdate(){
   var g=RealmGame.I;
   if(!g||!Controller||g.Screen!=GameScreen.Playing)return;
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
   if(Grounded){jumps=0;jumpGrace=.14f;if(vertical<0)vertical=-3f;}else jumpGrace-=dt;
   jumpBuffer=jumpPressed?.13f:Mathf.Max(0,jumpBuffer-dt);
   if(jumpBuffer>0&&(jumps<2||jumpGrace>0)){
    jumpBuffer=0;
    vertical=8.4f;jumps=jumpGrace>0?1:jumps+1;jumpGrace=0;
    g.Sound(jumps==1?"jump":"double_jump");
    if(jumps==2){
     Color elemColor=Power==0?new Color(1f,.45f,.1f):Power==1?new Color(.2f,.85f,1f):new Color(.2f,1f,.55f);
     HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.down,elemColor,8);
    }
    Visual.Restart("jump");
   }

   // Dash
   if(dashPressed&&RealmGame.I.Elapsed>=dashReady){
    dashUntil=RealmGame.I.Elapsed+.22f;dodgeVisualUntil=RealmGame.I.Elapsed+.63f;dashReady=RealmGame.I.Elapsed+.9f;
    dodgeRewarded=false;
    immuneUntil=Mathf.Max(immuneUntil,dashUntil+.1f);
    attackReady=RealmGame.I.Elapsed;charging=false;
    if(wish.sqrMagnitude<.1f)wish=transform.forward;
    wish.y=0;wish.Normalize();
    velocity=wish*9.5f;velocity.y=0;g.Sound("player_dash");
    Visual.Restart("dodge");
    HitSpark.Burst(transform.position+Vector3.up*.8f,-wish,new Color(1f,.9f,.7f),8);
   }

   // Horizontal acceleration
   if(RealmGame.I.Elapsed>=dashUntil){
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
   Controller.Move((velocity+Vector3.up*vertical)*dt);
   Energy=Mathf.Min(100,Energy+dt*12);
  }

  public void Warp(Vector3 p){Controller.enabled=false;transform.position=p;Controller.enabled=true;vertical=0;velocity=Vector3.zero;jumpBuffer=0;charging=false;dashUntil=0;attackBufferUntil=0;immuneUntil=RealmGame.I.Elapsed+1.2f;parryUntil=0;parryReady=0;counterUntil=0;RealmGame.I.CameraRig.Snap();}
  public bool Damage(int damage,Vector3 source){
   if(Health<=0)return false;
   if(RealmGame.I.Elapsed<parryUntil){ParrySuccess(source);return false;}
   if(RealmGame.I.Elapsed<immuneUntil){if(RealmGame.I.Elapsed<dashUntil)PerfectDodge(source);return false;}
   Health=Mathf.Max(0,Health-damage);RealmGame.I.DamageTaken+=damage;immuneUntil=RealmGame.I.Elapsed+1;hitUntil=RealmGame.I.Elapsed+.5f;
   Vector3 knock=transform.position-source;knock.y=0;if(knock.sqrMagnitude<.01f)knock=-transform.forward;knock.y=0;velocity=knock.normalized*4.5f;RealmGame.I.Sound("hurt");RealmGame.I.CameraRig.Shake=.18f;
   Visual.Restart(Health<=0?"death":"hit");
   HitSpark.Burst(transform.position+Vector3.up*.8f,(transform.position-source).normalized,new Color(1f,.2f,.2f),8);
   if(Health<=0)RealmGame.I.Defeat();
   return true;
  }

  void Attack(bool charged){
   // Never cut a dodge roll or hit flinch mid-clip: queue instead so the
   // swing starts exactly as the current visual blends out.
   if(RealmGame.I.Elapsed<attackReady||RealmGame.I.Elapsed<dodgeVisualUntil||RealmGame.I.Elapsed<hitUntil){
    attackBufferUntil=RealmGame.I.Elapsed+.3f;attackBufferCharged=charged;return;
   }
   var g=RealmGame.I;var weapon=g.CurrentWeapon;
   combo=RealmGame.I.Elapsed<comboUntil?(combo%3)+1:1;
   float baseSpeed=charged?2.55f:combo==2?3.15f:combo==3?2.9f:3.05f;
   float playSpeed=baseSpeed*Mathf.Max(.4f,weapon.Tempo);
   attackState=charged?"charged":"attack_"+Mathf.Clamp(combo,1,3);
   float clipLen=Visual?Visual.ClipLength(attackState):0f;
   float attackDuration=clipLen>0f?clipLen/playSpeed:(charged?.84f:(combo==2?.78f:(combo==3?.64f:.66f)))/weapon.Tempo;
   attackReady=RealmGame.I.Elapsed+attackDuration;
   comboUntil=attackReady+.35f;
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
   float lungeStep=(charged?3f:2f)*(Grounded?1f:.5f);
   velocity=Vector3.ClampMagnitude(velocity*.55f+lungeDir*lungeStep*.45f,MaxMoveSpeed);velocity.y=0;

   Visual.PlayAttack(combo,charged,weapon.Tempo);
   g.Sound("blade");

   float reach=(charged?3.4f:2.7f)*weapon.Reach;
   float damage=(charged?3.5f:combo==3?2.5f:1.5f)*weapon.Damage;
   if(RealmGame.I.Elapsed<counterUntil){damage*=1.6f;counterUntil=0;HitSpark.Burst(transform.position+Vector3.up*1.2f,transform.forward,new Color(1f,.9f,.52f),14);}

   // Dynamic 3D curved slash arc ribbon
   SlashArc.Create(transform.position+Vector3.up*.85f,transform.forward,combo,charged,Power);

   foreach(var e in g.Enemies.ToArray()){
    if(!e||e.Health<=0)continue;
    Vector3 delta=e.transform.position-transform.position;delta.y=0;
    if(delta.magnitude<reach+e.Radius&&Vector3.Dot(transform.forward,delta.normalized)>-.2f){
     e.Hit(damage+(g.Save.powerRank*.18f),Power,false,transform.forward);
    }
   }
  }

  // Timing-based guard: a short window that negates one incoming hit, stuns the
  // attacker and opens a counter. Aster has no parry clip yet, so it braces in
  // the charged pose until a dedicated animation is baked.
  void Parry(){
   float now=RealmGame.I.Elapsed;if(now<parryReady||!Grounded)return;
   parryUntil=now+.2f;parryReady=now+.55f;
   RealmGame.I.Sound("player_dash");
   Visual.Restart("charged");
   HitSpark.Burst(transform.position+Vector3.up*.9f+transform.forward*.4f,transform.forward,new Color(.8f,.92f,1f),6);
  }
  void ParrySuccess(Vector3 source){
   var g=RealmGame.I;parryUntil=0;parryReady=g.Elapsed+.55f;
   g.HitStop(.14f,.08f);g.CameraRig.Shake=.3f;g.Sound("impact");
   Energy=Mathf.Min(100,Energy+20);counterUntil=g.Elapsed+1.6f;
   HitSpark.Burst(transform.position+Vector3.up*.95f,-transform.forward,new Color(1f,.96f,.72f),28);
   DamageTip.Show(transform.position+Vector3.up*1.95f,"PARRY!",new Color(1f,.94f,.6f));
   var foe=NearestEnemy(source,3.2f);if(foe)foe.Stun(1.1f);
   Visual.Restart("charged");
  }
  // A dodge that actually shrugs off a hit during its active i-frame window
  // rewards the player with energy and a counter opening.
  void PerfectDodge(Vector3 source){
   if(dodgeRewarded)return;dodgeRewarded=true;
   var g=RealmGame.I;g.HitStop(.1f,.1f);g.CameraRig.Shake=.2f;g.Sound("player_dash");
   Energy=Mathf.Min(100,Energy+30);counterUntil=g.Elapsed+1.4f;
   HitSpark.Burst(transform.position+Vector3.up*.85f,-transform.forward,new Color(.7f,.96f,1f),24);
   DamageTip.Show(transform.position+Vector3.up*1.95f,"PERFECT DODGE",new Color(.65f,.95f,1f));
  }
  Enemy NearestEnemy(Vector3 source,float range){
   Enemy best=null;float closest=range;
   foreach(var foe in RealmGame.I.Enemies){if(!foe||foe.Health<=0)continue;float d=Vector3.Distance(foe.transform.position,source);if(d<closest){closest=d;best=foe;}}
   return best;
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

   foreach(var e in g.Enemies.ToArray())
    if(e&&e.Health>0&&Vector3.Distance(transform.position,e.transform.position)<6.5f)
     e.Hit(2.5f+g.Save.powerRank*.5f,Power,true,(e.transform.position-transform.position).normalized);

   if(Power==2){
    foreach(var p in FindObjectsByType<EnemyBolt>())
     if(Vector3.Distance(transform.position,p.transform.position)<7.5f)Destroy(p.gameObject);
    vertical=Mathf.Max(vertical,6f);
   }
  }
 }

 public class CharacterVisual:MonoBehaviour {
  public Animator animator;PlayableGraph graph;AnimationMixerPlayable mixer;AnimationClipPlayable[] playable=new AnimationClipPlayable[2];AnimationClip[] clips;string current="";float blend;int slot;bool hasGraph;Transform fallbackBody;
  public string CurrentState=>current;
  static readonly string[] Names={"idle","walk","run","attack_1","attack_2","attack_3","charged","jump","double_jump","dodge","hit","death"};

  public static CharacterVisual Create(string role,Transform parent,float height,Color color){
   var holder=new GameObject(role+" visual");holder.transform.SetParent(parent,false);var v=holder.AddComponent<CharacterVisual>();
   var prefab=Resources.Load<GameObject>("Characters/"+role);
   if(prefab){
    var model=Instantiate(prefab,holder.transform);
    model.transform.localPosition=Vector3.zero;
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
    if(role=="Aster")v.clips[i]=Resources.Load<AnimationClip>("Animations/Aster/"+Names[i]);
    else{
     string legacy=Names[i].StartsWith("attack_")?"attack":Names[i]=="charged"?"attack":Names[i]=="run"?"walk":Names[i];
     v.clips[i]=Resources.Load<AnimationClip>("Animations/"+role+"_"+legacy)??Resources.Load<AnimationClip>("Animations/Shared/"+legacy);
    }
   }
   if(role=="Aster"&&System.Array.Exists(v.clips,c=>c==null))throw new System.Exception("Aster Phase 1 animation set is incomplete");

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
  public void PlayAttack(int combo,bool charged,float tempo=1f){
   current="";string state=charged?"charged":"attack_"+Mathf.Clamp(combo,1,3);Play(state);
   float baseSpeed=charged?2.55f:combo==2?3.15f:combo==3?2.9f:3.05f;
   if(hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(baseSpeed*Mathf.Max(.4f,tempo));
  }
  public float ClipLength(string state){int i=System.Array.IndexOf(Names,state);if(i>=0&&clips!=null&&i<clips.Length&&clips[i]!=null)return clips[i].length;return 0f;}
  public void PlayWalk(float speedRatio=1f){
   // Hysteresis keeps a thumbstick near the walk/run threshold from restarting
   // clips every frame and making the character appear to twitch.
   string locomotion=speedRatio>.72f||(current=="run"&&speedRatio>.58f)?"run":"walk";Play(locomotion);
   if(hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(locomotion=="run"?Mathf.Lerp(.76f,1.02f,Mathf.InverseLerp(.68f,1f,speedRatio)):Mathf.Lerp(.62f,.9f,Mathf.InverseLerp(.08f,.68f,speedRatio)));
  }
  public void Play(string state){
   if(state=="attack")state="attack_1";
   if(current==state)return;current=state;if(!hasGraph)return;
   int i=System.Array.IndexOf(Names,state);AnimationClip clip=i>=0?clips[i]:null;
   if(!clip)clip=clips[0]?clips[0]:clips[1];if(!clip)return;
   slot=1-slot;
   if(playable[slot].IsValid()){mixer.DisconnectInput(slot);graph.DestroyPlayable(playable[slot]);}
   playable[slot]=AnimationClipPlayable.Create(graph,clip);
   playable[slot].SetApplyFootIK(false);
   if(state=="jump")playable[slot].SetSpeed(1.9f);
   if(state=="double_jump")playable[slot].SetSpeed(3.55f);
   if(state=="dodge")playable[slot].SetSpeed(3.35f);
   if(state=="hit")playable[slot].SetSpeed(2.07f);
   playable[slot].SetTime(0);
   graph.Connect(playable[slot],0,mixer,slot);blend=0;mixer.SetInputWeight(slot,0f);mixer.SetInputWeight(1-slot,1f);
  }

  void Update(){
   if(hasGraph){
    float speed=RealmGame.I.Screen==GameScreen.Playing?1:0;
    graph.GetRootPlayable(0).SetSpeed(speed);
    float blendRate=current=="dodge"||current=="hit"?16f:current=="double_jump"?15f:current.StartsWith("attack_")||current=="charged"?12f:9f;
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
  public Transform Target;public float Yaw,Shake;Vector3 currentFocus;Vector3 velocity;float groundY;float currentDist=8.3f;float distVelocity;bool initialized;
  public void Snap(){initialized=false;velocity=Vector3.zero;currentDist=8.3f;}
  void LateUpdate(){
   if(!Target)return;Vector3 targetFocus=Target.position+Vector3.up*1.35f;
   if(!initialized){currentFocus=targetFocus;groundY=Target.position.y;initialized=true;}
   else{var hero=Target.GetComponent<Hero>();if(hero&&hero.Grounded)groundY=Mathf.Lerp(groundY,Target.position.y,Time.deltaTime*14f);targetFocus.y=groundY+1.35f+Mathf.Clamp(Target.position.y-groundY,0,1.5f)*.18f;currentFocus=Vector3.Lerp(currentFocus,targetFocus,1-Mathf.Exp(-Time.deltaTime*10));}
   Vector3 dir=Quaternion.Euler(14f,Yaw,0)*new Vector3(0,0.45f,-1f).normalized;float targetDist=8.0f;
   int mask=1<<0;
   if(Physics.SphereCast(currentFocus,0.28f,dir,out var hit,targetDist,mask,QueryTriggerInteraction.Ignore)){
    if(hit.collider!=null&&!hit.collider.isTrigger&&!hit.collider.transform.IsChildOf(Target))targetDist=Mathf.Max(2.0f,hit.distance-0.25f);
   }
   currentDist=Mathf.SmoothDamp(currentDist,targetDist,ref distVelocity,0.12f);Vector3 desired=currentFocus+dir*currentDist;
   transform.position=Vector3.SmoothDamp(transform.position,desired,ref velocity,0.08f);transform.LookAt(currentFocus+Vector3.up*0.2f);
   if(Shake>0){Shake-=Time.deltaTime;transform.position+=Random.insideUnitSphere*0.06f;}
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
