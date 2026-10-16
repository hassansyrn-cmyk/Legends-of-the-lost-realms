using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public partial class Enemy {public bool DungeonShield;}
 public partial class RealmGame {public DungeonAdventure Crypt {get;private set;}public List<Vector3> NavigationRoute=>Crypt&&Crypt.Active?Crypt.Route:World.Route;}
 // A separately activated optional route. It uses the chapter's world ownership,
 // art and physics, but cannot advance or finish the main campaign gate.
 public sealed class DungeonAdventure:MonoBehaviour {
  public bool Active {get;private set;}public int Room {get;private set;}public int PuzzleCount {get;private set;}
  public Transform Entrance {get;private set;}public readonly List<Transform> Islands=new List<Transform>();public readonly List<Enemy> Foes=new List<Enemy>();public readonly List<DungeonFocus> Foci=new List<DungeonFocus>();
  readonly List<GameObject> mainObjects=new List<GameObject>();readonly List<bool> mainActive=new List<bool>();readonly List<Vector3> arrivals=new List<Vector3>();
  readonly List<Transform> exits=new List<Transform>();Vector3 returnPoint,oldCheckpoint;bool oldCheckpointActive,built;float entranceDwell,exitDwell;bool needsExit=true;
  RealmGame Game=>RealmGame.I;
  public List<Vector3> Route=>arrivals;
  public string Status=>Room>=5?"CRYPT RESTORED":"CRYPT "+(Room+1)+"/5";
  public static readonly string[] RoomNames={"THE LOST OATH","HALL OF WATCHERS","THE THREEFOLD CIPHER","THE BROKEN GUARD","OATHKEEPER'S VIGIL"};
  public static DungeonAdventure Build(RealmGame g){
   if(g.Level!=3)return null;var d=g.World.gameObject.AddComponent<DungeonAdventure>();
   var island=g.Adventure.Branch;Vector3 p=ExpansionChapter.ClearSpot(g.World,island,island.position+Vector3.right*2.6f);d.Entrance=Marker(island,p,"OATHKEEPER'S CRYPT — STAND TO ENTER",new Color(.3f,.7f,1f),true,0);return d;
  }
  static readonly Dictionary<string,Material> cryptMaterials=new Dictionary<string,Material>();
  static GameObject PlaceCryptModel(string name,Transform parent,Vector3 pos,Vector3 scale,bool solid=false,Color? accent=null){
   var prefab=Resources.Load<GameObject>("Props/Crypt/"+name);
   if(!prefab)return null;
   var go=new GameObject("Crypt "+name);go.transform.SetParent(parent,false);
   go.transform.localPosition=pos;go.transform.localScale=scale;
   var model=Instantiate(prefab,go.transform,false);
   var renderers=model.GetComponentsInChildren<Renderer>();
   if(renderers.Length>0){
    Bounds b=renderers[0].bounds;
    foreach(var r in renderers)b.Encapsulate(r.bounds);
    model.transform.position+=go.transform.position-new Vector3(b.center.x,b.min.y,b.center.z);
   }
   if(!cryptMaterials.TryGetValue(name,out var mat)||!mat){
    mat=new Material(Shader.Find("Standard")??Shader.Find("Mobile/Diffuse")){name="Crypt "+name};
    var tex=Resources.Load<Texture2D>("Props/Crypt/Textures/"+name+"_basecolor");
    var norm=Resources.Load<Texture2D>("Props/Crypt/Textures/"+name+"_normal");
    if(tex)mat.mainTexture=tex;
    mat.SetFloat("_Glossiness",.22f);
    mat.SetFloat("_Metallic",.18f);
    if(norm){
     mat.SetTexture("_BumpMap",norm);
     mat.SetFloat("_BumpScale",1f);
     mat.EnableKeyword("_NORMALMAP");
    }
    cryptMaterials[name]=mat;
   }
   foreach(var r in go.GetComponentsInChildren<Renderer>()){
    r.sharedMaterial=mat;
    if(accent.HasValue&&accent.Value!=Color.white){
     var block=new MaterialPropertyBlock();
     r.GetPropertyBlock(block);
     block.SetColor("_EmissionColor",accent.Value*.4f);
     r.SetPropertyBlock(block);
    }
   }
   if(solid)foreach(var mesh in go.GetComponentsInChildren<MeshFilter>()){
    var c=mesh.gameObject.AddComponent<MeshCollider>();
    c.sharedMesh=mesh.sharedMesh;
   }
   return go;
  }
  internal static Transform Marker(Transform parent,Vector3 point,string caption,Color color,bool entrance=false,int index=0){
   var go=new GameObject("Crypt "+caption);go.transform.SetParent(parent,false);go.transform.position=point;
   string assetPath;
   if(entrance){
    assetPath=index%2==0?"Props/Crypt/CryptEntrance":"Props/Expansion/PortalArch";
   }else if(caption.Contains("RETURN")){
    assetPath=index%2==0?"Props/Expansion/PortalArch":"Props/Crypt/CryptEntrance";
   }else if(caption.Contains("EMBER CIPHER")){
    assetPath="Props/Expansion/EmberFocus";
   }else if(caption.Contains("FROST CIPHER")){
    assetPath="Props/Expansion/FrostFocus";
   }else if(caption.Contains("GALE CIPHER")){
    assetPath="Props/Expansion/GaleFocus";
   }else{
    string[] altarPool={"Props/Crypt/CryptMemoryAltar","Props/Expansion/SealStone","Props/Expansion/CrystalShrine"};
    assetPath=altarPool[index%altarPool.Length];
   }
   var model=ExpansionChapter.PlacePuzzleModel(assetPath,go.transform,color,out float height);
   if(!model){
    var fallback=new GameObject("Crypt memory crystal");fallback.transform.SetParent(go.transform,false);fallback.transform.localPosition=new Vector3(0,.75f,.72f);fallback.transform.localScale=Vector3.one*.65f;RelicArt.Gem(fallback.transform,color);
    height=1.6f;
   }
   ExpansionChapter.ClearOverlappingProps(parent,point,model?model.transform.position:point,1.45f);
   Vector3 labelOffset=model?model.transform.localPosition:Vector3.zero;
   var label=new GameObject("Crypt inscription");label.transform.SetParent(go.transform,false);label.transform.localPosition=labelOffset+Vector3.up*(height+.35f);var t=label.AddComponent<TextMesh>();t.text=caption;t.fontSize=36;t.characterSize=.036f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(1f,.89f,.6f);var font=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");if(font){t.font=font;t.GetComponent<Renderer>().sharedMaterial=font.material;}label.AddComponent<ExpansionBillboard>();
   return go.transform;
  }
  void BuildRoute(){
   var g=Game;
   // Snapshot actual active flags; hidden/disabled chapter objects must stay so on return.
   foreach(Transform child in g.World.transform)if(child!=g.Player.transform&&!child.GetComponent<Light>()){mainObjects.Add(child.gameObject);mainActive.Add(child.gameObject.activeSelf);}
   for(int room=0;room<5;room++){
    var island=g.World.AdventureIsland(new Vector3(1000+(room%2)*1.2f,0,room*17f),700+room).transform;Islands.Add(island);
    PlaceCryptModel("CryptRoomDeck",island,Vector3.zero,new Vector3(14,1,14),false);
    Vector3 arrival=ExpansionChapter.ClearSpot(g.World,island,island.position+Vector3.back*4f);arrivals.Add(arrival+Vector3.up*.05f);
    exits.Add(Marker(island,ExpansionChapter.ClearSpot(g.World,island,island.position+new Vector3(-3f,0,-3f)),"RETURN TO ROOTBOUND RUINS",new Color(.5f,.8f,1f),false,room));
    var altar=Marker(island,ExpansionChapter.ClearSpot(g.World,island,island.position+Vector3.forward*2.5f),RoomNames[room],new Color(1f,.76f,.28f),false,room);
    var focus=altar.gameObject.AddComponent<DungeonFocus>();focus.Owner=this;focus.Room=room;focus.Order=-1;Foci.Add(focus);
    PlaceArch(island);
    if(room==2){for(int order=0;order<3;order++){var marker=Marker(island,ExpansionChapter.ClearSpot(g.World,island,island.position+new Vector3((order-1)*3f,0,-.7f)),new[]{"EMBER","FROST","GALE"}[order]+" CIPHER",RealmGame.ElementColors[order],false,order);var f=marker.gameObject.AddComponent<DungeonFocus>();f.Owner=this;f.Room=room;f.Order=order;Foci.Add(f);}}
   }
   built=true;
  }
  static void PlaceArch(Transform island){
   var go=PlaceCryptModel("CryptArch",island,new Vector3(4f,0,2.5f),Vector3.one*1.2f,true);
   if(!go)return;
   go.name="Prop Crypt arch";
   var renderers=go.GetComponentsInChildren<Renderer>();if(renderers.Length==0){Destroy(go);return;}
   Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
   if(!ChapterLayout.Supported(island,go.transform,bounds,out float deck)){Destroy(go);return;}
   go.transform.position+=Vector3.up*(deck-bounds.min.y);
   TrapArt.Reserve(island,island.InverseTransformPoint(go.transform.position),Mathf.Max(bounds.extents.x,bounds.extents.z)+.3f);
  }
  Enemy Spawn(Transform island,int kind,Vector3 offset,int room){var foe=Game.World.SpawnExpansionCompanion(island,kind,offset);foe.gameObject.name="Crypt room "+room+" defender";foe.gameObject.AddComponent<DungeonDefender>().Room=room;Foes.Add(foe);return foe;}
  public bool Enter(){
   var g=Game;if(Active||g.Screen!=GameScreen.Playing)return false;
   // Optional elite protects the entrance; don't allow free escape from its fight.
   if(g.Adventure.OptionalElite&&g.Adventure.OptionalElite.Health>0){g.Tell("Defeat the Rootbound Duelist before entering the crypt.",4f);return false;}
   returnPoint=Entrance.position+Vector3.back*.8f+Vector3.up*.05f;oldCheckpoint=g.Checkpoint;oldCheckpointActive=g.CheckpointActive;
   if(!built)BuildRoute();foreach(var obj in mainObjects)if(obj)obj.SetActive(false);foreach(var island in Islands)island.gameObject.SetActive(true);
   Room=g.Save.cryptCleared?0:Mathf.Clamp(g.Save.cryptRoom,0,4);PuzzleCount=0;Active=true;ResetEncounters();ActivateRoom();g.Player.Warp(arrivals[Room]);g.Checkpoint=arrivals[Room];g.CheckpointActive=true;needsExit=true;entranceDwell=exitDwell=0;g.Tell("OATHKEEPER'S CRYPT: "+Instruction(),6f);return true;
  }
  void ResetEncounters(){
   foreach(var foe in Foes)if(foe){foe.Health=0;foe.gameObject.SetActive(false);Destroy(foe.gameObject);}Foes.Clear();
   foreach(var focus in Foci)if(focus)focus.Completed=focus.Room<Room;
   for(int room=Room;room<5;room++){
    var island=Islands[room];if(room==1){Spawn(island,19,new Vector3(-2,0,0),room).DungeonShield=true;Spawn(island,7,new Vector3(2,0,1),room);}
    if(room==3){Spawn(island,24,new Vector3(-2,0,0),room);Spawn(island,17,new Vector3(2,0,1),room);}
    if(room==4){var keeper=Spawn(island,19,new Vector3(0,0,0),room);keeper.DisplayName="THE OATHKEEPER";keeper.NamedSignature=6;keeper.Health=keeper.MaxHealth=26f;}
   }
  }
  void ActivateRoom(){foreach(var foe in Foes)if(foe){var role=foe.GetComponent<DungeonDefender>();foe.gameObject.SetActive(Active&&role.Room==Room&&foe.Health>0);}foreach(var focus in Foci)if(focus)focus.gameObject.SetActive(focus.Room>=Room||Game.Save.cryptCleared);}
  string Instruction()=>Room==0?"Read the oath at the golden memory crystal. Return shrines can leave the dungeon at any time.":Room==1?"Defeat the shield watcher and caster, then restore the memory crystal.":Room==2?"Attune the three cipher crystals in order: Ember, Frost, Gale. Then restore the memory crystal.":Room==3?"Defeat the bruiser and ember archer, then restore the memory crystal.":Room==4?"Defeat the Oathkeeper, then restore his memory crystal to claim the Frost Emblem.":"Crypt restored. Stand at a return shrine to leave.";
  public bool CanRestore(DungeonFocus f){if(!Active||Room>=5||f.Room!=Room||f.Completed)return false;if(f.Order>=0)return Room==2&&f.Order==PuzzleCount&&Game.Player.Power==f.Order;if(Room==2&&PuzzleCount<3)return false;foreach(var e in Foes)if(e&&e.Health>0&&e.GetComponent<DungeonDefender>().Room==Room)return false;return true;}
  public void Restore(DungeonFocus f){if(!CanRestore(f))return;f.Completed=true;if(f.Order>=0){PuzzleCount++;Game.Sound("checkpoint");Game.Tell("Cipher attuned "+PuzzleCount+"/3. "+Instruction(),4f);return;}
   Room++;if(!Game.Save.cryptCleared){Game.Save.cryptRoom=Room;Game.Persist();}Game.Sound("checkpoint");
   if(Room>=5){CompleteDungeon();return;}ActivateRoom();Game.Checkpoint=arrivals[Room];Game.CheckpointActive=true;Game.Tell("Oath memory restored. "+Instruction(),6f);
  }
  void CompleteDungeon(){var g=Game;if(!g.Save.cryptCleared){g.Save.cryptCleared=true;g.Save.cryptRoom=5;g.Save.coins+=150;BuildProgression.Unlock(g,6);if(g.Save.wayfinderQuest==3)g.Save.wayfinderQuest=4;g.Persist();g.Tell("CRYPT RESTORED: Frost Emblem and 150 Gold banked. Return to Lyra if you hold her commission.",7f);}else g.Tell("Crypt restored again. Its first-clear reward has already been claimed.",5f);ActivateRoom();}
  public void Leave(){if(!Active)return;Active=false;foreach(var foe in Foes)if(foe)foe.gameObject.SetActive(false);foreach(var island in Islands)if(island)island.gameObject.SetActive(false);for(int i=0;i<mainObjects.Count;i++)if(mainObjects[i])mainObjects[i].SetActive(mainActive[i]);Game.Checkpoint=oldCheckpoint;Game.CheckpointActive=oldCheckpointActive;Game.Player.Warp(returnPoint);needsExit=true;exitDwell=entranceDwell=0;Game.Tell("Returned to Rootbound Ruins. Crypt room progress is saved.",5f);}
  void Update(){var g=Game;if(!g||!g.Player||g.Screen!=GameScreen.Playing)return;
   if(!Active){if(!Entrance)return;bool near=ExpansionChapter.NearPointOrModel(g.Player.transform.position,Entrance,1.65f);if(needsExit){if(!near)needsExit=false;return;}entranceDwell=near&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f?entranceDwell+Time.deltaTime:0;if(entranceDwell>=1.2f){entranceDwell=0;Enter();}return;}
   Transform exit=null;foreach(var t in exits)if(ExpansionChapter.NearPointOrModel(g.Player.transform.position,t,1.5f)){exit=t;break;}
   exitDwell=exit&&g.Player.Grounded&&g.Player.HorizontalSpeed<.5f?exitDwell+Time.deltaTime:0;if(exitDwell>=1.2f){Leave();return;}
  }
 }
 public sealed class DungeonFocus:MonoBehaviour {
  public DungeonAdventure Owner;public int Room,Order;public bool Completed;float dwell;
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!Owner.CanRestore(this)) {dwell=0;return;}bool ready=g.Player.Grounded&&g.Player.HorizontalSpeed<.5f&&ExpansionChapter.NearPointOrModel(g.Player.transform.position,transform,1.65f);dwell=ready?dwell+Time.deltaTime:0;if(dwell>=.85f*BuildProgression.ChannelMultiplier(g.Save))Owner.Restore(this);}
 }
 public sealed class DungeonDefender:MonoBehaviour {public int Room;}
}
