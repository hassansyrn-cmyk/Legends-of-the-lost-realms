using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LostRealms {
 [DefaultExecutionOrder(20)] public class Hero:MonoBehaviour {
  public CharacterController Controller;public int MaxHealth,Health,Power;public float Energy=100;public bool Grounded=>Controller&&Controller.isGrounded; public CharacterVisual Visual;
  const float MaxMoveSpeed=4.8f,GroundResponse=18f,AirResponse=8f,StopResponse=22f;
  Vector3 velocity;float vertical,jumpGrace,dashUntil,dashReady,dodgeVisualUntil,flipVisualUntil,hitUntil,immuneUntil,attackReady,comboUntil,chargeStart;int jumps,combo;bool charging;float jumpBuffer;string attackState="attack_1";
  public float HorizontalSpeed{get{var horizontal=velocity;horizontal.y=0;return horizontal.magnitude;}}
  void Awake(){
   Controller=gameObject.AddComponent<CharacterController>();Controller.height=1.75f;Controller.radius=.32f;Controller.center=Vector3.up*.9f;Controller.stepOffset=.35f;Controller.slopeLimit=48;
   MaxHealth=5+RealmGame.I.Save.healthRank;Health=MaxHealth;
   Visual=CharacterVisual.Create("Aster",transform,1.8f,new Color(.25f,.55f,.57f));
  }
  void Start(){if(RealmGame.I)EquippedWeapon.Equip(this,(WeaponId)RealmGame.I.Save.equippedWeapon);}
  void Update(){
   var g=RealmGame.I;
   if(!g||!g.CameraRig||!Visual||!Controller)return;
   if(g.Screen!=GameScreen.Playing){
    if(g.Screen==GameScreen.Defeated)Visual.Play("death");else Visual.Play("idle");
    return;
   }
   float dt=Time.deltaTime;
   Vector3 forward=Quaternion.Euler(0,g.CameraRig.Yaw,0)*Vector3.forward;
   Vector3 right=Quaternion.Euler(0,g.CameraRig.Yaw,0)*Vector3.right;
   Vector3 wish=forward*g.MoveInput.y+right*g.MoveInput.x;

   // Grounding and Jump Grace (Coyote time)
   if(Grounded){jumps=0;jumpGrace=.14f;flipVisualUntil=0;if(vertical<0)vertical=-2f;}else jumpGrace-=dt;
   jumpBuffer=g.JumpPressed?.13f:Mathf.Max(0,jumpBuffer-dt);
   if(jumpBuffer>0&&(jumps<2||jumpGrace>0)){
    jumpBuffer=0;
    vertical=8.4f;jumps=jumpGrace>0?1:jumps+1;jumpGrace=0;
    g.Sound(jumps==1?"jump":"double_jump");
    if(jumps==2){
     // This supplied Mixamo clip contains a complete airborne inversion. It is
     // visual-only; physics remains controlled by CharacterController.
     flipVisualUntil=RealmGame.I.Elapsed+.9f;Visual.Restart("double_jump");
     Color elemColor=Power==0?new Color(1f,.45f,.1f):Power==1?new Color(.2f,.85f,1f):new Color(.2f,1f,.55f);
     HitSpark.Burst(transform.position+Vector3.up*.2f,Vector3.down,elemColor,8);
    }else Visual.Restart("jump");
   }

   // Dash
   if(g.DashPressed&&RealmGame.I.Elapsed>=dashReady){
    dashUntil=RealmGame.I.Elapsed+.28f;dodgeVisualUntil=RealmGame.I.Elapsed+.63f;dashReady=RealmGame.I.Elapsed+.85f;
    immuneUntil=Mathf.Max(immuneUntil,dashUntil+.1f);
    attackReady=RealmGame.I.Elapsed;charging=false;
    if(wish.sqrMagnitude<.1f)wish=transform.forward;
    velocity=wish.normalized*16f;g.Sound("player_dash");
    Visual.Restart("dodge");
    HitSpark.Burst(transform.position+Vector3.up*.8f,-wish.normalized,new Color(1f,.9f,.7f),8);
   }

   // Smooth turning with subtle banking
   if(wish.sqrMagnitude>.02f){
    Quaternion targetRot=Quaternion.LookRotation(wish);
    float turnAngle=Vector3.SignedAngle(transform.forward,wish,Vector3.up);
    Quaternion bank=Quaternion.Euler(0,0,Mathf.Clamp(-turnAngle*0.25f,-8f,8f));
    transform.rotation=Quaternion.Slerp(transform.rotation,targetRot*bank,dt*14f);
   }

   // Horizontal acceleration
   if(RealmGame.I.Elapsed>=dashUntil){
    Vector3 desiredVelocity=wish.sqrMagnitude>.0001f?wish.normalized*(MaxMoveSpeed*Mathf.Clamp01(wish.magnitude)):Vector3.zero;
    float response=desiredVelocity.sqrMagnitude>.001f?(Grounded?GroundResponse:AirResponse):StopResponse;
    velocity=Vector3.MoveTowards(velocity,desiredVelocity,response*dt);
   }

   // Vertical physics with Jump Apex Float
   float grav=Mathf.Abs(vertical)<2.5f?11f:23f;
   vertical-=grav*dt;
   Controller.Move((velocity+Vector3.up*vertical)*dt);
   Energy=Mathf.Min(100,Energy+dt*12);

   // Attack inputs
   if(g.AttackPressed){charging=true;chargeStart=RealmGame.I.Elapsed;}
   if(charging&&g.AttackReleased){Attack(RealmGame.I.Elapsed-chargeStart>=.38f);charging=false;}
   if(charging&&RealmGame.I.Elapsed-chargeStart>=.75f){Attack(true);charging=false;}
   if(g.CastPressed)Cast();
   if(transform.position.y<-12){Health=0;g.DamageTaken++;g.Defeat();}

   // Character Animation state selection with natural speeds
   if(RealmGame.I.Elapsed<dodgeVisualUntil){
    Visual.Play("dodge");
   }else if(RealmGame.I.Elapsed<hitUntil){
    Visual.Play("hit");
   }else if(RealmGame.I.Elapsed<attackReady-.06f){
    Visual.Play(attackState);
   }else if(!Grounded){
    Visual.Play(RealmGame.I.Elapsed<flipVisualUntil?"double_jump":"jump");
   }else if(wish.sqrMagnitude>.01f){
    float speedRatio=Mathf.Clamp01(HorizontalSpeed/MaxMoveSpeed);
    Visual.PlayWalk(speedRatio);
   }else{
    Visual.Play("idle");
   }
  }

  public void Warp(Vector3 p){Controller.enabled=false;transform.position=p;Controller.enabled=true;vertical=0;velocity=Vector3.zero;jumpBuffer=0;charging=false;immuneUntil=RealmGame.I.Elapsed+1.2f;RealmGame.I.CameraRig.Snap();}
  public void Damage(int damage,Vector3 source){
   if(RealmGame.I.Elapsed<immuneUntil||Health<=0)return;
   Health=Mathf.Max(0,Health-damage);RealmGame.I.DamageTaken+=damage;immuneUntil=RealmGame.I.Elapsed+1;hitUntil=RealmGame.I.Elapsed+.38f;
   velocity=(transform.position-source).normalized*4;RealmGame.I.Sound("hurt");RealmGame.I.CameraRig.Shake=.18f;
   Visual.Restart(Health<=0?"death":"hit");
   HitSpark.Burst(transform.position+Vector3.up*.8f,(transform.position-source).normalized,new Color(1f,.2f,.2f),8);
   if(Health<=0)RealmGame.I.Defeat();
  }

  void Attack(bool charged){
   if(RealmGame.I.Elapsed<attackReady)return;
   var g=RealmGame.I;var weapon=g.CurrentWeapon;
   combo=RealmGame.I.Elapsed<comboUntil?(combo%3)+1:1;
   comboUntil=RealmGame.I.Elapsed+.95f;
   float attackDuration=(charged?.84f:(combo==2?.78f:(combo==3?.64f:.66f)))/weapon.Tempo;
   attackReady=RealmGame.I.Elapsed+attackDuration;
   attackState=charged?"charged":"attack_"+Mathf.Clamp(combo,1,3);

   // Small facing assist keeps nearby targets usable with a phone stick.
   Enemy closest=null;float nearest=3.5f;foreach(var foe in g.Enemies){if(!foe||foe.Health<=0)continue;Vector3 d=foe.transform.position-transform.position;d.y=0;if(d.magnitude<nearest&&Vector3.Dot(transform.forward,d.normalized)>.35f){closest=foe;nearest=d.magnitude;}}
   if(closest){Vector3 aim=closest.transform.position-transform.position;aim.y=0;if(aim.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(aim);}
   // Forward attack step for momentum
   Vector3 lungeDir=transform.forward;
   velocity=lungeDir*(charged?5.5f:3.6f);

   Visual.PlayAttack(combo,charged);
   g.Sound("blade");

   float reach=(charged?3.4f:2.7f)*weapon.Reach;
   float damage=(charged?3.5f:combo==3?2.5f:1.5f)*weapon.Damage;

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

  public void Restart(string state){current="";Play(state);}
  public void PlayAttack(int combo,bool charged){
   current="";Play(charged?"charged":"attack_"+Mathf.Clamp(combo,1,3));
   if(hasGraph&&playable[slot].IsValid())playable[slot].SetSpeed(charged?2.55f:combo==2?3.15f:combo==3?2.9f:3.05f);
  }
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
   if(state=="dodge")playable[slot].SetSpeed(2.9f);
   if(state=="hit")playable[slot].SetSpeed(1.85f);
   playable[slot].SetTime(0);
   graph.Connect(playable[slot],0,mixer,slot);blend=0;
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
   else{var hero=Target.GetComponent<Hero>();if(hero&&hero.Grounded)groundY=Target.position.y;targetFocus.y=groundY+1.35f+Mathf.Clamp(Target.position.y-groundY,0,1.5f)*.18f;currentFocus=Vector3.Lerp(currentFocus,targetFocus,1-Mathf.Exp(-Time.deltaTime*10));}
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
