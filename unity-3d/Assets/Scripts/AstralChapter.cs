using UnityEngine;
using System.Collections.Generic;
namespace LostRealms {
 public sealed class AstralChapter:MonoBehaviour {
  public int Chapter;
  public readonly List<AstralRoute> Mechanisms=new List<AstralRoute>();
  public readonly List<Transform> Seals=new List<Transform>();
  public bool RecordClaimed {get;private set;}
  public int Connected {get{int n=0;foreach(var route in Mechanisms)foreach(bool b in route.Attuned)if(b)n++;return n;}}
  public bool Ready=>(!GetComponent<RealmExploration>()||GetComponent<RealmExploration>().ObjectivesReady)&&Connected==Mechanisms.Count*2&&Mechanisms.Count>0&&(Chapter!=25||!RealmGame.I.Enemies.Exists(e=>e&&e.Kind==31&&e.Health>0));
  public string Counter=>GetComponent<RealmExploration>()?GetComponent<RealmExploration>().Status+" / "+Connected+"/"+(Mechanisms.Count*2)+" STARS":Ready?"ASTRAL GATE OPEN":"STAR LINKS "+Connected+"/"+(Mechanisms.Count*2);
  public string Objective=>"Explore the fractured islands. Recover three gate keys, reconnect both courts at each observatory, then seek the awakened gate. Open your field map to navigate.";
  public Transform Record;float dwell,sealDwell,hintAt;int nearbySeal=-1;bool sealExit;
  public void AddRoute(Transform hub,int id){var route=hub.gameObject.AddComponent<AstralRoute>();route.Owner=this;route.Index=id;route.Build();Mechanisms.Add(route);}
  public void AddSeals(Transform arena){for(int i=0;i<3;i++){var seal=AstralArt.Place("WardFocus",arena,new Vector3((i-1)*7,0,-7),Vector3.one*1.2f,true).transform;Seals.Add(seal);AstralArt.Label(arena,new[]{"EMBER","FROST","GALE"}[i],new Vector3((i-1)*7,.08f,-8.5f),RealmGame.ElementColors[i]);}}
  public bool GroundSeal(int index){var g=RealmGame.I;if(index<0||index>=Seals.Count||g.Player.Power!=index)return false;var boss=g.Enemies.Find(e=>e&&e.Kind==31&&e.Health>0);if(!boss||boss.AstralPhase-1!=index||!boss.AstralWard)return false;boss.OpenAstralWard();g.Sound("astral_align");g.Tell("FRACTURE EXPOSED — strike during the recovery",2.5f);return true;}
  static readonly string[] Records={"LYRA: The fracture began in this observatory. Reconnect its two star paths; neither sun nor moon can sustain the realm alone.","ASTRAL KEEPER: Our sentinels protect the broken charts. The violet pulse announces a fracture before it erupts.","OBSERVATORY RECORD: An occupied bridge cannot shift. Return to its hub before choosing the other star court.","LYRA: The Keeper bound three wards to Ember, Frost and Gale. Each phase can be grounded at its matching focus.","LYRA: The six realms are connected again. The Heart belongs to their people, not a guardian's crown."};
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   if(Record&&!RecordClaimed&&(Chapter!=25||Ready)&&Vector3.Distance(g.Player.transform.position,Record.position)<1.8f){dwell+=Time.deltaTime;InteractionCountdown.Report(Record,dwell,1f,"READING");if(dwell>=1){RecordClaimed=true;g.Coins+=100;g.Gems+=4;g.Tell(Records[Chapter-21]+" / +100 Gold +4 Gems",9);g.Sound("discover");}}else dwell=0;
   if(Chapter!=25)return;int id=Seals.FindIndex(s=>s&&Vector3.Distance(g.Player.transform.position,s.position)<1.8f);
   if(id<0){sealExit=false;sealDwell=0;nearbySeal=-1;return;}if(id!=nearbySeal){nearbySeal=id;sealDwell=0;sealExit=false;}if(sealExit)return;
   var boss=g.Enemies.Find(e=>e&&e.Kind==31&&e.Health>0);if(!boss)return;
   if(g.Elapsed>=hintAt){hintAt=g.Elapsed+1.5f;g.Tell("WARD "+new[]{"EMBER","FROST","GALE"}[boss.AstralPhase-1]+" — match your element and stand at its focus",1.6f);}
   if(g.Player.Power==id&&boss.AstralPhase-1==id&&boss.AstralWard){sealDwell+=Time.deltaTime;InteractionCountdown.Report(Seals[id],sealDwell,1f,"GROUNDING");if(sealDwell>=1){GroundSeal(id);sealExit=true;sealDwell=0;}}else sealDwell=0;
  }
 }
 public sealed class AstralRoute:MonoBehaviour {
  public AstralChapter Owner;public int Index;public bool[] Attuned=new bool[2];public int Side {get;private set;}public bool Shifting=>shiftUntil>0;
  public Transform Bridge;public readonly List<Transform> Courts=new List<Transform>();public readonly List<Transform> Focuses=new List<Transform>();
  float shiftUntil,channel,hintAt,padDwell;int requested,selected=-1;bool padExit;
  public Vector3 PadPosition(int side)=>transform.position+new Vector3(side==0?-2:2,.03f,-2.5f);
  bool OnBridge(Vector3 p){var q=Bridge.InverseTransformPoint(p);return Mathf.Abs(q.x)<2&&Mathf.Abs(q.z)<3.8f&&Mathf.Abs(q.y)<3;}
  public bool Occupied {get{var g=RealmGame.I;if(g.Player&&OnBridge(g.Player.transform.position))return true;foreach(var e in g.Enemies)if(e&&e.Health>0&&OnBridge(e.transform.position))return true;return false;}}
  public bool RequestShift(int side){if(side<0||side>1||Shifting||side==Side||Occupied)return false;requested=side;shiftUntil=RealmGame.I.Elapsed+.9f;RealmGame.I.Sound("astral_shift");return true;}
  public bool Guarded(int side)=>RealmGame.I.Enemies.Exists(e=>e&&e.Health>0&&!e.Boss&&Vector3.Distance(e.transform.position,Courts[side].position)<5.5f);
  public bool Attune(int side){if(side<0||side>1||Side!=side||Shifting||Guarded(side)||Attuned[side])return false;Attuned[side]=true;ObjectiveRestoration.Activate(Focuses[side].gameObject);RealmGame.I.Sound("astral_align");return true;}
  void PoseBridge(){var direction=(Courts[Side].position-transform.position).normalized;Bridge.position=transform.position+direction*11;Bridge.rotation=Quaternion.LookRotation(direction);}
  public void Build(){Side=Index%2;for(int side=0;side<2;side++){
   float sign=side==0?-1:1;var court=new GameObject("Island").transform;court.SetParent(Owner.transform,false);court.position=transform.position+new Vector3(sign*14,0,12);
   ExplorationArt.Fit("Islands/R2_Island_SteppingStone","Islands/Textures/R2_Island_SteppingStone_basecolor",court,new Vector3(0,-.2f,0),new Vector3(7.8f,4.2f,7.8f),true,false,new Color(.72f,.8f,.95f));Courts.Add(court);
   AstralArt.Place("Terrace",court,Vector3.zero,new Vector3(8,1,8),true);Focuses.Add(AstralArt.Place("WardFocus",court,new Vector3(0,0,1.5f),Vector3.one*1.1f,true).transform);
   RealmGame.I.World.SpawnAstralGuard(court.position+new Vector3(0,.05f,-1),side==0?30:29,court.position,8);
   if(side==0&&Owner.Chapter>=23)RealmGame.I.World.SpawnAstralGuard(court.position+new Vector3(1.7f,.05f,0),29,court.position,8);
   var pad=AstralArt.Place(side==0?"MoonPad":"SunPad",transform,transform.InverseTransformPoint(PadPosition(side)),new Vector3(1.3f,.25f,1.3f));pad.name=side==0?"MOON shift pad":"SUN shift pad";
   var label=new GameObject("Route label");label.transform.SetParent(pad.transform,false);label.transform.localPosition=Vector3.up*.35f;label.transform.localRotation=Quaternion.Euler(90,0,0);var text=label.AddComponent<TextMesh>();LocalizedWorldText.Set(text,side==0?"MOON":"SUN");text.anchor=TextAnchor.MiddleCenter;text.characterSize=.12f;text.fontSize=48;text.color=side==0?new Color(.7f,.65f,1):new Color(1,.8f,.3f);text.font=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");if(text.font)label.GetComponent<Renderer>().sharedMaterial=text.font.material;
  }
   Bridge=new GameObject("Astral shifting bridge").transform;Bridge.SetParent(Owner.transform,false);PoseBridge();AstralArt.Place("Bridge",Bridge,Vector3.zero,new Vector3(3.2f,1,7.2f),true);
   AstralArt.Place("Orrery",transform,new Vector3(3.2f,0,3.2f),Vector3.one*2.4f,true);
  }
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   if(Shifting&&g.Elapsed>=shiftUntil){if(Occupied){shiftUntil=g.Elapsed+.4f;g.Tell("BRIDGE OCCUPIED — leave the bridge before shifting",1.5f);}else{Side=requested;PoseBridge();Physics.SyncTransforms();shiftUntil=0;}}
   int pad=-1;for(int i=0;i<2;i++)if(Vector3.Distance(g.Player.transform.position,PadPosition(i))<.65f)pad=i;
   if(pad<0){padDwell=0;padExit=false;selected=-1;}else if(!padExit){if(pad!=selected){selected=pad;padDwell=0;}padDwell+=Time.deltaTime;InteractionCountdown.Report(transform,padDwell,.65f,"ALIGNING");if(padDwell>=.65f){if(RequestShift(pad))g.Tell("ROUTE SHIFTING — wait for the bridge to settle",1.3f);padExit=true;padDwell=0;}}
   if(Shifting)InteractionCountdown.Report(Bridge,.9f-(shiftUntil-g.Elapsed),.9f,"SHIFTING");
   if(!g.NoticeActive&&g.Elapsed>=hintAt&&Vector3.Distance(g.Player.transform.position,transform.position)<5){hintAt=g.Elapsed+2;g.Tell(Shifting?"ROUTE SHIFTING":Side==0?"MOON ROUTE — reconnect its focus, then return for SUN":"SUN ROUTE — reconnect its focus, then return for MOON",2.1f);}
   bool near=!Shifting&&!Attuned[Side]&&Vector3.Distance(g.Player.transform.position,Focuses[Side].position)<1.8f&&!Guarded(Side);
   if(near){channel+=Time.deltaTime;InteractionCountdown.Report(Focuses[Side],channel,1f,"ATTUNING");if(channel>=1){Attune(Side);channel=0;g.Tell("ASTRAL FOCUS CONNECTED",2);}}else channel=0;
   for(int i=0;i<2;i++){var tint=Attuned[i]?new Color(.35f,.85f,1):i==Side?new Color(1,.77f,.25f):new Color(.45f,.25f,.65f);foreach(var renderer in Focuses[i].GetComponentsInChildren<Renderer>()){renderer.material.EnableKeyword("_EMISSION");renderer.material.SetColor("_EmissionColor",tint*.2f);}}
  }
 }
}
