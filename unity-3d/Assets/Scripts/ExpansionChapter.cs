using System;
using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 // Stage-one authored objectives. Progress belongs to the run, survives checkpoint
 // retries, and is discarded only when a chapter is restarted.
 public sealed class ExpansionChapter:MonoBehaviour {
  public static bool EnabledFor(int chapter)=>chapter==2||chapter==6||chapter==9;
  public readonly List<ExpansionNode> Nodes=new List<ExpansionNode>();
  public Enemy OptionalElite {get;private set;}
  public Transform Branch {get;private set;}
  public bool BranchVisited {get;private set;}
  public bool RewardClaimed {get;private set;}
  public int CompletedCount=>Nodes.FindAll(n=>n.Completed).Count;
  public bool Ready=>CompletedCount==Nodes.Count&&Nodes.Count>0;
  public string Counter=>Ready?"GATE RESTORED":(level==2?"BEACONS ":level==6?"KEYS ":"CRYSTALS ")+CompletedCount+"/"+Nodes.Count;
  public string Objective=>Ready?"The realm gate is restored. Follow the golden trail.":level==2?"Clear the guards, then stand beside each crossing beacon. Either beacon on the second clearing works.":level==6?"Recover both guarded temple keys. The return passage opens after the second key.":"Attune crystals in order: EMBER, FROST, GALE. Select the matching power, then stand beside the crystal.";
  public string SealText=>"Realm gate sealed — "+Counter+". "+Objective;
  int level;Transform choice,shortcutStart,shortcutEnd;float choiceDwell,shortcutDwell,shortcutCooldown;bool shortcutNeedsExit;string lastHint;
  RealmGame Game=>RealmGame.I;
  public static void Reserve(Transform island,int stage,int index){
   if(!EnabledFor(stage)||(index!=3&&index!=7&&!(stage==9&&index==10)))return;
   TrapArt.Reserve(island,new Vector3(0,.05f,-2.5f),2f);
  }
  public static ExpansionChapter Build(RealmWorld world,int stage){
   if(!EnabledFor(stage))return null;
   var c=world.gameObject.AddComponent<ExpansionChapter>();c.level=stage;
   int[] route=stage==9?new[]{3,7,10}:new[]{3,7};
   for(int i=0;i<route.Length;i++){
    Transform island=Nearest(world,world.Route[route[i]]);Vector3 p=ClearSpot(world,island,world.Route[route[i]]+Vector3.back*2.5f);
    var node=c.MakeNode(island,p,i,stage==2?"CROSSING BEACON "+(i+1):stage==6?"TEMPLE KEY "+(i+1):new[]{"EMBER CRYSTAL","FROST CRYSTAL","GALE CRYSTAL"}[i]);
    node.Element=stage==9?i:-1;
    if(stage!=9)foreach(var foe in RealmGame.I.Enemies)if(foe&&Vector3.Distance(foe.transform.position,world.Route[route[i]])<7f)node.Guards.Add(foe);
    c.Nodes.Add(node);
   }
   // The second crossing can be restored from either flank, not two extra chores.
   if(stage==2){var n=c.Nodes[1];Vector3 p=ClearSpot(world,n.transform.parent,n.transform.position+Vector3.right*3f);c.choice=c.Decoration(n.transform.parent,p,"ALTERNATE BEACON",new Color(.35f,.8f,1f));}
   if(stage==6){c.shortcutStart=c.Decoration(c.Nodes[0].transform.parent,ClearSpot(world,c.Nodes[0].transform.parent,c.Nodes[0].transform.position+Vector3.left*2.5f),"RETURN PASSAGE",Color.cyan);c.shortcutEnd=c.Decoration(c.Nodes[1].transform.parent,ClearSpot(world,c.Nodes[1].transform.parent,c.Nodes[1].transform.position+Vector3.left*2.5f),"RETURN PASSAGE",Color.cyan);}
   Vector3 branchPoint=world.Route[4]+new Vector3(-9.5f,.4f,0);c.Branch=Nearest(world,branchPoint);
   c.OptionalElite=world.SpawnExpansionElite(c.Branch,stage==2?22:stage==6?25:19);
   c.OptionalElite.DisplayName=stage==2?"WATERFALL SENTINEL":stage==6?"BURIAL CHAMBER WARDEN":"CRYSTAL SENTINEL";
   c.OptionalElite.MaxHealth*=1.4f;c.OptionalElite.Health=c.OptionalElite.MaxHealth;
   c.Decoration(c.Branch,ClearSpot(world,c.Branch,c.Branch.position+Vector3.back*2.6f),"OPTIONAL: "+c.OptionalElite.DisplayName,new Color(1f,.78f,.3f));
   return c;
  }
  public static Transform Nearest(RealmWorld world,Vector3 point){Transform best=null;float distance=float.MaxValue;foreach(Transform t in world.transform){if(t.name!="Island")continue;float d=(t.position-point).sqrMagnitude;if(d<distance){distance=d;best=t;}}return best;}
  static Vector3 ClearSpot(RealmWorld world,Transform island,Vector3 target){
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
  ExpansionNode MakeNode(Transform island,Vector3 point,int order,string name){var t=Decoration(island,point,name,level==9?RealmGame.ElementColors[order]:new Color(.3f,.7f,1f));var n=t.gameObject.AddComponent<ExpansionNode>();n.Owner=this;n.Order=order;return n;}
  Transform Decoration(Transform island,Vector3 point,string title,Color color){
   var go=new GameObject("Adventure "+title);go.transform.SetParent(island,false);go.transform.position=point;
   Art.Shape("Engraved plinth",PrimitiveType.Cylinder,Vector3.up*.06f,new Vector3(.7f,.06f,.7f),new Color(.18f,.22f,.26f),go.transform);
   var crystal=new GameObject("Attunement crystal");crystal.transform.SetParent(go.transform,false);crystal.transform.localPosition=Vector3.up*.7f;crystal.transform.localScale=Vector3.one*.5f;RelicArt.Gem(crystal.transform,color);
   var label=new GameObject("Objective label");label.transform.SetParent(go.transform,false);label.transform.localPosition=Vector3.up*1.5f;
   var text=label.AddComponent<TextMesh>();text.text=title;text.fontSize=36;text.characterSize=.035f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1f,.9f,.65f);
   var font=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");if(font){text.font=font;text.GetComponent<Renderer>().sharedMaterial=font.material;}
   label.AddComponent<ExpansionBillboard>();return go.transform;
  }
  public bool CanActivate(ExpansionNode node){if(node.Completed)return false;if(level==9&&node.Order!=CompletedCount)return false;foreach(var e in node.Guards)if(e&&e.Health>0)return false;return node.Element<0||Game.Player.Power==node.Element;}
  public void Complete(ExpansionNode node){
   if(!CanActivate(node))return;node.Completed=true;node.MarkRestored();Game.Sound("checkpoint");
   Game.Tell(node.name.Replace("Adventure ","")+" RESTORED. "+Counter,4f);
   if(Ready){Game.Tell(Objective,5f);if(level==6)Game.Tell("Temple keys recovered. Stand by a return passage to travel between the two clearings.",5f);}
  }
  void Hint(string hint){if(hint==lastHint)return;lastHint=hint;if(hint!=null)Game.Tell(hint,4f);}
  bool Near(Transform t,float range)=>t&&Vector3.Distance(Game.Player.transform.position,t.position)<range;
  void Update(){
   var g=Game;if(!g||!g.Player||g.Screen!=GameScreen.Playing)return;
   string hint=null;foreach(var node in Nodes)if(!node.Completed&&Near(node.transform,3f)){
    bool guarded=node.Guards.Exists(e=>e&&e.Health>0);
    hint=guarded?"Defeat the guards to restore this point.":level==9&&node.Order!=CompletedCount?"Restore the crystals in order: EMBER, FROST, GALE.":node.Element>=0&&g.Player.Power!=node.Element?"Select "+new[]{"EMBER","FROST","GALE"}[node.Element]+" using the power selector above.":"Stand still beside "+node.name.Replace("Adventure ","")+" to restore it.";break;
   }
   if(choice&&!Nodes[1].Completed){bool ready=Near(choice,1.6f)&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f&&CanActivate(Nodes[1]);choiceDwell=ready?choiceDwell+Time.deltaTime:0;if(choiceDwell>=.85f)Complete(Nodes[1]);}
   bool onBranch=Near(Branch,7f)&&g.Player.Grounded&&ChapterLayout.Supported(Branch,null,new Bounds(g.Player.transform.position,new Vector3(.4f,.1f,.4f)),out float branchY)&&Mathf.Abs(branchY-g.Player.transform.position.y)<.35f;
   if(onBranch){BranchVisited=true;if(!RewardClaimed)hint=OptionalElite&&OptionalElite.Health>0?"OPTIONAL: defeat "+OptionalElite.DisplayName+" for "+(60+level*5)+" Gold and 4 Gems.":"Claiming the secret clearing reward.";}
   if(onBranch&&!RewardClaimed&&(!OptionalElite||OptionalElite.Health<=0)){RewardClaimed=true;g.Coins+=60+level*5;g.Gems+=4;g.Sound("upgrade");g.Tell("Secret clearing restored: +"+(60+level*5)+" Gold, +4 Gems. Finish the chapter to bank rewards.",5f);}
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
  public ExpansionChapter Owner;public int Order,Element=-1;public bool Completed;public readonly List<Enemy> Guards=new List<Enemy>();float dwell;
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||Completed||!g.Player)return;bool ready=Owner.CanActivate(this)&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f&&Vector3.Distance(g.Player.transform.position,transform.position)<1.6f;dwell=ready?dwell+Time.deltaTime:0;if(dwell>=.85f)Owner.Complete(this);}
  public void MarkRestored(){foreach(var gem in GetComponentsInChildren<GemVisual>())gem.Accent=new Color(1f,.8f,.3f);foreach(var r in GetComponentsInChildren<Renderer>()){if(r.GetComponent<TextMesh>())continue;var p=new MaterialPropertyBlock();p.SetColor("_Color",new Color(1f,.8f,.3f));r.SetPropertyBlock(p);}}
 }
 public sealed class ExpansionBillboard:MonoBehaviour {void LateUpdate(){if(Camera.main)transform.rotation=Camera.main.transform.rotation;}}
}
