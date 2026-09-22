using System;
using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public enum GameScreen { Menu, Map, Playing, Paused, Complete, Defeated, Settings }
[Serializable] public class Progress {
   public void NormalizeWeapons(){
    equippedWeapon=Mathf.Clamp(equippedWeapon,-1,40);
    if(weapons==null)weapons=new List<int>();
    if(equippedWeapon>=0&&!weapons.Contains(equippedWeapon))weapons.Add(equippedWeapon);
    for(int i=weapons.Count-1;i>=0;i--)if(weapons[i]<0||weapons[i]>40||weapons.IndexOf(weapons[i])!=i)weapons.RemoveAt(i);
   }
   public int version=2;public int unlocked=1, equippedWeapon=-1, coins, gems, healthRank, powerRank, arsenalRank, aetherRank, moxieRank, tempoRank, windRank; public int[] stars=new int[15]; public float[] best=new float[15]; public bool music=true,sound=true,postFx=true,shake=true,haptics=true; public List<int> weapons=new List<int>();
  }
 public class RealmGame : MonoBehaviour {
  public static RealmGame I; public static bool Testing=>Array.IndexOf(Environment.GetCommandLineArgs(),"-realmTest")>=0; public static readonly string[] Titles={"Mosslight Trail","Whispering Falls","Rootbound Ruins","The Elder Grove","Sunscorched Pass","Temple of Keys","Sandstone Colossus","Frostwind Climb","Crystal Hollow","Crown of Winter","Ember Foothills","Brimstone Rampart","Cindervein Gorge","Obsidian Ascent","Emberfall Summit"};
public static readonly string[] Realms={"VERDANT KINGDOM","BURNING DUNES","FROZEN PEAKS","EMBERFALL"};
   public static readonly Color[] Accents={new Color(.38f,.95f,.7f),new Color(1,.67f,.28f),new Color(.4f,.83f,1),new Color(1f,.35f,.28f)};
  public GameScreen Screen=GameScreen.Menu; public Progress Save=new Progress(); public Hero Player; public RealmWorld World; public FollowCamera CameraRig;
  public int Level=1, Realm, Coins, Gems, DamageTaken, EarnedStars, Combo; public float Elapsed; public Vector3 Checkpoint; public bool CheckpointActive; public string Notice=""; float noticeUntil,comboUntil; public Color Accent=>Accents[Realm];
  public WeaponDefinition CurrentWeapon=>WeaponCatalog.Get(Save.equippedWeapon);
  public Vector2 MoveInput; public bool JumpPressed,DashPressed,AttackPressed,AttackReleased,CastPressed,ParryPressed,SpellPressed,SpellReleased,GrapplePressed; public bool AttackHeld,SpellHeld,JumpHeld;
  public readonly List<Enemy> Enemies=new List<Enemy>(); public AudioSource Music,Sfx;
  public RealmAudio Audio {get;private set;} public RealmTrials Trial {get;private set;}
   Transform worldRoot; GUIStyle title,titleC,label,small,button,center,big,smallC,tinyC; Texture2D pixel,circleFill,circleRing; readonly TouchRouter touch=new TouchRouter(); float yawInput,hitStopUntil,bossIntroUntil;bool showArsenal;int arsenalPage;GameScreen arsenalReturn=GameScreen.Settings;readonly System.Collections.Generic.Dictionary<string,Texture2D> iconCache=new System.Collections.Generic.Dictionary<string,Texture2D>();
  public static readonly Color[] ElementColors={new Color(1f,.45f,.1f),new Color(.2f,.85f,1f),new Color(.2f,1f,.55f)};
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
   var existing=FindObjectsByType<RealmGame>(FindObjectsSortMode.None);
   if(existing.Length==0){new GameObject("Lost Realms 3D").AddComponent<RealmGame>();return;}
   // Never allow two game instances (two cameras/listeners/OnGUI = dead menus).
   for(int i=1;i<existing.Length;i++)if(existing[i])Destroy(existing[i].gameObject);
  }
  void Awake(){ I=this; Application.targetFrameRate=60; QualitySettings.vSyncCount=1; Time.fixedDeltaTime=1f/60f; UnityEngine.Screen.orientation=ScreenOrientation.LandscapeLeft;
try{if(!Testing&&PlayerPrefs.HasKey("LostRealms3D.v2"))Save=JsonUtility.FromJson<Progress>(PlayerPrefs.GetString("LostRealms3D.v2"))??new Progress();
     else if(!Testing&&PlayerPrefs.HasKey("LostRealms3D.v1")){Save=JsonUtility.FromJson<Progress>(PlayerPrefs.GetString("LostRealms3D.v1"))??new Progress();Save.version=2;Persist();}}catch{Save=new Progress();}
    if(Save.stars==null||Save.stars.Length!=15){var old=Save.stars;Save.stars=new int[15];if(old!=null)for(int i=0;i<old.Length&&i<15;i++)Save.stars[i]=old[i];}
    if(Save.best==null||Save.best.Length!=15){var old=Save.best;Save.best=new float[15];if(old!=null)for(int i=0;i<old.Length&&i<15;i++)Save.best[i]=old[i];}
    Save.unlocked=Mathf.Clamp(Save.unlocked,1,15);Save.equippedWeapon=Mathf.Clamp(Save.equippedWeapon,-1,40);
    // Collected-weapons inventory: init for old saves, grandfather the
    // currently equipped blade, drop dupes and out-of-range ids.
    Save.NormalizeWeapons();
    Save.healthRank=Mathf.Clamp(Save.healthRank,0,3);Save.powerRank=Mathf.Clamp(Save.powerRank,0,3);Save.arsenalRank=Mathf.Clamp(Save.arsenalRank,0,3);
    Save.aetherRank=Mathf.Clamp(Save.aetherRank,0,3);Save.moxieRank=Mathf.Clamp(Save.moxieRank,0,3);Save.tempoRank=Mathf.Clamp(Save.tempoRank,0,3);Save.windRank=Mathf.Clamp(Save.windRank,0,3);
   Music=gameObject.AddComponent<AudioSource>(); Music.loop=true; Music.volume=.24f; Sfx=gameObject.AddComponent<AudioSource>(); Sfx.volume=.7f;
   Audio=gameObject.AddComponent<RealmAudio>();Audio.Initialize(this);
   var cam=new GameObject("Adventure Camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.gameObject.AddComponent<AudioListener>(); cam.nearClipPlane=.25f; cam.farClipPlane=320; cam.fieldOfView=58; cam.gameObject.AddComponent<RealmPostFx>(); CameraRig=cam.gameObject.AddComponent<FollowCamera>();
   // Exactly one audio listener: silence any stray scene camera/listener so the
   // console spam and doubled audio cannot happen.
   foreach(var listener in FindObjectsByType<AudioListener>(FindObjectsSortMode.None))if(listener&&listener.gameObject!=cam.gameObject)listener.enabled=false;
   // Startup must never leave the menu dead: if the first level fails to build,
   // log it and still show a working main menu.
   try{LoadLevel(1);}catch(System.Exception e){Debug.LogError("STARTUP_LOAD_FAILED "+e);}
   Screen=GameScreen.Menu; Tell("The Heart of Realms is waiting.",4);
  }
  public void Persist(){if(Testing)return;PlayerPrefs.SetString("LostRealms3D.v2",JsonUtility.ToJson(Save));PlayerPrefs.Save();}
  public void LoadLevel(int id){
   showArsenal=false;arsenalPage=0;
   if(Audio)Audio.ClearRunSounds();
   Time.timeScale=1; touch.Reset(); if(worldRoot){worldRoot.gameObject.SetActive(false);Destroy(worldRoot.gameObject);} Enemies.Clear(); Level=Mathf.Clamp(id,1,15); Realm=Level<=4?0:Level<=7?1:Level<=10?2:3; Coins=Gems=DamageTaken=EarnedStars=0; Elapsed=0; CheckpointActive=false;Combo=0;comboUntil=0;
   worldRoot=new GameObject("Realm "+Level+" - "+Titles[Level-1]).transform; World=worldRoot.gameObject.AddComponent<RealmWorld>();
   try{World.Build(Level,Realm);}catch(System.Exception e){Debug.LogError("LEVEL_BUILD_FAILED "+Level+": "+e);}
   Trial=RealmTrials.Build(World,Level);
   GUI.enabled=true;
   var hero=new GameObject("Aster"); hero.transform.SetParent(worldRoot); hero.transform.position=World.Spawn; Player=hero.AddComponent<Hero>(); Checkpoint=World.Spawn;
   // Restore the camera only after the newly-created Hero Awake path completes.
   CameraRig.Target=Player.transform;
   CameraRig.Snap();
   if(World.IsBoss)CameraRig.ZoomBias=-1.3f;
   bossIntroUntil=World.IsBoss?Time.unscaledTime+4.4f:0f;
    string stageTrack=World.IsBoss?"boss_battle_theme":Realm==0?"verdant_theme":Realm==1?"desert_exploration_theme":Realm==2?"frozen_exploration_theme":"emberfall_exploration_theme";SetMusic(stageTrack);
    Screen=GameScreen.Playing; Tell(Level==1?"WASD / left stick to move. Jump twice to reach the next island.":World.IsBoss?"Break the guardian's corruption. Dodge red warnings, strike during recovery.":Titles[Level-1]+"  /  Follow the golden trail to the realm gate.",6);
  }
  void Update(){
   JumpPressed=DashPressed=AttackPressed=AttackReleased=CastPressed=ParryPressed=SpellPressed=SpellReleased=GrapplePressed=false; MoveInput=Vector2.zero; yawInput=0;
   if(hitStopUntil>0f&&Time.unscaledTime>=hitStopUntil){hitStopUntil=0f;Time.timeScale=1f;}
   if(!I||!Player)return;
   if(Input.GetKeyDown(KeyCode.Escape)){if(showArsenal)showArsenal=false;else if(Screen==GameScreen.Playing)Pause();else if(Screen==GameScreen.Paused)Resume();else Screen=GameScreen.Menu;}
    if(Screen==GameScreen.Menu||Screen==GameScreen.Map)SetMusic("verdant_theme");
    else if(Screen==GameScreen.Settings)SetMusic("frozen_exploration_theme");
    else if(Screen==GameScreen.Playing){
     string stageTrack=(World&&World.IsBoss)?"boss_battle_theme":Realm==0?"verdant_theme":Realm==1?"desert_exploration_theme":Realm==2?"frozen_exploration_theme":"emberfall_exploration_theme";
     SetMusic(stageTrack);
    }
    if(Screen!=GameScreen.Playing){AttackHeld=false;touch.Reset();return;} Elapsed+=Time.deltaTime; if(comboUntil>0f&&Elapsed>=comboUntil){Combo=0;comboUntil=0;}
    MoveInput=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
    JumpPressed=Input.GetKeyDown(KeyCode.Space);DashPressed=Input.GetKeyDown(KeyCode.LeftShift);AttackPressed=Input.GetKeyDown(KeyCode.J);AttackReleased=Input.GetKeyUp(KeyCode.J);AttackHeld=Input.GetKey(KeyCode.J);CastPressed=Input.GetKeyDown(KeyCode.K);ParryPressed=Input.GetKeyDown(KeyCode.L);SpellPressed=Input.GetKeyDown(KeyCode.F);SpellReleased=Input.GetKeyUp(KeyCode.F);SpellHeld=Input.GetKey(KeyCode.F);GrapplePressed=Input.GetKeyDown(KeyCode.G);JumpHeld=Input.GetKey(KeyCode.Space);
    if(Input.GetKeyDown(KeyCode.Q)){Player.Power=(Player.Power+1)%3;Sound("power_select");}
   if(Input.touchCount==0&&Input.GetMouseButton(1))yawInput=Input.GetAxis("Mouse X")*3;
   float sx=1280f/UnityEngine.Screen.width,sy=720f/UnityEngine.Screen.height;
   touch.BeginFrame();
   foreach(var t in Input.touches)touch.Sample(t.fingerId,new Vector2(t.position.x*sx,(UnityEngine.Screen.height-t.position.y)*sy),t.deltaPosition*sx,t.phase);
   if(Input.touchCount==0)touch.Reset();
   MoveInput+=touch.Move;JumpPressed|=touch.Jump;DashPressed|=touch.Dodge;CastPressed|=touch.Power;
   AttackPressed|=touch.BladePressed;AttackReleased|=touch.BladeReleased;AttackHeld|=touch.BladeHeld;ParryPressed|=touch.Parry;SpellPressed|=touch.Spell;SpellReleased|=touch.SpellReleased;SpellHeld|=touch.SpellHeld;yawInput+=touch.Yaw;
   MoveInput=Vector2.ClampMagnitude(MoveInput,1); CameraRig.Yaw+=yawInput;
  }
  public void Pause(){Screen=GameScreen.Paused;Audio.Suspend(true);touch.Reset();}
  public void Resume(){showArsenal=false;Screen=GameScreen.Playing;Audio.Suspend(false);}
  void SetMusic(string key){
   Audio.SetTrack(key);
  }
  void OnApplicationPause(bool paused){if(paused&&Screen==GameScreen.Playing)Pause();Persist();}
  void OnApplicationFocus(bool focused){if(!focused&&Screen==GameScreen.Playing)Pause();}
  public void Tell(string message,float seconds=3){Notice=message;noticeUntil=Time.unscaledTime+seconds;}
   public void ComboHit(){Combo++;comboUntil=Elapsed+1.1f;}
  public void Sound(string name){if(Audio)Audio.Play(name);}
  public void Sound(string name,float pitchMul){if(Audio)Audio.Play(name,1f,pitchMul);}
  // Short vibration tick on key feedback moments (parry, perfect dodge).
  // Mobile only, and gated by the Sanctuary HAPTICS toggle.
  public void Haptic(){if(Save.haptics&&Application.isMobilePlatform)Handheld.Vibrate();}
   public void TrapSound(string name,Vector3 position,float minDistance=3f,float maxDistance=15f,float volumeMul=1f){
    if(Audio)Audio.PlaySpatial(name,position,minDistance,maxDistance,volumeMul);
   }
  // A very short screen-wide time dip for perfect defense and counters. Physics
  // runs on the constant fixed step, so this slows the pacing, never the step.
  public void HitStop(float seconds,float scale=.12f){if(seconds<=0f)return;Time.timeScale=Mathf.Min(Time.timeScale,scale);float until=Time.unscaledTime+seconds;if(until>hitStopUntil)hitStopUntil=until;}
  public void Collect(bool gem){if(gem)Gems++;else Coins++;Sound(gem?"gem":"coin");}
   public void EquipWeapon(WeaponId id){
    if((int)id<0||(int)id>40)return;
    if(Save.weapons==null)Save.weapons=new List<int>();
    if((int)id>=0&&!Save.weapons.Contains((int)id))Save.weapons.Add((int)id);
    Save.equippedWeapon=(int)id;Persist();
    if(Player){EquippedWeapon.Equip(Player,id);Vfx.Play("ga_vfx_Sparks_01",Player.transform.position+Vector3.up*1.2f,Quaternion.identity,.9f);}
    var weapon=CurrentWeapon;Sound("weapon_pickup");Tell("EQUIPPED  "+weapon.Name+"  /  "+weapon.Summary,4.5f);
   }
   public void UnequipWeapon(){
    Save.equippedWeapon=-1;Persist();
    if(Player)foreach(var old in Player.GetComponentsInChildren<EquippedWeapon>(true)){old.gameObject.SetActive(false);old.transform.SetParent(null);Destroy(old.gameObject);}
    Sound("power_select");Tell("BARE FISTS  —  visit the Arsenal to rearm.",3.5f);
   }
  public void ActivateCheckpoint(Vector3 position){Checkpoint=position;CheckpointActive=true;Sound("checkpoint");Tell("Checkpoint restored. Your trail is safe.");}
  public void Respawn(){Player.Warp(Checkpoint);Player.Health=Player.MaxHealth;Player.Energy=100;CrumblePlatform.ResetAll();Sound("respawn");Vfx.Play("ga_vfx_Portal_01",Checkpoint,Quaternion.identity,1.1f);Tell("Returned to the checkpoint.");}
  public void Defeat(){Screen=GameScreen.Defeated;Sound("defeat");if(CameraRig)CameraRig.ZoomBias=1.6f;}
  public void Finish(){if(Screen!=GameScreen.Playing)return;if(World.IsBoss&&Enemies.Exists(x=>x&&x.Boss&&x.Health>0)){Tell("Defeat the guardian to open this gate.");return;}
EarnedStars=1+(Gems>0?1:0)+(DamageTaken==0?1:0); Save.stars[Level-1]=Mathf.Max(Save.stars[Level-1],EarnedStars); if(Save.best[Level-1]<=0||Elapsed<Save.best[Level-1])Save.best[Level-1]=Elapsed;
    Save.coins+=Coins+EarnedStars*10;Save.gems+=Gems;Save.unlocked=Mathf.Max(Save.unlocked,Mathf.Min(15,Level+1));Persist();Screen=GameScreen.Complete;Sound("complete");
  }
  public static string Clock(float seconds)=>$"{(int)seconds/60:00}:{(int)seconds%60:00}";
  float healthLag = 1f;
  void BoxOutline(Rect r,Color bg,Color border,float bw=2f){
   Box(r,bg);
   Box(new Rect(r.x,r.y,r.width,bw),border);
   Box(new Rect(r.x,r.yMax-bw,r.width,bw),border);
   Box(new Rect(r.x,r.y,bw,r.height),border);
   Box(new Rect(r.xMax-bw,r.y,bw,r.height),border);
   // Corner accents
   float cw=5f;
   Box(new Rect(r.x-1,r.y-1,cw,cw),border*1.3f);
   Box(new Rect(r.xMax-cw+1,r.y-1,cw,cw),border*1.3f);
   Box(new Rect(r.x-1,r.yMax-cw+1,cw,cw),border*1.3f);
   Box(new Rect(r.xMax-cw+1,r.yMax-cw+1,cw,cw),border*1.3f);
  }
  void Styles(){
   if(title!=null)return;pixel=Texture2D.whiteTexture;
   title=new GUIStyle(GUI.skin.label){fontSize=44,fontStyle=FontStyle.Bold,wordWrap=true,alignment=TextAnchor.UpperLeft};
   title.normal.textColor=new Color(1f,.94f,.82f);
   titleC=new GUIStyle(title){alignment=TextAnchor.MiddleCenter,fontSize=52,wordWrap=false};
   label=new GUIStyle(GUI.skin.label){fontSize=22,wordWrap=true};label.normal.textColor=new Color(.88f,.95f,.96f);
   small=new GUIStyle(label){fontSize=16};
   button=new GUIStyle(GUI.skin.button){fontSize=20,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
   button.normal.textColor=new Color(1f,.96f,.88f);button.padding=new RectOffset(12,12,10,10);
   // Strip the skin's button chrome so the hand-drawn frames below show through.
   button.normal.background=button.hover.background=button.active.background=button.focused.background=null;
   center=new GUIStyle(GUI.skin.label){fontSize=17,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
   center.normal.textColor=new Color(.94f,.98f,1f);
   big=new GUIStyle(center){fontSize=24};
   smallC=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
   tinyC=new GUIStyle(smallC){fontSize=13};tinyC.normal.textColor=new Color(.72f,.82f,.88f);
   circleFill=CircleTexture(false);circleRing=CircleTexture(true);
  }
  // Anti-aliased filled circle / ring, tinted at draw time via GUI.color.
  Texture2D CircleTexture(bool ring){
   const int S=128;var t=new Texture2D(S,S,TextureFormat.ARGB32,false);
   var c=new Color[S*S];var mid=new Vector2((S-1)*.5f,(S-1)*.5f);
   for(int y=0;y<S;y++)for(int x=0;x<S;x++){
    float d=Vector2.Distance(new Vector2(x,y),mid);
    float a=ring?Mathf.Clamp01(5.5f-Mathf.Abs(d-(S*.5f-5f))):Mathf.Clamp01((S*.5f-1f)-d);
    c[y*S+x]=new Color(1f,1f,1f,a);
   }
   t.SetPixels(c);t.Apply();t.wrapMode=TextureWrapMode.Clamp;return t;
  }
  void Box(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,pixel);GUI.color=Color.white;}
  void Text(float x,float y,float w,float h,string s,GUIStyle st=null){GUI.Label(new Rect(x,y,w,h),s,st??label);}
  bool Button(float x,float y,float w,float h,string s){
   Rect r=new Rect(x,y,w,h);
   Vector2 mpos=Event.current.mousePosition;
   bool hover=r.Contains(mpos);
   Color bg=hover?new Color(.12f,.22f,.28f,.98f):new Color(.05f,.11f,.16f,.94f);
   Color border=hover?Color.Lerp(Accent,Color.white,.4f):Accent*.85f;
   BoxOutline(r,bg,border,hover?2.5f:1.8f);
   return GUI.Button(r,s,button);
  }
  void Panel(float x,float y,float w,float h){
   Rect r=new Rect(x,y,w,h);
   // A restrained shadow and small rune-corner accents separate HUD groups
   // from the scene without burying the mobile playfield in opaque panels.
   Box(new Rect(x+4,y+5,w,h),new Color(.005f,.012f,.02f,.38f));
   BoxOutline(r,new Color(.028f,.06f,.09f,.74f),Accent,2f);
   Box(new Rect(x+8,y+7,w-16,2),new Color(Accent.r,Accent.g,Accent.b,.34f));
   Box(new Rect(x+8,y+h-8,w-16,1),new Color(Accent.r,Accent.g,Accent.b,.15f));
  }
  void OnGUI(){
   // Only the in-game HUD needs a live Player; menus must draw and respond even
   // if startup level construction ever fails.
   if(Screen==GameScreen.Playing&&!Player)return;
   Styles();
   GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(UnityEngine.Screen.width/1280f,UnityEngine.Screen.height/720f,1));
   if(showArsenal&&(Screen==GameScreen.Settings||Screen==GameScreen.Paused)){ArsenalView();return;}
   if(Screen==GameScreen.Playing){
    // Health & Realm Banner
    Panel(24,20,370,142);
    Text(42,28,320,24,$"REALM {Realm+1}  /  {Realms[Realm]}",small);
    Text(42,54,320,32,Titles[Level-1]);

    float targetH=Player?Mathf.Clamp01((float)Player.Health/Player.MaxHealth):1f;
    healthLag=Mathf.Lerp(healthLag,targetH,Time.unscaledDeltaTime*3.5f);
    BoxOutline(new Rect(42,98,230,14),new Color(.12f,.18f,.22f),new Color(.28f,.38f,.45f),1f);
    if(healthLag>targetH)Box(new Rect(43,99,228*healthLag,12),new Color(1f,.65f,.35f,.75f));
    Box(new Rect(43,99,228*targetH,12),new Color(.95f,.28f,.28f));
    Text(282,91,100,26,$"HP {Player.Health}/{Player.MaxHealth}",small);
    Text(42,116,330,20,$"WEAPON  {CurrentWeapon.Name}",small);
    if(Player&&Player.CounterReady)Text(42,138,330,20,"COUNTER READY  /  STRIKE NOW",small);
    if(Combo>=3){BoxOutline(new Rect(42,160,140,24),new Color(.05f,.09f,.12f,.85f),new Color(1f,.78f,.3f),1f);Text(52,163,122,18,$"COMBO  x{Combo}",small);}

    // Collectibles Panel
    Panel(404,20,240,68);
    Text(422,31,215,18,"TRAIL FINDINGS",small);
    Text(422,48,215,30,$"◉ {Coins:00}    ◆ {Gems}");

    // Power Selector & Energy Bar
    string[] powerNames={"EMBER [FIRE]","FROST [ICE]","GALE [WIND]"};
    Color[] powerCols={new Color(1f,.45f,.1f),new Color(.2f,.85f,1f),new Color(.2f,1f,.55f)};
    if(Button(660,20,205,50,powerNames[Player.Power])){Player.Power=(Player.Power+1)%3;Sound("power_select");}
    Text(660,72,205,15,"AETHER",small);
    BoxOutline(new Rect(660,88,205,8),new Color(.1f,.15f,.2f),powerCols[Player.Power]*.5f,1f);
    Box(new Rect(661,89,203*Mathf.Clamp01(Player.Energy/100f),6),powerCols[Player.Power]);

    // Timer and Pause Button
    Panel(882,20,135,50);Text(898,32,105,28,$"TIME {Clock(Elapsed)}",small);
    if(Button(1035,20,120,50,"PAUSE"))Pause();

    // Route compass: points at the next island, or the gate once past the last.
    {
     Vector3 pos=Player.transform.position;Vector3 tgt=pos;bool found=false;float bestZ=float.MaxValue;
     foreach(var node in World.Route){float dz=node.z-pos.z;if(dz>1f&&dz<bestZ){bestZ=dz;tgt=node;found=true;}}
     if(!found)tgt=new Vector3(0,pos.y,World.EndZ);
     Vector3 to=tgt-pos;to.y=0;float cd=to.magnitude;
     Vector3 fwd=Player.transform.forward;fwd.y=0;
     float ang=to.sqrMagnitude>.01f?Vector3.SignedAngle(fwd.normalized,to.normalized,Vector3.up):0f;
     float cx=500,cw=280;
     Panel(cx-8,532,cw+16,30);
     Box(new Rect(cx,545,cw,6),new Color(.1f,.16f,.2f,.9f));
     Box(new Rect(cx+cw*.5f-1,540,2,16),new Color(.35f,.45f,.5f,.9f));
     float mx=cx+cw*.5f+Mathf.Clamp(ang/90f,-1f,1f)*cw*.5f;
     Box(new Rect(mx-3,541,6,14),Accent);
     Text(cx+8,512,cw,20,$"{(int)cd}m to {(found?"next isle":"realm gate")}",small);
    }

     foreach(var foe in Enemies){if(!foe||foe.Boss||foe.Health<=0||Vector3.Distance(Player.transform.position,foe.transform.position)>12)continue;Vector3 v=Camera.main.WorldToViewportPoint(foe.transform.position+Vector3.up*(foe.Kind==6?2.9f:2.1f));float x=v.x*1280,y=(1-v.y)*720;if(v.z<=0||y<175||y>540||x<45||x>1235)continue;Box(new Rect(x-34,y,68,7),new Color(.03f,.07f,.08f,.85f));Box(new Rect(x-40,y,5,7),ElementColors[foe.WeakElement]);Box(new Rect(x-33,y+1,66*Mathf.Clamp01(foe.Health/foe.MaxHealth),5),ElementColors[foe.WeakElement]);}
    // Boss Bar
    var boss=Enemies.Find(x=>x&&x.Boss&&x.Health>0);
    if(boss&&Vector3.Distance(Player.transform.position,boss.transform.position)<32){
Panel(390,105,500,68);
      Text(410,112,470,26,boss.DisplayName+"   PHASE "+boss.BossPhase+"/3",small);
     BoxOutline(new Rect(410,144,460,14),new Color(.25f,.12f,.15f),new Color(.6f,.25f,.3f),1f);
     Box(new Rect(411,145,458*Mathf.Clamp01(boss.Health/boss.MaxHealth),12),Accent);
    }

    // Boss title card: fades in, holds, fades out on guardian levels.
    if(Time.unscaledTime<bossIntroUntil){
     float remain=bossIntroUntil-Time.unscaledTime,total=4.4f,age=total-remain;
     float alpha=age<.5f?age/.5f:remain<1f?remain/1f:1f;
     var intro=Enemies.Find(x=>x&&x.Boss);
     string introName=intro?intro.DisplayName:"GUARDIAN";
     GUI.color=new Color(1f,1f,1f,alpha*.82f);
     Box(new Rect(0,168,1280,4),Accent);
     Box(new Rect(0,288,1280,4),Accent);
     Box(new Rect(0,172,1280,116),new Color(.01f,.03f,.05f,alpha*.66f));
     GUI.color=new Color(1f,1f,1f,alpha);
     Text(140,188,1000,40,"✦  GUARDIAN OF THE "+Realms[Realm]+"  ✦",tinyC);
     GUI.Label(new Rect(90,216,1100,58),introName,titleC);
     Text(140,276,1000,20,"Break the corruption. Dodge the red warnings.",tinyC);
     GUI.color=Color.white;
    }

    if(Time.unscaledTime<noticeUntil){
     Panel(260,192,760,62);
     Text(282,204,716,48,Notice,small);
    }

    MiniMap();

    // Off-screen danger arrows: pulsing diamonds on the screen edge point at
    // telegraphing enemies outside the view, so nothing hits from off-screen
    // without warning.
    if(Camera.main){
     foreach(var foe in Enemies){
      if(!foe||foe.Health<=0||!foe.Telegraphing)continue;
      if(Vector3.Distance(Player.transform.position,foe.transform.position)>34)continue;
      Vector3 v=Camera.main.WorldToViewportPoint(foe.transform.position+Vector3.up*1.2f);
      bool behind=v.z<0f;
      float vx=behind?-v.x:v.x;
      if(!behind&&v.x>.04f&&v.x<.96f&&v.y>.06f&&v.y<.94f)continue;
      float sx=Mathf.Clamp(vx,.05f,.95f)*1280f;
      float sy=(1f-Mathf.Clamp(v.y,.07f,.93f))*720f;
      float ang=Mathf.Atan2(sy-360f,sx-640f)*Mathf.Rad2Deg+45f;
      float pulse=.5f+.45f*Mathf.Sin(Time.unscaledTime*9f);
      Color warn=new Color(1f,.28f,.2f,pulse);
      var matrix=GUI.matrix;
      GUIUtility.RotateAroundPivot(ang,new Vector2(sx,sy));
      Box(new Rect(sx-11,sy-11,22,22),warn);
      GUI.matrix=matrix;
     }
    }
    if(Trial){
     Panel(28,280,300,112);
     Text(42,290,270,22,Trial.Title,small);
     Text(42,317,270,52,Trial.Status,small);
     if(!Trial.Completed){float fraction=Trial.Active?(float)Trial.Count/Trial.Goal:0f;Box(new Rect(42,378,270,4),new Color(.15f,.23f,.26f));Box(new Rect(42,378,270*fraction,4),Accent);}
    }

    if(Application.isMobilePlatform){
     // Virtual joystick: fixed base ring plus a knob tracking live input.
     float jx=150,jy=612,jr=86;
     GUI.color=new Color(.02f,.05f,.09f,.42f);GUI.DrawTexture(new Rect(jx-jr,jy-jr,jr*2,jr*2),circleFill);
     GUI.color=new Color(Accent.r,Accent.g,Accent.b,.5f);GUI.DrawTexture(new Rect(jx-jr,jy-jr,jr*2,jr*2),circleRing);
     GUI.color=new Color(.06f,.13f,.18f,.9f);GUI.DrawTexture(new Rect(jx-25,jy-25,50,50),circleFill);
     GUI.color=new Color(Accent.r,Accent.g,Accent.b,.95f);
     GUI.DrawTexture(new Rect(jx+touch.Move.x*50-19,jy-touch.Move.y*50-19,38,38),circleFill);
     GUI.color=Color.white;
     Text(jx-60,jy+jr+6,120,18,"MOVE",tinyC);
     // Action cluster: big attack button, movement row along the bottom,
     // cast/defend actions above it (rects owned by TouchRouter.ActionRect).
     RoundButton(TouchRouter.ActionRect(2),"BLADE","HOLD TO CHARGE",true);
     RoundButton(TouchRouter.ActionRect(3),"JUMP","×2");
     RoundButton(TouchRouter.ActionRect(0),"DODGE","");
     RoundButton(TouchRouter.ActionRect(1),"POWER","ELEMENT");
     RoundButton(TouchRouter.ActionRect(4),"PARRY","");
     RoundButton(TouchRouter.ActionRect(5),"SPELL","HOLD TO LOB");
    }else Text(28,675,1200,28,"WASD Move   SPACE Double jump   SHIFT Dash / air-dash   G Grapple   J Blade (hold charge)   K Power   L Parry   F Spell (hold=lob)   Q Element",small);
    return;
   }

   // Backdrop Dimmer
   Box(new Rect(0,0,1280,720),new Color(.015f,.03f,.045f,.75f));

   if(Screen==GameScreen.Menu){
    // Centered hero composition: eyebrow, big title, divider, tagline,
    // journey status, then a single stacked column of actions.
    Text(240,84,800,26,"✦  A  3 D  A D V E N T U R E  ✦",tinyC);
    GUI.Label(new Rect(140,118,1000,120),"LEGENDS OF THE LOST REALMS",titleC);
    Box(new Rect(440,252,400,2),new Color(Accent.r,Accent.g,Accent.b,.55f));
    Box(new Rect(628,249,24,8),Accent);
    Text(290,270,700,56,"Four realms. One lost heart.\nCross the floating isles, master elemental blades, restore the ancient portals.",smallC);
    Text(290,340,700,22,$"JOURNEY  {Save.unlocked}/15 CHAPTERS      ✦      {TotalStars()}/45 STARS      ✦      {Save.coins} GOLD      ✦      {Save.gems} GEMS",tinyC);
    if(Button(410,382,460,64,"►   CONTINUE JOURNEY"))LoadLevel(Save.unlocked);
    if(Button(450,458,380,54,"REALM ATLAS"))Screen=GameScreen.Map;
    if(Button(450,522,380,54,"SANCTUARY"))Screen=GameScreen.Settings;
    if(Button(450,586,380,54,"✦ NEW JOURNEY")){Save=new Progress();Save.equippedWeapon=-1;Persist();Sound("upgrade");LoadLevel(1);}
    Text(240,672,800,22,"UNITY 3D EDITION   *   TOUCH + KEYBOARD   *   JOURNEY AUTOSAVES",tinyC);
    return;
   }

   if(Screen==GameScreen.Map){
    Panel(50,45,1180,630);
    Text(80,65,1080,56,"Realm Atlas",title);
    Text(80,130,1050,34,$"Treasury: {Save.coins} Gold   *   {Save.gems} Gems   *   {TotalStars()}/45 Stars");
    Box(new Rect(80,170,1120,2),new Color(Accent.r,Accent.g,Accent.b,.35f));
    bool prevEnabled=GUI.enabled;
    for(int i=0;i<15;i++){
     float x=80+(i%5)*228,y=180+(i/5)*140;
     bool unlocked=i+1<=Save.unlocked;
     GUI.enabled=unlocked;
     string starStr=unlocked?(Save.stars[i]>0?new string('★',Save.stars[i]):"---"):"LOCKED";
     bool go=Button(x,y,212,132,$"CHAPTER {i+1:00}\n\n{Titles[i]}\n\n{starStr}");
     GUI.enabled=prevEnabled;
     if(go)LoadLevel(i+1);
    }
    GUI.enabled=prevEnabled;
    if(Button(80,610,200,56,"◄ BACK"))Screen=GameScreen.Menu;
    return;
   }

   if(Screen==GameScreen.Settings){
    Panel(200,55,880,620);
    Text(240,78,800,58,"Sanctuary & Blessings",title);
    Box(new Rect(240,145,800,2),new Color(Accent.r,Accent.g,Accent.b,.35f));
    if(Button(240,165,385,58,"MUSIC: "+(Save.music?"ENABLED":"MUTED"))){
     Save.music=!Save.music;Persist();
    }
    if(Button(655,165,385,58,"SOUND EFFECTS: "+(Save.sound?"ENABLED":"MUTED"))){
     Save.sound=!Save.sound;Persist();
    }
    Text(240,233,800,28,$"Available Resources:  {Save.coins} Gold   *   {Save.gems} Gems");
    if(Button(240,266,800,52,$"VITALITY RANK {Save.healthRank}/3  (Max HP +{Save.healthRank})   -   Cost: {(Save.healthRank<3?(50+Save.healthRank*40).ToString()+" Gold":"MAXED")}")){
     int cost=50+Save.healthRank*40;
     if(Save.healthRank<3){if(Save.coins>=cost){Save.coins-=cost;Save.healthRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(Button(240,326,520,52,$"ARSENAL RANK {Save.arsenalRank}/3  •  DAMAGE +{Save.arsenalRank*8}%\n{(Save.arsenalRank<3?(80+Save.arsenalRank*60).ToString()+" Gold":"MAXED")}")){
     int cost=80+Save.arsenalRank*60;
     if(Save.arsenalRank<3){if(Save.coins>=cost){Save.coins-=cost;Save.arsenalRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(Button(770,326,270,52,$"⚔ ARSENAL ({Save.weapons.Count})")){showArsenal=true;arsenalPage=0;arsenalReturn=GameScreen.Settings;Sound("power_select");}
    if(showArsenal){ArsenalView();return;}
    if(Button(240,386,800,52,$"ELEMENTAL POWER RANK {Save.powerRank}/3  (Spell/Power +{Save.powerRank*20}%)   -   Cost: {(Save.powerRank<3?(3+Save.powerRank*2).ToString()+" Gems":"MAXED")}")){
     int cost=3+Save.powerRank*2;
     if(Save.powerRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.powerRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(Button(240,446,385,52,$"AETHER RANK {Save.aetherRank}/3\nENERGY REGEN +{Save.aetherRank*25}%   •   Cost: {(Save.aetherRank<3?(4+Save.aetherRank*2).ToString()+" Gems":"MAXED")}")){
     int cost=4+Save.aetherRank*2;
     if(Save.aetherRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.aetherRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(Button(655,446,385,52,$"MOXIE RANK {Save.moxieRank}/3\n+{Save.moxieRank} AIR DASH CHARGE   •   Cost: {(Save.moxieRank<3?(5+Save.moxieRank*2).ToString()+" Gems":"MAXED")}")){
     int cost=5+Save.moxieRank*2;
     if(Save.moxieRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.moxieRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(Button(240,506,385,52,$"TEMPO RANK {Save.tempoRank}/3\nCOUNTER +{Save.tempoRank*15}%  PARRY +{Save.tempoRank*5} EN   •   Cost: {(Save.tempoRank<3?(5+Save.tempoRank*2).ToString()+" Gems":"MAXED")}")){
     int cost=5+Save.tempoRank*2;
     if(Save.tempoRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.tempoRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(Button(655,506,385,52,$"SECOND WIND RANK {Save.windRank}/3\n{Save.windRank} FREE REVIVE/LEVEL   •   Cost: {(Save.windRank<3?(7+Save.windRank*3).ToString()+" Gems":"MAXED")}")){
     int cost=7+Save.windRank*3;
     if(Save.windRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.windRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(Button(240,566,385,46,"VISUAL FX: "+(Save.postFx?"ENHANCED":"OFF"))){Save.postFx=!Save.postFx;Persist();}
    if(Button(655,566,385,46,"SCREEN SHAKE: "+(Save.shake?"ENABLED":"OFF"))){Save.shake=!Save.shake;Persist();}
     if(Button(240,624,385,46,"HAPTICS: "+(Save.haptics?"ON":"OFF"))){Save.haptics=!Save.haptics;Persist();}
     if(Button(655,624,385,46,"◄ BACK"))Screen=GameScreen.Menu;
     return;
    }
   // Arsenal: every collected weapon, persisted. Equip with a tap, unequip
   // back to bare fists. Pickup drops auto-equip; this is where you switch.
   // Opened from Sanctuary (returns to tracks) or the pause menu (returns
   // to pause) so loadouts can change mid-run while safely frozen.
   Texture2D WeaponIcon(WeaponDefinition def){
    if(string.IsNullOrEmpty(def.Resource))return null;
    string key=def.Resource.Substring(def.Resource.LastIndexOf('/')+1);
    if(iconCache.TryGetValue(key,out var tex))return tex;
    tex=Resources.Load<Texture2D>("Weapons/Icons/"+key);
    iconCache[key]=tex;return tex;
   }
   void ArsenalView(){
    Panel(200,55,880,620);
    Text(240,78,800,58,$"Arsenal  —  {Save.weapons.Count} Collected",title);
    Box(new Rect(240,145,800,2),new Color(Accent.r,Accent.g,Accent.b,.35f));
    bool fists=Save.equippedWeapon<0;
    if(Button(240,156,800,44,fists?"✓ BARE FISTS EQUIPPED":"◄ UNEQUIP  —  fight bare-fisted")){
     if(!fists)UnequipWeapon();else Sound("power_select");
    }
    var list=Save.weapons;
    int pages=Mathf.Max(1,(list.Count+6)/7);
    arsenalPage=Mathf.Clamp(arsenalPage,0,pages-1);
    if(list.Count==0)Text(240,260,800,60,"No weapons collected yet.\nBlades you find in the chapters will wait for you here.",small);
    for(int k=0;k<7;k++){
     int idx=arsenalPage*7+k;if(idx>=list.Count)break;
     var def=WeaponCatalog.Get(list[idx]);
     bool eq=Save.equippedWeapon==list[idx];
     if(Button(240,212+k*56,800,48,"")){
      if(eq)UnequipWeapon();else EquipWeapon((WeaponId)list[idx]);
     }
     Text(302,214+k*56,550,23,def.Name,small);
     Text(302,237+k*56,565,20,$"Damage ×{def.Damage:0.00}   Reach ×{def.Reach:0.00}   Speed ×{def.Tempo:0.00}",small);
     Text(865,222+k*56,164,24,eq?"✓ EQUIPPED":"EQUIP",smallC);
     var icon=WeaponIcon(def);
     if(icon)GUI.DrawTexture(new Rect(248,214+k*56,44,44),icon,ScaleMode.ScaleToFit);else Text(248,222+k*56,44,24,"⚔",smallC);
    }
    if(pages>1){
     if(arsenalPage>0&&Button(240,610,180,46,"◄ PREV")){arsenalPage--;Sound("power_select");}
     Text(430,610,220,46,$"PAGE {arsenalPage+1}/{pages}",smallC);
     if(arsenalPage<pages-1&&Button(660,610,180,46,"NEXT ►")){arsenalPage++;Sound("power_select");}
    }
    if(arsenalReturn==GameScreen.Paused){
     if(Button(860,610,180,46,"◄ PAUSE")){showArsenal=false;Sound("power_select");}
    }else if(Button(860,610,180,46,"◄ TRACKS")){showArsenal=false;Sound("power_select");}
   }

   // Pause / Complete / Defeat Screens
   Panel(320,120,640,485);
   string heading=Screen==GameScreen.Paused?"A Moment of Rest":Screen==GameScreen.Complete?(Level==15?"The Lost Realms Restored!":"Chapter Cleared!"):"The Light Remains";
   Text(355,150,570,95,heading,title);
   Box(new Rect(355,255,570,2),new Color(Accent.r,Accent.g,Accent.b,.4f));
   if(Screen==GameScreen.Complete){
    string starDisplay=new string('★',EarnedStars);
    Text(355,275,570,80,$"TRIUMPH!  {starDisplay}   Elapsed: {Clock(Elapsed)}\nRewards: +{Coins+EarnedStars*10} Gold   +{Gems} Gems");
    if(Button(355,385,570,60,Level==15?"RETURN TO REALM ATLAS":"NEXT CHAPTER")){
     if(Level==15)Screen=GameScreen.Map;else LoadLevel(Level+1);
    }
    }else if(Screen==GameScreen.Paused){
     Text(355,280,570,60,"Your journey is paused. All progress is safe.");
     if(Button(355,360,570,56,"RESUME JOURNEY"))Resume();
     if(Button(355,424,570,44,"⚔ ARSENAL  —  change weapon")){showArsenal=true;arsenalPage=0;arsenalReturn=GameScreen.Paused;Sound("power_select");}
    }else{
    Text(355,280,570,65,"Rise again at your last shrine checkpoint.");
    if(Button(355,375,570,60,"TRY AGAIN")){Respawn();Resume();}
   }
    if(showArsenal&&Screen==GameScreen.Paused){ArsenalView();return;}
    if(Button(355,475,270,58,"🗺 ATLAS"))Screen=GameScreen.Map;
   if(Button(645,475,280,58,"↺ RESTART"))LoadLevel(Level);
  }
  // Top-down realm map: route trail, gate, enemies and the heading player.
   // Cheap IMGUI squares only, so it stays in-sync with the touch layout (the
   // panel never overlaps the action buttons or the compass).
   void MiniMap(){
    if(!World||World.Route.Count<2)return;
    Rect rect=new Rect(1032,78,228,150);
    Panel(1028,74,236,158);
    Text(1044,86,180,16,"REALM MAP",small);
    float minX=float.MaxValue,maxX=float.MinValue,minZ=float.MaxValue,maxZ=float.MinValue;
    foreach(var n in World.Route){minX=Mathf.Min(minX,n.x);maxX=Mathf.Max(maxX,n.x);minZ=Mathf.Min(minZ,n.z);maxZ=Mathf.Max(maxZ,n.z);}
    minX-=4;maxX+=4;minZ-=4;maxZ+=4;
    float spanX=Mathf.Max(1f,maxX-minX),spanZ=Mathf.Max(1f,maxZ-minZ);
    float scale=Mathf.Min(rect.width/spanX,rect.height/spanZ);
    float ox=rect.x+rect.width*.5f,oz=rect.y+rect.height*.5f;
    Vector2 ToPad(Vector3 w){return new Vector2(ox+(w.x-(minX+maxX)*.5f)*scale,oz+(w.z-(minZ+maxZ)*.5f)*scale);}
    for(int i=0;i<World.Route.Count;i++){Vector2 p=ToPad(World.Route[i]);Box(new Rect(p.x-2.5f,p.y-2.5f,5,5),new Color(1f,.8f,.3f,.9f));}
    Vector3 gatePos=World.Route[World.Route.Count-1];gatePos.z+=5f;
    Vector2 gp=ToPad(gatePos);Box(new Rect(gp.x-4,gp.y-4,8,8),new Color(.95f,.95f,1f));
    Text(gp.x-16,gp.y+8,34,16,"GATE",small);
    foreach(var foe in Enemies){if(!foe||foe.Health<=0||Vector3.Distance(foe.transform.position,Player.transform.position)>60f)continue;Vector2 ep=ToPad(foe.transform.position);Box(new Rect(ep.x-2,ep.y-2,4,4),foe.Boss?new Color(1,.3f,.3f):new Color(1,.5f,.5f,.9f));}
    Vector2 pp=ToPad(Player.transform.position);Box(new Rect(pp.x-4,pp.y-4,8,8),new Color(.35f,1f,.5f));
    Vector3 fwd=Player.transform.forward;fwd.y=0;if(fwd.sqrMagnitude>.01f){Vector3 tip=Player.transform.position+fwd.normalized*2.2f;Vector2 tp=ToPad(tip);Box(new Rect(tp.x-1.5f,tp.y-1.5f,3,3),Color.white);}
   }
   // Circular action button drawn over the TouchRouter.ActionRect hitbox.
   void RoundButton(Rect r,string name,string sub,bool primary=false){
    GUI.color=new Color(Accent.r,Accent.g,Accent.b,primary?.95f:.7f);
    GUI.DrawTexture(r,circleRing);
    GUI.color=new Color(.02f,.05f,.09f,primary?.62f:.5f);
    GUI.DrawTexture(r,circleFill);
    if(primary){
     GUI.color=new Color(Accent.r,Accent.g,Accent.b,.28f);
     GUI.DrawTexture(new Rect(r.x+r.width*.08f,r.y+r.height*.08f,r.width*.84f,r.height*.84f),circleRing);
    }
    GUI.color=Color.white;
    GUI.Label(new Rect(r.x,r.y+r.height*(primary?.16f:.26f),r.width,r.height*.38f),name,primary?big:center);
    if(sub.Length>0)GUI.Label(new Rect(r.x,r.y+r.height*(primary?.58f:.6f),r.width,r.height*.26f),sub,tinyC);
   }
  int TotalStars(){int n=0;foreach(int s in Save.stars)n+=s;return n;}
 }
}
