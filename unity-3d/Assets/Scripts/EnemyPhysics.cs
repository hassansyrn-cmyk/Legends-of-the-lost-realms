using UnityEngine;
namespace LostRealms {
 public partial class Enemy {
  public CharacterController Controller {get;private set;}
  public bool Grounded=>Controller&&Controller.enabled&&Controller.isGrounded;
  public float VerticalVelocity=>verticalVelocity;
  public Collider LastContact {get;private set;}
  void OnControllerColliderHit(ControllerColliderHit hit){LastContact=hit.collider;}
  Vector3 desiredMotion,horizontalMotion,platformMotion;float verticalVelocity;
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
   var flags=Controller.Move(displacement);
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
   if(!DeckAhead(delta))return;
   desiredMotion=delta.sqrMagnitude>.00001f?delta.normalized*Mathf.Min(speed,delta.magnitude/Mathf.Max(dt,.001f)):Vector3.zero;
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
