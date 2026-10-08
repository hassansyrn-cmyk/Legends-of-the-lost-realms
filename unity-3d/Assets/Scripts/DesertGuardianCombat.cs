using UnityEngine;
namespace LostRealms {
 public partial class Enemy {
  // A dedicated arena fighter: movement gets time between ranged patterns,
  // and every damaging action locks its aim before the warning expires.
  public readonly int[] GuardianPatternCounts=new int[5];
  public string GuardianAction {get;private set;}="idle";
  public float GuardianChargeTravel {get;private set;}
  int desertPattern,desertCycle,desertPhase=1;bool desertSecondPunch;
  float desertChaseTime,desertNextAttack,desertChargeUntil;Vector3 desertDirection,desertChargePrevious;
  static readonly string[] DesertActions={"slam","sweep","stomp","eruption","charge_ready"};
  void BeginDesertPattern(int pattern){
   var g=RealmGame.I;Face(g.Player.transform.position,.4f);desertDirection=transform.forward;
   if(pattern==4){
    bool clear=false;Vector3 aim=desertDirection;
    // Lock the nearest supported lane rather than warning a charge into a
    // caldera seam that the edge guard would immediately stop.
    foreach(float angle in new[]{0f,20f,-20f,35f,-35f,50f,-50f}){var direction=Quaternion.AngleAxis(angle,Vector3.up)*aim;if(!DeckAhead(direction)||!ClearBossStep(direction))continue;desertDirection=direction;clear=true;break;}
    if(!clear){BeginDesertPattern(3);return;}
    transform.rotation=Quaternion.LookRotation(desertDirection);
   }
   desertPattern=pattern;desertSecondPunch=false;state=State.Windup;
   timer=pattern==3?1.15f:pattern==4?.85f:pattern==2?1f:.9f;
   target=Clamp(g.Player.transform.position);target.y=baseY;attackOrigin=transform.position;
   GuardianAction=DesertActions[pattern];Visual.PlayTimed(GuardianAction,timer+.35f);
   Vector3 spot=pattern==3?target:pattern==4?Clamp(transform.position+desertDirection*3f):pattern==0?attackOrigin+desertDirection*2.1f:transform.position;
   warning=CombatTelegraph.Create(spot,pattern==3?1.3f:pattern==4||pattern==0?1.8f:pattern==1?4.3f:2.2f,timer,g.World.transform);
   if(pattern==4)for(int i=2;i<=3;i++)CombatTelegraph.Create(Clamp(transform.position+desertDirection*i*2.5f),1.8f,timer,g.World.transform);
   g.Sound("boss_warning",RealmAudio.BossPitch(Kind));
  }
  void FinishDesertAction(){
   state=State.Recover;timer=phase>=3?.65f:.9f;desertNextAttack=RealmGame.I.Elapsed+timer+.9f;desertChaseTime=0;GuardianAction="idle";Visual.Play("idle");
  }
  void DesertImpact(Vector3 point,float radius,int damage){
   point.y=baseY;StrikeZone.Create(point,radius,damage,.01f);
   Vfx.Play("ga_vfx_Shockwave_01",point+Vector3.up*.12f,Quaternion.identity,radius*.45f);
   KenneyPuff.Burst(point+Vector3.up*.2f,new Color(.75f,.32f,.12f),18,1.4f);
  }
  void ConnectDesertCharge(){
   var g=RealmGame.I;if(g.Player.Damage(2+(phase==3?1:0),transform.position))g.Player.ApplyForce(desertDirection*6f+Vector3.up*1.5f);
   desertChargeUntil=0;horizontalMotion=desiredMotion=Vector3.zero;FinishDesertAction();
  }
  void LimitDesertChargeStep(ref Vector3 displacement){
   if(!IsDesertLavaGuardian||GuardianAction!="charge")return;
   Vector3 toward=RealmGame.I.Player.transform.position-transform.position;toward.y=0;float distance=toward.magnitude;
   float clearance=Controller.radius+RealmGame.I.Player.Controller.radius+.5f;
   if(distance<=clearance+.04f){ConnectDesertCharge();displacement.x=displacement.z=0;return;}
   Vector3 planar=new Vector3(displacement.x,0,displacement.z);float closing=Vector3.Dot(planar,toward.normalized);
   if(closing>distance-clearance){float scale=(distance-clearance)/closing;displacement.x*=scale;displacement.z*=scale;}
  }
  void UpdateDesertGuardian(float dt){
   var g=RealmGame.I;float now=g.Elapsed;
   if(desertPhase!=phase){
    desertPhase=phase;desertChargeUntil=0;ClearGlow();if(warning)Destroy(warning);
    state=State.Recover;timer=1.25f;poseState="roar";poseUntil=now+1.15f;GuardianAction="roar";Visual.PlayTimed("roar",1.15f);bossAddAt=now+6f;
    g.Sound("boss_roar_"+Kind,RealmAudio.BossPitch(Kind));g.CameraRig.Shake=.25f;return;
   }
   Vector3 delta=g.Player.transform.position-transform.position;delta.y=0;float distance=delta.magnitude;
   if(desertChargeUntil>now){
    GuardianChargeTravel+=Vector3.Distance(desertChargePrevious,transform.position);desertChargePrevious=transform.position;
    GuardianAction="charge";Visual.Play("run_fast");Visual.SetCurrentSpeed(1.7f);
    if(!DeckAhead(desertDirection)){desertChargeUntil=0;FinishDesertAction();return;}
    MoveBodyTo(Clamp(transform.position+desertDirection*(5.8f+phase*.35f)*dt),5.8f+phase*.35f,dt);
    if(distance<Controller.radius+g.Player.Controller.radius+.5f)ConnectDesertCharge();
    return;
   }
   if(GuardianAction=="charge"){FinishDesertAction();return;}
   timer-=dt;
   if(state==State.Patrol||state==State.Notice){
    if(distance>26f){GuardianAction="idle";Visual.Play("idle");return;}
    Face(g.Player.transform.position,dt);
    if(distance>3.1f){
     desertChaseTime+=dt;MoveTo(g.Player.transform.position,phase>=3?3.4f:2.85f,dt);
     GuardianAction="chase";Visual.Play(LocomotionMoving?(phase>=3?"run":"walk"):"idle");Visual.SetCurrentSpeed(phase>=3?1.4f:1.15f);
    }else{GuardianAction="idle";Visual.Play("idle");}
    if(timer>0||now<desertNextAttack||Mathf.Abs(g.Player.transform.position.y-transform.position.y)>2.5f)return;
    // Chase before casting at range. Close combat cycles all five patterns;
    // a retreating player invites either a charge or a locked lava fissure.
    if(distance>3.6f&&desertChaseTime<3.5f)return;
    int pattern=desertCycle++%5;
    if(distance>4.3f)pattern=desertCycle%2==1?4:3;
    else if(pattern==4)pattern=0;
    BeginDesertPattern(pattern);return;
   }
   if(state==State.Windup){
    Glow(new Color(1,.24f,.07f),.2f);
    if(timer>0)return;
    ClearGlow();if(warning){Destroy(warning);warning=null;}state=State.Attack;timer=.4f;GuardianPatternCounts[desertPattern]++;
    int damage=2+(phase==3?1:0);
    if(desertPattern==4){
     desertChargeUntil=now+1.15f;desertChargePrevious=transform.position;GuardianAction="charge";Visual.Restart("run_fast");g.Sound("enemy_dash");
    }else if(desertPattern==3){
     LavaEruptionCount++;Vector3 across=Vector3.Cross(desertDirection,Vector3.up);
     int count=phase>=2?5:3;
     for(int i=0;i<count;i++){int side=i==0?0:(i%2==1?-(i+1)/2:i/2);Vector3 point=Clamp(target+across*(side*2.4f)+desertDirection*(i==0?0:1.4f));StrikeZone.Create(point,1.3f,damage,.35f+i*.22f);LavaPatternBurst.Create(point,.35f+i*.22f,1.3f);}
     g.Sound("ember_cast");
    }else if(desertPattern==2){
     DesertImpact(attackOrigin,2.2f,damage);
     for(int i=0;i<8;i++){float angle=i*Mathf.PI*.25f;var point=Clamp(attackOrigin+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*4.4f);StrikeZone.Create(point,.95f,damage,.45f);LavaPatternBurst.Create(point,.45f,.95f);}
     g.Sound("boss");g.CameraRig.Shake=.35f;
    }else if(desertPattern==1){DesertImpact(attackOrigin+desertDirection*.9f,3.4f,damage);g.Sound("boss");}
    else{DesertImpact(attackOrigin+desertDirection*2.1f,1.8f,damage);g.Sound("boss");g.CameraRig.Shake=.25f;}
   }else if(state==State.Attack&&timer<=0){
    if(desertPattern==0&&!desertSecondPunch){
     desertSecondPunch=true;state=State.Windup;timer=.65f;GuardianAction="attack_2";
     Face(g.Player.transform.position,.3f);desertDirection=transform.forward;attackOrigin=transform.position;
     Visual.PlayTimed("attack_2",1f);warning=CombatTelegraph.Create(attackOrigin+desertDirection*2.1f,1.8f,timer,g.World.transform);g.Sound("enemy_warning");
    }else FinishDesertAction();
   }else if(state==State.Recover){Visual.Play("idle");GuardianAction="idle";if(timer<=0){state=State.Notice;timer=0;}}
  }
 }
 public sealed class LavaPatternBurst:MonoBehaviour {
  float at,radius;
  public static void Create(Vector3 point,float delay,float size){var obj=new GameObject("Lava fissure burst");obj.transform.SetParent(RealmGame.I.World.transform);obj.transform.position=point;var burst=obj.AddComponent<LavaPatternBurst>();burst.at=RealmGame.I.Elapsed+delay;burst.radius=size;}
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||g.Elapsed<at)return;Vfx.Play("ga_vfx_Explosion_02",transform.position+Vector3.up*.15f,Quaternion.identity,radius);KenneyPuff.Burst(transform.position,new Color(1,.27f,.04f),12,radius);Destroy(gameObject);}
 }
}
