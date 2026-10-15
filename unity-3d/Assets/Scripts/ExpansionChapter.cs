using System;
using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // Authored campaign objectives. Progress belongs to the run, survives checkpoint
 // retries, and is discarded only when a chapter is restarted.
 public sealed class ExpansionChapter:MonoBehaviour {
  public static bool EnabledFor(int chapter)=>chapter>=1&&chapter<=15;
  public readonly List<ExpansionNode> Nodes=new List<ExpansionNode>();
  public Enemy OptionalElite {get;private set;}
  public Transform Branch {get;private set;}
  public bool BranchVisited {get;private set;}
  public bool RewardClaimed {get;private set;}
  public ChapterAdventure Definition=>ChapterAdventure.Get(level);
  public int CompletedCount {get{int count=0;foreach(var n in Nodes)if(n.Completed)count++;return count;}}
  public bool Ready=>CompletedCount==Nodes.Count&&Nodes.Count>0&&(Definition.Kind!=AdventureKind.Guardian||!Game.Enemies.Exists(e=>e&&e.Boss&&e.Health>0));
  public string Counter=>Ready?"GATE RESTORED":CompletedCount==Nodes.Count?"GUARDIAN REMAINS":Definition.Label+" "+CompletedCount+"/"+Nodes.Count;
  public string Objective=>Ready?"The realm gate is restored. Follow the golden trail.":Definition.Instruction;
  public string SealText=>"Realm gate sealed — "+Counter+". "+Objective;
  public Vector3 SecretPosition=>secretPoint.position;
  public int GoldReward=>Definition.EliteKind>=0?60+level*5:25+level*3;
  public int GemReward=>Definition.EliteKind>=0?4:2;
  int level;Transform choice,shortcutStart,shortcutEnd,secretPoint;float choiceDwell,shortcutDwell,shortcutCooldown,secretDwell;bool shortcutNeedsExit;string lastHint;
  RealmGame Game=>RealmGame.I;
  public static void Reserve(Transform island,int stage,int index){
   if(!EnabledFor(stage)||Array.IndexOf(ChapterAdventure.Get(stage).Route,index)<0)return;
   TrapArt.Reserve(island,new Vector3(0,.05f,-2.5f),2f);
  }
  public static ExpansionChapter Build(RealmWorld world,int stage){
   if(!EnabledFor(stage))return null;
   var c=world.gameObject.AddComponent<ExpansionChapter>();c.level=stage;
   int[] route=c.Definition.Route;
   for(int i=0;i<route.Length;i++){
    Transform island=Nearest(world,world.Route[route[i]]);Vector3 p=ClearSpot(world,island,world.Route[route[i]]+Vector3.back*2.5f);
    var node=c.MakeNode(island,p,i,c.Definition.Elements!=null?new[]{"EMBER","FROST","GALE"}[c.Definition.Elements[i]]+" "+c.Definition.Label:c.Definition.Label+" "+(i+1));
    node.Element=c.Definition.Elements!=null?c.Definition.Elements[i]:-1;node.DwellTime=c.Definition.Kind==AdventureKind.Ritual?2.5f:.85f;
    if(c.Definition.Guarded)foreach(var foe in RealmGame.I.Enemies)if(foe&&Vector3.Distance(foe.transform.position,world.Route[route[i]])<7f)node.Guards.Add(foe);
    c.Nodes.Add(node);
   }
   // The second crossing can be restored from either flank, not two extra chores.
   if(stage==2){var n=c.Nodes[1];Vector3 p=ClearSpot(world,n.transform.parent,n.transform.position+Vector3.right*3f);c.choice=c.Decoration(n.transform.parent,p,"ALTERNATE BEACON",new Color(.35f,.8f,1f));}
   if(stage==6){c.shortcutStart=c.Decoration(c.Nodes[0].transform.parent,ClearSpot(world,c.Nodes[0].transform.parent,c.Nodes[0].transform.position+Vector3.left*2.5f),"RETURN PASSAGE",Color.cyan);c.shortcutEnd=c.Decoration(c.Nodes[1].transform.parent,ClearSpot(world,c.Nodes[1].transform.parent,c.Nodes[1].transform.position+Vector3.left*2.5f),"RETURN PASSAGE",Color.cyan);}
   Vector3 branchPoint=world.Route[4]+new Vector3(-9.5f,.4f,0);c.Branch=Nearest(world,world.IsBoss?world.Route[1]:branchPoint);
   if(c.Definition.EliteKind>=0){
    c.OptionalElite=world.SpawnExpansionElite(c.Branch,c.Definition.EliteKind);
    c.OptionalElite.DisplayName=c.Definition.Secret;c.OptionalElite.NamedSignature=c.Definition.Signature;
    c.OptionalElite.MaxHealth*=1.4f;c.OptionalElite.Health=c.OptionalElite.MaxHealth;
   }
   c.secretPoint=c.Decoration(c.Branch,ClearSpot(world,c.Branch,c.Branch.position+(world.IsBoss?new Vector3(2.8f,0,2f):Vector3.back*2.6f)),"OPTIONAL: "+c.Definition.Secret,new Color(1f,.78f,.3f));
   return c;
  }
  public static Transform Nearest(RealmWorld world,Vector3 point){Transform best=null;float distance=float.MaxValue;foreach(Transform t in world.transform){if(t.name!="Island")continue;float d=(t.position-point).sqrMagnitude;if(d<distance){distance=d;best=t;}}return best;}
  internal static Vector3 ClearSpot(RealmWorld world,Transform island,Vector3 target){
   Physics.SyncTransforms();
   Vector3 result=default;float best=float.MaxValue;
   for(int x=-7;x<=7;x++)for(int z=-7;z<=7;z++){
    Vector3 p=target+new Vector3(x*.45f,0,z*.45f);
    if(!ChapterLayout.Supported(island,null,new Bounds(p,new Vector3(.8f,.1f,.8f)),out float y))continue;
    p.y=y+.03f;if(!RealmTrials.ShrineClear(p,world.transform,.9f))continue;
    float score=(p-target).sqrMagnitude;if(score<best){best=score;result=p;}
   }
   if(best==float.MaxValue)throw new InvalidOperationException("No supported objective slot in chapter "+RealmGame.I.Level);
   TrapArt.Reserve(island,island.InverseTransformPoint(result),1.2f);return result;
  }
  ExpansionNode MakeNode(Transform island,Vector3 point,int order,string name){var t=Decoration(island,point,name,Definition.Elements!=null?RealmGame.ElementColors[Definition.Elements[order]]:new Color(.3f,.7f,1f));var n=t.gameObject.AddComponent<ExpansionNode>();n.Owner=this;n.Order=order;return n;}
  static readonly Dictionary<string,Material> puzzleMaterials=new Dictionary<string,Material>();
  static string ModelForTitle(string title){
   if(title.Contains("RETURN PASSAGE"))return "Props/Expansion/ReturnPassage";
   if(title.Contains("KEY"))return "Props/Expansion/TempleKey";
   if(title.Contains("EMBER"))return "Props/Expansion/EmberFocus";
   if(title.Contains("FROST"))return "Props/Expansion/FrostFocus";
   if(title.Contains("GALE"))return "Props/Expansion/GaleFocus";
   if(title.Contains("RITUAL"))return "Props/Expansion/RitualAltar";
   if(title.Contains("OUTPOST")||title.Contains("RAMPART")||title.Contains("BANNER"))return "Props/Expansion/RallyBanner";
   if(title.Contains("BEACON"))return "Stormveil/Conductor";
   if(title.Contains("SEAL"))return "Astral/WardFocus";
   if(title.Contains("SIGIL"))return "Relics/DawnSigil";
   if(title.Contains("WARD"))return "Relics/Wardstone";
   if(title.Contains("MEMORY"))return "Stormveil/MemoryPlaque";
   if(title.Contains("CACHE")||title.Contains("OPTIONAL"))return "Props/Expansion/SecretCache";
   return "Props/Expansion/TrailShrine";
  }
  static GameObject PlacePuzzleModel(string assetPath,Transform parent,Color accent,out float visualHeight){
   visualHeight=1.8f;
   var prefab=Resources.Load<GameObject>(assetPath);
   if(!prefab)return null;
   string modelName=System.IO.Path.GetFileNameWithoutExtension(assetPath);
   var go=new GameObject("PuzzleModel "+modelName);
   go.transform.SetParent(parent,false);
   go.transform.localPosition=Vector3.zero;
   var model=Instantiate(prefab,go.transform,false);
   var renderers=model.GetComponentsInChildren<Renderer>();
   float targetHeight=assetPath.Contains("ReturnPassage")?2.8f:
                      assetPath.Contains("TempleKey")?1.35f:
                      assetPath.Contains("SecretCache")?1.45f:
                      assetPath.Contains("Conductor")?2.2f:
                      assetPath.Contains("MemoryPlaque")?1.9f:
                      assetPath.Contains("WardFocus")?2.1f:
                      assetPath.Contains("Relics/")?1.5f:
                      assetPath.Contains("RitualAltar")?1.5f:
                      assetPath.Contains("RallyBanner")?2.4f:1.8f;
   visualHeight=targetHeight;
   if(renderers.Length>0){
    Bounds b=renderers[0].bounds;
    foreach(var r in renderers)b.Encapsulate(r.bounds);
    float scale=targetHeight/Mathf.Max(.01f,b.size.y);
    go.transform.localScale=Vector3.one*scale;
    b=renderers[0].bounds;
    foreach(var r in renderers)b.Encapsulate(r.bounds);
    model.transform.position+=go.transform.position-new Vector3(b.center.x,b.min.y,b.center.z);
   }
   if(modelName=="TempleKey"){
    Art.Shape("Key plinth",PrimitiveType.Cylinder,Vector3.up*.1f,new Vector3(1f,.18f,1f),new Color(.2f,.24f,.28f),go.transform);
    model.transform.position+=Vector3.up*.55f;
    var spinner=go.AddComponent<PuzzleSpinner>();spinner.Target=model.transform;
   }
   if(modelName=="ReturnPassage"){
    CombatTelegraph.Ring(go.transform,1.55f,Color.cyan,"Return passage ring");
   }
   if(!puzzleMaterials.TryGetValue(assetPath,out var mat)||!mat){
    mat=new Material(Shader.Find("Standard")??Shader.Find("Mobile/Diffuse")??Shader.Find("Sprites/Default")){name="Puzzle "+modelName};
    string folder=assetPath.Contains("/")?assetPath.Substring(0,assetPath.LastIndexOf('/')):"Props/Expansion";
    var tex=Resources.Load<Texture2D>(folder+"/Textures/"+modelName+"_basecolor");
    if(!tex&&folder=="Astral")tex=Resources.Load<Texture2D>("Astral/AstralPalette");
    if(!tex&&folder=="Stormveil")tex=Resources.Load<Texture2D>("Stormveil/StormPalette");
    var norm=Resources.Load<Texture2D>(folder+"/Textures/"+modelName+"_normal");
    if(tex)mat.mainTexture=tex;
    mat.SetFloat("_Glossiness",.3f);
    mat.SetFloat("_Metallic",.18f);
    if(norm){mat.SetTexture("_BumpMap",norm);mat.SetFloat("_BumpScale",1f);mat.EnableKeyword("_NORMALMAP");}
    puzzleMaterials[assetPath]=mat;
   }
   foreach(var r in model.GetComponentsInChildren<Renderer>()){
    r.sharedMaterial=mat;
    if(accent!=Color.white){
     var block=new MaterialPropertyBlock();
     r.GetPropertyBlock(block);
     block.SetColor("_EmissionColor",accent*.4f);
     r.SetPropertyBlock(block);
    }
   }
   return go;
  }
  Transform Decoration(Transform island,Vector3 point,string title,Color color){
   var go=new GameObject("Adventure "+title);go.transform.SetParent(island,false);go.transform.position=point;
   string assetPath=ModelForTitle(title);
   var model=PlacePuzzleModel(assetPath,go.transform,color,out float height);
   if(!model){
    Art.Shape("Engraved plinth",PrimitiveType.Cylinder,Vector3.up*.06f,new Vector3(.85f,.08f,.85f),new Color(.18f,.22f,.26f),go.transform);
    var crystal=new GameObject("Attunement crystal");crystal.transform.SetParent(go.transform,false);crystal.transform.localPosition=Vector3.up*.75f;crystal.transform.localScale=Vector3.one*.6f;RelicArt.Gem(crystal.transform,color);
    height=1.5f;
   }
   var label=new GameObject("Objective label");label.transform.SetParent(go.transform,false);label.transform.localPosition=Vector3.up*(height+.35f);
   var text=label.AddComponent<TextMesh>();text.text=title;text.fontSize=36;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1f,.9f,.65f);
   var font=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");if(font){text.font=font;text.GetComponent<Renderer>().sharedMaterial=font.material;}
   label.AddComponent<ExpansionBillboard>();foreach(var collider in go.GetComponentsInChildren<Collider>()){collider.enabled=false;Destroy(collider);}return go.transform;
  }
  public bool CanActivate(ExpansionNode node){if(node.Completed)return false;if(Definition.Ordered&&node.Order!=CompletedCount)return false;foreach(var e in node.Guards)if(e&&e.Health>0)return false;return node.Element<0||Game.Player.Power==node.Element;}
  public void Complete(ExpansionNode node){
   if(!CanActivate(node))return;
   if(Definition.Kind==AdventureKind.Ambush&&!node.WaveStarted){
    node.WaveStarted=true;node.Guards.Add(Game.World.SpawnExpansionCompanion(node.transform.parent,level==5?19:16,new Vector3(-1.4f,0,1.6f)));node.Guards.Add(Game.World.SpawnExpansionCompanion(node.transform.parent,level==5?7:17,new Vector3(1.4f,0,2f)));
    Game.Tell("AMBUSH! Clear the reinforcements, then restore the rally point.",4f);return;
   }
   node.Completed=true;BuildProgression.OnObjective(Game,level,CompletedCount);node.MarkRestored();Game.Sound("checkpoint");
   Game.Tell(node.name.Replace("Adventure ","")+" RESTORED. "+Counter,4f);
   if(Ready){Game.Tell(Objective,5f);if(level==6)Game.Tell("Temple keys recovered. Stand by a return passage to travel between the two clearings.",5f);}
  }
  void Hint(string hint){if(hint==lastHint)return;lastHint=hint;if(hint!=null)Game.Tell(hint,4f);}
  bool Near(Transform t,float range)=>t&&Vector3.Distance(Game.Player.transform.position,t.position)<range;
  void Update(){
   var g=Game;if(!g||!g.Player||g.Screen!=GameScreen.Playing)return;
   string hint=null;foreach(var node in Nodes)if(!node.Completed&&Near(node.transform,3f)){
    bool guarded=node.Guards.Exists(e=>e&&e.Health>0);
    hint=guarded?"Defeat the guards to restore this point.":Definition.Ordered&&node.Order!=CompletedCount?"Restore the marked objectives in order. "+Definition.Instruction:node.Element>=0&&g.Player.Power!=node.Element?"Select "+new[]{"EMBER","FROST","GALE"}[node.Element]+" using the power selector above.":"Stand still beside "+node.name.Replace("Adventure ","")+" to restore it.";break;
   }
   if(choice&&!Nodes[1].Completed){bool ready=Near(choice,1.6f)&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f&&CanActivate(Nodes[1]);choiceDwell=ready?choiceDwell+Time.deltaTime:0;if(choiceDwell>=.85f)Complete(Nodes[1]);}
   bool onBranch=Near(Branch,7f)&&g.Player.Grounded&&RealmProps.TryDeckSurface(Branch,Branch.InverseTransformPoint(g.Player.transform.position).x,Branch.InverseTransformPoint(g.Player.transform.position).z,secretPoint,out float branchLocalY)&&Mathf.Abs(Branch.TransformPoint(new Vector3(0,branchLocalY,0)).y-g.Player.transform.position.y)<.35f;
   if(onBranch&&(Definition.EliteKind>=0||Near(secretPoint,2f))){BranchVisited=true;if(!RewardClaimed)hint=OptionalElite&&OptionalElite.Health>0?"OPTIONAL: defeat "+OptionalElite.DisplayName+" for "+GoldReward+" Gold and "+GemReward+" Gems.":"Claiming the secret clearing reward.";}
   bool cacheReady=onBranch&&Near(secretPoint,1.6f)&&g.Player.HorizontalSpeed<.5f;secretDwell=cacheReady?secretDwell+Time.deltaTime:0;
   if(onBranch&&!RewardClaimed&&(Definition.EliteKind>=0?(!OptionalElite||OptionalElite.Health<=0):secretDwell>=.85f)){
    RewardClaimed=true;BuildProgression.OnSecret(g,level);g.Coins+=GoldReward;g.Gems+=GemReward;g.Sound("upgrade");g.Tell(Definition.Secret+" discovered: +"+GoldReward+" Gold, +"+GemReward+" Gems. Finish the chapter to bank rewards.",5f);
   }
   if(level==6&&Ready&&g.Elapsed>=shortcutCooldown){
    if(shortcutNeedsExit&&!Near(shortcutStart,1.5f)&&!Near(shortcutEnd,1.5f))shortcutNeedsExit=false;
    Transform source=shortcutNeedsExit?null:Near(shortcutStart,1.5f)?shortcutStart:Near(shortcutEnd,1.5f)?shortcutEnd:null;
    bool clear=source&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f&&!g.Enemies.Exists(e=>e&&e.Health>0&&Vector3.Distance(e.transform.position,g.Player.transform.position)<5f);
    shortcutDwell=clear?shortcutDwell+Time.deltaTime:0;
    if(source)hint="Stand still in the return passage to use the shortcut.";
    if(shortcutDwell>=1.2f){var destination=source==shortcutStart?shortcutEnd:shortcutStart;g.Player.Warp(destination.position+Vector3.up*.05f);shortcutCooldown=g.Elapsed+3f;shortcutDwell=0;shortcutNeedsExit=true;g.Sound("respawn");g.Tell("Temple return passage crossed. Step away before using it again.",3f);}
   }
   Hint(hint);
  }
 }
 public sealed class ExpansionNode:MonoBehaviour {
  public ExpansionChapter Owner;public int Order,Element=-1;public bool Completed,WaveStarted;public float DwellTime=.85f;public readonly List<Enemy> Guards=new List<Enemy>();float dwell;
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||Completed||!g.Player)return;bool ready=Owner.CanActivate(this)&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f&&Vector3.Distance(g.Player.transform.position,transform.position)<1.6f;dwell=ready?dwell+Time.deltaTime:0;if(dwell>=DwellTime*BuildProgression.ChannelMultiplier(g.Save))Owner.Complete(this);}
  public void MarkRestored(){foreach(var gem in GetComponentsInChildren<GemVisual>())gem.Accent=new Color(1f,.8f,.3f);foreach(var r in GetComponentsInChildren<Renderer>()){if(r.GetComponent<TextMesh>())continue;var p=new MaterialPropertyBlock();p.SetColor("_Color",new Color(1f,.8f,.3f));r.SetPropertyBlock(p);}}
 }
 public sealed class ExpansionBillboard:MonoBehaviour {void LateUpdate(){if(Camera.main)transform.rotation=Camera.main.transform.rotation;}}
 public sealed class PuzzleSpinner:MonoBehaviour {
  public Transform Target;
  void Update(){if(Target)Target.Rotate(0,40f*Time.deltaTime,0,Space.World);}
 }
}
