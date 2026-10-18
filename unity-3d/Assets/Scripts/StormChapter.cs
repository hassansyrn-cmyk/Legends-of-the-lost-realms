using UnityEngine;
using System.Collections.Generic;
namespace LostRealms {
 public sealed class StormChapter:MonoBehaviour {
  public int Chapter;public readonly List<StormCircuit> Circuits=new List<StormCircuit>();public readonly List<Transform> ArenaRods=new List<Transform>();
  public bool MemoryClaimed {get;private set;}public int Completed {get{int n=0;foreach(var c in Circuits)if(c.Completed)n++;return n;}}
  public bool Ready=>(!GetComponent<RealmExploration>()||GetComponent<RealmExploration>().ObjectivesReady)&&Completed==Circuits.Count&&Circuits.Count>0&&(Chapter!=20||!RealmGame.I.Enemies.Exists(e=>e&&e.Boss&&e.Health>0));
  public string Counter=>GetComponent<RealmExploration>()?GetComponent<RealmExploration>().Status+" / "+Completed+"/"+Circuits.Count+" LINKS":Ready?"STORM GATE OPEN":Completed==Circuits.Count?"REGENT REMAINS":"CIRCUITS "+Completed+"/"+Circuits.Count;
  public string Objective=>Ready?"The gate has awakened. Find its light on your field map.":"Explore the islands in every direction. Find three gate keys and restore the three conductor courts. Use the field map to retrace your steps.";
  Transform memory;float memoryDwell,rodDwell,rodCooldown;int nearbyRod=-1;bool rodMustExit;float hintAt;
  public void AddCircuit(Transform island,int count,int id){var c=island.gameObject.AddComponent<StormCircuit>();c.Owner=this;c.Index=id;c.Build(count);Circuits.Add(c);}
  public void AddMemory(Transform island){memory=StormArt.Place("MemoryPlaque",island,Vector3.zero,Vector3.one*1.1f,true).transform;}
  public void AddArenaRods(Transform island){foreach(var p in new[]{new Vector3(-8,0,-5),new Vector3(8,0,-5),new Vector3(-8,0,5),new Vector3(8,0,5)})ArenaRods.Add(StormArt.Place("Conductor",island,p,Vector3.one,true).transform);}
  public bool GroundArena(int index){var g=RealmGame.I;if(index<0||index>=ArenaRods.Count||g.Elapsed<rodCooldown)return false;var boss=g.Enemies.Find(e=>e&&e.Kind==28&&e.Health>0);if(!boss)return false;rodCooldown=g.Elapsed+10f;boss.InterruptStorm();g.Sound("storm_ground");g.Tell("LIGHTNING GROUNDED — the Regent is exposed",2.5f);return true;}
  static readonly string[] Memories={"LYRA: The Warden's fall revealed another fracture, above the cloud sea. These conductors once kept Stormveil safe.","WATCHKEEPER RECORD: The Regent sealed our towers. Each relay feeds the next; amber arms carry no current.","GALEWEAVER RECORD: Listen to the wind. Cross while the warning is green; the rising gust is your cue to wait.","STORMKEEPER RECORD: Grounding a conductor draws the Regent's storm away. Its armour opens while the current drains.","LYRA: Stormveil is free, but a silver fracture still leads into the Astral Reach. Our journey continues."};
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   if(memory&&!MemoryClaimed&&(Chapter!=20||Ready)&&Vector3.Distance(g.Player.transform.position,memory.position)<1.7f){memoryDwell+=Time.deltaTime;InteractionCountdown.Report(memory,memoryDwell,.9f,"READING");if(memoryDwell>.9f){MemoryClaimed=true;g.Coins+=80;g.Gems+=3;g.Tell(Memories[Chapter-16]+"  /  +80 Gold +3 Gems",8f);g.Sound("discover");}}else memoryDwell=0;
   if(Chapter==20){int id=ArenaRods.FindIndex(r=>r&&Vector3.Distance(r.position,g.Player.transform.position)<1.8f);if(id<0){rodMustExit=false;rodDwell=0;nearbyRod=-1;}else if(!rodMustExit){if(id!=nearbyRod){nearbyRod=id;rodDwell=0;}if(g.Elapsed>=hintAt){g.Tell(g.Elapsed<rodCooldown?"CONDUCTOR RECHARGING":"Stand beside this conductor to ground the Regent's lightning",1.4f);hintAt=g.Elapsed+1.2f;}}
    if(id>=0&&g.Elapsed<rodCooldown)InteractionCountdown.Report(ArenaRods[id],10f-(rodCooldown-g.Elapsed),10f,"RECHARGING");
    if(id>=0&&!rodMustExit&&g.Elapsed>=rodCooldown){rodDwell+=Time.deltaTime;InteractionCountdown.Report(ArenaRods[id],rodDwell,1.1f,"GROUNDING");if(rodDwell>=1.1f){GroundArena(id);rodMustExit=true;rodDwell=0;}}
   }
  }
 }
 public sealed class StormCircuit:MonoBehaviour {
  public StormChapter Owner;public int Index;public bool Completed {get;private set;}public int[] Orientations {get;private set;}
  public int Round {get;private set;}public int Rounds=>Index==2?2:1;
  public readonly List<Transform> Relays=new List<Transform>();
  readonly List<LineRenderer> wires=new List<LineRenderer>();readonly List<Transform> arms=new List<Transform>();
  readonly List<Transform> pads=new List<Transform>();readonly List<Transform> receivers=new List<Transform>();
  int selected=-1;float dwell,hintAt,powered;bool needsExit;LineRenderer sourceWire;
  public int CorrectFor(int relay)=>(Owner.Chapter+Index+relay+Round)%3;
  int RelayInPath(int step){if(Round==0||step==0)return step;return Relays.Count-step;}
  Vector3 Target(int relay){for(int step=0;step<Relays.Count;step++)if(RelayInPath(step)==relay)return step+1<Relays.Count?Relays[RelayInPath(step+1)].position:receivers[Round].position;return transform.position;}
  public Vector3 BeamDirection(int relay,int choice){var d=Target(relay)-Relays[relay].position;d.y=0;return Quaternion.Euler(0,(choice-CorrectFor(relay))*70,0)*d.normalized;}
  public Vector3 PadPosition(int relay,int choice)=>Relays[relay].position+new Vector3((choice-1)*1.15f,.035f,-1.5f);
  public bool Guarded=>RealmGame.I.Enemies.Exists(e=>e&&e.Health>0&&!e.Boss&&Vector3.Distance(e.transform.position,transform.position)<7.5f);
  LineRenderer Line(GameObject obj){var l=obj.AddComponent<LineRenderer>();l.useWorldSpace=true;l.positionCount=2;l.startWidth=l.endWidth=.055f;l.sharedMaterial=StormArt.Beam;return l;}
  public void Build(int count){
   count=Mathf.Min(4,count+(Index==0?0:1));Orientations=new int[count];
   // A dogleg, a triangle, and a two-stage criss-cross court. Pads never share a lane.
   var layouts=new[]{new[]{new Vector3(-2.5f,0,-1),new Vector3(2.5f,0,1),new Vector3(-2.5f,0,3.5f)},new[]{new Vector3(-3.2f,0,-2.5f),new Vector3(3.2f,0,-2.5f),new Vector3(0,0,2),new Vector3(3.5f,0,4.5f)},new[]{new Vector3(-3.4f,0,-2.5f),new Vector3(3.4f,0,-2.5f),new Vector3(-3.4f,0,2.5f),new Vector3(3.4f,0,2.5f)}};
   for(int i=0;i<count;i++){
    var t=StormArt.Place("Conductor",transform,layouts[Index%3][i],Vector3.one*.7f,true).transform;Relays.Add(t);
    var arm=StormArt.Place("RelayArm",t,Vector3.up*.93f,Vector3.one*.85f).transform;arm.name="Copper directional arm";arm.GetChild(0).localRotation=Quaternion.Euler(90,0,0)*arm.GetChild(0).localRotation;arms.Add(arm);wires.Add(Line(arm.gameObject));
    for(int c=0;c<3;c++){var pad=StormArt.Place("TurnPad",transform,transform.InverseTransformPoint(PadPosition(i,c)),new Vector3(.9f,.25f,.9f));pad.name="Direction choice "+i+" / "+c;pads.Add(pad.transform);}
    Orientations[i]=(CorrectFor(i)+1)%3;
   }
   for(int i=0;i<Rounds;i++){
    var t=StormArt.Place("Conductor",transform,new Vector3(i==0?0:4.5f,0,5.8f),Vector3.one*.8f,true).transform;receivers.Add(t);
    var label=new GameObject("Receiver label");label.transform.SetParent(t,false);label.transform.localPosition=Vector3.up*2;var text=label.AddComponent<TextMesh>();LocalizedWorldText.Set(text,i==0?"FOCUS I":"FOCUS II");text.characterSize=.09f;text.fontSize=36;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(.55f,.85f,1);label.AddComponent<ExpansionBillboard>();
   }
   var source=new GameObject("Incoming current");source.transform.SetParent(transform,false);sourceWire=Line(source);
   RefreshPads();
  }
  // The supplied arrow artwork is angled 22.5 degrees from its imported backward axis.
  void RefreshPads(){for(int i=0;i<Relays.Count;i++)for(int c=0;c<3;c++)pads[i*3+c].rotation=Quaternion.LookRotation(BeamDirection(i,c))*Quaternion.Euler(0,202.5f,0);}
  public bool Turn(int relay,int choice){if(Completed||Guarded||relay<0||relay>=Relays.Count||choice<0||choice>2)return false;Orientations[relay]=choice;return true;}
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   var first=Relays[0].position+Vector3.up*.72f;sourceWire.SetPosition(0,first+Vector3.left*1.7f);sourceWire.SetPosition(1,first);sourceWire.startColor=sourceWire.endColor=new Color(.5f,.85f,1);
   bool linked=true;
   for(int step=0;step<Relays.Count;step++){
    int i=RelayInPath(step);bool incoming=linked;bool good=Orientations[i]==CorrectFor(i);linked&=good;
    var a=Relays[i].position+Vector3.up*.72f;var direction=BeamDirection(i,Orientations[i]);var b=good?Target(i)+Vector3.up*.72f:a+direction*2.5f;
    arms[i].rotation=Quaternion.LookRotation(direction);wires[i].enabled=incoming;wires[i].SetPosition(0,a);wires[i].SetPosition(1,b);wires[i].startColor=wires[i].endColor=new Color(.45f,.85f,1);
   }
   if(Completed)return;
   if(linked&&!Guarded){powered+=Time.deltaTime;InteractionCountdown.Report(receivers[Round],powered,1.1f,"CHARGING");if(powered>1.1f){
    LocalizedWorldText.Set(receivers[Round].GetComponentInChildren<TextMesh>(),"FOCUS "+(Round+1)+" LIT");
    if(Round+1<Rounds){Round++;powered=0;RefreshPads();g.Tell("FIRST FOCUS LIT — redirect the current along the new route to FOCUS II",4);g.Sound("storm_ground");}
    else {Completed=true;g.Tell("CIRCUIT "+(Index+1)+" RESTORED  /  "+Owner.Counter,3);g.Sound("storm_ground");}
   }}else powered=0;
   int pad=-1;for(int i=0;i<Relays.Count;i++)for(int c=0;c<3;c++)if(Vector3.Distance(g.Player.transform.position,PadPosition(i,c))<.62f)pad=i*3+c;
   if(pad<0){selected=-1;dwell=0;needsExit=false;return;}
   if(g.Elapsed>hintAt){hintAt=g.Elapsed+2;g.Tell(Guarded?"CLEAR THE CONDUCTOR GUARDS":"FOLLOW THE LIGHT — each arrow aims its relay. Feed the next conductor, then the numbered focus.",2);}
   if(Guarded||needsExit)return;if(pad!=selected){selected=pad;dwell=0;}dwell+=Time.deltaTime;InteractionCountdown.Report(Relays[pad/3],dwell,.65f,"TURNING");if(dwell>.65f){Turn(pad/3,pad%3);needsExit=true;g.Sound("storm_turn");}
  }
 }
}
