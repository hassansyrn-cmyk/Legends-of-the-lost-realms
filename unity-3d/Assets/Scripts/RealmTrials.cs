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
  public string Status=>Completed?"RESTORED  /  +"+GoldReward+" GOLD  +2 GEMS":Active?Objective+"  "+Count+"/"+Goal:"Stand beside the trial shrine to begin";
  int level;float dwell;Transform shrine;GameObject shrineVisual;
  readonly List<TrialEcho> echoes=new List<TrialEcho>();
  RealmGame Game=>RealmGame.I;
  public static RealmTrials Build(RealmWorld world,int stage){
   if(world.IsBoss)return null;
   var trial=world.gameObject.AddComponent<RealmTrials>();trial.level=stage;trial.Kind=(TrialKind)((stage-1)%3);
   if(stage==6||stage==12)trial.Kind=TrialKind.Resolve;
   var root=new GameObject("Optional trial shrine");root.transform.SetParent(world.transform,false);root.transform.position=(world.Route.Count>1?world.Route[1]:world.Route.Count>0?world.Route[0]:Vector3.zero)+new Vector3(-2.65f,.14f,0);trial.shrine=root.transform;
   PlaceShrine(world,root.transform);
   Color accent=Color.Lerp(GameColor(),Color.white,.35f);
   string[] shrinePool={"Props/Expansion/CrystalShrine","Props/Expansion/TrailShrine","Props/Expansion/SealStone","Props/Expansion/WatchkeeperBeacon"};
   string shrineAsset=shrinePool[(stage-1)%shrinePool.Length];
   trial.shrineVisual=ExpansionChapter.PlacePuzzleModel(shrineAsset,root.transform,accent,out float visualHeight);
   Transform island=ChapterLayout.IslandOf(root.transform,world.transform);
   ExpansionChapter.ClearOverlappingProps(island,root.transform.position,trial.shrineVisual?trial.shrineVisual.transform.position:root.transform.position,1.5f);
   var label=new GameObject("Trial shrine label");label.transform.SetParent(root.transform,false);
   label.transform.localPosition=(trial.shrineVisual?trial.shrineVisual.transform.localPosition:Vector3.zero)+Vector3.up*(visualHeight+.35f);
   var text=label.AddComponent<TextMesh>();text.text=trial.Title;text.fontSize=34;text.characterSize=.034f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1f,.9f,.65f);
   var font=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");if(font){text.font=font;text.GetComponent<Renderer>().sharedMaterial=font.material;}
   label.AddComponent<ExpansionBillboard>();
   if(trial.Kind==TrialKind.Echoes){
    for(int i=0;i<3;i++){
     int rIdx=Mathf.Min(3+i*3,world.Route.Count-1);
     if(rIdx<0)continue;
     var echo=new GameObject("Trial echo "+(i+1));echo.transform.SetParent(world.transform,false);
     echo.transform.position=world.Route[rIdx]+new Vector3(i%2==0?-1.25f:1.25f,.85f,-1.5f);
     var e=echo.AddComponent<TrialEcho>();e.Owner=trial;e.Origin=echo.transform.position;
     var coreGo=new GameObject("Echo gem");coreGo.transform.SetParent(echo.transform,false);
     RelicArt.Gem(coreGo.transform,accent);
     e.Core=coreGo.transform;
     trial.echoes.Add(e);echo.SetActive(false);
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
   for(int pass=0;pass<2;pass++){
    for(int route=Mathf.Min(1,world.Route.Count-1);route>=0;route--){
     Transform island=null;float nearest=float.MaxValue;
     foreach(Transform t in world.transform){if(t.name!="Island")continue;float d=(t.position-world.Route[route]).sqrMagnitude;if(d<nearest){nearest=d;island=t;}}
     if(!island)continue;
     foreach(float x in new[]{-2.65f,2.65f,-2.1f,2.1f,-1.5f,1.5f,-.85f,.85f})foreach(float z in new[]{0f,2f,-2f,1f,-1f,3f,-3f}){
      Vector3 p=world.Route[route]+new Vector3(x,0,z);
      if(!ChapterLayout.Supported(island,shrine,new Bounds(p,new Vector3(1.05f,.1f,1.05f)),out float y))continue;
      if(pass==0&&!ChapterLayout.Supported(island,shrine,new Bounds(p+Vector3.forward*.68f,new Vector3(.95f,.1f,.95f)),out _))continue;
      p.y=y+.04f;
      if(!ShrineClear(p,world.transform,1.5f))continue;
      if(pass==0&&!ExpansionChapter.ModelSpotClear(p+Vector3.forward*.68f,world.transform,.9f))continue;
      shrine.position=p;
      TrapArt.Reserve(island,island.InverseTransformPoint(p),1.6f);
      return;
     }
    }
   }
   Debug.LogWarning("Trial shrine has no clear supported slot in chapter "+RealmGame.I.Level);
  }
  void Update(){
   var g=Game;if(!g||!g.Player||g.Screen!=GameScreen.Playing)return;
   if(Active||Completed)return;
   Vector3 pPos=g.Player.transform.position;
   Vector3 d=pPos-shrine.position;
   Vector3 dm=pPos-(shrine.position+Vector3.forward*.72f);
   bool near=(new Vector2(d.x,d.z).sqrMagnitude<1.55f*1.55f||new Vector2(dm.x,dm.z).sqrMagnitude<1.55f*1.55f)&&Mathf.Abs(d.y)<1.2f&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f;
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
   if(shrineVisual){
    var props=new MaterialPropertyBlock();props.SetColor("_Color",new Color(1f,.85f,.4f));
    foreach(var r in shrineVisual.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(props);
   }
  }
 }
 public sealed class TrialEcho:MonoBehaviour {
  readonly RewardAnchor anchor=new RewardAnchor();
  public RealmTrials Owner;public Vector3 Origin {get=>anchor.Position;set=>anchor.Bind(transform,value);}public Transform Core;bool collected;
  void Update(){
   var g=RealmGame.I;if(!g||!g.Player||g.Screen!=GameScreen.Playing||!Owner.Active||collected)return;
   transform.position=Origin+Vector3.up*Mathf.Sin(g.Elapsed*1.5f+Origin.z)*.1f;
   if(Core)Core.Rotate(0,24f*Time.deltaTime,0,Space.World);
   if(Vector3.Distance(g.Player.transform.position+Vector3.up*.8f,transform.position)>1.25f)return;
   collected=true;Owner.CollectEcho();g.Sound("gem");Destroy(gameObject);
  }
 }
}
