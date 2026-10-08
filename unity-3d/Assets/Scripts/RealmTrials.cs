using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 public enum TrialKind { Hunt, Echoes, Elements, Resolve }
 // Optional, local-to-this-run objectives. Rewards use the normal chapter bank.
 public sealed class RealmTrials:MonoBehaviour {
  public TrialKind Kind {get;private set;}
  public bool Active {get;private set;}
  public bool Completed {get;private set;}
  public int Count {get;private set;}
  public int Goal=>Kind==TrialKind.Echoes||Kind==TrialKind.Resolve?3:4;
  public int GoldReward=>25+level*2;
  public Vector3 ShrinePosition=>shrine.position;
  public string Title=>Kind==TrialKind.Hunt?"TRIAL OF COURAGE":Kind==TrialKind.Echoes?"ECHOES OF THE REALM":Kind==TrialKind.Resolve?"TRIAL OF RESOLVE":"ELEMENTAL ATTUNEMENT";
  public string Objective=>Kind==TrialKind.Hunt?"Defeat 4 enemies":Kind==TrialKind.Echoes?"Recover 3 luminous echoes":Kind==TrialKind.Resolve?"Deflect or perfectly dodge 3 incoming attacks":"Land 4 weakness hits (match the enemy pip)";
  public string Status=>Completed?"RESTORED  /  +"+GoldReward+" GOLD  +2 GEMS":Active?Objective+"  "+Count+"/"+Goal:"Stand within the shrine ring to begin";
  int level;float dwell;Transform shrine,core;MeshRenderer border;
  readonly List<TrialEcho> echoes=new List<TrialEcho>();
  RealmGame Game=>RealmGame.I;
  public static RealmTrials Build(RealmWorld world,int stage){
   if(world.IsBoss)return null;
   var trial=world.gameObject.AddComponent<RealmTrials>();trial.level=stage;trial.Kind=(TrialKind)((stage-1)%3);
   if(stage==6||stage==12)trial.Kind=TrialKind.Resolve;
   var root=new GameObject("Optional trial shrine");root.transform.SetParent(world.transform,false);root.transform.position=(world.Route.Count>1?world.Route[1]:world.Route.Count>0?world.Route[0]:Vector3.zero)+new Vector3(-2.65f,.14f,0);trial.shrine=root.transform;
   PlaceShrine(world,root.transform);
   Color stone=new Color(.19f,.24f,.27f);Color accent=Color.Lerp(GameColor(),Color.white,.35f);
   Art.Shape("Trial plinth",PrimitiveType.Cylinder,Vector3.down*.04f,new Vector3(.95f,.12f,.95f),stone,root.transform);
   trial.border=CombatTelegraph.Ring(root.transform,1.45f,accent,"Invitation ring");
   trial.core=Art.Crystal(Vector3.up*.7f,.32f,accent,root.transform).transform;
   CombatTelegraph.Ring(root.transform,.65f,accent,"Inner engraving");
   if(trial.Kind==TrialKind.Echoes){
    for(int i=0;i<3;i++){
     int rIdx=Mathf.Min(3+i*3,world.Route.Count-1);
     if(rIdx<0)continue;
     var echo=new GameObject("Trial echo "+(i+1));echo.transform.SetParent(world.transform,false);
     echo.transform.position=world.Route[rIdx]+new Vector3(i%2==0?-1.25f:1.25f,.85f,-1.5f);
     var e=echo.AddComponent<TrialEcho>();e.Owner=trial;e.Origin=echo.transform.position;
     e.Core=Art.Crystal(Vector3.zero,.23f,accent,echo.transform).transform;
     CombatTelegraph.Ring(echo.transform,.42f,accent,"Echo halo");trial.echoes.Add(e);echo.SetActive(false);
    }
   }
   return trial;
  }
  static Color GameColor()=>RealmGame.I?RealmGame.I.Accent:Color.cyan;
  internal static bool ShrineClear(Vector3 point,Transform world,float radius=1.1f){
   foreach(var collider in Physics.OverlapCapsule(point+Vector3.up*.25f,point+Vector3.up*1.5f,radius,~0,QueryTriggerInteraction.Ignore)){
    for(var t=collider.transform;t&&t!=world;t=t.parent)
     if(ChapterLayout.IsProp(t)||ChapterLayout.IsTrap(t)||t.GetComponent<Enemy>())return false;
   }
   return true;
  }
  static void PlaceShrine(RealmWorld world,Transform shrine){
   Physics.SyncTransforms();
   // Prefer the first destination, then the cleared spawn plaza if its scenery is dense.
   for(int route=Mathf.Min(1,world.Route.Count-1);route>=0;route--){
    Transform island=null;float nearest=float.MaxValue;
    foreach(Transform t in world.transform){if(t.name!="Island")continue;float d=(t.position-world.Route[route]).sqrMagnitude;if(d<nearest){nearest=d;island=t;}}
    if(!island)continue;
    foreach(float x in new[]{-2.65f,0f,2.65f,-1.5f,1.5f,-.75f,.75f})foreach(float z in new[]{0f,2f,-2f,1f,-1f,3f,-3f}){
     Vector3 p=world.Route[route]+new Vector3(x,0,z);
     if(!ChapterLayout.Supported(island,shrine,new Bounds(p,new Vector3(1.05f,.1f,1.05f)),out float y))continue;
     p.y=y+.14f;if(!ShrineClear(p,world.transform,1.6f))continue;
     shrine.position=p;return;
    }
   }
   Debug.LogWarning("Trial shrine has no clear supported slot in chapter "+RealmGame.I.Level);
  }
  void Update(){
   var g=Game;if(!g||!g.Player||g.Screen!=GameScreen.Playing)return;
   core.localRotation=Quaternion.Euler(45,g.Elapsed*18f,45);
   core.localPosition=Vector3.up*(.7f+Mathf.Sin(g.Elapsed*1.6f)*.07f);
   if(Active||Completed)return;
   Vector3 d=g.Player.transform.position-shrine.position;
   bool near=new Vector2(d.x,d.z).sqrMagnitude<1.45f*1.45f&&Mathf.Abs(d.y)<1.2f&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f;
   dwell=near?dwell+Time.deltaTime:0;
   if(dwell>=.85f)Begin();
  }
  public void Begin(){
   if(Active||Completed)return;Active=true;
   foreach(var e in echoes)if(e)e.gameObject.SetActive(true);
   Game.Tell(Title+"  /  "+Objective+". Reward: "+GoldReward+" gold + 2 gems.",5f);
   Game.Sound("checkpoint");Vfx.Play("ga_vfx_Sparks_01",shrine.position+Vector3.up*.7f,Quaternion.identity,.6f);
  }
  public void EnemyDefeated(){if(Kind==TrialKind.Hunt)Advance();}
  public void WeaknessHit(){if(Kind==TrialKind.Elements)Advance();}
  public void CollectEcho(){if(Kind==TrialKind.Echoes)Advance();}
  public void PerfectDefense(){if(Kind==TrialKind.Resolve)Advance();}
  void Advance(){
   if(!Active||Completed)return;
   Count=Mathf.Min(Goal,Count+1);if(Count<Goal){Game.Tell(Title+"  /  "+Count+"/"+Goal+" — "+Objective,2.5f);return;}
   Completed=true;Active=false;Game.Coins+=GoldReward;Game.Gems+=2;
   Game.Sound("upgrade");Game.Tell(Title+" RESTORED  /  +"+GoldReward+" gold +2 gems. Finish the chapter to bank them.",5f);
   DamageTip.Show(Game.Player.transform.position+Vector3.up*2f,"TRIAL COMPLETE",new Color(1f,.85f,.4f));
   Vfx.Play("eric_FX_LootDrop_Blue",Game.Player.transform.position,Quaternion.identity,.65f);
   var props=new MaterialPropertyBlock();props.SetColor("_Color",new Color(1f,.82f,.4f));border.SetPropertyBlock(props);
  }
 }
 public sealed class TrialEcho:MonoBehaviour {
  readonly RewardAnchor anchor=new RewardAnchor();
  public RealmTrials Owner;public Vector3 Origin {get=>anchor.Position;set=>anchor.Bind(transform,value);}public Transform Core;bool collected;
  void Update(){
   var g=RealmGame.I;if(!g||!g.Player||g.Screen!=GameScreen.Playing||!Owner.Active||collected)return;
   transform.position=Origin+Vector3.up*Mathf.Sin(g.Elapsed*1.5f+Origin.z)*.1f;
   Core.Rotate(0,24f*Time.deltaTime,0,Space.World);
   if(Vector3.Distance(g.Player.transform.position+Vector3.up*.8f,transform.position)>1.25f)return;
   collected=true;Owner.CollectEcho();g.Sound("gem");Destroy(gameObject);
  }
 }
}
