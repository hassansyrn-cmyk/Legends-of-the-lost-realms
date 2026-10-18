using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 // Publish all ferry transforms together before the character controllers step.
 [DefaultExecutionOrder(-5)] public sealed class ExplorationPhysicsStep:MonoBehaviour {
  void FixedUpdate(){var g=RealmGame.I;if(g&&g.Screen==GameScreen.Playing)Physics.SyncTransforms();}
 }
 // Chapter-local exploration state survives falls/checkpoints, but resets on restart.
 public sealed class RealmExploration:MonoBehaviour {
  public readonly List<ExplorationSite> Sites=new List<ExplorationSite>();
  public readonly List<Vector2Int> Links=new List<Vector2Int>();
  public readonly List<ExplorationFind> Finds=new List<ExplorationFind>();
  public readonly List<ExplorationCrossing> Crossings=new List<ExplorationCrossing>();
  public Vector3 GatePosition;public int Keys;public const int RequiredKeys=3;
  public bool GateRevealed {get;private set;}public bool ObjectivesReady=>Keys==RequiredKeys;
  public bool Astral;public string District;
  bool announced;float nextScan;GameObject beacon;
  public string Status=>"KEYS "+Keys+"/3";
  public void Collect(ExplorationFind find){
   if(find.Claimed)return;find.Claimed=true;var g=RealmGame.I;
   if(find.Key){Keys++;g.Sound("upgrade");g.Tell(find.Title+" recovered — "+Keys+" / 3 gate keys",4);}
   else {g.Coins+=60;g.Gems+=2;g.Sound("discover");g.Tell("HIDDEN CACHE — +60 Gold, +2 Gems. Finish the chapter to bank them.",4);}
   Vfx.Play("ga_vfx_LootDrop_01",find.transform.position+Vector3.up*.7f,Quaternion.identity,.85f);
   find.gameObject.SetActive(false);
  }
  void Update(){
   var g=RealmGame.I;if(!g||!g.Player||g.Screen!=GameScreen.Playing)return;
   if(g.Elapsed>=nextScan){nextScan=g.Elapsed+.3f;
    foreach(var site in Sites)if(site&&Vector3.Distance(g.Player.transform.position,site.transform.position)<19){
     if(!site.Visited){site.Visited=true;if(!g.NoticeActive)g.Tell(site.Title,2.2f);}District=site.Title;
    }
    if(Vector3.Distance(g.Player.transform.position,GatePosition)<19)GateRevealed=true;
   }
   if(g.GateOpen()&&!announced){announced=true;GateRevealed=true;g.Tell("THE GATE IS AWAKENING — follow the rising light. Your field map marks its island.",6);
    beacon=new GameObject("Awakened gate beacon");beacon.transform.SetParent(transform,false);beacon.transform.position=GatePosition;
    var line=beacon.AddComponent<LineRenderer>();line.sharedMaterial=StormArt.Beam;line.positionCount=2;line.SetPosition(0,GatePosition+Vector3.up);line.SetPosition(1,GatePosition+Vector3.up*28);line.startWidth=.28f;line.endWidth=.06f;line.startColor=new Color(.45f,.85f,1,.8f);line.endColor=new Color(.7f,.85f,1,0);
    Vfx.Play("eric_FX_LightPillar",GatePosition,Quaternion.identity,1.3f);
   }
  }
 }
 public sealed class ExplorationSite:MonoBehaviour {public int Index;public string Title;public float Width;public bool Visited;public bool Objective;}
 public sealed class ExplorationCrossing {public readonly List<Transform> Stops=new List<Transform>();public bool Ferry;}
 public sealed class ExplorationFind:MonoBehaviour {
  public RealmExploration Owner;public bool Key,Claimed;public string Title;public GameObject Visual;float dwell;
  public bool Guarded {get {foreach(var e in RealmGame.I.Enemies)if(e&&e.Health>0&&!e.Boss&&Vector3.Distance(e.transform.position,transform.position)<8)return true;return false;}}
  void Update(){var g=RealmGame.I;if(Claimed||!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   bool near=g.Player.Grounded&&Vector3.Distance(g.Player.transform.position,transform.position)<1.7f;
   dwell=near&&!Guarded?dwell+Time.deltaTime:0;InteractionCountdown.Report(transform,dwell,.65f,"COLLECTING");if(dwell>=.65f)Owner.Collect(this);
   if(Visual&&Key)Visual.transform.localRotation=Quaternion.Euler(0,g.Elapsed*22,0);
  }
 }
 public partial class RealmWorld {
  public RealmExploration Exploration {get;private set;}
  static readonly Vector2[] ExplorationLayout={new Vector2(0,-42),new Vector2(0,0),new Vector2(-42,0),new Vector2(-42,42),new Vector2(0,42),new Vector2(42,42),new Vector2(42,0),new Vector2(42,-42),new Vector2(0,-84),new Vector2(-42,-42),new Vector2(0,84),new Vector2(-42,-84)};
  static readonly Vector2Int[] ExplorationConnections={new Vector2Int(0,1),new Vector2Int(1,2),new Vector2Int(2,3),new Vector2Int(3,4),new Vector2Int(4,5),new Vector2Int(5,6),new Vector2Int(6,7),new Vector2Int(7,0),new Vector2Int(0,8),new Vector2Int(2,9),new Vector2Int(9,0),new Vector2Int(4,10),new Vector2Int(8,11),new Vector2Int(9,11)};
  static readonly string[] StormDistricts={"WAYFARER'S LANDING","THE BROKEN CROSSROADS","COPPER WATCH","THE WESTERN KEYHOUSE","TEMPEST GARDENS","LIGHTNING BASTION","THE DROWNED ARCHIVE","SMUGGLER'S PERCH","THE LOWER VAULT","WATCHKEEPER'S REST","THE HIGH CONDUCTOR","THE LOST GATE"};
  static readonly string[] AstralDistricts={"PILGRIM'S LANDING","THE SHATTERED CROSSROADS","WARDEN'S TREASURY","THE MOON OBSERVATORY","STARFALL GARDENS","THE CELESTIAL ARSENAL","THE HOLLOW ARCHIVE","THE SUN OBSERVATORY","THE FORGOTTEN VAULT","ARCHIVIST'S REST","THE ECLIPSE OBSERVATORY","THE HIDDEN GATE"};
  void BuildExploration(int stage,bool astral){
   level=stage;realm=astral?5:4;IsBoss=stage==(astral?25:20);random=new System.Random(stage*793);
   accent=astral?new Color(.64f,.55f,1):new Color(.35f,.78f,1);top=new Color(.5f,.56f,.6f);stone=new Color(.19f,.25f,.33f);
   Exploration=gameObject.AddComponent<RealmExploration>();Exploration.Astral=astral;
   gameObject.AddComponent<ExplorationPhysicsStep>();
   var sun=new GameObject("Realm sunlight").AddComponent<Light>();sun.transform.SetParent(transform,false);sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.intensity=1.1f;
   StormChapter storm=null;AstralChapter stars=null;if(astral){stars=gameObject.AddComponent<AstralChapter>();stars.Chapter=stage;}else{storm=gameObject.AddComponent<StormChapter>();storm.Chapter=stage;}
   int variant=(stage-(astral?21:16));var layout=(Vector2[])ExplorationLayout.Clone();
   // Different districts expand in different chapters; loops and return routes remain legible.
   if(variant==1){layout[3]+=new Vector2(-14,5);layout[7]+=new Vector2(14,-6);layout[10]+=new Vector2(13,8);}
   if(variant==2){layout[2].x-=12;layout[3].x-=12;layout[5].x+=12;layout[6].x+=12;layout[11]+=new Vector2(-12,-12);}
   if(variant==3){layout[4].y+=10;layout[10].y+=19;layout[8].x+=14;layout[9].x-=12;}
   if(variant==4){layout[11]=new Vector2(-88,42);layout[3].x-=6;}
   float turn=variant==1?90:variant==2?180:variant==3?-90:0;var rotation=Quaternion.Euler(0,turn,0);
   var positions=new Vector3[layout.Length];var widths=new float[layout.Length];var islands=new Transform[layout.Length];
   var puzzles=astral?new[]{3,7,10}:new[]{2,5,10};var keys=astral?new[]{2,6,8}:new[]{3,6,8};
   for(int i=0;i<layout.Length;i++){
    float height=i==0?0:(i==10?2.4f:i==8?-1.2f:(i%3)*.65f);if(i==11&&IsBoss)height=1.3f;
    positions[i]=rotation*new Vector3(layout[i].x,height,layout[i].y);bool puzzle=System.Array.IndexOf(puzzles,i)>=0;
    widths[i]=i==11&&IsBoss?30:puzzle?17:i==1?17:13;
    islands[i]=ExplorationIsland(positions[i],widths[i],i,astral,false);Route.Add(positions[i]);
    var site=islands[i].gameObject.AddComponent<ExplorationSite>();site.Index=i;site.Title=(astral?AstralDistricts:StormDistricts)[i];site.Width=widths[i];site.Objective=puzzle||System.Array.IndexOf(keys,i)>=0;site.Visited=i==0;Exploration.Sites.Add(site);
    if(i==0)Spawn=positions[i]+Vector3.up*.08f;
   }
   for(int i=0;i<ExplorationConnections.Length;i++){
    var link=ExplorationConnections[i];if(IsBoss&&link.y==11)continue;
    // Later chapters open different circular return routes, not a single forward runway.
    ExplorationConnect(islands[link.x],widths[link.x],islands[link.y],widths[link.y],i,astral,variant);Exploration.Links.Add(link);
   }
   if(IsBoss){var link=new Vector2Int(3,11);ExplorationConnect(islands[3],widths[3],islands[11],widths[11],14,astral,variant);Exploration.Links.Add(link);}
   for(int i=0;i<islands.Length;i++){
    var island=islands[i];float w=widths[i];bool puzzle=System.Array.IndexOf(puzzles,i)>=0;
    if(i==0||i==1||i==4||i==9){Checkpoint(island,positions[i],w,w);if(i>0)HealPickup(positions[i]+new Vector3(2,.6f,-1));}
    if(i>0&&i!=9&&i!=11){
     int[] cast=astral?new[]{29,30,16,20,15,18}:new[]{26,27,22,16,18,25};
     int kind=puzzle?(astral?29:26):cast[(i+variant)%cast.Length];SpawnEnemy(positions[i]+new Vector3(-2,.08f,1.5f),kind,false,positions[i],w,w);
     if(puzzle||i==6||i==8)SpawnEnemy(positions[i]+new Vector3(3,.08f,1.5f),astral?30:27,false,positions[i],w,w);
    }
    if(puzzle){if(astral)stars.AddRoute(island,stars.Mechanisms.Count);else storm.AddCircuit(island,stage>=18?3:2,storm.Circuits.Count);}
    if(i>0&&!puzzle&&i!=9&&i!=11)ExplorationTrap(island,i+variant,astral);
    if(i==0||i==1||i==4||i==9||i==11)ExplorationLandmark(island,i,astral,w);
    ExplorationDress(island,i,astral,w,puzzle);
    for(int n=0;n<3;n++)Pickup(positions[i]+new Vector3((n-1)*1.35f,.5f,-2),false);
    if(i>0&&i%3==0)Pickup(positions[i]+new Vector3(2,.65f,-2),true);
   }
   for(int k=0;k<keys.Length;k++)ExplorationTreasure(islands[keys[k]],true,k);
   ExplorationTreasure(islands[astral?5:7],false,0);ExplorationTreasure(islands[9],false,1);
   if(astral)stars.Record=AstralArt.Place("Record",islands[9],new Vector3(-3,0,1),Vector3.one*1.4f,true).transform;
   else {var rest=new GameObject("Watchkeeper memory").transform;rest.SetParent(islands[9],false);rest.localPosition=new Vector3(-3,0,1);storm.AddMemory(rest);}
   var gateCenter=positions[11];Exploration.GatePosition=gateCenter+Vector3.forward*(IsBoss?10:3);Gate(Exploration.GatePosition);EndZ=gateCenter.z+7;
   if(IsBoss){new GameObject("BossArenaMarker").transform.SetParent(islands[11],false);SpawnEnemy(gateCenter+Vector3.up*.08f,astral?31:28,true,gateCenter,30,30);if(astral)stars.AddSeals(islands[11]);else storm.AddArenaRods(islands[11]);}
   ChapterLayout.PlaceTurretsOnDeck(transform);ChapterLayout.GroundTraps(transform);ChapterLayout.SettleEnemies(transform);
   RealmAtmosphere.Apply(this,realm);ExplorationBackdrop(astral);Physics.SyncTransforms();
  }
  Transform ExplorationIsland(Vector3 position,float width,int index,bool astral,bool small){
   var root=new GameObject("Island").transform;root.SetParent(transform,false);root.position=position;
   bool natural=small?index%3!=0:index==0||index==4||index==6||index==8;
   if(natural){
    string terrain=(astral?"R2_":"")+"Island_"+(small?"SteppingStone":(index%2==0?"Meadow":"Plateau"));
    var land=ExplorationArt.Fit("Islands/"+terrain,"Islands/Textures/"+terrain+"_basecolor",root,Vector3.zero,new Vector3(width,small?3.2f:5.8f,width),true,true,astral?new Color(.82f,.76f,1):new Color(.72f,.88f,.77f));
    land.name="IslandMeshVisual";Physics.SyncTransforms();
    foreach(var hit in Physics.RaycastAll(position+Vector3.up*8,Vector3.down,20,~0,QueryTriggerInteraction.Ignore))if(hit.transform.IsChildOf(land.transform)&&hit.normal.y>.55f){land.transform.position+=Vector3.up*(position.y-hit.point.y);break;}
    Physics.SyncTransforms();return root;
   }
   // Supplied terraces include their own complete cliff keel. Uniform scaling
   // preserves the masonry and avoids overlapping a second terrain mesh.
   var terrace=astral?AstralArt.Place("Terrace",root,Vector3.zero,Vector3.one*width,true):StormArt.Place("Terrace",root,Vector3.zero,Vector3.one*width,true);
   terrace.name="IslandMeshVisual";
   return root;
  }
  void ExplorationConnect(Transform a,float aw,Transform b,float bw,int id,bool astral,int variant){
   var crossing=new ExplorationCrossing();crossing.Stops.Add(a);Exploration.Crossings.Add(crossing);
   var delta=b.position-a.position;var horizontal=new Vector3(delta.x,0,delta.z);var direction=horizontal.normalized;
   // The octagonal/circular decks have less reach on diagonals; use an inscribed footprint.
   var start=a.position+direction*(aw*.42f);var end=b.position-direction*(bw*.42f);float length=Vector3.Distance(start,end);
   bool ferry=id%4==2;crossing.Ferry=ferry;int count=Mathf.Max(1,Mathf.CeilToInt((length-3.4f)/8f));
   for(int k=0;k<count;k++){
    float u=(k+1f)/(count+1f);var p=Vector3.Lerp(start,end,u);float width=ferry?5.8f:5.2f;
    var step=ExplorationIsland(p,width,100+id*10+k,astral,true);
    crossing.Stops.Add(step);
    // Frequent moving routes, with full docking pauses at the authored route.
    // Fixed refuges remain between crossings; motion is shared within each chain.
    if(id%5!=0){
     var mover=step.gameObject.AddComponent<MovingIsland>();mover.Origin=p;
     var side=Vector3.Cross(direction,Vector3.up);int pattern=(id+variant)%3;
     mover.Style=pattern==0?IslandMotionStyle.ExplorationOrbit:pattern==1?IslandMotionStyle.ExplorationEight:IslandMotionStyle.ExplorationLift;
     float amplitude=3.6f+variant*.3f;
     mover.Offset=pattern==2?Vector3.up*1.65f+side*2.2f:side*amplitude;
     mover.CrossAxis=pattern==0?direction*1.3f+Vector3.up*1.3f:pattern==1?direction*1.5f+Vector3.up*.9f:direction*1.5f;
     mover.Speed=.85f+variant*.035f;mover.TravelTime=6.6f;mover.DwellTime=1.65f;mover.PhaseOffset=(id%4)*2.1f;
    }
    if(k==0&&id%3==1){if(astral){var vent=AstralArt.Place("FractureVent",step,Vector3.up*.025f,new Vector3(1.7f,.12f,1.7f));vent.AddComponent<AstralFracture>();}else{var wind=step.gameObject.AddComponent<StormWindWindow>();wind.Length=4;wind.Direction=Vector3.Cross(direction,Vector3.up);wind.PhaseOffset=id;}}
   }
   crossing.Stops.Add(b);
  }
  void ExplorationTrap(Transform island,int pick,bool astral){
   var spot=new Vector3(-3.8f,.06f,3.1f);switch(pick%4){
    case 0:DartTurret.Place(island,spot,accent,stone,astral?DartTurret.TurretKind.Dragon:DartTurret.TurretKind.Stone);break;
    case 1:CrusherPillar.Place(island,spot,accent,stone);break;
    case 2:if(astral)FrostTotem.Place(island,spot,accent,stone);else SerpentStatue.Place(island,spot,accent,stone);break;
    case 3:if(astral){var go=AstralArt.Place("FractureVent",island,spot,new Vector3(2.4f,.15f,2.4f));go.AddComponent<AstralFracture>();}else FlameBrazier.Place(island,spot,accent,stone);break;
   }
  }
  void ExplorationTreasure(Transform island,bool key,int number){
   var localPos=new Vector3(0,0,3.1f);
   if(RealmProps.TryDeckSurface(island,localPos.x,localPos.z,null,out float deckY))localPos.y=deckY;
   var root=new GameObject(key?"Gate key "+number:"Hidden supply cache");root.transform.SetParent(island,false);root.transform.localPosition=localPos;
   TrapArt.Reserve(island,localPos,1.6f);
   var find=root.AddComponent<ExplorationFind>();find.Owner=Exploration;find.Key=key;find.Title=key?new[]{"WATCH KEY","ARCHIVE KEY","VAULT KEY"}[number]:"HIDDEN CACHE";Exploration.Finds.Add(find);
   string modelName=key?((level+number)%2==0?"RunicKey":"TempleKey"):((level+number)%2==0?"TreasureCache":"SecretCache");
   Vector3 fitSize=key?new Vector3(.65f,1.35f,.65f):new Vector3(1.35f,1.1f,1.05f);
   find.Visual=ExplorationArt.Fit("Props/Expansion/"+modelName,"Props/Expansion/Textures/"+modelName+"_basecolor",root.transform,new Vector3(0,key?.25f:0,0),fitSize,false,false,Color.white);
   var col=root.AddComponent<BoxCollider>();
   col.center=new Vector3(0,fitSize.y*.5f,0);
   col.size=new Vector3(.65f,fitSize.y,.55f);
   ExpansionChapter.ClearOverlappingProps(island,root.transform.position,root.transform.position,1.4f);
   var label=new GameObject("Treasure label");label.transform.SetParent(root.transform,false);label.transform.localPosition=Vector3.up*1.85f;var text=label.AddComponent<TextMesh>();LocalizedWorldText.Set(text,key?find.Title:"SUPPLY CACHE");text.fontSize=40;text.characterSize=.032f;text.font=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");if(text.font)label.GetComponent<Renderer>().sharedMaterial=text.font.material;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.86f,.45f);label.AddComponent<ExpansionBillboard>();
  }
  void ExplorationLandmark(Transform island,int i,bool astral,float width){
   Vector3 p=i==11&&IsBoss?new Vector3(-width*.31f,0,width*.25f):new Vector3(-width*.26f,0,-width*.23f);
   if(astral){var arch=AstralArt.Place("Arch",island,p,Vector3.one*4.2f,true);arch.transform.localRotation=Quaternion.Euler(0,35+i*13,0);AstralArt.Place("Orrery",island,new Vector3(width*.3f,0,width*.3f),Vector3.one*2.1f,true);}
   else StormArt.Place("Tower",island,p,Vector3.one*5.2f,true);
   if(i!=11)for(int n=0;n<2;n++){
    var tree=ExplorationArt.Fit("Props/Village/rpgpp_lt_bush_01","Props/Textures/rpgpp_lt_tex_a",island,new Vector3(width*.34f,0,-width*.25f+n*2),new Vector3(1.2f,1.15f,1.2f),false,false,astral?new Color(.66f,.65f,1):new Color(.62f,.8f,.75f));
   }
  }
  void ExplorationDress(Transform island,int index,bool astral,float width,bool puzzle){
   if(index==11)return; // Keep the guardian's complete combat floor clear.
   if(puzzle){
    var p=new Vector3(-width*.28f,0,width*.28f);
    if(astral)AstralArt.Place("Arch",island,new Vector3(p.x,0,-p.z),Vector3.one*3.4f,true);
    else StormArt.Place("Tower",island,p,Vector3.one*4.2f,true);
   }
   if(puzzle)return; // The complete relay/pad footprint must remain unobstructed.
   // Small corner gardens leave all four axial jump approaches and the combat floor open.
   for(int n=0;n<3;n++){
    var local=new Vector3(width*(.25f+n*.025f),0,-width*(.25f-n*.04f));
    if(!RealmProps.TryDeckSurface(island,local.x,local.z,null,out float y))continue;local.y=y;
    string asset=n==0?"Rock_02":n==1?"Flowers_01":"Grass_01";
    var size=n==0?new Vector3(1.3f,.65f,1.1f):new Vector3(.8f,.5f,.8f);
    ExplorationArt.Fit("Props/Nature/"+asset,"Props/Textures/Nature_basecolor",island,local,size,false,n==0,astral?new Color(.7f,.67f,1):new Color(.7f,.9f,.8f));
   }
  }
  void ExplorationBackdrop(bool astral){
   for(int i=0;i<16;i++){
    float angle=i*Mathf.PI*2/16;var p=new Vector3(Mathf.Cos(angle)*124,-15-i%3*4,Mathf.Sin(angle)*124);
    string rock=(astral?"R2_":"")+"Island_Crag";
    ExplorationArt.Fit("Islands/"+rock,"Islands/Textures/"+rock+"_basecolor",transform,p,new Vector3(18+i%3*5,18+i%4*6,18+i%3*5),true,false,astral?new Color(.5f,.54f,.75f):new Color(.5f,.62f,.62f));
   }
  }
 }
 public static class ExplorationArt {
  static readonly Dictionary<string,Material> Materials=new Dictionary<string,Material>();
  public static GameObject Fit(string resource,string texture,Transform parent,Vector3 pos,Vector3 size,bool top,bool solid,Color tint){
   var prefab=Resources.Load<GameObject>(resource);if(!prefab)return null;
   var root=new GameObject("Prop "+resource);root.transform.SetParent(parent,false);root.transform.localPosition=pos;
   var model=Object.Instantiate(prefab,root.transform,false);var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return root;
   Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
   // Scale the identity wrapper along world axes. Imported roots can be rotated 270 degrees.
   root.transform.localScale=new Vector3(size.x/Mathf.Max(.001f,b.size.x),size.y/Mathf.Max(.001f,b.size.y),size.z/Mathf.Max(.001f,b.size.z));
   b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);model.transform.position+=root.transform.position-new Vector3(b.center.x,top?b.max.y:b.min.y,b.center.z);
   string key=texture+tint.ToString();if(!Materials.TryGetValue(key,out var mat)||!mat){mat=new Material(Shader.Find("Standard")){mainTexture=Resources.Load<Texture2D>(texture),color=tint};mat.SetFloat("_Glossiness",.2f);Materials[key]=mat;}
   foreach(var r in renderers)r.sharedMaterial=mat;
   foreach(var c in root.GetComponentsInChildren<Collider>()){c.enabled=false;Object.Destroy(c);}
   if(solid)foreach(var mesh in model.GetComponentsInChildren<MeshFilter>()){var c=mesh.gameObject.AddComponent<MeshCollider>();c.sharedMesh=mesh.sharedMesh;}
   return root;
  }
 }
}
