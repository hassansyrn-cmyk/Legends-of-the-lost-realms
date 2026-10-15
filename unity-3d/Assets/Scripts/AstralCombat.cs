using UnityEngine;
namespace LostRealms {
 public partial class Enemy {
  float astralOpenUntil,astralChargeUntil,astralNext,oracleHealAt;int astralKnownPhase=1,astralMove;Vector3 astralDirection;bool astralContact;
  public int AstralPhase=>phase;
  public bool AstralWard=>Kind==31&&(phase!=astralKnownPhase||RealmGame.I.Elapsed>=astralOpenUntil);
  public int AstralPatternMask {get;private set;}
  public void OpenAstralWard(){if(Kind!=31||Health<=0)return;astralOpenUntil=RealmGame.I.Elapsed+10;astralChargeUntil=0;if(warning)Destroy(warning);warning=null;ClearGlow();state=State.Recover;timer=2.2f;Visual.Restart("gethit");}
  void UpdateStarOracle(float dt){var g=RealmGame.I;timer-=dt;var d=g.Player.transform.position-transform.position;d.y=0;float distance=d.magnitude;
   if(distance>18){Visual.Play("idle");return;}Face(g.Player.transform.position,dt);
   if(state==State.Windup){Visual.Play("cast");if(timer>0)return;if(warning)Destroy(warning);warning=null;StrikeZone.Create(target,1.5f,2,.04f);Vfx.Play("ga_vfx_Implosion_01",target,Quaternion.identity,.6f);
    if(g.Elapsed>=oracleHealAt){oracleHealAt=g.Elapsed+7;foreach(var e in g.Enemies)if(e&&e.Kind==29&&e.Health>0&&Vector3.Distance(e.transform.position,transform.position)<5)e.Health=Mathf.Min(e.MaxHealth,e.Health+1.5f);}
    state=State.Recover;timer=2.4f;return;
   }
   if(distance<4)MoveTo(transform.position-d.normalized*2,2,dt);else if(distance>9)MoveTo(g.Player.transform.position,1.7f,dt);Visual.Play(LocomotionMoving?"walk":"idle");
   if(timer<=0&&distance<12){state=State.Windup;timer=1.3f;target=g.Player.transform.position;target.y=baseY;warning=CombatTelegraph.Create(target,1.5f,timer,g.World.transform);Visual.PlayTimed("cast",1.6f);g.Sound("astral_pulse");}
  }
  void UpdateFractureKeeper(float dt){var g=RealmGame.I;timer-=dt;var d=g.Player.transform.position-transform.position;d.y=0;float distance=d.magnitude;
   if(phase!=astralKnownPhase){astralKnownPhase=phase;astralOpenUntil=0;burnUntil=freezeUntil=0;astralChargeUntil=0;if(warning)Destroy(warning);warning=null;state=State.Recover;timer=1.6f;Visual.Restart("victory");g.Tell("NEW WARD — ground the "+new[]{"EMBER","FROST","GALE"}[phase-1]+" focus",3);return;}
   if(astralChargeUntil>g.Elapsed){Visual.Play("run_fast");if(DeckAhead(astralDirection)){MoveTo(Clamp(transform.position+astralDirection*3),7,dt);}else astralChargeUntil=0;
    if(!astralContact&&distance<2){astralContact=true;g.Player.Damage(3,transform.position);astralChargeUntil=0;}return;}
   if(state==State.Patrol||state==State.Notice){
    if(distance>24){Visual.Play("idle");return;}Face(g.Player.transform.position,dt);if(distance>3){MoveTo(g.Player.transform.position,2.8f+phase*.2f,dt);Visual.Play(LocomotionMoving?"walk":"idle");}else Visual.Play("idle");
    if(timer>0||g.Elapsed<astralNext)return;
    astralMove=attackCount++%3;AstralPatternMask|=1<<astralMove;state=State.Windup;timer=astralMove==0?1.15f:1.4f;attackOrigin=transform.position;target=g.Player.transform.position;target.y=baseY;astralDirection=d.normalized;
    Visual.PlayTimed(astralMove==0?"victory":astralMove==1?"cast":"attack_2",timer+.4f);g.Sound("astral_pulse");
    if(astralMove==0){for(int i=1;i<=4;i++)CombatTelegraph.Create(Clamp(attackOrigin+astralDirection*i*2),1.4f,timer,g.World.transform);}
    else if(astralMove==1){warning=CombatTelegraph.Create(target,1.6f,timer,g.World.transform);if(phase>=2)foreach(int sign in new[]{-1,1})CombatTelegraph.Create(Clamp(target+Vector3.right*sign*4),1.6f,timer,g.World.transform);}
    else {var side=Vector3.Cross(Vector3.up,astralDirection);for(int i=1;i<=3;i++)CombatTelegraph.Create(Clamp(attackOrigin+astralDirection*2+side*(i-2)*3),1.5f,timer,g.World.transform);if(phase==3)CombatTelegraph.Create(Clamp(target+Vector3.forward*4),2.1f,timer,g.World.transform);}
    return;
   }
   if(state==State.Windup){if(timer>0)return;if(warning)Destroy(warning);warning=null;state=State.Attack;timer=.6f;astralNext=g.Elapsed+2.8f;
    if(astralMove==0){astralChargeUntil=g.Elapsed+1;astralContact=false;timer=1.1f;Visual.Restart("run_fast");}
    else if(astralMove==1){StrikeZone.Create(target,1.6f,2,.04f);if(phase>=2)foreach(int sign in new[]{-1,1})StrikeZone.Create(Clamp(target+Vector3.right*sign*4),1.6f,2,.04f);Vfx.Play("ga_vfx_Implosion_01",target,Quaternion.identity,.9f);}
    else{var side=Vector3.Cross(Vector3.up,astralDirection);for(int i=1;i<=3;i++)StrikeZone.Create(Clamp(attackOrigin+astralDirection*2+side*(i-2)*3),1.5f,3,.04f);if(phase==3)StrikeZone.Create(Clamp(target+Vector3.forward*4),2.1f,2,.04f);Visual.Restart("attack");}
   }else if(state==State.Attack){if(timer<=0){astralChargeUntil=0;state=State.Recover;timer=1.8f;}}
   else {Visual.Play("idle");if(timer<=0){state=State.Notice;timer=.3f;}}
  }
 }
}
