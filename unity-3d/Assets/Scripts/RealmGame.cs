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
  public int Level=1, Realm, Coins, Gems, DamageTaken, EarnedStars, Combo, Kills, CoinsTotal, GemsTotal, KillsTotal; public float Elapsed; public Vector3 Checkpoint; public bool CheckpointActive; public string Notice=""; float noticeUntil,comboUntil; public Color Accent=>Accents[0];
  public WeaponDefinition CurrentWeapon=>WeaponCatalog.Get(Save.equippedWeapon);
  public Vector2 MoveInput; public bool JumpPressed,DashPressed,AttackPressed,AttackReleased,CastPressed,ParryPressed,SpellPressed,SpellReleased,GrapplePressed; public bool AttackHeld,SpellHeld,JumpHeld;
  public readonly List<Enemy> Enemies=new List<Enemy>(); public AudioSource Music,Sfx;
  public RealmAudio Audio {get;private set;} public RealmTrials Trial {get;private set;}
   Transform worldRoot; GUIStyle title,titleC,label,small,button,center,big,smallC,tinyC,btnText; Texture2D pixel,circleFill,circleRing,btnBlade,btnJump,btnDodge,btnPower,btnParry,btnSpell,btnPause,joyBase,joyKnob; Texture2D bgMainMenu,avatarAster,btnPrimaryNorm,btnPrimaryHigh,btnStdNorm,btnStdHigh,btnSecNorm,btnSecHigh,panelLarge,panelMedium,cardUnlocked,cardSelected,cardLocked,cardCompleted,headerOrnament,dividerLine,barFrame,barFill,resourceCapsule,iconArsenal,iconAtlas,iconBack,iconClose,iconCoin,iconGem,iconLock,iconMainMenu,iconNewJourney,iconContinue,iconResume,iconRestart,iconSanctuary,iconStar; readonly TouchRouter touch=new TouchRouter(); float yawInput,hitStopUntil,bossIntroUntil;bool showArsenal;int arsenalPage;GameScreen arsenalReturn=GameScreen.Settings;readonly System.Collections.Generic.Dictionary<string,Texture2D> iconCache=new System.Collections.Generic.Dictionary<string,Texture2D>();
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
   Time.timeScale=1; touch.Reset(); if(worldRoot){worldRoot.gameObject.SetActive(false);Destroy(worldRoot.gameObject);} Enemies.Clear(); Level=Mathf.Clamp(id,1,15); Realm=Level<=4?0:Level<=7?1:Level<=10?2:3; Coins=Gems=DamageTaken=EarnedStars=0; Kills=CoinsTotal=GemsTotal=KillsTotal=0; Elapsed=0; CheckpointActive=false;Combo=0;comboUntil=0;
   worldRoot=new GameObject("Realm "+Level+" - "+Titles[Level-1]).transform; World=worldRoot.gameObject.AddComponent<RealmWorld>();   try{World.Build(Level,Realm);}catch(System.Exception e){Debug.LogError("LEVEL_BUILD_FAILED "+Level+": "+e);}
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
   if(Input.touchCount>0){
    foreach(var t in Input.touches)touch.Sample(t.fingerId,new Vector2(t.position.x*sx,(UnityEngine.Screen.height-t.position.y)*sy),t.deltaPosition*sx,t.phase);
   }else if(Application.isEditor){
    Vector2 mp=new Vector2(Input.mousePosition.x*sx,(UnityEngine.Screen.height-Input.mousePosition.y)*sy);
    bool inControl=mp.x<420&&mp.y>380;
    if(!inControl){for(int i=0;i<6;i++)if(TouchRouter.ActionRect(i).Contains(mp)){inControl=true;break;}}
    if(inControl){
     if(Input.GetMouseButtonDown(0))touch.Sample(-1,mp,Vector2.zero,TouchPhase.Began);
     else if(Input.GetMouseButton(0))touch.Sample(-1,mp,Vector2.zero,TouchPhase.Moved);
     else if(Input.GetMouseButtonUp(0))touch.Sample(-1,mp,Vector2.zero,TouchPhase.Ended);
     else touch.Reset();
    }else touch.Reset();
   }else touch.Reset();
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
   // Chapter completion: average of coins/gems/foes percentages (each
   // clamped; empty categories count as complete). Stars: 60/80/95%.
   public static float CompletionFor(int coins,int coinsTotal,int gems,int gemsTotal,int kills,int killsTotal){
    float c=coinsTotal>0?Mathf.Clamp01((float)coins/coinsTotal):1f;
    float g=gemsTotal>0?Mathf.Clamp01((float)gems/gemsTotal):1f;
    float k=killsTotal>0?Mathf.Clamp01((float)kills/killsTotal):1f;
    return (c+g+k)/3f;
   }
   public static int StarsFor(float completion)=>completion>=.95f?3:completion>=.8f?2:completion>=.6f?1:0;
   public float Completion()=>CompletionFor(Coins,CoinsTotal,Gems,GemsTotal,Kills,KillsTotal);
   public int GateStars()=>StarsFor(Completion());
   public bool GateOpen()=>Completion()>=.6f;
   public string GateSealedText(){
    float c=CoinsTotal>0?(float)Coins/CoinsTotal:1f,g=GemsTotal>0?(float)Gems/GemsTotal:1f,k=KillsTotal>0?(float)Kills/KillsTotal:1f;
    return $"Realm gate sealed — completion {(int)(Completion()*100f)}% (need 60%): coins {(int)(Mathf.Clamp01(c)*100f)}% • gems {(int)(Mathf.Clamp01(g)*100f)}% • foes {(int)(Mathf.Clamp01(k)*100f)}%";
   }
   public void EquipWeapon(WeaponId id){
    if((int)id<0||(int)id>40)return;
    if(Save.weapons==null)Save.weapons=new List<int>();
    if((int)id>=0&&!Save.weapons.Contains((int)id))Save.weapons.Add((int)id);
    Save.equippedWeapon=(int)id;Persist();
    if(Player){EquippedWeapon.Equip(Player,id);}
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
    // Anti-skip: the gate only opens at 60%+ completion (coins/gems/foes).
    if(!GateOpen()){Tell(GateSealedText(),4f);Sound("power_fail");return;}
    EarnedStars=GateStars(); Save.stars[Level-1]=Mathf.Max(Save.stars[Level-1],EarnedStars); if(Save.best[Level-1]<=0||Elapsed<Save.best[Level-1])Save.best[Level-1]=Elapsed;
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
  Texture2D LoadUITex(string subfolder, string name){
   string path = string.IsNullOrEmpty(subfolder) ? "UI/" + name : "UI/" + subfolder + "/" + name;
   var tex = Resources.Load<Texture2D>(path);
   if (!tex){
    var sp = Resources.Load<Sprite>(path);
    if (sp) tex = sp.texture;
   }
#if UNITY_EDITOR
   if (!tex){
    string diskPath = System.IO.Path.Combine(Application.dataPath, "Resources/" + path + ".png");
    if (System.IO.File.Exists(diskPath)){
     byte[] bytes = System.IO.File.ReadAllBytes(diskPath);
     tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
     tex.LoadImage(bytes);
    }
   }
#endif
   return tex;
  }
  Texture2D LoadButtonTex(string name) => LoadUITex("Buttons", name);
  void LoadUITextures(){
   if(!btnBlade)btnBlade=LoadButtonTex("UI_Action_Blade_Attack_HoldToCharge");
   if(!btnJump)btnJump=LoadButtonTex("UI_Action_Jump_DoubleJump_x2");
   if(!btnDodge)btnDodge=LoadButtonTex("UI_Action_Dodge_Evade");
   if(!btnPower)btnPower=LoadButtonTex("UI_Action_ElementalPower");
   if(!btnParry)btnParry=LoadButtonTex("UI_Action_Parry_Defense");
   if(!btnSpell)btnSpell=LoadButtonTex("UI_Action_Spell_LockOn_HoldToLock");
   if(!btnPause)btnPause=LoadButtonTex("UI_Menu_Pause");
   if(!joyBase)joyBase=LoadButtonTex("UI_Move_Joystick_OuterBase");
   if(!joyKnob)joyKnob=LoadButtonTex("UI_Move_Joystick_InnerAnalog");
   if(!bgMainMenu)bgMainMenu=LoadUITex("Backgrounds","BG_MainMenu_Fullscreen_Landscape_Primary");
   if(!avatarAster)avatarAster=LoadUITex("Portraits","UI_Avatar_Aster_HP_Headshot");
   if(!btnPrimaryNorm)btnPrimaryNorm=LoadUITex("Buttons","UI_Button_Primary_Normal");
   if(!btnPrimaryHigh)btnPrimaryHigh=LoadUITex("Buttons","UI_Button_Primary_Highlighted");
   if(!btnStdNorm)btnStdNorm=LoadUITex("Buttons","UI_Button_Standard_Normal");
   if(!btnStdHigh)btnStdHigh=LoadUITex("Buttons","UI_Button_Standard_Highlighted");
   if(!btnSecNorm)btnSecNorm=LoadUITex("Buttons","UI_Button_Secondary_Small_Normal");
   if(!btnSecHigh)btnSecHigh=LoadUITex("Buttons","UI_Button_Secondary_Small_Highlighted");
   if(!panelLarge)panelLarge=LoadUITex("Frames","UI_Panel_Large_MenuFrame");
   if(!panelMedium)panelMedium=LoadUITex("Frames","UI_Panel_Medium_DialogFrame");
   if(!cardUnlocked)cardUnlocked=LoadUITex("Frames","UI_ChapterCard_Unlocked");
   if(!cardSelected)cardSelected=LoadUITex("Frames","UI_ChapterCard_Selected");
   if(!cardLocked)cardLocked=LoadUITex("Frames","UI_ChapterCard_Locked");
   if(!cardCompleted)cardCompleted=LoadUITex("Frames","UI_ChapterCard_Completed");
   if(!headerOrnament)headerOrnament=LoadUITex("Frames","UI_Header_Ornament");
   if(!dividerLine)dividerLine=LoadUITex("Frames","UI_Divider_Line");
   if(!barFrame)barFrame=LoadUITex("Frames","UI_ProgressBar_Frame");
   if(!barFill)barFill=LoadUITex("Frames","UI_ProgressBar_Fill");
   if(!resourceCapsule)resourceCapsule=LoadUITex("Frames","UI_Resource_Capsule");
   if(!iconArsenal)iconArsenal=LoadUITex("Icons","UI_Icon_Arsenal");
   if(!iconAtlas)iconAtlas=LoadUITex("Icons","UI_Icon_Atlas");
   if(!iconBack)iconBack=LoadUITex("Icons","UI_Icon_Back");
   if(!iconClose)iconClose=LoadUITex("Icons","UI_Icon_Close");
   if(!iconCoin)iconCoin=LoadUITex("Icons","UI_Icon_Coin");
   if(!iconGem)iconGem=LoadUITex("Icons","UI_Icon_Gem");
   if(!iconLock)iconLock=LoadUITex("Icons","UI_Icon_Lock");
   if(!iconMainMenu)iconMainMenu=LoadUITex("Icons","UI_Icon_MainMenu");
   if(!iconNewJourney)iconNewJourney=LoadUITex("Icons","UI_Icon_NewJourney");
   if(!iconContinue)iconContinue=LoadUITex("Icons","UI_Icon_Play_Continue");
   if(!iconResume)iconResume=LoadUITex("Icons","UI_Icon_Play_Resume");
   if(!iconRestart)iconRestart=LoadUITex("Icons","UI_Icon_Restart");
   if(!iconSanctuary)iconSanctuary=LoadUITex("Icons","UI_Icon_Sanctuary");
   if(!iconStar)iconStar=LoadUITex("Icons","UI_Icon_Star");
  }
  void LoadButtonTextures() => LoadUITextures();
  void Styles(){
   LoadUITextures();
   if(title!=null)return;pixel=Texture2D.whiteTexture;
   title=new GUIStyle(GUI.skin.label){fontSize=44,fontStyle=FontStyle.Bold,wordWrap=true,alignment=TextAnchor.UpperLeft};
   title.normal.textColor=new Color(1f,.94f,.82f);
   titleC=new GUIStyle(title){alignment=TextAnchor.MiddleCenter,fontSize=50,wordWrap=false};
   label=new GUIStyle(GUI.skin.label){fontSize=22,wordWrap=true};label.normal.textColor=new Color(.88f,.95f,.96f);
   small=new GUIStyle(label){fontSize=16};
   button=new GUIStyle(GUI.skin.button){fontSize=20,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
   button.normal.textColor=new Color(1f,.96f,.88f);button.padding=new RectOffset(12,12,10,10);
   // Strip the skin's button chrome so the hand-drawn frames below show through.
   button.normal.background=button.hover.background=button.active.background=button.focused.background=null;
   center=new GUIStyle(GUI.skin.label){fontSize=17,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true};
   center.normal.textColor=new Color(.94f,.98f,1f);
   btnText=new GUIStyle(center){fontSize=19};
   btnText.normal.textColor=new Color(1f,.96f,.88f);
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
  void Text(float x,float y,float w,float h,string s,GUIStyle st=null){GUI.Label(new Rect(x,y,w,h),s,st??label);
  }
  bool MenuButton(Rect r,string text,Texture2D icon=null,bool primary=false,bool smallBtn=false,GUIStyle st=null){
   Vector2 mpos=Event.current.mousePosition;
   bool hover=r.Contains(mpos);
   Texture2D normTex=primary?btnPrimaryNorm:(smallBtn?btnSecNorm:btnStdNorm);
   Texture2D highTex=primary?btnPrimaryHigh:(smallBtn?btnSecHigh:btnStdHigh);
   Texture2D tex=hover?(highTex??normTex):normTex;
   GUIStyle style=st??(primary?big:(smallBtn?smallC:btnText));

   if(tex!=null){
    GUI.color=hover?new Color(1.15f,1.15f,1.15f,1f):Color.white;
    GUI.DrawTexture(r,tex,ScaleMode.StretchToFill);
    GUI.color=Color.white;
    if(icon!=null){
     float isz=Mathf.Min(r.height-14,30);
     float iy=r.y+(r.height-isz)*0.5f;
     GUI.DrawTexture(new Rect(r.x+14,iy,isz,isz),icon,ScaleMode.ScaleToFit);
    }
    Rect textRect=icon!=null?new Rect(r.x+36,r.y,r.width-48,r.height):r;
    GUI.Label(textRect,text,style);
    return GUI.Button(r,GUIContent.none,GUIStyle.none);
   }else{
    Color bg=hover?new Color(.12f,.22f,.28f,.98f):new Color(.05f,.11f,.16f,.94f);
    Color border=hover?Color.Lerp(Accent,Color.white,.4f):Accent*.85f;
    BoxOutline(r,bg,border,hover?2.5f:1.8f);
    if(icon!=null){
     float isz=Mathf.Min(r.height-14,28);
     GUI.DrawTexture(new Rect(r.x+14,r.y+(r.height-isz)*0.5f,isz,isz),icon,ScaleMode.ScaleToFit);
    }
    Rect textRect=icon!=null?new Rect(r.x+36,r.y,r.width-48,r.height):r;
    GUI.Label(textRect,text,style);
    return GUI.Button(r,GUIContent.none,GUIStyle.none);
   }
  }
  bool Button(float x,float y,float w,float h,string s){
   return MenuButton(new Rect(x,y,w,h),s);
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
   LoadButtonTextures();
   // Only the in-game HUD needs a live Player; menus must draw and respond even
   // if startup level construction ever fails.
   if(Screen==GameScreen.Playing&&!Player)return;
   Styles();
   GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(UnityEngine.Screen.width/1280f,UnityEngine.Screen.height/720f,1));
   if(showArsenal&&(Screen==GameScreen.Settings||Screen==GameScreen.Paused)){ArsenalView();return;}
   if(Screen==GameScreen.Playing){
    // Health & Realm Banner
    Panel(24,20,380,146);
    if(avatarAster){
     Box(new Rect(32,28,74,74),new Color(.02f,.06f,.09f,.92f));
     GUI.DrawTexture(new Rect(33,29,72,72),avatarAster,ScaleMode.ScaleToFit);
     BoxOutline(new Rect(32,28,74,74),Color.clear,Accent*.85f,1.5f);
    }
    float textX=avatarAster?114:42;
    Text(textX,28,320,22,$"REALM {Realm+1}  /  {Realms[Realm]}",small);
    Text(textX,50,320,30,Titles[Level-1]);

    float targetH=Player?Mathf.Clamp01((float)Player.Health/Player.MaxHealth):1f;
    healthLag=Mathf.Lerp(healthLag,targetH,Time.unscaledDeltaTime*3.5f);
    if(barFrame&&barFill){
     Rect fr=new Rect(textX,78,200,24);
     GUI.DrawTexture(fr,barFrame,ScaleMode.StretchToFill);
     float fx=textX+16f,fy=81f,fw=168f,fh=18f;
     if(healthLag>targetH){
      GUI.color=new Color(1f,.7f,.3f,.8f);
      GUI.DrawTexture(new Rect(fx,fy,fw*healthLag,fh),barFill,ScaleMode.StretchToFill);
     }
     GUI.color=new Color(.95f,.18f,.18f);
     GUI.DrawTexture(new Rect(fx,fy,fw*targetH,fh),barFill,ScaleMode.StretchToFill);
     GUI.color=Color.white;
    }else{
     BoxOutline(new Rect(textX,80,200,20),new Color(.12f,.18f,.22f),new Color(.28f,.38f,.45f),1f);
     if(healthLag>targetH)Box(new Rect(textX+1,81,198*healthLag,18),new Color(1f,.65f,.35f,.75f));
     Box(new Rect(textX+1,81,198*targetH,18),new Color(.95f,.2f,.2f));
    }
    Text(textX+208,78,80,24,$"HP {(Player?Player.Health:8)}/{(Player?Player.MaxHealth:8)}",tinyC);
    Text(textX,106,280,20,$"WEAPON  {CurrentWeapon.Name}",small);
    if(Player&&Player.CounterReady)Text(textX,124,280,18,"COUNTER READY  /  STRIKE NOW",tinyC);
    if(Combo>=3){BoxOutline(new Rect(textX,146,140,24),new Color(.05f,.09f,.12f,.85f),new Color(1f,.78f,.3f),1f);Text(textX+10,149,122,18,$"COMBO  x{Combo}",small);}

    // Collectibles + gate progress panel
    Panel(418,20,230,94);
    Text(432,28,205,18,"TRAIL FINDINGS",small);
    if(iconCoin&&iconGem){
     GUI.DrawTexture(new Rect(432,48,22,22),iconCoin,ScaleMode.ScaleToFit);
     Text(458,47,55,26,$"{Coins:00}",label);
     GUI.DrawTexture(new Rect(520,48,20,22),iconGem,ScaleMode.ScaleToFit);
     Text(544,47,55,26,$"{Gems}",label);
    }else{
     Text(432,48,205,30,$"◉ {Coins:00}    ◆ {Gems}");
    }
    Text(432,74,205,20,GateOpen()?"GATE OPEN  "+new string('★',GateStars()):"GATE "+(int)(Completion()*100f)+"%  -  need 60%",small);

    // Power Selector & Energy Bar
    string[] powerNames={"EMBER [FIRE]","FROST [ICE]","GALE [WIND]"};
    Color[] powerCols={new Color(1f,.22f,.18f),new Color(.2f,.85f,1f),new Color(.2f,1f,.55f)};
    if(MenuButton(new Rect(660,20,205,48),powerNames[Player.Power],iconArsenal,smallBtn:true)){Player.Power=(Player.Power+1)%3;Sound("power_select");}
    Text(660,70,205,16,"AETHER",tinyC);
    float enFrac=Mathf.Clamp01(Player.Energy/100f);
    if(barFrame&&barFill){
     Rect afr=new Rect(660,86,205,18);
     GUI.DrawTexture(afr,barFrame,ScaleMode.StretchToFill);
     float afx=660+16f,afy=88f,afw=173f,afh=14f;
     GUI.color=powerCols[Player.Power];
     GUI.DrawTexture(new Rect(afx,afy,afw*enFrac,afh),barFill,ScaleMode.StretchToFill);
     GUI.color=Color.white;
    }else{
     BoxOutline(new Rect(660,86,205,8),new Color(.1f,.15f,.2f),powerCols[Player.Power]*.5f,1f);
     Box(new Rect(661,87,203*enFrac,6),powerCols[Player.Power]);
    }

    // Timer and Pause Button
    Panel(882,20,135,50);Text(898,32,105,28,$"TIME {Clock(Elapsed)}",small);
    if(btnPause){
     Rect pr=new Rect(1035,19,120,52);
     bool hover=pr.Contains(Event.current.mousePosition);
     GUI.color=hover?new Color(1.15f,1.15f,1.15f,1f):Color.white;
     GUI.DrawTexture(pr,btnPause,ScaleMode.ScaleToFit);
     GUI.color=Color.white;
     if(GUI.Button(pr,GUIContent.none,GUIStyle.none))Pause();
    }else if(Button(1035,20,120,50,"PAUSE"))Pause();

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
    // Trial box removed per user request for uncluttered playfield.

    bool showTouchUI=Application.isMobilePlatform||Application.isEditor;
    if(showTouchUI){
     // Virtual joystick: fixed base ring plus a knob tracking live input.
     float jx=150,jy=612,jr=86;
     if(joyBase){
      GUI.color=Color.white;
      GUI.DrawTexture(new Rect(jx-jr,jy-jr,jr*2,jr*2),joyBase);
      float kw=58f;
      if(joyKnob)GUI.DrawTexture(new Rect(jx+touch.Move.x*50-kw*.5f,jy-touch.Move.y*50-kw*.5f,kw,kw),joyKnob);
     }else{
      GUI.color=new Color(.02f,.05f,.09f,.42f);GUI.DrawTexture(new Rect(jx-jr,jy-jr,jr*2,jr*2),circleFill);
      GUI.color=new Color(Accent.r,Accent.g,Accent.b,.5f);GUI.DrawTexture(new Rect(jx-jr,jy-jr,jr*2,jr*2),circleRing);
      GUI.color=new Color(.06f,.13f,.18f,.9f);GUI.DrawTexture(new Rect(jx-25,jy-25,50,50),circleFill);
      GUI.color=new Color(Accent.r,Accent.g,Accent.b,.95f);
      GUI.DrawTexture(new Rect(jx+touch.Move.x*50-19,jy-touch.Move.y*50-19,38,38),circleFill);
      GUI.color=Color.white;
      Text(jx-60,jy+jr+6,120,18,"MOVE",tinyC);
     }
     // Action cluster: big attack button, movement row along the bottom,
     // cast/defend actions above it (rects owned by TouchRouter.ActionRect).
     RoundButton(TouchRouter.ActionRect(2),btnBlade,"BLADE","HOLD TO CHARGE",true,touch.BladeHeld||(Application.isEditor&&Input.GetKey(KeyCode.J)));
     RoundButton(TouchRouter.ActionRect(3),btnJump,"JUMP","×2",false,touch.Jump||(Application.isEditor&&Input.GetKey(KeyCode.Space)));
     RoundButton(TouchRouter.ActionRect(0),btnDodge,"DODGE","",false,touch.Dodge||(Application.isEditor&&Input.GetKey(KeyCode.LeftShift)));
     RoundButton(TouchRouter.ActionRect(1),btnPower,"POWER","ELEMENT",false,touch.Power||(Application.isEditor&&Input.GetKey(KeyCode.K)));
     RoundButton(TouchRouter.ActionRect(4),btnParry,"PARRY","",false,touch.Parry||(Application.isEditor&&Input.GetKey(KeyCode.L)));
     RoundButton(TouchRouter.ActionRect(5),btnSpell,"SPELL","HOLD TO LOB",false,touch.SpellHeld||(Application.isEditor&&Input.GetKey(KeyCode.F)));
    }
    if(!Application.isMobilePlatform)Text(300,685,680,24,"WASD Move   •   SPACE Jump   •   SHIFT Dash   •   J Attack   •   K Power   •   L Parry   •   F Spell",tinyC);
    return;
   }

   // Backdrop Dimmer & Menu Background
   if(bgMainMenu){
    GUI.DrawTexture(new Rect(0,0,1280,720),bgMainMenu,ScaleMode.ScaleAndCrop);
    Box(new Rect(0,0,1280,720),new Color(.015f,.03f,.045f,Screen==GameScreen.Menu?.32f:.78f));
   }else{
    Box(new Rect(0,0,1280,720),new Color(.015f,.03f,.045f,.75f));
   }

   if(Screen==GameScreen.Menu){
    Text(240,48,800,24,"✦  A  3 D  A D V E N T U R E  ✦",tinyC);
    GUI.Label(new Rect(140,74,1000,68),"LEGENDS OF THE LOST REALMS",titleC);
    if(headerOrnament)GUI.DrawTexture(new Rect(440,144,400,26),headerOrnament,ScaleMode.ScaleToFit);
    else{Box(new Rect(440,154,400,2),new Color(Accent.r,Accent.g,Accent.b,.55f));Box(new Rect(628,151,24,8),Accent);}
    Text(240,174,800,44,"Four realms. One lost heart.\nCross the floating isles, master elemental blades, restore the ancient portals.",smallC);

    if(resourceCapsule)GUI.DrawTexture(new Rect(310,224,660,38),resourceCapsule,ScaleMode.StretchToFill);
    Text(310,232,660,22,$"JOURNEY  {Save.unlocked}/15 CHAPTERS    ✦    {TotalStars()}/45 STARS    ✦    {Save.coins} GOLD    ✦    {Save.gems} GEMS",tinyC);

    if(MenuButton(new Rect(410,278,460,66),"CONTINUE JOURNEY",iconContinue,primary:true))LoadLevel(Save.unlocked);
    if(MenuButton(new Rect(440,356,400,56),"REALM ATLAS",iconAtlas))Screen=GameScreen.Map;
    if(MenuButton(new Rect(440,424,400,56),"SANCTUARY",iconSanctuary))Screen=GameScreen.Settings;
    if(MenuButton(new Rect(440,492,400,56),"NEW JOURNEY",iconNewJourney)){Save=new Progress();Save.equippedWeapon=-1;Persist();Sound("upgrade");LoadLevel(1);}

    Text(240,672,800,22,"UNITY 3D EDITION   *   TOUCH + KEYBOARD   *   JOURNEY AUTOSAVES",tinyC);
    return;
   }

   if(Screen==GameScreen.Map){
    Panel(45,35,1190,650);

    Text(80,52,600,48,"Realm Atlas",title);
    if(headerOrnament)GUI.DrawTexture(new Rect(80,100,320,20),headerOrnament,ScaleMode.ScaleToFit);

    if(resourceCapsule)GUI.DrawTexture(new Rect(720,52,480,42),resourceCapsule,ScaleMode.StretchToFill);
    if(iconCoin&&iconGem&&iconStar){
     GUI.DrawTexture(new Rect(740,61,24,24),iconCoin,ScaleMode.ScaleToFit);
     Text(768,62,80,24,$"{Save.coins}",small);
     GUI.DrawTexture(new Rect(860,61,22,24),iconGem,ScaleMode.ScaleToFit);
     Text(886,62,80,24,$"{Save.gems}",small);
     GUI.DrawTexture(new Rect(980,61,24,24),iconStar,ScaleMode.ScaleToFit);
     Text(1008,62,120,24,$"{TotalStars()}/45 Stars",small);
    }else{
     Text(740,62,440,24,$"Treasury: {Save.coins} Gold  •  {Save.gems} Gems  •  {TotalStars()}/45 Stars",small);
    }

    if(dividerLine)GUI.DrawTexture(new Rect(80,128,1120,16),dividerLine,ScaleMode.StretchToFill);
    else Box(new Rect(80,135,1120,2),new Color(Accent.r,Accent.g,Accent.b,.35f));

    bool prevEnabled=GUI.enabled;
    for(int i=0;i<15;i++){
     float x=80+(i%5)*228,y=150+(i/5)*150;
     bool unlocked=i+1<=Save.unlocked;
     bool completed=unlocked&&Save.stars[i]>=3;
     bool selected=unlocked&&i+1==Save.unlocked&&!completed;

     Rect cardRect=new Rect(x,y,216,140);
     Vector2 mpos=Event.current.mousePosition;
     bool hover=unlocked&&cardRect.Contains(mpos);

     Texture2D ctex=completed?(cardCompleted??cardUnlocked):selected?(cardSelected??cardUnlocked):unlocked?cardUnlocked:cardLocked;
     if(ctex){
      GUI.color=hover?new Color(1.15f,1.15f,1.15f,1f):unlocked?Color.white:new Color(.75f,.75f,.75f,.85f);
      GUI.DrawTexture(cardRect,ctex,ScaleMode.StretchToFill);
      GUI.color=Color.white;
     }else{
      BoxOutline(cardRect,hover?new Color(.12f,.22f,.28f,.95f):new Color(.04f,.09f,.13f,.85f),unlocked?Accent:new Color(.3f,.35f,.4f),hover?2f:1f);
     }

     if(!unlocked){
      if(iconLock)GUI.DrawTexture(new Rect(x+92,y+32,32,32),iconLock,ScaleMode.ScaleToFit);
      Text(x+10,y+70,196,20,$"CHAPTER {i+1:00}",tinyC);
      Text(x+10,y+94,196,20,"LOCKED",tinyC);
     }else{
      Text(x+10,y+16,196,22,$"CHAPTER {i+1:00}",tinyC);
      Text(x+10,y+42,196,44,Titles[i],center);
      int stars=Save.stars[i];
      if(stars>0&&iconStar){
       float sw=20f,sp=6f;
       float sx=x+(216-(stars*sw+(stars-1)*sp))*0.5f;
       for(int s=0;s<stars;s++)GUI.DrawTexture(new Rect(sx+s*(sw+sp),y+96,sw,sw),iconStar,ScaleMode.ScaleToFit);
      }else{
       Text(x+10,y+96,196,22,stars>0?new string('★',stars):"—",tinyC);
      }
     }

     if(unlocked&&GUI.Button(cardRect,GUIContent.none,GUIStyle.none)){
      LoadLevel(i+1);
     }
    }
    GUI.enabled=prevEnabled;
    if(MenuButton(new Rect(80,615,190,50),"BACK",iconBack))Screen=GameScreen.Menu;
    return;
   }

      if(Screen==GameScreen.Settings){
    Panel(200,45,880,630);

    Text(240,56,800,40,"Sanctuary & Blessings",titleC);
    if(headerOrnament)GUI.DrawTexture(new Rect(440,96,400,24),headerOrnament,ScaleMode.ScaleToFit);
    if(dividerLine)GUI.DrawTexture(new Rect(240,122,800,12),dividerLine,ScaleMode.StretchToFill);
    else Box(new Rect(240,126,800,2),new Color(Accent.r,Accent.g,Accent.b,.35f));

    Text(240,138,800,24,$"Available Resources:  {Save.coins} Gold   •   {Save.gems} Gems",smallC);

    // Primary Upgrades (Gold)
    string vitCost=Save.healthRank<3?(50+Save.healthRank*40)+" Gold":"MAXED";
    if(MenuButton(new Rect(240,170,800,44),$"VITALITY RANK {Save.healthRank}/3  (+{Save.healthRank} Max HP)   —   Cost: {vitCost}",smallBtn:true,st:smallC)){
     int cost=50+Save.healthRank*40;
     if(Save.healthRank<3){if(Save.coins>=cost){Save.coins-=cost;Save.healthRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    string arsCost=Save.arsenalRank<3?(80+Save.arsenalRank*60)+" Gold":"MAXED";
    if(MenuButton(new Rect(240,222,520,44),$"ARSENAL RANK {Save.arsenalRank}/3  (+{Save.arsenalRank*8}% DMG)   —   {arsCost}",smallBtn:true,st:smallC)){
     int cost=80+Save.arsenalRank*60;
     if(Save.arsenalRank<3){if(Save.coins>=cost){Save.coins-=cost;Save.arsenalRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(MenuButton(new Rect(770,222,270,44),$"ARSENAL ({Save.weapons.Count})",iconArsenal,smallBtn:true,st:smallC)){showArsenal=true;arsenalPage=0;arsenalReturn=GameScreen.Settings;Sound("power_select");}
    if(showArsenal){ArsenalView();return;}

    // Divine Blessings (Gems)
    string powCost=Save.powerRank<3?(3+Save.powerRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(240,274,800,44),$"ELEMENTAL POWER RANK {Save.powerRank}/3  (+{Save.powerRank*20}% Power)   —   Cost: {powCost}",smallBtn:true,st:smallC)){
     int cost=3+Save.powerRank*2;
     if(Save.powerRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.powerRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }

    string aethCost=Save.aetherRank<3?(4+Save.aetherRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(240,326,395,44),$"AETHER {Save.aetherRank}/3 (+{Save.aetherRank*25}% Regen)  —  {aethCost}",smallBtn:true,st:smallC)){
     int cost=4+Save.aetherRank*2;
     if(Save.aetherRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.aetherRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    string moxCost=Save.moxieRank<3?(5+Save.moxieRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(645,326,395,44),$"MOXIE {Save.moxieRank}/3 (+{Save.moxieRank} Dash)  —  {moxCost}",smallBtn:true,st:smallC)){
     int cost=5+Save.moxieRank*2;
     if(Save.moxieRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.moxieRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }

    string tempoCost=Save.tempoRank<3?(5+Save.tempoRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(240,378,395,44),$"TEMPO {Save.tempoRank}/3 (+{Save.tempoRank*15}% Ctr)  —  {tempoCost}",smallBtn:true,st:smallC)){
     int cost=5+Save.tempoRank*2;
     if(Save.tempoRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.tempoRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    string windCost=Save.windRank<3?(7+Save.windRank*3)+" Gems":"MAXED";
    if(MenuButton(new Rect(645,378,395,44),$"SECOND WIND {Save.windRank}/3 ({Save.windRank} Revive)  —  {windCost}",smallBtn:true,st:smallC)){
     int cost=7+Save.windRank*3;
     if(Save.windRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.windRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }

    // Audio & Preferences
    if(MenuButton(new Rect(240,436,260,42),"MUSIC: "+(Save.music?"ENABLED":"MUTED"),smallBtn:true,st:smallC)){Save.music=!Save.music;Persist();}
    if(MenuButton(new Rect(510,436,260,42),"SOUND: "+(Save.sound?"ENABLED":"MUTED"),smallBtn:true,st:smallC)){Save.sound=!Save.sound;Persist();}
    if(MenuButton(new Rect(780,436,260,42),"HAPTICS: "+(Save.haptics?"ON":"OFF"),smallBtn:true,st:smallC)){Save.haptics=!Save.haptics;Persist();}

    if(MenuButton(new Rect(240,486,395,42),"VISUAL FX: "+(Save.postFx?"ENHANCED":"OFF"),smallBtn:true,st:smallC)){Save.postFx=!Save.postFx;Persist();}
    if(MenuButton(new Rect(645,486,395,42),"SCREEN SHAKE: "+(Save.shake?"ENABLED":"OFF"),smallBtn:true,st:smallC)){Save.shake=!Save.shake;Persist();}

    if(dividerLine)GUI.DrawTexture(new Rect(240,540,800,10),dividerLine,ScaleMode.StretchToFill);

    if(MenuButton(new Rect(480,562,320,48),"BACK",iconBack,smallBtn:true,st:smallC))Screen=GameScreen.Menu;
    return;
   }

   Texture2D WeaponIcon(WeaponDefinition def){
    if(string.IsNullOrEmpty(def.Resource))return null;
    string key=def.Resource.Substring(def.Resource.LastIndexOf('/')+1);
    if(iconCache.TryGetValue(key,out var tex))return tex;
    tex=Resources.Load<Texture2D>("Weapons/Icons/"+key);
    iconCache[key]=tex;return tex;
   }
   void ArsenalView(){
    Panel(200,50,880,625);
    Text(240,60,800,48,$"Arsenal  —  {Save.weapons.Count} Collected",titleC);
    if(headerOrnament)GUI.DrawTexture(new Rect(440,110,400,24),headerOrnament,ScaleMode.ScaleToFit);
    if(dividerLine)GUI.DrawTexture(new Rect(240,136,800,12),dividerLine,ScaleMode.StretchToFill);
    else Box(new Rect(240,145,800,2),new Color(Accent.r,Accent.g,Accent.b,.35f));
    bool fists=Save.equippedWeapon<0;
    if(MenuButton(new Rect(240,154,800,44),fists?"✓ BARE FISTS EQUIPPED":"◄ UNEQUIP  —  fight bare-fisted",smallBtn:true,st:smallC)){
     if(!fists)UnequipWeapon();else Sound("power_select");
    }
    var list=Save.weapons;
    int pages=Mathf.Max(1,(list.Count+6)/7);
    arsenalPage=Mathf.Clamp(arsenalPage,0,pages-1);
    if(list.Count==0)Text(240,260,800,60,"No weapons collected yet.\nBlades you find in the chapters will wait for you here.",smallC);
    for(int k=0;k<7;k++){
     int idx=arsenalPage*7+k;if(idx>=list.Count)break;
     var def=WeaponCatalog.Get(list[idx]);
     bool eq=Save.equippedWeapon==list[idx];
     float ry=208+k*56;
     if(MenuButton(new Rect(240,ry,800,50),"")){
      if(eq)UnequipWeapon();else EquipWeapon((WeaponId)list[idx]);
     }
     // Dedicated illuminated weapon icon slot
     Rect slot=new Rect(246,ry+4,42,42);
     BoxOutline(slot,new Color(.02f,.06f,.10f,.92f),eq?new Color(1f,.85f,.35f):new Color(.38f,.95f,.7f,.6f),1.5f);
     var icon=WeaponIcon(def);
     if(icon)GUI.DrawTexture(new Rect(slot.x+2,slot.y+2,38,38),icon,ScaleMode.ScaleToFit);
     else Text(slot.x,slot.y+8,42,24,"⚔",smallC);

     // Name & Stats
     Text(302,ry+6,530,20,def.Name.ToUpper(),small);
     Text(302,ry+26,530,18,$"Damage ×{def.Damage:0.00}   •   Reach ×{def.Reach:0.00}   •   Speed ×{def.Tempo:0.00}",tinyC);

     // Equip status badge on right
     if(eq){
      GUI.color=new Color(1f,.9f,.35f);
      Text(850,ry+14,175,22,"✓ EQUIPPED",smallC);
      GUI.color=Color.white;
     }else{
      Text(850,ry+14,175,22,"EQUIP",tinyC);
     }
    }
    if(pages>1){
     if(arsenalPage>0&&MenuButton(new Rect(240,610,180,46),"PREV",iconBack,smallBtn:true,st:smallC)){arsenalPage--;Sound("power_select");}
     Text(430,610,220,46,$"PAGE {arsenalPage+1}/{pages}",smallC);
     if(arsenalPage<pages-1&&MenuButton(new Rect(660,610,180,46),"NEXT",iconContinue,smallBtn:true,st:smallC)){arsenalPage++;Sound("power_select");}
    }
    if(arsenalReturn==GameScreen.Paused){
     if(MenuButton(new Rect(860,610,180,46),"PAUSE",iconBack,smallBtn:true,st:smallC)){showArsenal=false;Sound("power_select");}
    }else if(MenuButton(new Rect(860,610,180,46),"BACK",iconBack,smallBtn:true,st:smallC)){showArsenal=false;Sound("power_select");}
   }

   // Pause / Complete / Defeat Screens
   Panel(320,90,640,535);

   string heading=Screen==GameScreen.Paused?"A Moment of Rest":Screen==GameScreen.Complete?(Level==15?"The Lost Realms Restored!":"Chapter Cleared!"):"The Light Remains";
   Text(340,115,600,50,heading,titleC);
   if(headerOrnament)GUI.DrawTexture(new Rect(440,166,400,26),headerOrnament,ScaleMode.ScaleToFit);
   if(dividerLine)GUI.DrawTexture(new Rect(355,196,570,14),dividerLine,ScaleMode.StretchToFill);
   else Box(new Rect(355,200,570,2),new Color(Accent.r,Accent.g,Accent.b,.4f));

   if(Screen==GameScreen.Complete){
    if(iconStar!=null&&EarnedStars>0){
     float sw=40f,sp=14f;
     float sx=640f-(EarnedStars*sw+(EarnedStars-1)*sp)*0.5f;
     for(int s=0;s<EarnedStars;s++)GUI.DrawTexture(new Rect(sx+s*(sw+sp),220,sw,sw),iconStar,ScaleMode.ScaleToFit);
    }else{
     Text(355,220,570,36,new string('★',EarnedStars),titleC);
    }
    Text(355,270,570,44,$"TRIUMPH!   Elapsed: {Clock(Elapsed)}\nRewards: +{Coins+EarnedStars*10} Gold   +{Gems} Gems",smallC);
    if(MenuButton(new Rect(355,340,570,58),Level==15?"RETURN TO REALM ATLAS":"NEXT CHAPTER",iconContinue,primary:true)){
     if(Level==15)Screen=GameScreen.Map;else LoadLevel(Level+1);
    }
    if(MenuButton(new Rect(355,412,570,52),"MAIN MENU",iconMainMenu)){showArsenal=false;Audio.Suspend(false);Time.timeScale=1;Screen=GameScreen.Menu;}
   }else if(Screen==GameScreen.Paused){
    Text(355,222,570,36,"Your journey is paused. All progress is safe.",smallC);
    if(MenuButton(new Rect(355,270,570,56),"RESUME JOURNEY",iconResume,primary:true))Resume();
    if(MenuButton(new Rect(355,338,570,50),"ARSENAL  —  change weapon",iconArsenal)){showArsenal=true;arsenalPage=0;arsenalReturn=GameScreen.Paused;Sound("power_select");}
    if(MenuButton(new Rect(355,400,275,50),"ATLAS",iconAtlas))Screen=GameScreen.Map;
    if(MenuButton(new Rect(650,400,275,50),"RESTART",iconRestart))LoadLevel(Level);
    if(MenuButton(new Rect(355,462,570,50),"MAIN MENU",iconMainMenu)){showArsenal=false;Audio.Suspend(false);Time.timeScale=1;Screen=GameScreen.Menu;}
    if(showArsenal){ArsenalView();return;}
    return;
   }else{
    Text(355,222,570,36,"Rise again at your last shrine checkpoint.",smallC);
    if(MenuButton(new Rect(355,280,570,56),"TRY AGAIN",iconResume,primary:true)){Respawn();Resume();}
    if(MenuButton(new Rect(355,348,275,50),"ATLAS",iconAtlas))Screen=GameScreen.Map;
    if(MenuButton(new Rect(650,348,275,50),"RESTART",iconRestart))LoadLevel(Level);
    if(MenuButton(new Rect(355,410,570,50),"MAIN MENU",iconMainMenu))Screen=GameScreen.Menu;
   }
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
   void RoundButton(Rect r,Texture2D tex,string name,string sub,bool primary=false,bool pressed=false){
    if(tex){
     Rect drawRect=r;
     if(pressed){
      float pad=r.width*0.035f;
      drawRect=new Rect(r.x+pad,r.y+pad,r.width-pad*2,r.height-pad*2);
      GUI.color=new Color(1.15f,1.15f,1.15f,1f);
     }else{
      GUI.color=Color.white;
     }
     GUI.DrawTexture(drawRect,tex);
     GUI.color=Color.white;
     return;
    }
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
