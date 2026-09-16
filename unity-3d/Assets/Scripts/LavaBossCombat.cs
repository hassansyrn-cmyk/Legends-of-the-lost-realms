using UnityEngine;

namespace LostRealms {
 public partial class Enemy {
  public bool IsLavaBoss=>Boss&&Kind==21;
  int lavaPhase;int lavaMove;float lavaWindup;string lavaAction;bool lavaRoaring;
  void UpdateLavaBoss(float dt){
   var g=RealmGame.I;
   if(lavaPhase!=phase){
    lavaPhase=phase;if(warning)Destroy(warning);ClearGlow();Visual.transform.localPosition=Vector3.zero;
    state=State.Recover;timer=phase==1?2f:2.4f;lavaRoaring=true;
    Visual.PlayBossAction(phase==3?"flex":"roar",timer);g.Sound("boss_roar");return;
   }
   timer-=dt;
   Vector3 delta=g.Player.transform.position-transform.position;delta.y=0;
   if(state==State.Patrol||state==State.Notice){
    Face(g.Player.transform.position,dt);
    if(delta.magnitude>4.1f){Visual.Play(phase>=2?"run":"walk");MoveTo(g.Player.transform.position,phase>=2?2.7f:1.9f,dt);}
    else Visual.Play("idle");
    if(timer>0||delta.magnitude>9f)return;
    lavaMove=attackCount++%3;
    // Greatsword takes carry the weapon strikes; the creature jump is a body slam.
    lavaAction=lavaMove==0?"slash":lavaMove==1?(phase>=2?"overhead":"attack_2"):(phase==3?"charged":"attack_3");
    if(lavaMove!=2&&delta.magnitude>5.4f){timer=.2f;return;}
    attackOrigin=transform.position;
    target=lavaMove==2?Clamp(g.Player.transform.position):Clamp(transform.position+transform.forward*2.4f);
    lavaWindup=lavaMove==2?1.25f:lavaMove==1?1.05f:.85f;
    timer=lavaWindup;state=State.Windup;
    Visual.PlayBossAction(lavaAction,lavaWindup+.65f);
    warning=CombatTelegraph.Create(target,lavaMove==2?2.7f:2.1f,lavaWindup,g.World.transform);
    g.Sound("enemy_warning");
   }else if(state==State.Windup){
    Glow(new Color(1,.2f,.08f),.15f);
    if(lavaMove==2){float t=1-Mathf.Clamp01(timer/lavaWindup);transform.position=Vector3.Lerp(attackOrigin,target,Mathf.SmoothStep(0,1,t));Visual.transform.localPosition=Vector3.up*Mathf.Sin(t*Mathf.PI)*1.15f;}
    if(timer>0)return;
    Visual.transform.localPosition=Vector3.zero;ClearGlow();if(warning)Destroy(warning);
    state=State.Attack;timer=.65f;
    StrikeZone.Create(target,lavaMove==2?2.7f:2.1f,lavaMove==2?3:2,.01f);
    Vfx.Play("ga_vfx_Shockwave_01",target+Vector3.up*.08f,Quaternion.identity,lavaMove==2?1.3f:.65f);
    ImpactMarks.Place(target,lavaMove==2?1.2f:.6f,new Color(.33f,.17f,.1f));g.Sound("boss");
    if(phase>=2&&lavaMove==1){for(int i=-1;i<=1;i++){Vector3 direction=Quaternion.Euler(0,i*20,0)*transform.forward;EnemyBolt.Create(transform.position+Vector3.up*1.4f,direction,5.5f,2,true,17);}}
   }else if(state==State.Attack){if(timer<=0){state=State.Recover;timer=phase==3?.85f:1.25f;Visual.Play("idle");}}
   else if(state==State.Recover){
    Visual.transform.localPosition=Vector3.zero;
    if(!lavaRoaring)Visual.Play("idle");
    if(timer<=0){lavaRoaring=false;state=State.Notice;timer=.2f;}
   }
  }
 }

 // World-space socket avoids inheriting the Mixamo armature's centimetre scale.
 public sealed class LavaBossWeapon:MonoBehaviour {
  Transform hand;Transform model;
  public static void Attach(CharacterVisual visual){
   if(!visual||!visual.animator||!visual.animator.isHuman)return;
   var hand=visual.animator.GetBoneTransform(HumanBodyBones.RightHand);if(!hand)return;
   var root=new GameObject("Warden Juggernaut greatsword");root.transform.SetParent(visual.transform,false);
   var weapon=root.AddComponent<LavaBossWeapon>();weapon.hand=hand;
   var created=WeaponCatalog.CreateModel(WeaponId.Juggernaut,root.transform);if(!created)return;weapon.model=created.transform;
   weapon.model.localScale*=1.85f;weapon.model.localRotation=Quaternion.Euler(WeaponCatalog.Get(WeaponId.Juggernaut).EquipEuler);
   weapon.LateUpdate();
  }
  void LateUpdate(){if(hand)transform.SetPositionAndRotation(hand.position,hand.rotation);}
 }
}
