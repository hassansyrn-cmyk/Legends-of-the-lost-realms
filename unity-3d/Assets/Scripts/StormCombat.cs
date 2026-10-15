using UnityEngine;
namespace LostRealms {
 public partial class Enemy {
  int stormMove,stormSeenPhase=1;float stormNext,stormChargeUntil;Vector3 stormDir;bool stormContact;public int StormPatternMask {get;private set;}
  public void InterruptStorm(){if(Kind!=28||Health<=0)return;stormChargeUntil=0;if(warning)Destroy(warning);warning=null;ClearGlow();state=State.Recover;timer=3f;poseState="gethit";poseUntil=RealmGame.I.Elapsed+.6f;Visual.Restart("gethit");}
  void StormLightning(Vector3 position,float radius,int damage){StrikeZone.Create(position,radius,damage,.04f);Vfx.Play("ga_vfx_Electricity_01",position+Vector3.up*.25f,Quaternion.identity,1.1f);RealmGame.I.Sound("storm_thunder");}
  void UpdateStormScribe(float dt){
   var g=RealmGame.I;timer-=dt;var d=g.Player.transform.position-transform.position;d.y=0;float distance=d.magnitude;
   if(state==State.Patrol||state==State.Notice){if(distance>18||Mathf.Abs(g.Player.transform.position.y-baseY)>3){Visual.Play("idle");return;}
    Face(g.Player.transform.position,dt);if(distance<4){MoveBodyTo(Clamp(transform.position-d.normalized*2f),1.9f,dt);Visual.Play("walk");}else if(distance>9){MoveTo(g.Player.transform.position,1.9f,dt);Visual.Play("walk");}else Visual.Play("idle");
    if(timer<=0&&distance<11){state=State.Windup;timer=1.2f;target=g.Player.transform.position;Visual.PlayTimed("cast",1.3f);warning=CombatTelegraph.Create(target,1.35f,timer,g.World.transform);g.Sound("storm_turn");}
   }else if(state==State.Windup){Visual.Play("cast");if(timer<=0){if(warning)Destroy(warning);StormLightning(target,1.35f,2);state=State.Recover;timer=2.7f;}}
   else {Visual.Play("idle");if(timer<=0){state=State.Notice;timer=.25f;}}
  }
  void UpdateStormGuardian(float dt){
   var g=RealmGame.I;timer-=dt;var d=g.Player.transform.position-transform.position;d.y=0;float distance=d.magnitude;
   if(stormSeenPhase!=phase){stormSeenPhase=phase;InterruptStorm();timer=1.6f;poseState="victory";poseUntil=g.Elapsed+1.3f;Visual.Restart("victory");g.Tell("TEMPEST REGENT / PHASE "+phase+" — ground the copper conductors",3f);return;}
   if(stormChargeUntil>g.Elapsed&&state==State.Attack){
    if(!DeckAhead(stormDir)){stormChargeUntil=0;state=State.Recover;timer=1.8f;return;}
    Visual.Play("run_fast");MoveBodyTo(Clamp(transform.position+stormDir*7.2f*dt),7.2f,dt);
    if(distance<2.4f&&!stormContact){stormContact=true;g.Player.Damage(3,transform.position);stormChargeUntil=0;state=State.Recover;timer=1.8f;}return;
   }
   if(state==State.Patrol||state==State.Notice){
    if(distance>22||Mathf.Abs(g.Player.transform.position.y-baseY)>3){Visual.Play("idle");return;}
    Face(g.Player.transform.position,dt);if(distance>3.1f){MoveTo(g.Player.transform.position,2.6f+phase*.18f,dt);Visual.Play(LocomotionMoving?"walk":"idle");}else Visual.Play("idle");
    if(timer>0||g.Elapsed<stormNext)return;
    // Four distinct patterns, with locked telegraphs and long counters.
    // Commit to closing for the claw pattern instead of replacing every
    // distant melee with another stationary lightning cast.
    if(attackCount%4==0&&distance>4.5f)return;
    stormMove=attackCount++%4;
    state=State.Windup;timer=stormMove==0?.9f:stormMove==2?1.4f:1.25f;target=g.Player.transform.position;target.y=baseY;attackOrigin=transform.position;stormDir=d.normalized;
    StormPatternMask|=1<<stormMove;Visual.PlayTimed(stormMove==0?"attack":stormMove==2?"victory":"cast",timer+.3f);g.Sound("storm_turn");
    if(stormMove==0)warning=CombatTelegraph.Create(attackOrigin+stormDir*2.5f,2.25f,timer,g.World.transform);
    else if(stormMove==1){warning=CombatTelegraph.Create(target,1.7f,timer,g.World.transform);if(phase>=2)foreach(float side in new[]{-1f,1f})CombatTelegraph.Create(Clamp(target+Vector3.right*side*3.5f),1.5f,timer,g.World.transform);}
    else if(stormMove==2){for(int i=1;i<=4;i++)CombatTelegraph.Create(Clamp(attackOrigin+stormDir*(i*2.1f)),1.5f,timer,g.World.transform);}
    else {foreach(float side in new[]{-1f,1f}){CombatTelegraph.Create(Clamp(center+Vector3.right*side*6.5f),3f,timer,g.World.transform);CombatTelegraph.Create(Clamp(center+Vector3.forward*side*6.5f),3f,timer,g.World.transform);}g.Tell("STORM CROSS — the center is safe; ground a conductor to interrupt",2f);}
    return;
   }
   if(state==State.Windup){Visual.Play(stormMove==0?"attack":stormMove==2?"victory":"cast");if(timer>0)return;if(warning)Destroy(warning);warning=null;stormNext=g.Elapsed+2.7f;state=State.Attack;timer=.5f;
    if(stormMove==0){StrikeZone.Create(attackOrigin+stormDir*2.5f,2.25f,3,.04f);Visual.Restart("attack_2");}
    else if(stormMove==1){StormLightning(target,1.7f,2);if(phase>=2)foreach(float side in new[]{-1f,1f})StormLightning(Clamp(target+Vector3.right*side*3.5f),1.5f,2);}
    else if(stormMove==2){stormChargeUntil=g.Elapsed+1.1f;stormContact=false;Visual.Restart("run_fast");timer=1.15f;}
    else foreach(float side in new[]{-1f,1f}){StormLightning(Clamp(center+Vector3.right*side*6.5f),3f,2);StormLightning(Clamp(center+Vector3.forward*side*6.5f),3f,2);}
   }else if(state==State.Attack){if(timer<=0){stormChargeUntil=0;state=State.Recover;timer=1.7f;}}
   else {Visual.Play("idle");if(timer<=0){state=State.Notice;timer=.25f;}}
  }
 }
}
