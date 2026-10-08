using System;
using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public enum GameScreen { Menu, Map, Playing, Paused, Complete, Defeated, Settings }
[Serializable] public class Progress {
   public void NormalizeWeapons(){
    equippedWeapon=Mathf.Clamp(equippedWeapon,-1,WeaponCatalog.MaxId);
    if(weapons==null)weapons=new List<int>();
    if(equippedWeapon>=0&&!weapons.Contains(equippedWeapon))weapons.Add(equippedWeapon);
    for(int i=weapons.Count-1;i>=0;i--)if(weapons[i]<0||weapons[i]>WeaponCatalog.MaxId||weapons.IndexOf(weapons[i])!=i)weapons.RemoveAt(i);
   }
   public void NormalizeSkins(){
    equippedSkin=Mathf.Clamp(equippedSkin,0,5);
    if(skins==null)skins=new List<int>();
    if(!skins.Contains(0))skins.Add(0);
    if(!skins.Contains(equippedSkin))skins.Add(equippedSkin);
    for(int i=skins.Count-1;i>=0;i--)if(skins[i]<0||skins[i]>5||skins.IndexOf(skins[i])!=i)skins.RemoveAt(i);
   }
   public void NormalizeCodex(){Codex.Normalize(this);}
   public int version=2;public int unlocked=1, equippedWeapon=-1, equippedSkin=0, coins, gems, healthRank, powerRank, arsenalRank, aetherRank, moxieRank, tempoRank, windRank, tipsSeen; public int[] stars=new int[15]; public float[] best=new float[15]; public bool music=true,sound=true,postFx=true,shake=true,haptics=true,softLock=true; public List<int> weapons=new List<int>(); public List<int> skins=new List<int>{0};
   public int[] bestiary=new int[22]; public List<string> achievements=new List<string>(); public int bossKills, lifetimeParries, lifetimeDodges, bestCombo;
  }
 public class RealmGame : MonoBehaviour {
  public static RealmGame I; public static bool Testing=>Array.IndexOf(Environment.GetCommandLineArgs(),"-realmTest")>=0||Array.IndexOf(Environment.GetCommandLineArgs(),"-menuVisualProbe")>=0; public static readonly string[] Titles={"Mosslight Trail","Whispering Falls","Rootbound Ruins","The Elder Grove","Sunscorched Pass","Temple of Keys","Sandstone Colossus","Frostwind Climb","Crystal Hollow","Crown of Winter","Ember Foothills","Brimstone Rampart","Cindervein Gorge","Obsidian Ascent","Emberfall Summit"};
public static readonly string[] Realms={"VERDANT KINGDOM","BURNING DUNES","FROZEN PEAKS","EMBERFALL"};
   public static readonly Color[] Accents={new Color(.38f,.95f,.7f),new Color(1,.67f,.28f),new Color(.4f,.83f,1),new Color(1f,.35f,.28f)};
  public GameScreen Screen=GameScreen.Menu; public Progress Save=new Progress(); public Hero Player; public RealmWorld World; public FollowCamera CameraRig;
  public int Level=1, Realm, Coins, Gems, DamageTaken, EarnedStars, Combo, Kills, CoinsTotal, GemsTotal, KillsTotal; public float Elapsed; public Vector3 Checkpoint; public bool CheckpointActive; public string Notice=""; float noticeUntil,comboUntil; public Color Accent=>Accents[0];
  public WeaponDefinition CurrentWeapon=>WeaponCatalog.Get(Save.equippedWeapon);
  public Vector2 MoveInput; public bool JumpPressed,DashPressed,AttackPressed,AttackReleased,CastPressed,ParryPressed,SpellPressed,SpellReleased,GrapplePressed; public bool AttackHeld,SpellHeld,JumpHeld;
  public readonly List<Enemy> Enemies=new List<Enemy>(); public AudioSource Music,Sfx;
  public RealmAudio Audio {get;private set;} public RealmTrials Trial {get;private set;} public ExpansionChapter Adventure {get;private set;} public CampaignPacing Pacing {get;private set;}
   WardrobePreview wardrobePreview; int wardrobePreviewSkin=-1;
   Transform worldRoot; GUIStyle title,titleC,label,small,button,center,big,smallC,tinyC,btnText; Texture2D hudPanel; Texture2D pixel,circleFill,circleRing,btnBlade,btnJump,btnDodge,btnPower,btnParry,btnSpell,btnPause,joyBase,joyKnob; static readonly Color MenuGold=new Color(.82f,.65f,.32f), MenuBlue=new Color(.38f,.68f,.94f); Font fantasyHeading,fantasyButton,fantasyBody; Texture2D iconWardrobe,iconAchievements,iconBestiary,iconVault,chestNormal,chestGolden,chestEpic,rewardHalo; Texture2D bgMainMenu,avatarAster,btnPrimaryNorm,btnPrimaryHigh,btnStdNorm,btnStdHigh,btnSecNorm,btnSecHigh,panelLarge,panelMedium,cardUnlocked,cardSelected,cardLocked,cardCompleted,headerOrnament,dividerLine,barFrame,barFill,resourceCapsule,iconArsenal,iconAtlas,iconBack,iconClose,iconCoin,iconGem,iconLock,iconMainMenu,iconNewJourney,iconContinue,iconResume,iconRestart,iconSanctuary,iconStar; readonly TouchRouter touch=new TouchRouter(); float yawInput,hitStopUntil,bossIntroUntil,heartbeatNext;bool showArsenal,showSkins,showCodex,showAchievements,showVault;int arsenalPage,skinPage,codexPage,achievementsPage;GameScreen arsenalReturn=GameScreen.Settings,skinReturn=GameScreen.Settings,codexReturn=GameScreen.Settings,achievementsReturn=GameScreen.Settings,vaultReturn=GameScreen.Settings;enum VaultState{Browse,Opening,Revealed};VaultState vaultState=VaultState.Browse;ChestType openingChestType=ChestType.Normal;float vaultAnimTimer=0f,rewardRevealStarted=-100f;ChestReward currentChestReward=null;bool showDropRates=false;readonly System.Collections.Generic.Dictionary<string,Texture2D> iconCache=new System.Collections.Generic.Dictionary<string,Texture2D>();
  public static readonly Color[] ElementColors={new Color(1f,.45f,.1f),new Color(.2f,.85f,1f),new Color(.2f,1f,.55f)};
  static int debugTurretCycle;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){
   var existing=FindObjectsByType<RealmGame>(FindObjectsSortMode.None);
   if(existing.Length==0){new GameObject("Lost Realms 3D").AddComponent<RealmGame>();return;}
   // Never allow two game instances (two cameras/listeners/OnGUI = dead menus).
   for(int i=1;i<existing.Length;i++)if(existing[i])Destroy(existing[i].gameObject);
  }
  void Awake(){ I=this;Pacing=gameObject.AddComponent<CampaignPacing>(); Application.targetFrameRate=60; QualitySettings.vSyncCount=1; Time.fixedDeltaTime=1f/60f; UnityEngine.Screen.orientation=ScreenOrientation.LandscapeLeft;
try{if(!Testing&&PlayerPrefs.HasKey("LostRealms3D.v2"))Save=JsonUtility.FromJson<Progress>(PlayerPrefs.GetString("LostRealms3D.v2"))??new Progress();
     else if(!Testing&&PlayerPrefs.HasKey("LostRealms3D.v1")){Save=JsonUtility.FromJson<Progress>(PlayerPrefs.GetString("LostRealms3D.v1"))??new Progress();Save.version=2;Persist();}}catch{Save=new Progress();}
    if(Save.stars==null||Save.stars.Length!=15){var old=Save.stars;Save.stars=new int[15];if(old!=null)for(int i=0;i<old.Length&&i<15;i++)Save.stars[i]=old[i];}
    if(Save.best==null||Save.best.Length!=15){var old=Save.best;Save.best=new float[15];if(old!=null)for(int i=0;i<old.Length&&i<15;i++)Save.best[i]=old[i];}
    Save.unlocked=Mathf.Clamp(Save.unlocked,1,15);Save.equippedWeapon=Mathf.Clamp(Save.equippedWeapon,-1,WeaponCatalog.MaxId);
    // Collected-weapons inventory: init for old saves, grandfather the
    // currently equipped blade, drop dupes and out-of-range ids.
    Save.NormalizeWeapons();
    Save.NormalizeSkins();
    Save.NormalizeCodex();
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
   if(Pacing)Pacing.Close("restarted");
   showArsenal=false;arsenalPage=0;showCodex=false;showAchievements=false;showVault=false;
   if(Audio)Audio.ClearRunSounds();
   Time.timeScale=1; touch.Reset(); if(worldRoot){worldRoot.gameObject.SetActive(false);Destroy(worldRoot.gameObject);} Enemies.Clear(); Level=Mathf.Clamp(id,1,15); Realm=Level<=4?0:Level<=7?1:Level<=10?2:3; Coins=Gems=DamageTaken=EarnedStars=0; Kills=CoinsTotal=GemsTotal=KillsTotal=0; Elapsed=0; CheckpointActive=false;Combo=0;comboUntil=0;
   worldRoot=new GameObject("Realm "+Level+" - "+Titles[Level-1]).transform; World=worldRoot.gameObject.AddComponent<RealmWorld>();   try{World.Build(Level,Realm);}catch(System.Exception e){Debug.LogError("LEVEL_BUILD_FAILED "+Level+": "+e);}
   Trial=RealmTrials.Build(World,Level);
   Adventure=ExpansionChapter.Build(World,Level);
   GUI.enabled=true;
   var hero=new GameObject("Aster"); hero.transform.SetParent(worldRoot); hero.transform.position=World.Spawn; Player=hero.AddComponent<Hero>(); Checkpoint=World.Spawn;
   // Restore the camera only after the newly-created Hero Awake path completes.
   CameraRig.Target=Player.transform;
   CameraRig.Snap();
   if(World.IsBoss)CameraRig.ZoomBias=-1.3f;
   bossIntroUntil=World.IsBoss?Time.unscaledTime+4.4f:0f;
    string stageTrack=World.IsBoss?"boss_battle_theme":Realm==0?"verdant_theme":Realm==1?"desert_exploration_theme":Realm==2?"frozen_exploration_theme":"emberfall_exploration_theme";SetMusic(stageTrack);
    Screen=GameScreen.Playing; Pacing.Begin(); Tell(Adventure?Adventure.Objective:Level==1?"WASD / left stick to move. Jump twice to reach the next island.":World.IsBoss?"Break the guardian's corruption. Dodge red warnings, strike during recovery.":Titles[Level-1]+"  /  Follow the golden trail to the realm gate.",6);
  }
  void Update(){
   if(wardrobePreview&&(!showSkins||showVault||(Screen!=GameScreen.Settings&&Screen!=GameScreen.Paused))){Destroy(wardrobePreview);wardrobePreview=null;wardrobePreviewSkin=-1;}

   JumpPressed=DashPressed=AttackPressed=AttackReleased=CastPressed=ParryPressed=SpellPressed=SpellReleased=GrapplePressed=false; MoveInput=Vector2.zero; yawInput=0;
   if(hitStopUntil>0f&&Time.unscaledTime>=hitStopUntil){hitStopUntil=0f;Time.timeScale=1f;}
   RewardedAdManager.Update(Time.unscaledDeltaTime);
   if(vaultState==VaultState.Opening){
    vaultAnimTimer+=Time.unscaledDeltaTime;
    if(vaultAnimTimer>=0.7f){
     vaultState=VaultState.Revealed;
     rewardRevealStarted=Time.unscaledTime;
     Sound(currentChestReward!=null&&currentChestReward.Rarity==RewardRarity.Epic?"level_clear":"upgrade");
     if(CameraRig)CameraRig.Shake=currentChestReward!=null&&currentChestReward.Rarity==RewardRarity.Epic?.35f:.15f;
    }
   }
   if(!I||!Player)return;
   if(Application.isEditor){
    if(Input.GetKeyDown(KeyCode.Alpha1))LoadLevel(1);
    else if(Input.GetKeyDown(KeyCode.Alpha2))LoadLevel(2);
    else if(Input.GetKeyDown(KeyCode.Alpha5))LoadLevel(5);
    else if(Input.GetKeyDown(KeyCode.Alpha8))LoadLevel(8);
    else if(Input.GetKeyDown(KeyCode.Alpha0)||Input.GetKeyDown(KeyCode.Minus))LoadLevel(12);
    else if(Input.GetKeyDown(KeyCode.T)&&Player!=null){
     Vector3 tpos=Player.transform.position+Player.transform.forward*3.5f;
     var tk=(DartTurret.TurretKind)(debugTurretCycle++%3);
     DartTurret.Place(World?World.transform:transform,tpos,Accent,Color.gray,tk);
     Tell("Spawned "+tk+" Turret ahead! [T cycles: Stone -> Dragon -> Lion]",3);
    }
   }
   if(Input.GetKeyDown(KeyCode.Escape)){if(RewardedAdManager.IsShowing)RewardedAdManager.Close();else if(showVault)showVault=false;else if(showSkins)showSkins=false;else if(showArsenal)showArsenal=false;else if(showCodex)showCodex=false;else if(showAchievements)showAchievements=false;else if(Screen==GameScreen.Playing)Pause();else if(Screen==GameScreen.Paused)Resume();else Screen=GameScreen.Menu;}
    if(Screen==GameScreen.Menu||Screen==GameScreen.Map)SetMusic("verdant_theme");
    else if(Screen==GameScreen.Settings)SetMusic("frozen_exploration_theme");
    else if(Screen==GameScreen.Playing){
     string stageTrack=(World&&World.IsBoss)?"boss_battle_theme":Realm==0?"verdant_theme":Realm==1?"desert_exploration_theme":Realm==2?"frozen_exploration_theme":"emberfall_exploration_theme";
     SetMusic(stageTrack);
    }
    if(Screen!=GameScreen.Playing){AttackHeld=false;touch.Reset();return;} Elapsed+=Time.deltaTime; if(comboUntil>0f&&Elapsed>=comboUntil){Combo=0;comboUntil=0;}
     if(World&&World.IsBoss&&Player&&Player.Health>0){
      float hpFrac=(float)Player.Health/Player.MaxHealth;
      if(hpFrac<.25f&&Time.unscaledTime>=heartbeatNext){heartbeatNext=Time.unscaledTime+1.14f;Sound("land_soft",.65f);}
     }
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
  public void Resume(){showSkins=false;showArsenal=false;showCodex=false;showAchievements=false;showVault=false;Screen=GameScreen.Playing;Audio.Suspend(false);}
  void SetMusic(string key){
   if(Audio)Audio.SetTrack(key);
  }
  void OnApplicationPause(bool paused){if(paused&&Screen==GameScreen.Playing)Pause();Persist();}
  void OnApplicationFocus(bool focused){if(!focused&&Screen==GameScreen.Playing)Pause();}
  public void Tell(string message,float seconds=3){Notice=message;noticeUntil=Time.unscaledTime+seconds;}
   public void ComboHit(){Combo++;comboUntil=Elapsed+1.1f;if(Save!=null)Save.bestCombo=Mathf.Max(Save.bestCombo,Combo);}
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
  public void HitStop(float seconds,float scale=.12f){seconds=Mathf.Min(seconds,.05f);if(seconds<=0f)return;Time.timeScale=Mathf.Min(Time.timeScale,scale);float until=Time.unscaledTime+seconds;if(until>hitStopUntil)hitStopUntil=until;}
  public void HitStopFeel(float seconds,float scale=.12f){seconds=Mathf.Clamp(seconds,0f,.14f);if(seconds<=0f)return;Time.timeScale=Mathf.Min(Time.timeScale,scale);float until=Time.unscaledTime+seconds;if(until>hitStopUntil)hitStopUntil=until;}
  public void TriggerTip(int bitMask,string message,float duration=4.5f){if(Save==null||(Save.tipsSeen&bitMask)!=0)return;Save.tipsSeen|=bitMask;Persist();Tell(message,duration);}
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
   public int GateStars()=>Adventure&&Adventure.Ready?Mathf.Max(1,StarsFor(Completion())):StarsFor(Completion());
   public bool GateOpen()=>Adventure?Adventure.Ready:Completion()>=.6f;
   public string GateSealedText(){
    if(Adventure)return Adventure.SealText;
    float c=CoinsTotal>0?(float)Coins/CoinsTotal:1f,g=GemsTotal>0?(float)Gems/GemsTotal:1f,k=KillsTotal>0?(float)Kills/KillsTotal:1f;
    return $"Realm gate sealed — completion {(int)(Completion()*100f)}% (need 60%): coins {(int)(Mathf.Clamp01(c)*100f)}% • gems {(int)(Mathf.Clamp01(g)*100f)}% • foes {(int)(Mathf.Clamp01(k)*100f)}%";
   }
   public void EquipWeapon(WeaponId id){
    if((int)id<0||(int)id>WeaponCatalog.MaxId)return;
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
   public SkinDefinition CurrentSkin=>SkinCatalog.Get(Save.equippedSkin);
   public bool BuySkin(SkinId id){
    int skinIndex=(int)id;
    var def=SkinCatalog.Get(id);
    if(Save.skins==null)Save.skins=new List<int>{0};
    if(Save.skins.Contains(skinIndex))return false;
    if(Save.coins<def.CostCoins||Save.gems<def.CostGems){
     Sound("power_fail");
     Tell("Need "+(Save.coins<def.CostCoins?def.CostCoins+" Gold":def.CostGems+" Gems")+" for "+def.Name);
     return false;
    }
    Save.coins-=def.CostCoins;
    Save.gems-=def.CostGems;
    Save.skins.Add(skinIndex);
    Save.equippedSkin=skinIndex;
    Persist();
    if(Player)Player.ApplySkin(def);
    Sound("upgrade");
    Tell("UNLOCKED & EQUIPPED "+def.Name+"!",4.5f);
    return true;
   }
   public void EquipSkin(SkinId id){
    int skinIndex=(int)id;
    if(Save.skins==null||!Save.skins.Contains(skinIndex))return;
    Save.equippedSkin=skinIndex;
    Persist();
    var def=SkinCatalog.Get(id);
    if(Player)Player.ApplySkin(def);
    Sound("power_select");
    Tell("EQUIPPED OUTFIT: "+def.Name,4f);
   }
  public void ActivateCheckpoint(Vector3 position){Checkpoint=position;CheckpointActive=true;Sound("checkpoint");Tell("Checkpoint restored. Your trail is safe.");}
  public void Respawn(){Player.Warp(Checkpoint);Player.Health=Player.MaxHealth;Player.Energy=100;CrumblePlatform.ResetAll();Sound("respawn");Vfx.Play("ga_vfx_Portal_01",Checkpoint,Quaternion.identity,1.1f);Tell("Returned to the checkpoint.");}
  public void Defeat(){if(Screen==GameScreen.Defeated)return;Pacing.Death();Screen=GameScreen.Defeated;Sound("defeat");if(CameraRig)CameraRig.ZoomBias=1.6f;}
   public void Finish(){if(Screen!=GameScreen.Playing)return;if(World.IsBoss&&Enemies.Exists(x=>x&&x.Boss&&x.Health>0)){Tell("Defeat the guardian to open this gate.");return;}
    // Anti-skip: the gate only opens at 60%+ completion (coins/gems/foes).
    if(!GateOpen()){Tell(GateSealedText(),4f);Sound("power_fail");return;}
    EarnedStars=GateStars(); Save.stars[Level-1]=Mathf.Max(Save.stars[Level-1],EarnedStars); if(Save.best[Level-1]<=0||Elapsed<Save.best[Level-1])Save.best[Level-1]=Elapsed;
    Save.coins+=Coins+EarnedStars*10;Save.gems+=Gems;Save.unlocked=Mathf.Max(Save.unlocked,Mathf.Min(15,Level+1));Persist();Pacing.Close("completed");Screen=GameScreen.Complete;Sound("complete");
  }
  public static string Clock(float seconds)=>$"{(int)seconds/60:00}:{(int)seconds%60:00}";
  float healthLag = 1f;
  void BoxOutline(Rect r,Color bg,Color border,float bw=2f){
   if(Screen!=GameScreen.Playing){ border=new Color(MenuGold.r,MenuGold.g,MenuGold.b,Mathf.Max(.55f,border.a)); bg=new Color(.015f,.035f,.075f,bg.a); }
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
   if(!hudPanel)hudPanel=LoadUITex("Frames","UI_HUD_Panel");
   if(!iconWardrobe)iconWardrobe=LoadUITex("Icons","UI_Icon_Wardrobe");
   if(!iconAchievements)iconAchievements=LoadUITex("Icons","UI_Icon_Achievements");
   if(!iconBestiary)iconBestiary=LoadUITex("Icons","UI_Icon_Bestiary");
   if(!iconVault)iconVault=LoadUITex("Icons","UI_Icon_Vault");
   if(!chestNormal)chestNormal=LoadUITex("Icons","UI_Chest_Normal");
   if(!chestGolden)chestGolden=LoadUITex("Icons","UI_Chest_Golden");
   if(!chestEpic)chestEpic=LoadUITex("Icons","UI_Chest_Epic");
   if(!rewardHalo)rewardHalo=LoadUITex("Icons","UI_Reward_Halo");
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
   fantasyHeading=Resources.Load<Font>("UI/Fonts/CinzelDecorative-Bold");
   fantasyButton=Resources.Load<Font>("UI/Fonts/Cinzel-Bold");
   fantasyBody=Resources.Load<Font>("UI/Fonts/CrimsonText-Regular");
   foreach(var heading in new[]{title,titleC}){if(fantasyHeading)heading.font=fantasyHeading;heading.fontStyle=FontStyle.Normal;heading.alignment=TextAnchor.MiddleCenter;}
   foreach(var action in new[]{button,center,btnText,big}){if(fantasyButton)action.font=fantasyButton;action.fontStyle=FontStyle.Normal;}
   foreach(var body in new[]{label,small,smallC,tinyC}){if(fantasyBody)body.font=fantasyBody;body.fontStyle=FontStyle.Normal;}
   titleC.fontSize=40;title.fontSize=36;small.fontSize=18;smallC.fontSize=18;tinyC.fontSize=16;
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
  GUIStyle FitText(GUIStyle source,Rect r,string value,bool wrap=false){
   var fitted=new GUIStyle(source){wordWrap=wrap,clipping=TextClipping.Clip,padding=new RectOffset(0,0,0,0)};
   fitted.fontSize=Mathf.Min(source.fontSize,Mathf.Max(12,Mathf.FloorToInt(r.height*.82f)));
   var content=new GUIContent(value);
   while(fitted.fontSize>11&&(wrap?fitted.CalcHeight(content,r.width)>r.height:fitted.CalcSize(content).x>r.width))fitted.fontSize--;
   if(Array.IndexOf(Environment.GetCommandLineArgs(),"-menuVisualProbe")>=0&&Event.current.type==EventType.Repaint&&(wrap?fitted.CalcHeight(content,r.width)>r.height+1:fitted.CalcSize(content).x>r.width+1))Debug.LogWarning("MENU_TEXT_OVERFLOW "+value);
   return fitted;
  }
  void Text(float x,float y,float w,float h,string value,GUIStyle st=null){
   var rect=new Rect(x,y,w,h);var source=st??label;
   GUI.Label(rect,value,FitText(source,rect,value,source.wordWrap&&(value.Contains("\n")||h>=source.fontSize*1.6f)));
  }
  bool MenuButton(Rect r,string text,Texture2D icon=null,bool primary=false,bool smallBtn=false,GUIStyle st=null){
   bool hover=r.Contains(Event.current.mousePosition);
   var tex=hover?(primary?btnPrimaryHigh:smallBtn?btnSecHigh:btnStdHigh):(primary?btnPrimaryNorm:smallBtn?btnSecNorm:btnStdNorm);
   Color previous=GUI.color;
   GUI.color=GUI.enabled?Color.white:new Color(.6f,.6f,.6f,1);
   if(tex)GUI.DrawTexture(r,tex,ScaleMode.StretchToFill);
   else BoxOutline(r,new Color(.02f,.05f,.1f,.98f),MenuGold);
   float inset=r.width*.115f;
   float iconSize=Mathf.Min(primary?30f:26f,r.height*.48f);
   if(icon)GUI.DrawTexture(new Rect(r.x+inset,r.center.y-iconSize*.5f,iconSize,iconSize),icon,ScaleMode.ScaleToFit);
   float textLeft=inset+(icon?iconSize+9f:0f);
   Rect textRect=new Rect(r.x+textLeft,r.y+r.height*.22f,r.width-textLeft-inset,r.height*.56f);
   var style=new GUIStyle(st??(primary?big:smallBtn?smallC:btnText)){alignment=TextAnchor.MiddleCenter,wordWrap=false,font=fantasyButton?fantasyButton:btnText.font,fontStyle=FontStyle.Normal};
   style.fontSize=primary?23:smallBtn?16:19;
   if(st==tinyC)style.fontSize=14;
   style.normal.textColor=new Color(1f,.94f,.79f);
   GUI.Label(textRect,text,FitText(style,textRect,text));
   GUI.color=previous;
   return GUI.Button(r,GUIContent.none,GUIStyle.none);
  }
  void ImageFrame(Rect r,Texture2D texture,float edge=14f){
   if(!texture)return;
   float sx=.12f,sy=.24f;
   float[] dx={r.x,r.x+edge,r.xMax-edge,r.xMax};float[] dy={r.y,r.y+edge,r.yMax-edge,r.yMax};
   float[] ux={0,sx,1-sx,1};float[] uy={1,1-sy,sy,0};
   for(int y=0;y<3;y++)for(int x=0;x<3;x++)GUI.DrawTextureWithTexCoords(new Rect(dx[x],dy[y],dx[x+1]-dx[x],dy[y+1]-dy[y]),texture,new Rect(ux[x],uy[y+1],ux[x+1]-ux[x],uy[y]-uy[y+1]));
  }
  void MenuCard(Rect r,bool selected=false){
   GUI.color=Color.white;ImageFrame(r,selected?cardSelected:cardUnlocked,12f);
  }
  bool Button(float x,float y,float w,float h,string s){
   return MenuButton(new Rect(x,y,w,h),s);
  }
  void Panel(float x,float y,float w,float h){
   Rect r=new Rect(x,y,w,h);
   if(Screen==GameScreen.Playing&&hudPanel){GUI.color=Color.white;ImageFrame(r,hudPanel,8f);return;}
   if(Screen!=GameScreen.Playing&&w>400&&h>150&&panelLarge){GUI.color=Color.white;ImageFrame(r,w<700?panelMedium:panelLarge,20f);return;}
   BoxOutline(r,new Color(.028f,.06f,.09f,.86f),Screen==GameScreen.Playing?Accent:MenuGold,1.5f);
  }
  // Values stay live; the generated panel provides the frame, never baked text.
  void HudMeter(Rect r,float fraction,Color fill,float lag=0f){
   ImageFrame(r,hudPanel,4f);
   Rect inner=new Rect(r.x+4,r.y+4,r.width-8,r.height-8);
   if(lag>fraction)Box(new Rect(inner.x,inner.y,inner.width*Mathf.Clamp01(lag),inner.height),new Color(1f,.64f,.3f,.8f));
   Box(new Rect(inner.x,inner.y,inner.width*Mathf.Clamp01(fraction),inner.height),fill);
  }
  bool HudButton(Rect r,string caption){
   GUI.color=r.Contains(Event.current.mousePosition)?new Color(1f,1f,1f,.85f):Color.white;
   ImageFrame(r,hudPanel,8f);GUI.color=Color.white;
   var style=new GUIStyle(center){font=fantasyButton,fontSize=16,wordWrap=false};
   Rect content=new Rect(r.x+12,r.y+10,r.width-24,r.height-20);
   GUI.Label(content,caption,FitText(style,content,caption));
   return GUI.Button(r,GUIContent.none,GUIStyle.none);
  }
  public void SelectWardrobePreview(int id){
   wardrobePreviewSkin=Mathf.Clamp(id,0,SkinCatalog.Count-1);
   if(!wardrobePreview)wardrobePreview=gameObject.AddComponent<WardrobePreview>();
   wardrobePreview.Show(SkinCatalog.Get(wardrobePreviewSkin));
  }
  void OnGUI(){
   LoadButtonTextures();
   // Only the in-game HUD needs a live Player; menus must draw and respond even
   // if startup level construction ever fails.
   if(Screen==GameScreen.Playing&&!Player)return;
   Styles();
   GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(UnityEngine.Screen.width/1280f,UnityEngine.Screen.height/720f,1));
   if(showSkins&&!showVault&&(Screen==GameScreen.Settings||Screen==GameScreen.Paused)){WardrobeView();return;}
   if(showArsenal&&(Screen==GameScreen.Settings||Screen==GameScreen.Paused)){ArsenalView();return;}
   if(showCodex&&(Screen==GameScreen.Settings||Screen==GameScreen.Paused)){BestiaryView();if(RewardedAdManager.IsShowing)RewardedAdManager.DrawGUI(pixel,titleC,smallC,btnText);return;}
   if(showVault&&(Screen==GameScreen.Settings||Screen==GameScreen.Paused)){VaultView();if(RewardedAdManager.IsShowing)RewardedAdManager.DrawGUI(pixel,titleC,smallC,btnText);return;}
   if(showAchievements&&(Screen==GameScreen.Settings||Screen==GameScreen.Paused)){AchievementsView();return;}
   if(Screen==GameScreen.Playing){
    // Health & Realm Banner
    Panel(24,20,380,146);
    if(avatarAster){
     Box(new Rect(32,28,74,74),new Color(.02f,.06f,.09f,.92f));
     GUI.DrawTexture(new Rect(33,29,72,72),avatarAster,ScaleMode.ScaleToFit);
     BoxOutline(new Rect(32,28,74,74),Color.clear,MenuGold,1f);
    }
    float textX=avatarAster?114:42;
    Text(textX,30,276,20,$"REALM {Realm+1}  /  {Realms[Realm]}",small);
    Text(textX,51,276,25,Titles[Level-1]);

    float targetH=Player?Mathf.Clamp01((float)Player.Health/Player.MaxHealth):1f;
    healthLag=Mathf.Lerp(healthLag,targetH,Time.unscaledDeltaTime*3.5f);
    HudMeter(new Rect(textX,82,200,16),targetH,new Color(.85f,.18f,.22f),healthLag);
    Text(textX+208,80,68,20,$"HP {(Player?Player.Health:8)}/{(Player?Player.MaxHealth:8)}",tinyC);
    Text(textX,106,280,20,$"WEAPON  {CurrentWeapon.Name}",small);
    if(Player&&Player.CounterReady)Text(textX,124,280,18,"COUNTER READY  /  STRIKE NOW",tinyC);
    if(Combo>=3){Panel(textX,172,140,28);Text(textX+10,177,122,18,$"COMBO  x{Combo}",small);}

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
    Text(432,74,205,20,Adventure?Adventure.Counter:GateOpen()?"GATE OPEN  "+new string('★',GateStars()):"GATE "+(int)(Completion()*100f)+"%  -  need 60%",small);

    // Power Selector & Energy Bar
    string[] powerNames={"EMBER [FIRE]","FROST [ICE]","GALE [WIND]"};
    Color[] powerCols={new Color(1f,.22f,.18f),new Color(.2f,.85f,1f),new Color(.2f,1f,.55f)};
    if(HudButton(new Rect(660,20,205,48),powerNames[Player.Power])){Player.Power=(Player.Power+1)%3;Sound("power_select");}
    Text(660,70,205,16,"AETHER",tinyC);
    float enFrac=Mathf.Clamp01(Player.Energy/100f);
    HudMeter(new Rect(660,88,205,14),enFrac,powerCols[Player.Power]);

    // Live timer and clickable pause use the same minimal panel artwork.
    Panel(882,20,135,48);Text(894,30,111,28,$"TIME {Clock(Elapsed)}",smallC);
    if(HudButton(new Rect(1035,20,120,48),"PAUSE"))Pause();

     foreach(var foe in Enemies){if(!foe||foe.Boss||foe.Health<=0||Vector3.Distance(Player.transform.position,foe.transform.position)>12)continue;Vector3 v=Camera.main.WorldToViewportPoint(foe.transform.position+Vector3.up*(foe.Kind==6?2.9f:2.1f));float x=v.x*1280,y=(1-v.y)*720;if(v.z<=0||y<175||y>540||x<45||x>1235)continue;Box(new Rect(x-34,y,68,7),new Color(.03f,.07f,.08f,.85f));Box(new Rect(x-40,y,5,7),ElementColors[foe.WeakElement]);Box(new Rect(x-33,y+1,66*Mathf.Clamp01(foe.Health/foe.MaxHealth),5),ElementColors[foe.WeakElement]);}
    // Boss Bar
    var boss=Enemies.Find(x=>x&&x.Boss&&x.Health>0);
    if(boss&&Vector3.Distance(Player.transform.position,boss.transform.position)<32){
     Panel(418,122,472,60);
     Text(438,128,432,26,boss.DisplayName+"   PHASE "+boss.BossPhase+"/3",small);
     HudMeter(new Rect(438,160,432,14),Mathf.Clamp01(boss.Health/boss.MaxHealth),new Color(.85f,.18f,.22f));
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
     if(World&&World.IsBoss&&CombatTelegraph.Active!=null){
      foreach(var tele in CombatTelegraph.Active){
       if(!tele)continue;
       if(Vector3.Distance(Player.transform.position,tele.transform.position)>34f)continue;
       Vector3 tv=Camera.main.WorldToViewportPoint(tele.transform.position+Vector3.up*.4f);
       bool tbehind=tv.z<0f;
       float tvx=tbehind?-tv.x:tv.x;
       if(!tbehind&&tv.x>.04f&&tv.x<.96f&&tv.y>.06f&&tv.y<.94f)continue;
       float tsx=Mathf.Clamp(tvx,.05f,.95f)*1280f;
       float tsy=(1f-Mathf.Clamp(tv.y,.07f,.93f))*720f;
       float tang=Mathf.Atan2(tsy-360f,tsx-640f)*Mathf.Rad2Deg+45f;
       float tpulse=.5f+.45f*Mathf.Sin(Time.unscaledTime*11f);
       Color twarn=new Color(1f,.55f,.15f,tpulse);
       var tmatrix=GUI.matrix;
       GUIUtility.RotateAroundPivot(tang,new Vector2(tsx,tsy));
       Box(new Rect(tsx-9,tsy-9,18,18),twarn);
       GUI.matrix=tmatrix;
      }
     }
    }
    // Trial box removed per user request for uncluttered playfield.

    bool showTouchUI=Application.isMobilePlatform||Application.isEditor;
    if(showTouchUI){
     // Virtual joystick: fixed base ring plus a knob tracking live input.
     float jx=150,jy=612,jr=86;
     if(joyBase){
      GUI.color=Color.white;
      GUI.color=new Color(1,1,1,.8f);
      GUI.DrawTexture(new Rect(jx-jr,jy-jr,jr*2,jr*2),joyBase);
      GUI.color=Color.white;
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
     RoundButton(TouchRouter.ActionRect(1),btnPower,"POWER","",false,touch.Power||(Application.isEditor&&Input.GetKey(KeyCode.K)));
     RoundButton(TouchRouter.ActionRect(4),btnParry,"PARRY","",false,touch.Parry||(Application.isEditor&&Input.GetKey(KeyCode.L)));
     RoundButton(TouchRouter.ActionRect(5),btnSpell,"SPELL","HOLD",false,touch.SpellHeld||(Application.isEditor&&Input.GetKey(KeyCode.F)));
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
    Text(140,74,1000,68,"LEGENDS OF THE LOST REALMS",titleC);
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
     GUI.DrawTexture(new Rect(770,62,22,22),iconCoin,ScaleMode.ScaleToFit);
     Text(800,62,80,24,$"{Save.coins}",small);
     GUI.DrawTexture(new Rect(900,62,22,22),iconGem,ScaleMode.ScaleToFit);
     Text(930,62,80,24,$"{Save.gems}",small);
     GUI.DrawTexture(new Rect(1030,62,22,22),iconStar,ScaleMode.ScaleToFit);
     Text(1060,62,110,24,$"{TotalStars()}/45 Stars",small);
    }else{
     Text(740,62,440,24,$"Treasury: {Save.coins} Gold  •  {Save.gems} Gems  •  {TotalStars()}/45 Stars",small);
    }

    if(dividerLine)GUI.DrawTexture(new Rect(80,128,1120,16),dividerLine,ScaleMode.StretchToFill);
    else Box(new Rect(80,135,1120,2),new Color(Accent.r,Accent.g,Accent.b,.35f));

    bool prevEnabled=GUI.enabled;
    for(int i=0;i<15;i++){
     float x=80+(i%5)*228,y=150+(i/5)*150;
     bool unlocked=(Application.isEditor&&Array.IndexOf(Environment.GetCommandLineArgs(),"-menuVisualProbe")<0)||i+1<=Save.unlocked;
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
      Text(x+14,y+31,188,20,$"CHAPTER {i+1:00}",tinyC);
      Text(x+14,y+53,188,38,Titles[i],center);
      int stars=Save.stars[i];
      if(stars>0&&iconStar){
       float sw=20f,sp=6f;
       float sx=x+(216-(stars*sw+(stars-1)*sp))*0.5f;
       for(int s=0;s<stars;s++)GUI.DrawTexture(new Rect(sx+s*(sw+sp),y+100,sw,sw),iconStar,ScaleMode.ScaleToFit);
      }else{
       Text(x+14,y+100,188,20,stars>0?new string('★',stars):"—",tinyC);
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

    Text(240,138,800,24,$"Available:  {Save.coins} Gold   •   {Save.gems} Gems    |    * Recommended",smallC);

    // Recommended next upgrade (cheapest unmaxed track)
    int bestRecTrack = Save.healthRank<3 ? 0 : Save.arsenalRank<3 ? 1 : Save.powerRank<3 ? 2 : Save.aetherRank<3 ? 3 : Save.moxieRank<3 ? 4 : Save.tempoRank<3 ? 5 : Save.windRank<3 ? 6 : -1;
    string recTag(int track)=>track==bestRecTrack?" *":"";

    // Primary Upgrades (Gold) & Wardrobe
    string vitCost=Save.healthRank<3?(50+Save.healthRank*40)+" Gold":"MAXED";
    if(MenuButton(new Rect(240,170,520,44),$"VITALITY {Save.healthRank}/3  |  +{Save.healthRank} HP  |  {vitCost}{recTag(0)}",smallBtn:true,st:smallC)){
     int cost=50+Save.healthRank*40;
     if(Save.healthRank<3){if(Save.coins>=cost){Save.coins-=cost;Save.healthRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(MenuButton(new Rect(770,170,270,44),"TREASURE VAULT",iconVault,smallBtn:true,st:smallC)){showVault=true;vaultState=VaultState.Browse;vaultReturn=GameScreen.Settings;Sound("power_select");}
    if(showVault){VaultView();return;}

    string arsCost=Save.arsenalRank<3?(80+Save.arsenalRank*60)+" Gold":"MAXED";
    if(MenuButton(new Rect(240,222,520,44),$"ARSENAL {Save.arsenalRank}/3  |  +{Save.arsenalRank*8}% DMG  |  {arsCost}{recTag(1)}",smallBtn:true,st:smallC)){
     int cost=80+Save.arsenalRank*60;
     if(Save.arsenalRank<3){if(Save.coins>=cost){Save.coins-=cost;Save.arsenalRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(MenuButton(new Rect(770,222,270,44),$"ARSENAL ({Save.weapons.Count})",iconArsenal,smallBtn:true,st:smallC)){showArsenal=true;arsenalPage=0;arsenalReturn=GameScreen.Settings;Sound("power_select");}
    if(showArsenal){ArsenalView();return;}

    // Divine Blessings (Gems)
    string powCost=Save.powerRank<3?(3+Save.powerRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(240,274,520,44),$"POWER {Save.powerRank}/3  |  +{Save.powerRank*20}% POWER  |  {powCost}{recTag(2)}",smallBtn:true,st:smallC)){
     int cost=3+Save.powerRank*2;
     if(Save.powerRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.powerRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(MenuButton(new Rect(770,274,270,44),$"WARDROBE ({Save.skins.Count}/{SkinCatalog.Count})",iconWardrobe,smallBtn:true,st:smallC)){showSkins=true;skinPage=0;skinReturn=GameScreen.Settings;Sound("power_select");}
    if(showSkins){WardrobeView();return;}

    string aethCost=Save.aetherRank<3?(4+Save.aetherRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(240,326,255,44),$"AETHER {Save.aetherRank}/3 | +{Save.aetherRank*25}% | {aethCost}",smallBtn:true,st:tinyC)){
     int cost=4+Save.aetherRank*2;
     if(Save.aetherRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.aetherRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    string moxCost=Save.moxieRank<3?(5+Save.moxieRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(505,326,255,44),$"MOXIE {Save.moxieRank}/3 | +{Save.moxieRank} DASH | {moxCost}",smallBtn:true,st:tinyC)){
     int cost=5+Save.moxieRank*2;
     if(Save.moxieRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.moxieRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(MenuButton(new Rect(770,326,270,44),$"BESTIARY ({Codex.Discovered(Save)}/{Codex.Kinds})",iconBestiary,smallBtn:true,st:smallC)){showCodex=true;codexPage=0;codexReturn=GameScreen.Settings;Sound("power_select");}
    if(showCodex){BestiaryView();return;}

    string tempoCost=Save.tempoRank<3?(5+Save.tempoRank*2)+" Gems":"MAXED";
    if(MenuButton(new Rect(240,378,255,44),$"TEMPO {Save.tempoRank}/3 | +{Save.tempoRank*15}% | {tempoCost}",smallBtn:true,st:tinyC)){
     int cost=5+Save.tempoRank*2;
     if(Save.tempoRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.tempoRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    string windCost=Save.windRank<3?(7+Save.windRank*3)+" Gems":"MAXED";
    if(MenuButton(new Rect(505,378,255,44),$"REVIVE {Save.windRank}/3 | {Save.windRank} USES | {windCost}",smallBtn:true,st:tinyC)){
     int cost=7+Save.windRank*3;
     if(Save.windRank<3){if(Save.gems>=cost){Save.gems-=cost;Save.windRank++;Persist();Sound("upgrade");}else Sound("power_fail");}
    }
    if(MenuButton(new Rect(770,378,270,44),$"ACHIEVEMENTS ({Codex.UnlockedCount(Save)}/{Codex.All.Length})",iconAchievements,smallBtn:true,st:smallC)){showAchievements=true;achievementsPage=0;achievementsReturn=GameScreen.Settings;Sound("power_select");}
    if(showAchievements){AchievementsView();return;}

    // Audio & Preferences
    if(MenuButton(new Rect(240,436,260,42),"MUSIC: "+(Save.music?"ENABLED":"MUTED"),smallBtn:true,st:smallC)){Save.music=!Save.music;Persist();}
    if(MenuButton(new Rect(510,436,260,42),"SOUND: "+(Save.sound?"ENABLED":"MUTED"),smallBtn:true,st:smallC)){Save.sound=!Save.sound;Persist();}
    if(MenuButton(new Rect(780,436,260,42),"HAPTICS: "+(Save.haptics?"ON":"OFF"),smallBtn:true,st:smallC)){Save.haptics=!Save.haptics;Persist();}

    if(MenuButton(new Rect(240,486,260,42),"VISUAL FX: "+(Save.postFx?"ENHANCED":"OFF"),smallBtn:true,st:smallC)){Save.postFx=!Save.postFx;Persist();}
    if(MenuButton(new Rect(510,486,260,42),"SCREEN SHAKE: "+(Save.shake?"ENABLED":"OFF"),smallBtn:true,st:smallC)){Save.shake=!Save.shake;Persist();}
    if(MenuButton(new Rect(780,486,260,42),"TARGET LOCK: "+(Save.softLock?"ON":"OFF"),smallBtn:true,st:smallC)){Save.softLock=!Save.softLock;Persist();}

    if(dividerLine)GUI.DrawTexture(new Rect(240,540,800,10),dividerLine,ScaleMode.StretchToFill);

    if(MenuButton(new Rect(480,562,320,48),"BACK",iconBack,smallBtn:true,st:smallC))Screen=GameScreen.Menu;
    return;
   }

   void WardrobeView(){
    if(wardrobePreviewSkin<0)SelectWardrobePreview(Save.equippedSkin);
    Panel(170,32,940,656);
    Text(210,50,860,28,"Wardrobe & Outfits",titleC);
    if(headerOrnament)GUI.DrawTexture(new Rect(440,82,400,12),headerOrnament,ScaleMode.ScaleToFit);
    if(dividerLine)GUI.DrawTexture(new Rect(210,95,860,8),dividerLine,ScaleMode.StretchToFill);
    Text(210,105,860,20,$"Available: {Save.coins} Gold  •  {Save.gems} Gems    |    Tap an outfit to preview",smallC);

    for(int i=0;i<SkinCatalog.Count;i++){
     var def=SkinCatalog.Get(i);
     bool owned=Save.skins!=null&&Save.skins.Contains(i),eq=Save.equippedSkin==i,selected=wardrobePreviewSkin==i;
     float ry=128+i*81;
     MenuCard(new Rect(210,ry,600,76),selected);
     // Preview selection is independent of equipping or spending currency.
     if(GUI.Button(new Rect(218,ry+4,394,68),GUIContent.none,GUIStyle.none)){
      SelectWardrobePreview(i);Sound("power_select");
     }
     Rect slot=new Rect(218,ry+4,68,68);
     BoxOutline(slot,new Color(.03f,.07f,.12f,.95f),selected?MenuGold:MenuBlue,1f);
     var portrait=SkinPortrait(def);
     if(portrait)GUI.DrawTexture(new Rect(slot.x+2,slot.y+2,64,64),portrait,ScaleMode.ScaleToFit,true);
     Text(298,ry+10,310,22,def.Name.ToUpper(),small);
     Text(298,ry+36,310,28,def.Title,tinyC);
     if(eq){Text(626,ry+26,164,24,"EQUIPPED",smallC);}
     else if(owned){
      if(MenuButton(new Rect(626,ry+14,164,48),"EQUIP",smallBtn:true,st:smallC)){
       SelectWardrobePreview(i);EquipSkin(def.Id);
      }
     }else if(MenuButton(new Rect(626,ry+14,164,48),"EPIC CHEST",smallBtn:true,st:tinyC)){
      showVault=true;vaultState=VaultState.Browse;vaultReturn=skinReturn;Sound("power_select");
     }
    }

    var previewDef=SkinCatalog.Get(wardrobePreviewSkin);
    bool previewOwned=Save.skins!=null&&Save.skins.Contains(wardrobePreviewSkin);
    MenuCard(new Rect(826,128,244,480));
    Text(840,140,216,32,previewDef.Name,smallC);
    if(wardrobePreview&&wardrobePreview.Frame&&string.IsNullOrEmpty(wardrobePreview.Error))
     GUI.DrawTexture(new Rect(842,174,212,318),wardrobePreview.Frame,ScaleMode.ScaleToFit,true);
    else Text(842,264,212,40,wardrobePreview?wardrobePreview.Error:"Loading preview",smallC);
    Text(840,494,216,20,Save.equippedSkin==wardrobePreviewSkin?"EQUIPPED":previewOwned?"OUTFIT PREVIEW":"LOCKED OUTFIT PREVIEW",tinyC);
    Text(840,524,216,70,previewDef.StatSummary,tinyC);
    // Full outfit details remain available without compressing the list rows.
    Text(210,616,600,48,previewDef.Description,tinyC);
    if(MenuButton(new Rect(826,620,244,44),skinReturn==GameScreen.Paused?"BACK TO PAUSE":"BACK TO SANCTUARY",iconBack,smallBtn:true,st:smallC)){
     showSkins=false;Sound("power_select");
    }
   }

   void ArsenalView(){
    Panel(200,50,880,625);
    Text(240,70,800,38,$"Arsenal  —  {Save.weapons.Count} Collected",titleC);
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
     MenuCard(new Rect(240,ry,800,50),eq);
     if(GUI.Button(new Rect(240,ry,800,50),GUIContent.none,GUIStyle.none)){
      if(eq)UnequipWeapon();else EquipWeapon((WeaponId)list[idx]);
     }
     // Dedicated illuminated weapon icon slot
     Rect slot=new Rect(252,ry+9,34,34);
     BoxOutline(slot,new Color(.02f,.06f,.10f,.92f),eq?new Color(1f,.85f,.35f):new Color(.38f,.68f,.94f,.6f),1.5f);
     var icon=WeaponIcon(def);
     if(icon)GUI.DrawTexture(new Rect(slot.x+2,slot.y+2,30,30),icon,ScaleMode.ScaleToFit);
     else Text(slot.x,slot.y+8,42,24,"⚔",smallC);

     // Name & Stats
     Text(302,ry+12,530,18,def.Name.ToUpper(),center);
     Text(302,ry+31,530,16,$"Damage ×{def.Damage:0.00}   •   Reach ×{def.Reach:0.00}   •   Speed ×{def.Tempo:0.00}",tinyC);

     // Equip status badge on right
     if(eq){
      GUI.color=new Color(1f,.9f,.35f);
      Text(852,ry+15,166,22,"✓ EQUIPPED",smallC);
      GUI.color=Color.white;
     }else{
      Text(852,ry+15,166,22,"EQUIP",tinyC);
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
   Panel(320,80,640,560);

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
    if(MenuButton(new Rect(355,262,570,58),"RESUME JOURNEY",iconResume,primary:true))Resume();
    if(MenuButton(new Rect(355,332,280,46),"ARSENAL",iconArsenal)){showArsenal=true;arsenalPage=0;arsenalReturn=GameScreen.Paused;Sound("power_select");}
    if(MenuButton(new Rect(645,332,280,46),"WARDROBE",iconWardrobe,smallBtn:true,st:smallC)){showSkins=true;skinPage=0;skinReturn=GameScreen.Paused;Sound("power_select");}
    if(showSkins){WardrobeView();return;}
    if(MenuButton(new Rect(355,390,570,46),"TREASURE VAULT & CHESTS",iconVault,smallBtn:true,st:smallC)){showVault=true;vaultState=VaultState.Browse;vaultReturn=GameScreen.Paused;Sound("power_select");}
    if(showVault){VaultView();return;}
    if(MenuButton(new Rect(355,448,280,46),$"BESTIARY ({Codex.Discovered(Save)}/{Codex.Kinds})",iconBestiary,smallBtn:true,st:smallC)){showCodex=true;codexPage=0;codexReturn=GameScreen.Paused;Sound("power_select");}
    if(showCodex){BestiaryView();return;}
    if(MenuButton(new Rect(645,448,280,46),$"ACHIEVEMENTS ({Codex.UnlockedCount(Save)}/{Codex.All.Length})",iconAchievements,smallBtn:true,st:smallC)){showAchievements=true;achievementsPage=0;achievementsReturn=GameScreen.Paused;Sound("power_select");}
    if(showAchievements){AchievementsView();return;}
    if(MenuButton(new Rect(355,506,280,46),"ATLAS",iconAtlas))Screen=GameScreen.Map;
    if(MenuButton(new Rect(645,506,280,46),"RESTART",iconRestart))LoadLevel(Level);
    if(MenuButton(new Rect(355,564,570,46),"MAIN MENU",iconMainMenu)){showSkins=false;showArsenal=false;showCodex=false;showAchievements=false;showVault=false;Audio.Suspend(false);Time.timeScale=1;Screen=GameScreen.Menu;}
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
     var actionStyle=new GUIStyle(center){font=fantasyButton,fontSize=primary?23:14,wordWrap=false,alignment=TextAnchor.MiddleCenter};
     actionStyle.normal.textColor=new Color(1f,.94f,.79f);
     Rect nameRect=new Rect(drawRect.x+drawRect.width*.1f,drawRect.y+drawRect.height*.59f,drawRect.width*.8f,drawRect.height*.18f);
     GUI.Label(nameRect,name,FitText(actionStyle,nameRect,name));
     if(sub.Length>0){
      // Keep the whole hint above the bottom jewel and within the circle's
      // narrower lower chord, including when the pressed artwork shrinks.
      var hintStyle=new GUIStyle(tinyC){fontSize=primary?11:9,wordWrap=false,alignment=TextAnchor.MiddleCenter,clipping=TextClipping.Clip,padding=new RectOffset(0,0,0,0)};
      Rect hintRect=new Rect(drawRect.x+drawRect.width*.22f,drawRect.y+drawRect.height*.77f,drawRect.width*.56f,drawRect.height*.11f);
      var hintContent=new GUIContent(sub);
      while(hintStyle.fontSize>8&&hintStyle.CalcSize(hintContent).x>hintRect.width)hintStyle.fontSize--;
      GUI.Label(hintRect,hintContent,hintStyle);
     }
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
   Texture2D WeaponIcon(WeaponDefinition def){
    if(string.IsNullOrEmpty(def.Resource))return null;
    string key=def.Resource.Substring(def.Resource.LastIndexOf('/')+1);
    if(iconCache.TryGetValue(key,out var tex))return tex;
    tex=Resources.Load<Texture2D>("Weapons/Icons/"+key);
    iconCache[key]=tex;return tex;
   }
   Texture2D SkinPortrait(SkinDefinition def){
    string key="skin:"+def.PrefabName;
    if(iconCache.TryGetValue(key,out var tex))return tex;
    string path=def.Id==SkinId.Wanderer?"UI/Portraits/UI_Avatar_Aster_HP_Headshot":"Characters/Icons/"+def.PrefabName;
    tex=Resources.Load<Texture2D>(path);
    iconCache[key]=tex;return tex;
   }

  int TotalStars(){int n=0;foreach(int s in Save.stars)n+=s;return n;}

   void BestiaryView(){
    Panel(180,36,920,648);
    Text(220,54,840,28,$"Bestiary  —  {Codex.Discovered(Save)}/{Codex.Kinds} Discovered",titleC);
    if(headerOrnament)GUI.DrawTexture(new Rect(440,84,400,12),headerOrnament,ScaleMode.ScaleToFit);
    if(dividerLine)GUI.DrawTexture(new Rect(220,102,840,6),dividerLine,ScaleMode.StretchToFill);
    else Box(new Rect(220,98,840,2),new Color(Accent.r,Accent.g,Accent.b,.35f));

    Text(220,112,840,18,$"Total Slain: {Codex.TotalKills(Save)}    |    Guardians Felled: {Save.bossKills}",smallC);

    int perPage=5;
    int pages=(Codex.Kinds+perPage-1)/perPage;
    codexPage=Mathf.Clamp(codexPage,0,pages-1);

    for(int i=0;i<perPage;i++){
     int idx=codexPage*perPage+i;if(idx>=Codex.Kinds)break;
     var entry=Codex.Entries[idx];
     int kills=Save.bestiary!=null&&idx<Save.bestiary.Length?Save.bestiary[idx]:0;
     bool seen=kills>0;
     bool boss=Codex.IsBoss(idx);
     float ry=134+i*90;

     Color border=boss?(seen?new Color(1f,.85f,.35f,.9f):new Color(.6f,.5f,.2f,.6f)):(seen?new Color(.38f,.68f,.94f,.7f):new Color(.3f,.35f,.42f,.6f));
     MenuCard(new Rect(220,ry,840,84),boss&&seen);

     Rect slot=new Rect(228,ry+6,72,72);
     BoxOutline(slot,new Color(.03f,.07f,.12f,.95f),border,1.5f);
     var badge=boss?iconAchievements:seen?iconBestiary:iconLock;
     if(badge)GUI.DrawTexture(new Rect(slot.x+16,slot.y+16,40,40),badge,ScaleMode.ScaleToFit);

     if(seen){
      GUI.color=boss?new Color(1f,.92f,.45f):Color.white;
      Text(312,ry+12,430,18,entry.Name.ToUpper()+(boss?"  [GUARDIAN]":""),small);
      GUI.color=new Color(.38f,.68f,.94f);
      Text(312,ry+32,430,16,$"Habitat: {entry.Habitat}    •    Slain: {kills}",tinyC);
      GUI.color=Color.white;
      Text(312,ry+50,430,28,entry.Behavior,tinyC);

      // Weakness badge on the right
      int weakElem=Enemy.WeaknessOf(idx);
      Color elemCol=weakElem==0?new Color(1f,.55f,.2f):weakElem==1?new Color(.35f,.85f,1f):new Color(.4f,1f,.6f);
      GUI.color=elemCol;
      Box(new Rect(750,ry+24,140,32),new Color(elemCol.r*.25f,elemCol.g*.25f,elemCol.b*.25f,.8f));
      BoxOutline(new Rect(750,ry+24,140,32),new Color(0,0,0,0),elemCol,1.2f);
      Text(750,ry+30,140,20,$"WEAK: {Codex.Weakness(idx).ToUpper()}",smallC);
      GUI.color=Color.white;
     }else{
      GUI.color=new Color(.6f,.65f,.7f);
      Text(312,ry+12,520,18,"???  [UNDISCOVERED]",small);
      Text(312,ry+32,520,16,$"Habitat: {entry.Habitat}",tinyC);
      GUI.color=new Color(.5f,.55f,.6f);
      Text(312,ry+50,520,28,"Encounter and defeat this creature in the realms to unlock lore and elemental weaknesses.",tinyC);
      GUI.color=Color.white;
     }
    }

    if(pages>1){
     if(codexPage>0&&MenuButton(new Rect(220,600,160,44),"PREV",iconBack,smallBtn:true,st:smallC)){codexPage--;Sound("power_select");}
     Text(420,600,240,44,$"PAGE {codexPage+1}/{pages}",smallC);
     if(codexPage<pages-1&&MenuButton(new Rect(680,600,160,44),"NEXT",iconContinue,smallBtn:true,st:smallC)){codexPage++;Sound("power_select");}
    }

    if(MenuButton(new Rect(860,600,200,44),codexReturn==GameScreen.Paused?"PAUSE":"SANCTUARY",iconBack,smallBtn:true,st:smallC)){
     showCodex=false;Sound("power_select");
    }
   }

   void AchievementsView(){
    Panel(180,36,920,648);
    int unlocked=Codex.UnlockedCount(Save);
    Text(220,54,840,28,$"Achievements  —  {unlocked}/{Codex.All.Length} Complete",titleC);
    if(headerOrnament)GUI.DrawTexture(new Rect(440,84,400,12),headerOrnament,ScaleMode.ScaleToFit);
    if(dividerLine)GUI.DrawTexture(new Rect(220,102,840,6),dividerLine,ScaleMode.StretchToFill);
    else Box(new Rect(220,98,840,2),new Color(Accent.r,Accent.g,Accent.b,.35f));

    Text(220,112,840,18,"Accomplish milestones to earn Gold and Gems for your journey.",smallC);

    int perPage=5;
    int pages=(Codex.All.Length+perPage-1)/perPage;
    achievementsPage=Mathf.Clamp(achievementsPage,0,pages-1);

    for(int i=0;i<perPage;i++){
     int idx=achievementsPage*perPage+i;if(idx>=Codex.All.Length)break;
     var ach=Codex.All[idx];
     bool done=Save.achievements!=null&&Save.achievements.Contains(ach.Id);
     int cur=ach.Value(Save);
     float ry=134+i*90;

     Color border=done?new Color(1f,.85f,.35f,.9f):new Color(.35f,.4f,.5f,.6f);
     MenuCard(new Rect(220,ry,840,84),done);

     Rect slot=new Rect(228,ry+6,72,72);
     BoxOutline(slot,new Color(.03f,.07f,.12f,.95f),border,1.5f);
     Texture2D achievementIcon=done?iconAchievements:iconStar;
     GUI.color=done?new Color(1f,.9f,.35f):new Color(.6f,.65f,.7f);
     if(achievementIcon)GUI.DrawTexture(new Rect(slot.x+16,slot.y+16,40,40),achievementIcon,ScaleMode.ScaleToFit);
     GUI.color=Color.white;

     Text(312,ry+12,430,18,ach.Title.ToUpper(),small);
     GUI.color=new Color(.75f,.8f,.85f);
     Text(312,ry+32,430,18,ach.Description,tinyC);
     GUI.color=Color.white;

     // Progress bar
     float pct=Mathf.Clamp01((float)cur/Mathf.Max(1,ach.Target));
     Rect barBg=new Rect(312,ry+58,320,10);
     Box(barBg,new Color(.08f,.12f,.18f,.9f));
     if(pct>0f)Box(new Rect(barBg.x,barBg.y,barBg.width*pct,barBg.height),done?new Color(1f,.85f,.3f):new Color(.38f,.68f,.94f));
     BoxOutline(barBg,new Color(0,0,0,0),new Color(.3f,.35f,.45f,.6f),1f);
     Text(642,ry+54,90,18,$"{cur}/{ach.Target}",tinyC);

     // Status / Rewards on right
     if(done){
      GUI.color=new Color(1f,.9f,.35f);
      Box(new Rect(750,ry+24,140,32),new Color(.25f,.20f,.05f,.8f));
      BoxOutline(new Rect(750,ry+24,140,32),new Color(0,0,0,0),new Color(1f,.85f,.35f),1.5f);
      Text(750,ry+30,140,20,"✓ COMPLETED",smallC);
      GUI.color=Color.white;
     }else{
      string rew=(ach.RewardGold>0?$"{ach.RewardGold} Gold ":"")+(ach.RewardGems>0?$"{ach.RewardGems} Gems":"");
      GUI.color=new Color(.4f,.9f,1f);
      Text(750,ry+30,140,20,rew,smallC);
      GUI.color=Color.white;
     }
    }

    if(pages>1){
     if(achievementsPage>0&&MenuButton(new Rect(220,600,160,44),"PREV",iconBack,smallBtn:true,st:smallC)){achievementsPage--;Sound("power_select");}
     Text(420,600,240,44,$"PAGE {achievementsPage+1}/{pages}",smallC);
     if(achievementsPage<pages-1&&MenuButton(new Rect(680,600,160,44),"NEXT",iconContinue,smallBtn:true,st:smallC)){achievementsPage++;Sound("power_select");}
    }

    if(MenuButton(new Rect(860,600,200,44),achievementsReturn==GameScreen.Paused?"PAUSE":"SANCTUARY",iconBack,smallBtn:true,st:smallC)){
     showAchievements=false;Sound("power_select");
    }
   }


   void VaultView(){
    Panel(160,36,960,648);
    Text(200,54,880,28,"Treasure Vault & Chests",titleC);
    if(headerOrnament)GUI.DrawTexture(new Rect(440,84,400,12),headerOrnament,ScaleMode.ScaleToFit);
    if(dividerLine)GUI.DrawTexture(new Rect(200,102,880,6),dividerLine,ScaleMode.StretchToFill);
    else Box(new Rect(200,98,880,2),new Color(Accent.r,Accent.g,Accent.b,.35f));

    Text(200,112,880,18,$"Treasury:  {Save.coins} Gold    •    {Save.gems} Gems    |    Weapons: {Save.weapons.Count}/{WeaponCatalog.MaxId+1}    •    Outfits: {Save.skins.Count}/6",smallC);

    if(showDropRates){
     Rect modal=new Rect(320,130,640,460);
     BoxOutline(modal,new Color(.04f,.08f,.12f,.98f),Accent,2f);
     Text(340,146,600,28,"CHEST PROBABILITIES & RULES",titleC);
     Text(360,186,560,18,"• NORMAL CHEST (150 Gold or Free via Ad):",small);
     Text(380,208,540,18,"- 65% Common Resources (50-150 Gold, 1-2 Gems)",tinyC);
     Text(380,226,540,18,"- 35% Standard Weapons (Non-Special)",tinyC);

     Text(360,256,560,18,"• GOLDEN CHEST (600 Gold or 10 Gems):",small);
     Text(380,278,540,18,"- 30% Rich Resources (200-450 Gold, 3-6 Gems)",tinyC);
     Text(380,296,540,18,"- 50% Standard Weapons",tinyC);
     Text(380,314,540,18,"- 20% RARE SPECIAL-HOLD WEAPONS (12 Elemental/Charged weapons)",tinyC);

     Text(360,344,560,18,"• EPIC CHEST (35 Gems):",small);
     Text(380,366,540,18,"- 15% Stash (500-1000 Gold, 8-15 Gems)",tinyC);
     Text(380,384,540,18,"- 35% Standard Weapons",tinyC);
     Text(380,402,540,18,"- 35% RARE SPECIAL-HOLD WEAPONS",tinyC);
     Text(380,420,540,18,"- 15% EPIC CHARACTER OUTFITS (5 Unlockable Skins)",tinyC);

     Text(360,452,560,36,"★ DUPLICATE PROTECTION: Already owned weapons & outfits automatically convert into compensatory Gold and Gems so no pull is ever wasted.",tinyC);

     if(MenuButton(new Rect(540,514,200,44),"CLOSE",iconClose,smallBtn:true,st:smallC)){showDropRates=false;Sound("power_select");}
     return;
    }

    if(vaultState==VaultState.Opening){
     Rect box=new Rect(420,200,440,280);
     float pulse=1f+.04f*Mathf.Sin(Time.unscaledTime*12f);
     BoxOutline(box,new Color(.03f,.07f,.12f,.96f),Color.Lerp(Accent,Color.white,.4f*pulse),2.5f);
     Text(440,240,400,36,"UNLOCKING CHEST...",titleC);
     float pct=Mathf.Clamp01(vaultAnimTimer/0.7f);
     Rect barBg=new Rect(480,300,320,16);
     Box(barBg,new Color(.08f,.12f,.18f,.9f));
     Box(new Rect(barBg.x,barBg.y,barBg.width*pct,barBg.height),new Color(1f,.85f,.3f));
     BoxOutline(barBg,Color.clear,Accent,1f);
     Text(440,336,400,24,"Gathering the realm's treasures...",tinyC);
     return;
    }

    if(vaultState==VaultState.Revealed&&currentChestReward!=null){
     Rect rev=new Rect(340,130,600,460);
     Color rBorder=currentChestReward.Rarity==RewardRarity.Epic?new Color(.82f,.45f,1f):currentChestReward.Rarity==RewardRarity.Rare?new Color(1f,.85f,.35f):new Color(.38f,.68f,.94f);
     BoxOutline(rev,new Color(.03f,.06f,.10f,.98f),rBorder,2.5f);

     string rarTag=currentChestReward.Rarity==RewardRarity.Epic?"✦ EPIC REWARD ✦":currentChestReward.Rarity==RewardRarity.Rare?"★ RARE REWARD ★":"• COMMON REWARD •";
     GUI.color=rBorder;
     Text(360,146,560,26,rarTag,smallC);
     GUI.color=Color.white;

     Rect iconBox=new Rect(580,182,120,120);
     BoxOutline(iconBox,new Color(.05f,.10f,.16f,.9f),rBorder,1.5f);
     DrawRareRewardReveal(new Vector2(640,242));
     if(currentChestReward.Type==ChestRewardType.Weapon){
      var wdef=WeaponCatalog.Get(currentChestReward.Weapon);
      var wicon=WeaponIcon(wdef);
      if(wicon)GUI.DrawTexture(new Rect(iconBox.x+10,iconBox.y+10,100,100),wicon,ScaleMode.ScaleToFit);
      else Text(iconBox.x,iconBox.y+42,120,36,"⚔",big);
     }else if(currentChestReward.Type==ChestRewardType.Skin){
      var sdef=SkinCatalog.Get((int)currentChestReward.Skin);
      var sportrait=SkinPortrait(sdef);
      if(sportrait)GUI.DrawTexture(new Rect(iconBox.x+6,iconBox.y+6,108,108),sportrait,ScaleMode.ScaleToFit);
      else Text(iconBox.x,iconBox.y+42,120,36,"👑",big);
     }else{
      if(iconGem&&currentChestReward.Gems>0)GUI.DrawTexture(new Rect(iconBox.x+36,iconBox.y+36,48,48),iconGem,ScaleMode.ScaleToFit);
      else if(iconCoin)GUI.DrawTexture(new Rect(iconBox.x+36,iconBox.y+36,48,48),iconCoin,ScaleMode.ScaleToFit);
      else Text(iconBox.x,iconBox.y+42,120,36,"💎",big);
     }

     Text(360,314,560,28,currentChestReward.Title.ToUpper(),center);
     GUI.color=new Color(.8f,.88f,.95f);
     Text(360,344,560,24,currentChestReward.Subtitle,smallC);
     GUI.color=Color.white;

     if(currentChestReward.IsDuplicate){
      GUI.color=new Color(1f,.85f,.35f);
      Text(360,378,560,22,$"DUPLICATE CONVERTED: +{currentChestReward.Gold} Gold  +{currentChestReward.Gems} Gems added to treasury!",tinyC);
      GUI.color=Color.white;
     }

     if(!currentChestReward.IsDuplicate&&currentChestReward.Type==ChestRewardType.Weapon){
      if(MenuButton(new Rect(420,418,440,44),$"EQUIP {currentChestReward.Title.ToUpper()} NOW",iconArsenal,smallBtn:true,st:smallC)){
       EquipWeapon(currentChestReward.Weapon);vaultState=VaultState.Browse;Sound("power_select");
      }
     }else if(!currentChestReward.IsDuplicate&&currentChestReward.Type==ChestRewardType.Skin){
      if(MenuButton(new Rect(420,418,440,44),$"EQUIP {currentChestReward.Title.ToUpper()} NOW",smallBtn:true,st:smallC)){
       EquipSkin(currentChestReward.Skin);vaultState=VaultState.Browse;Sound("power_select");
      }
     }

     if(MenuButton(new Rect(500,476,280,44),"CONTINUE",iconContinue,smallBtn:true,st:smallC)){
      vaultState=VaultState.Browse;Sound("power_select");
     }
     return;
    }

    float cy=136f,cw=284f,ch=440f;

    // Card 1: Normal Chest
    Rect c1=new Rect(200,cy,cw,ch);
    BoxOutline(c1,new Color(.03f,.07f,.11f,.92f),new Color(.55f,.65f,.75f,.8f),1.5f);
    Text(c1.x+10,c1.y+14,cw-20,24,"NORMAL CHEST",center);
    Text(c1.x+10,c1.y+38,cw-20,18,"Iron & Bronze Cache",tinyC);
    DrawVaultChest(c1,chestNormal);
    Text(c1.x+14,c1.y+166,cw-28,20,"• 65% Common Resources",tinyC);
    Text(c1.x+14,c1.y+188,cw-28,20,"• 35% Standard Weapons",tinyC);
    Text(c1.x+14,c1.y+210,cw-28,20,"• Duplicate conversion refunds",tinyC);

    bool canNorm=ChestSystem.CanOpen(ChestType.Normal,Save);
    GUI.color=canNorm?Color.white:new Color(1f,.45f,.45f);
    if(MenuButton(new Rect(c1.x+22,c1.y+260,cw-44,46),$"OPEN: {ChestSystem.NormalGoldCost} GOLD",smallBtn:true,st:smallC)){
     if(canNorm){ChestSystem.DeductCost(ChestType.Normal,Save);StartOpeningChest(ChestType.Normal);}
     else Sound("power_fail");
    }
    GUI.color=new Color(.38f,.68f,.94f);
    if(MenuButton(new Rect(c1.x+22,c1.y+316,cw-44,46),"WATCH AD (FREE)",iconContinue,smallBtn:true,st:smallC)){
     RewardedAdManager.ShowRewardedAd(()=>{StartOpeningChest(ChestType.Normal);});
    }
    GUI.color=Color.white;

    // Card 2: Golden Chest
    Rect c2=new Rect(500,cy,cw,ch);
    BoxOutline(c2,new Color(.07f,.05f,.02f,.92f),new Color(1f,.82f,.3f,.95f),1.8f);
    GUI.color=new Color(1f,.9f,.45f);
    Text(c2.x+10,c2.y+14,cw-20,24,"GOLDEN CHEST",center);
    GUI.color=Color.white;
    Text(c2.x+10,c2.y+38,cw-20,18,"Gilded Sunscar Vault",tinyC);
    DrawVaultChest(c2,chestGolden);
    Text(c2.x+14,c2.y+166,cw-28,20,"• 30% Rich Resources",tinyC);
    Text(c2.x+14,c2.y+188,cw-28,20,"• 50% Standard Weapons",tinyC);
    GUI.color=new Color(1f,.88f,.45f);
    Text(c2.x+14,c2.y+210,cw-28,20,"• 20% RARE SPECIAL WEAPONS",tinyC);
    GUI.color=Color.white;

    bool canGoldCoins=ChestSystem.CanOpen(ChestType.Golden,Save,false);
    GUI.color=canGoldCoins?Color.white:new Color(1f,.45f,.45f);
    if(MenuButton(new Rect(c2.x+22,c2.y+260,cw-44,46),$"OPEN: {ChestSystem.GoldenGoldCost} GOLD",smallBtn:true,st:smallC)){
     if(canGoldCoins){ChestSystem.DeductCost(ChestType.Golden,Save,false);StartOpeningChest(ChestType.Golden);}
     else Sound("power_fail");
    }
    bool canGoldGems=ChestSystem.CanOpen(ChestType.Golden,Save,true);
    GUI.color=canGoldGems?Color.white:new Color(1f,.45f,.45f);
    if(MenuButton(new Rect(c2.x+22,c2.y+316,cw-44,46),$"OPEN: {ChestSystem.GoldenGemCost} GEMS",smallBtn:true,st:smallC)){
     if(canGoldGems){ChestSystem.DeductCost(ChestType.Golden,Save,true);StartOpeningChest(ChestType.Golden);}
     else Sound("power_fail");
    }
    GUI.color=Color.white;

    // Card 3: Epic Chest
    Rect c3=new Rect(800,cy,cw,ch);
    BoxOutline(c3,new Color(.08f,.03f,.12f,.94f),new Color(.82f,.45f,1f,.95f),2f);
    GUI.color=new Color(.88f,.6f,1f);
    Text(c3.x+10,c3.y+14,cw-20,24,"EPIC CHEST",center);
    GUI.color=Color.white;
    Text(c3.x+10,c3.y+38,cw-20,18,"Astral Mythic Coffer",tinyC);
    DrawVaultChest(c3,chestEpic);
    Text(c3.x+14,c3.y+166,cw-28,20,"• 35% Special-Hold Weapons",tinyC);
    GUI.color=new Color(.88f,.6f,1f);
    Text(c3.x+14,c3.y+188,cw-28,20,"• 15% EPIC CHARACTER OUTFITS",tinyC);
    GUI.color=Color.white;
    Text(c3.x+14,c3.y+210,cw-28,20,"• 35% Standard Weapons / 15% Stash",tinyC);

    bool canEpic=ChestSystem.CanOpen(ChestType.Epic,Save);
    GUI.color=canEpic?Color.white:new Color(1f,.45f,.45f);
    if(MenuButton(new Rect(c3.x+22,c3.y+288,cw-44,52),$"OPEN: {ChestSystem.EpicGemCost} GEMS",smallBtn:true,st:smallC)){
     if(canEpic){ChestSystem.DeductCost(ChestType.Epic,Save);StartOpeningChest(ChestType.Epic);}
     else Sound("power_fail");
    }
    GUI.color=Color.white;

    if(MenuButton(new Rect(200,594,220,44),"PROBABILITIES",smallBtn:true,st:smallC)){showDropRates=true;Sound("power_select");}
    if(MenuButton(new Rect(864,594,220,44),vaultReturn==GameScreen.Paused?"PAUSE":"SANCTUARY",iconBack,smallBtn:true,st:smallC)){showVault=false;Sound("power_select");}
   }

   void DrawVaultChest(Rect card,Texture2D artwork){
    // All three tiers share one art area, scale and baseline.
    Rect artArea=new Rect(card.x+42,card.y+62,200,96);
    if(artwork)GUI.DrawTexture(artArea,artwork,ScaleMode.ScaleToFit);
    else if(iconVault)GUI.DrawTexture(artArea,iconVault,ScaleMode.ScaleToFit);
   }

   // Animate generated artwork in menu coordinates; unscaled time also works while paused.
   bool HasRareItemReveal()=>currentChestReward!=null&&!currentChestReward.IsDuplicate
    &&currentChestReward.Type!=ChestRewardType.Resources&&currentChestReward.Rarity>=RewardRarity.Rare;
   void DrawRareRewardReveal(Vector2 origin){
    if(!HasRareItemReveal()||!rewardHalo)return;
    float age=Mathf.Max(0f,Time.unscaledTime-rewardRevealStarted);
    bool epic=currentChestReward.Rarity==RewardRarity.Epic;
    float duration=epic?2.8f:2.1f;
    if(age>=duration)return;
    float phase=Mathf.Clamp01(age/duration);
    float fade=Mathf.SmoothStep(0f,1f,age/.16f)*(1f-Mathf.SmoothStep(.55f,1f,phase));
    Color priorColor=GUI.color;Matrix4x4 priorMatrix=GUI.matrix;
    Color tint=epic?new Color(.80f,.53f,1f):new Color(1f,.83f,.37f);
    // Wide, shallow halo keeps the burst clear of the title and reward buttons.
    float size=Mathf.Lerp(150f,epic?230f:210f,1f-Mathf.Pow(1f-phase,3f));
    GUI.color=new Color(tint.r,tint.g,tint.b,fade*.95f);
    GUI.DrawTexture(new Rect(origin.x-size/2,origin.y-64,size,128),rewardHalo,ScaleMode.StretchToFill);
    if(iconStar){
     int count=epic?16:10;
     for(int i=0;i<count;i++){
      float angle=(i*360f/count+age*(epic?22f:12f))*Mathf.Deg2Rad;
      float radius=76f+phase*(epic?48f:32f);
      Vector2 point=origin+new Vector2(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius*.47f);
      float sparkle=(i%3==0?12f:8f)*(1f-phase*.45f);
      GUI.color=new Color(1f,epic?.8f:.94f,epic?1f:.62f,fade);
      GUIUtility.RotateAroundPivot(age*45f+i*24f,point);
      GUI.DrawTexture(new Rect(point.x-sparkle/2,point.y-sparkle/2,sparkle,sparkle),iconStar,ScaleMode.ScaleToFit);
      GUI.matrix=priorMatrix;
     }
    }
    GUI.matrix=priorMatrix;GUI.color=priorColor;
   }

   void StartOpeningChest(ChestType type){
    openingChestType=type;
    vaultState=VaultState.Opening;
    vaultAnimTimer=0f;
    currentChestReward=ChestSystem.Open(type,Save);
    Codex.Dirty=true;
    Persist();
    Sound("power_select");
   }

 }
}
