using UnityEngine;
namespace LostRealms {
 // Golem boss controller (Sep 2026 boss-animation pass): the three realm
 // guardians (kinds 8/9/10, the Dungeon Mason golem) previously reused the
 // generic enemy AI with a single swing. This partial mirrors LavaBossCombat:
 // melee combos (Attack01 → Attack02), a telegraphed ground slam (radial
 // StrikeZone + shockwave), a phase-3 charge, Victory pose on phase
 // transitions and a GetHit stagger when heavy hits interrupt their attacks.
 // Extra clips come from Animations/<Role>_{attack_2,victory,gethit} baked by
 // GolemBossSetup, resolved through CharacterVisual.extraClips.
 public partial class Enemy {
  public bool IsGolemBoss=>false;
  int golemSeenPhase=1;int golemMove;bool golemComboPending;bool golemHitThisCharge;
  float slamReady,chargeReady,chargeUntil;Vector3 chargeDir;
  // Pose override: while set, the AI pauses and the pose clip plays out
  // (victory / gethit / dizzy). Shared with the LavaBoss warden.
  float poseUntil;string poseState="idle";
  void UpdateGolemBoss(float dt){
   var g=RealmGame.I;
   if(g==null||g.Screen!=GameScreen.Playing||state==State.Dead)return;
   if(golemSeenPhase!=phase){
    // Phase transition: victory flourish + arena shockwave, then a shorter
    // summon cooldown keeps phase 2/3 busy.
    golemSeenPhase=phase;ClearGlow();if(warning){Destroy(warning);warning=null;}
    state=State.Recover;timer=1.5f;
    poseState="victory";poseUntil=g.Elapsed+1.35f;Visual.Restart("victory");
    slamReady=g.Elapsed+3.5f;chargeReady=g.Elapsed+5f;
    bossAddAt=g.Elapsed+5f;
     g.Sound("boss_roar_"+Kind,RealmAudio.BossPitch(Kind));g.CameraRig.Shake=.3f;
    return;
   }
   timer-=dt;
   Vector3 delta=g.Player.transform.position-transform.position;delta.y=0;
   float distance=delta.magnitude;
   // Charge in progress: bulldoze toward the locked direction.
   if(chargeUntil>g.Elapsed){
    Visual.Play("run_fast");
    transform.position=Clamp(transform.position+chargeDir*6.4f*dt);
    if(distance<1.9f&&!golemHitThisCharge){
     golemHitThisCharge=true;
     if(g.Player.Damage(2,transform.position))g.Player.ApplyForce(delta.normalized*8f+Vector3.up*2.5f);
     chargeUntil=0;state=State.Recover;timer=1f;
    }
    return;
   }
   if(state==State.Patrol||state==State.Notice){
    Face(g.Player.transform.position,dt);
    if(distance>2.2f){
     Visual.Play(phase>=2?"run":"walk");
     MoveTo(g.Player.transform.position,(phase>=3?1.35f:1f)*1.9f,dt);
    }else Visual.Play("idle");
    if(timer>0)return;
    // Attack selection: slam (close, phase 2+), charge (mid, phase 3),
    // otherwise a melee swing that may combo into a second.
    if(phase>=2&&distance<4.2f&&g.Elapsed>=slamReady){
     golemMove=9;state=State.Windup;timer=1.15f;golemComboPending=false;
     target=transform.position;target.y=baseY;
     Visual.Restart("attack_2");
      warning=CombatTelegraph.Create(transform.position,3.2f,timer,g.World.transform);
      g.Sound("boss_warning",RealmAudio.BossPitch(Kind));
      return;
    }
    if(phase>=3&&distance>5f&&distance<13f&&g.Elapsed>=chargeReady){
     chargeDir=delta.normalized;chargeUntil=g.Elapsed+1.25f;chargeReady=g.Elapsed+7f;
     golemHitThisCharge=false;state=State.Attack;timer=1.3f;
     Visual.Restart("run_fast");
     g.Sound("enemy_dash");
     return;
    }
    if(distance<3f){
     golemMove=attackCount++%5;
     golemComboPending=golemMove==1||(phase>=2&&golemMove==3);
     target=g.Player.transform.position;target.y=baseY;
     attackOrigin=transform.position;
     state=State.Windup;timer=golemComboPending?.55f:.85f-.06f*phase;
     Visual.Restart(golemComboPending?"attack_2":"attack");
     warning=CombatTelegraph.Create(g.Player.transform.position,1.9f,timer,g.World.transform);
     g.Sound("enemy_warning");
     return;
    }
    if(distance>19f){ReleaseToken();state=State.Patrol;timer=0;}
    return;
   }
   if(state==State.Windup){
    Visual.Play(golemMove==9?"attack_2":"attack");
    Glow(new Color(1,.25f,.1f),.2f);
    if(timer<=0){
     ClearGlow();if(warning){Destroy(warning);warning=null;}
     state=State.Attack;timer=.5f;
     if(golemMove==9){
      // Ground slam: radial crushing zone with dust and shockwave.
       StrikeZone.Create(transform.position,3.2f,3,.01f);
      Vfx.Play("ga_vfx_Shockwave_01",transform.position+Vector3.up*.15f,Quaternion.identity,1.4f);
      KenneyPuff.Burst(transform.position+Vector3.up*.25f,new Color(.62f,.56f,.46f),16,1.5f);
       g.Sound("boss",RealmAudio.BossPitch(Kind));g.CameraRig.Shake=.35f;
      slamReady=g.Elapsed+7f;
     }else{
      Vector3 strike=Clamp(golemComboPending?g.Player.transform.position:attackOrigin+transform.forward*1.6f);
      strike.y=baseY;
      StrikeZone.Create(strike,1.9f,2,.01f);
       HitSpark.Burst(strike+Vector3.up*.4f,Vector3.up,new Color(1f,.6f,.2f),12);
       g.Sound("boss",RealmAudio.BossPitch(Kind));
     }
    }
   }else if(state==State.Attack){
    if(timer<=0){
     if(golemComboPending){
      // Chain into the second swing with its own tracking telegraph.
      golemComboPending=false;state=State.Windup;timer=.5f;
      target=g.Player.transform.position;target.y=baseY;
      Visual.Restart("attack_2");
      warning=CombatTelegraph.Create(g.Player.transform.position,1.9f,timer,g.World.transform);
      g.Sound("enemy_warning");
     }else{
      state=State.Recover;timer=Mathf.Max(.7f,1.15f-.1f*phase);
      Visual.Play("idle");
     }
    }
   }else if(state==State.Recover){
    Visual.Play("idle");
    if(timer<=0){state=State.Notice;timer=.15f;}
   }
  }
 }
}
