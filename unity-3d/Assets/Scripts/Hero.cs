using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
namespace LostRealms {
 [DefaultExecutionOrder(20)] public class Hero:MonoBehaviour {
  public CharacterController Controller;public int MaxHealth,Health,Power;public float Energy=100;public bool Grounded=>Controller&&Controller.isGrounded; public CharacterVisual Visual;
  Vector3 velocity;float vertical,jumpGrace,dashUntil,dashReady,immuneUntil,attackReady,comboUntil,chargeStart;int jumps,combo;bool charging;
  void Awake(){Controller=gameObject.AddComponent<CharacterController>();Controller.height=1.75f;Controller.radius=.32f;Controller.center=Vector3.up*.9f;Controller.stepOffset=.35f;Controller.slopeLimit=48;MaxHealth=5+RealmGame.I.Save.healthRank;Health=MaxHealth;Visual=CharacterVisual.Create("Aster",transform,1.8f,new Color(.25f,.55f,.57f));}
  void Update(){var g=RealmGame.I;if(g.Screen!=GameScreen.Playing){if(g.Screen==GameScreen.Defeated)Visual.Play("death");else Visual.Play("idle");return;}float dt=Time.deltaTime;
   Vector3 forward=Quaternion.Euler(0,g.CameraRig.Yaw,0)*Vector3.forward;Vector3 right=Quaternion.Euler(0,g.CameraRig.Yaw,0)*Vector3.right;Vector3 wish=forward*g.MoveInput.y+right*g.MoveInput.x;
   if(Grounded){jumps=0;jumpGrace=.12f;if(vertical<0)vertical=-2;}else jumpGrace-=dt;
   if(g.JumpPressed&&(jumps<2||jumpGrace>0)){vertical=8.4f;jumps=jumpGrace>0?1:jumps+1;jumpGrace=0;g.Sound(jumps==1?"jump":"double_jump");}
   if(g.DashPressed&&RealmGame.I.Elapsed>=dashReady){dashUntil=RealmGame.I.Elapsed+.21f;dashReady=RealmGame.I.Elapsed+.85f;immuneUntil=Mathf.Max(immuneUntil,dashUntil+.1f);if(wish.sqrMagnitude<.1f)wish=transform.forward;velocity=wish.normalized*16;g.Sound("player_dash");}
   if(wish.sqrMagnitude>.02f)transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(wish),dt*16);
   if(RealmGame.I.Elapsed>=dashUntil){float accel=g.Realm==2&&Grounded?7:16;velocity=Vector3.Lerp(velocity,wish*6.1f,dt*accel);} vertical-=23*dt;
   Controller.Move((velocity+Vector3.up*vertical)*dt);Energy=Mathf.Min(100,Energy+dt*12);
   if(g.AttackPressed){charging=true;chargeStart=RealmGame.I.Elapsed;}if(charging&&g.AttackReleased){Attack(RealmGame.I.Elapsed-chargeStart>=.4f);charging=false;}if(charging&&RealmGame.I.Elapsed-chargeStart>=.8f){Attack(true);charging=false;}
   if(g.CastPressed)Cast();
   if(transform.position.y<-12){Health=0;g.DamageTaken++;g.Defeat();}
   if(RealmGame.I.Elapsed<attackReady-.12f)Visual.Play("attack");else Visual.Play(!Grounded?"jump":wish.sqrMagnitude>.01f?"walk":"idle");
  }
  public void Warp(Vector3 p){Controller.enabled=false;transform.position=p;Controller.enabled=true;vertical=0;velocity=Vector3.zero;immuneUntil=RealmGame.I.Elapsed+1.2f;RealmGame.I.CameraRig.Snap();}
  public void Damage(int damage,Vector3 source){if(RealmGame.I.Elapsed<immuneUntil||Health<=0)return;Health=Mathf.Max(0,Health-damage);RealmGame.I.DamageTaken+=damage;immuneUntil=RealmGame.I.Elapsed+1;velocity=(transform.position-source).normalized*4;RealmGame.I.Sound("hurt");RealmGame.I.CameraRig.Shake=.18f;if(Health<=0)RealmGame.I.Defeat();}
  void Attack(bool charged){if(RealmGame.I.Elapsed<attackReady)return;var g=RealmGame.I;combo=RealmGame.I.Elapsed<comboUntil?(combo%4)+1:1;comboUntil=RealmGame.I.Elapsed+1;attackReady=RealmGame.I.Elapsed+(charged?.7f:.4f);float reach=charged?3.3f:2.6f;float damage=charged?3:combo==4?2:1;g.Sound("blade");Visual.Restart("attack");
   foreach(var e in g.Enemies.ToArray()){if(!e||e.Health<=0)continue;Vector3 delta=e.transform.position-transform.position;delta.y=0;if(delta.magnitude<reach+e.Radius&&Vector3.Dot(transform.forward,delta.normalized)>-.1f)e.Hit(damage+(g.Save.powerRank*.15f),Power,false);}
   Pulse.Create(transform.position+Vector3.up*.8f+transform.forward,charged?1.8f:1.1f,new Color(1,.87f,.54f),.25f);
  }
  void Cast(){var g=RealmGame.I;float cost=28-g.Save.powerRank*2;if(Energy<cost){g.Sound("power_fail");g.Tell("Wait for your energy to recharge.");return;}Energy-=cost;g.Sound(Power==0?"ember_cast":Power==1?"frost_cast":"gale_cast");Pulse.Create(transform.position+Vector3.up*.5f,5,new[]{new Color(1,.32f,.12f),new Color(.35f,.8f,1),new Color(.5f,1,.7f)}[Power],.55f);
   foreach(var e in g.Enemies.ToArray())if(e&&e.Health>0&&Vector3.Distance(transform.position,e.transform.position)<6)e.Hit(2+g.Save.powerRank,Power,true);
   if(Power==2){foreach(var p in FindObjectsByType<EnemyBolt>())if(Vector3.Distance(transform.position,p.transform.position)<7)Destroy(p.gameObject);vertical=Mathf.Max(vertical,5);}
  }
 }
 public class CharacterVisual:MonoBehaviour {
  Animator animator;PlayableGraph graph;AnimationMixerPlayable mixer;AnimationClipPlayable[] playable=new AnimationClipPlayable[2];AnimationClip[] clips=new AnimationClip[5];string current="";float blend;int slot;bool hasGraph;Transform fallbackBody;
  static readonly string[] Names={"idle","walk","attack","death","jump"};
  public static CharacterVisual Create(string role,Transform parent,float height,Color color){var holder=new GameObject(role+" visual");holder.transform.SetParent(parent,false);var v=holder.AddComponent<CharacterVisual>();
   var prefab=Resources.Load<GameObject>("Characters/"+role);if(prefab){var model=Instantiate(prefab,holder.transform);model.transform.localPosition=Vector3.zero;v.animator=model.GetComponentInChildren<Animator>();if(v.animator){v.animator.applyRootMotion=false;v.animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}}
   else{v.fallbackBody=Art.Shape("Body",PrimitiveType.Capsule,Vector3.up*.9f,new Vector3(.65f,.65f,.5f),color,holder.transform).transform;Art.Shape("Head",PrimitiveType.Sphere,Vector3.up*1.65f,Vector3.one*.45f,new Color(.85f,.71f,.52f),holder.transform);Art.Shape("Blade",PrimitiveType.Cube,new Vector3(.5f,.9f,.28f),new Vector3(.1f,.9f,.1f),Color.white,holder.transform);holder.transform.localScale=Vector3.one*height/1.8f;}
   for(int i=0;i<Names.Length;i++)v.clips[i]=Resources.Load<AnimationClip>("Animations/"+role+"_"+Names[i])??Resources.Load<AnimationClip>("Animations/Shared/"+Names[i]);
   if(v.animator&&System.Array.Exists(v.clips,c=>c!=null)){v.graph=PlayableGraph.Create(role+" motion");v.graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);v.mixer=AnimationMixerPlayable.Create(v.graph,2);var output=AnimationPlayableOutput.Create(v.graph,"Character",v.animator);output.SetSourcePlayable(v.mixer);v.hasGraph=true;v.graph.Play();}
   return v;
  }
  public void Restart(string state){current="";Play(state);}
  public void Play(string state){if(current==state)return;current=state;if(!hasGraph)return;int i=System.Array.IndexOf(Names,state);AnimationClip clip=i>=0?clips[i]:null;if(!clip)clip=clips[0]?clips[0]:clips[1];if(!clip)return;
   slot=1-slot;if(playable[slot].IsValid()){mixer.DisconnectInput(slot);graph.DestroyPlayable(playable[slot]);}playable[slot]=AnimationClipPlayable.Create(graph,clip);playable[slot].SetApplyFootIK(false);graph.Connect(playable[slot],0,mixer,slot);blend=0;
  }
  void Update(){if(hasGraph){float speed=RealmGame.I.Screen==GameScreen.Playing?1:0;graph.GetRootPlayable(0).SetSpeed(speed);blend=Mathf.MoveTowards(blend,1,Time.deltaTime*8);mixer.SetInputWeight(slot,blend);mixer.SetInputWeight(1-slot,1-blend);}else if(!fallbackBody){transform.localPosition=Vector3.up*(.15f+Mathf.Sin(RealmGame.I.Elapsed*1.8f)*.12f);transform.localRotation=Quaternion.Euler(0,0,Mathf.Sin(RealmGame.I.Elapsed)*2);}else if(fallbackBody){float bob=current=="walk"?Mathf.Sin(RealmGame.I.Elapsed*11)*.07f:Mathf.Sin(RealmGame.I.Elapsed*2)*.015f;fallbackBody.localPosition=Vector3.up*(.9f+bob);}}
  void OnDestroy(){if(hasGraph&&graph.IsValid())graph.Destroy();}
 }
 public class FollowCamera:MonoBehaviour {
  public Transform Target;public float Yaw,Shake;Vector3 velocity;bool snapped;public void Snap(){snapped=false;velocity=Vector3.zero;}
  void LateUpdate(){if(!Target)return;Vector3 focus=Target.position+Vector3.up*1.15f;Vector3 offset=Quaternion.Euler(0,Yaw,0)*new Vector3(0,4.1f,-7.2f);Vector3 desired=focus+offset;
   if(Physics.SphereCast(focus,.22f,offset.normalized,out var hit,offset.magnitude,~0,QueryTriggerInteraction.Ignore)&&hit.collider.gameObject!=Target.gameObject)desired=focus+offset.normalized*Mathf.Max(1.5f,hit.distance-.2f);
   if(!snapped){transform.position=desired;snapped=true;}else transform.position=Vector3.SmoothDamp(transform.position,desired,ref velocity,.12f);
   transform.LookAt(focus+Vector3.up*.25f);if(Shake>0){Shake-=Time.deltaTime;transform.position+=Random.insideUnitSphere*.07f;}
  }
 }
 public class Pulse:MonoBehaviour {float age,duration,radius;Color color;public static void Create(Vector3 at,float radius,Color color,float duration){var root=Art.Ring(at,.5f,color,RealmGame.I.World.transform);var p=root.AddComponent<Pulse>();p.duration=duration;p.radius=radius;p.color=color;}void Update(){if(RealmGame.I.Screen!=GameScreen.Playing)return;age+=Time.deltaTime;transform.localScale=Vector3.one*Mathf.Lerp(.15f,radius*2,age/duration);if(age>=duration)Destroy(gameObject);}}
}




