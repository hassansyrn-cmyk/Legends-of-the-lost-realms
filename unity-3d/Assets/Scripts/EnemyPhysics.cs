using UnityEngine;
namespace LostRealms {
 public partial class Enemy {
  public CharacterController Controller {get;private set;}
  public bool Grounded=>Controller&&Controller.enabled&&Controller.isGrounded;
  public float VerticalVelocity=>verticalVelocity;
  public Collider LastContact {get;private set;}
  void OnControllerColliderHit(ControllerColliderHit hit){LastContact=hit.collider;}
  Vector3 desiredMotion,horizontalMotion,platformMotion;float verticalVelocity;
  static readonly float[] BossStepAngles={0f,35f,-35f,70f,-70f,100f,-100f};
  bool bodyMoved;
  float bossBlockedTime;
  public bool LocomotionMoving=>bodyMoved;
  void BuildBody(float height,float radius){
   Controller=gameObject.AddComponent<CharacterController>();
   Controller.radius=radius;Controller.height=Mathf.Max(height,radius*2f);
   Controller.center=Vector3.up*(Controller.height*.5f);
   Controller.skinWidth=.025f;Controller.stepOffset=.35f;Controller.slopeLimit=48f;Controller.minMoveDistance=0;
  }
  void Start(){
   // Traversal padding helps Aster's jumps, but must not suspend enemies over a mesh hole.
   var world=GetComponentInParent<RealmWorld>();if(!world||!Controller)return;
   foreach(var c in world.GetComponentsInChildren<Collider>()){
    if(c is MeshCollider||c.isTrigger||c.attachedRigidbody)continue;
    var r=c.GetComponent<Renderer>();if(r&&!r.enabled)Physics.IgnoreCollision(Controller,c);
   }
  }
  // AI chooses a velocity; only this fixed step moves the collision capsule.
  void FixedUpdate(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!Controller)return;
   if(Health<=0){SettleCorpse();return;}
   if(!Controller.enabled)return;
   float dt=Time.fixedDeltaTime;bool grounded=Grounded;
   if(grounded&&verticalVelocity<0){verticalVelocity=-2f;falling=false;baseY=transform.position.y;}
   verticalVelocity=Mathf.Max(-20f,verticalVelocity-(Mathf.Abs(verticalVelocity)<1.5f?11f:23f)*dt);
   float response=desiredMotion.sqrMagnitude<.001f?22f:grounded?18f:8f;
   horizontalMotion=Vector3.MoveTowards(horizontalMotion,desiredMotion,response*dt);
   Vector3 displacement=horizontalMotion*dt+Vector3.up*(verticalVelocity*dt)+platformMotion;platformMotion=Vector3.zero;
   if(pushRemaining>0){float step=Mathf.Min(pushRemaining,dt);displacement+=pushDir*(pushSpeed*step);pushRemaining-=step;}
   LimitDesertChargeStep(ref displacement);
   Vector3 before=transform.position;var flags=Controller.Move(displacement);Vector3 moved=transform.position-before-platformMotion;moved.y=0;bodyMoved=moved.sqrMagnitude>.00000025f;
   if((flags&CollisionFlags.Below)!=0){verticalVelocity=-2f;falling=false;baseY=transform.position.y;}
   else falling=verticalVelocity<-1.5f;
   if(transform.position.y<baseY-11f){
    ReleaseToken();if(warning)Destroy(warning);Health=0;state=State.Dead;comboLeft=0;ClearGlow();
    if(Visual)Visual.Restart("death");if(g.Trial)g.Trial.EnemyDefeated();
    AshPuff.Burst(transform.position+Vector3.up*.5f,new Color(.36f,.3f,.27f),14,1f,false);
    g.Coins+=Boss?20:3;g.Kills++;g.Sound("enemy_defeat");CombatFeel.OnEnemyKilled(this);Destroy(gameObject,.1f);
   }
  }
  void MoveBodyTo(Vector3 destination,float speed,float dt){
   Vector3 delta=destination-transform.position;delta.y=0;
   if(IsDesertLavaGuardian&&speed<5f&&delta.sqrMagnitude>.001f){
    bossBlockedTime=bodyMoved?0:bossBlockedTime+dt;
    Vector3 forward=delta.normalized;bool clear=false;
    foreach(float angle in BossStepAngles){
     if(angle==0&&bossBlockedTime>.35f)continue;
     Vector3 candidate=Quaternion.AngleAxis(angle,Vector3.up)*forward;
     if(!DeckAhead(candidate)||!ClearBossStep(candidate))continue;
     delta=candidate*delta.magnitude;clear=true;break;
    }
    if(!clear)return;
   }else if(!DeckAhead(delta))return;
   desiredMotion=delta.sqrMagnitude>.00001f?delta.normalized*Mathf.Min(speed,delta.magnitude/Mathf.Max(dt,.001f)):Vector3.zero;
  }
  bool ClearBossStep(Vector3 direction){
   float radius=Mathf.Max(.2f,Controller.radius-.06f);
   var bottom=transform.position+Vector3.up*(Controller.radius+.15f);var top=transform.position+Vector3.up*(Controller.height-Controller.radius);
   foreach(var hit in Physics.CapsuleCastAll(bottom,top,radius,direction,1f,~0,QueryTriggerInteraction.Ignore)){
    if(hit.collider.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<Hero>()||hit.normal.y>.55f)continue;
    if(Physics.GetIgnoreCollision(Controller,hit.collider))continue;
    // Terrain is traversed by the controller's slope/step rules. Capsule
    // sweeps beginning against a mound often report a horizontal normal.
    // Props and buildings retain clearance checks and real collision.
    if(hit.collider is MeshCollider){var island=ChapterLayout.IslandOf(transform,RealmGame.I.World.transform);bool prop=false;for(var t=hit.collider.transform;t&&t!=island;t=t.parent)if(t.name.StartsWith("Prop ")||t.GetComponent<BreakableCrate>()||ChapterLayout.IsTrap(t)){prop=true;break;}if(island&&hit.collider.transform.IsChildOf(island)&&!prop)continue;}
    return false;
   }
   return true;
  }
  void SettleCorpse(){
   float dt=Time.fixedDeltaTime;
   verticalVelocity=Mathf.Max(-20f,verticalVelocity-(Mathf.Abs(verticalVelocity)<1.5f?11f:23f)*dt);
   Vector3 next=transform.position+Vector3.up*(verticalVelocity*dt);float best=float.NegativeInfinity;
   foreach(var hit in Physics.RaycastAll(transform.position+Vector3.up*.15f,Vector3.down,Mathf.Abs(verticalVelocity*dt)+.2f,~0,QueryTriggerInteraction.Ignore)){
    if(hit.normal.y<.55f||hit.collider.GetComponentInParent<Enemy>()||hit.collider.GetComponentInParent<Hero>())continue;
    if(Physics.GetIgnoreCollision(Controller,hit.collider))continue;
    if(hit.point.y>=next.y&&hit.point.y>best)best=hit.point.y;
   }
   if(best>float.NegativeInfinity){next.y=best;verticalVelocity=0;}
   transform.position=next;
  }
  bool DeckAhead(Vector3 direction){
   direction.y=0;if(direction.sqrMagnitude<.001f)return true;
   var g=RealmGame.I;if(!g||!g.World)return false;
   var island=ChapterLayout.IslandOf(transform,g.World.transform);if(!island)return false;
   var point=transform.position+direction.normalized*(Controller.radius+.3f);
   var local=island.InverseTransformPoint(point);
   if(IsDesertLavaGuardian){
    // A large capsule spans tile seams. Require a supported footprint rather
    // than stopping on one missing/steep triangle beneath its centre ray.
    Vector3 across=Vector3.Cross(Vector3.up,direction.normalized);int supported=0;
    foreach(var offset in new[]{Vector3.zero,across*.55f,-across*.55f,direction.normalized*.55f,-direction.normalized*.55f}){
     var sample=island.InverseTransformPoint(point+offset);if(!RealmProps.TryDeckSurface(island,sample.x,sample.z,transform,out float deck))continue;
     float h=island.TransformPoint(new Vector3(sample.x,deck,sample.z)).y;if(h>=transform.position.y-1.25f&&h<=transform.position.y+1.25f)supported++;
    }
    return supported>=3;
   }
   if(!RealmProps.TryDeckSurface(island,local.x,local.z,transform,out float y))return false;
   float height=island.TransformPoint(new Vector3(local.x,y,local.z)).y;
   return height>=transform.position.y-.55f&&height<=transform.position.y+.4f;
  }
  public void CarryByPlatform(Vector3 delta){platformMotion+=delta;ShiftCenter(delta);}
  public void WarpBody(Vector3 point){
   bool enabled=Controller&&Controller.enabled;if(enabled)Controller.enabled=false;
   transform.position=point;if(enabled)Controller.enabled=true;
   desiredMotion=horizontalMotion=platformMotion=Vector3.zero;verticalVelocity=0;falling=false;baseY=point.y;
  }
  // Environmental damage has one policy shared by every trap generation.
  public void HitByTrap(float damage,int power,bool elemental,Vector3 knockDir=default){if(!Boss)Hit(damage,power,elemental,knockDir);}
 }
}
