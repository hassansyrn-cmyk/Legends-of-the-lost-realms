using System;
using System.Collections.Generic;
using UnityEngine;
namespace LostRealms {
 public enum GameScreen { Menu, Map, Playing, Paused, Complete, Defeated, Settings }
 [Serializable] public class Progress {
  public int unlocked=1, coins, gems, healthRank, powerRank; public int[] stars=new int[10]; public float[] best=new float[10]; public bool music=true,sound=true;
 }
 public class RealmGame : MonoBehaviour {
  public static RealmGame I; public static bool Testing=>Array.IndexOf(Environment.GetCommandLineArgs(),"-realmTest")>=0; public static readonly string[] Titles={"Mosslight Trail","Whispering Falls","Rootbound Ruins","The Elder Grove","Sunscorched Pass","Temple of Keys","Sandstone Colossus","Frostwind Climb","Crystal Hollow","Crown of Winter"};
  public static readonly string[] Realms={"VERDANT KINGDOM","BURNING DUNES","FROZEN PEAKS"};
  public static readonly Color[] Accents={new Color(.38f,.95f,.7f),new Color(1,.67f,.28f),new Color(.4f,.83f,1)};
  public GameScreen Screen=GameScreen.Menu; public Progress Save=new Progress(); public Hero Player; public RealmWorld World; public FollowCamera CameraRig;
  public int Level=1, Realm, Coins, Gems, DamageTaken, EarnedStars; public float Elapsed; public Vector3 Checkpoint; public bool CheckpointActive; public string Notice=""; float noticeUntil; public Color Accent=>Accents[Realm];
  public Vector2 MoveInput; public bool JumpPressed,DashPressed,AttackPressed,AttackReleased,CastPressed; public bool AttackHeld;
  public readonly List<Enemy> Enemies=new List<Enemy>(); public AudioSource Music,Sfx;
  Transform worldRoot; GUIStyle title,label,small,button; Texture2D pixel; Vector2 joyOrigin; int joyFinger=-1, camFinger=-1; float yawInput;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Boot(){if(FindAnyObjectByType<RealmGame>()==null)new GameObject("Lost Realms 3D").AddComponent<RealmGame>();}
  void Awake(){ I=this; Application.targetFrameRate=60; QualitySettings.vSyncCount=1; UnityEngine.Screen.orientation=ScreenOrientation.LandscapeLeft;
   try{if(!Testing&&PlayerPrefs.HasKey("LostRealms3D.v1"))Save=JsonUtility.FromJson<Progress>(PlayerPrefs.GetString("LostRealms3D.v1"))??new Progress();}catch{Save=new Progress();}
   if(Save.stars==null||Save.stars.Length!=10)Save.stars=new int[10]; if(Save.best==null||Save.best.Length!=10)Save.best=new float[10]; Save.unlocked=Mathf.Clamp(Save.unlocked,1,10);
   Music=gameObject.AddComponent<AudioSource>(); Music.loop=true; Music.volume=.24f; Sfx=gameObject.AddComponent<AudioSource>(); Sfx.volume=.7f;
   var cam=new GameObject("Adventure Camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.gameObject.AddComponent<AudioListener>(); cam.nearClipPlane=.25f; cam.farClipPlane=320; cam.fieldOfView=58; CameraRig=cam.gameObject.AddComponent<FollowCamera>();
   LoadLevel(1); Screen=GameScreen.Menu; Tell("The Heart of Realms is waiting.",4);
  }
  public void Persist(){if(Testing)return;PlayerPrefs.SetString("LostRealms3D.v1",JsonUtility.ToJson(Save));PlayerPrefs.Save();}
  public void LoadLevel(int id){
   Time.timeScale=1; if(worldRoot){worldRoot.gameObject.SetActive(false);Destroy(worldRoot.gameObject);} Enemies.Clear(); Level=Mathf.Clamp(id,1,10); Realm=Level<=4?0:Level<=7?1:2; Coins=Gems=DamageTaken=EarnedStars=0; Elapsed=0; CheckpointActive=false;
   worldRoot=new GameObject("Realm "+Level+" - "+Titles[Level-1]).transform; World=worldRoot.gameObject.AddComponent<RealmWorld>(); World.Build(Level,Realm);
   var hero=new GameObject("Aster"); hero.transform.SetParent(worldRoot); hero.transform.position=World.Spawn; Player=hero.AddComponent<Hero>(); Checkpoint=World.Spawn; CameraRig.Target=Player.transform; CameraRig.Snap();
   var clip=Resources.Load<AudioClip>("Audio/"+(World.IsBoss?"boss_battle_theme":Realm==0?"verdant_theme":Realm==1?"desert_exploration_theme":"frozen_exploration_theme"));Music.clip=clip;if(clip&&Save.music)Music.Play();
   Screen=GameScreen.Playing; Tell(Level==1?"WASD / left stick to move. Jump twice to reach the next island.":World.IsBoss?"Break the guardian's corruption. Dodge red warnings, strike during recovery.":Titles[Level-1]+"  /  Follow the golden trail to the realm gate.",6);
  }
  void Update(){
   JumpPressed=DashPressed=AttackPressed=AttackReleased=CastPressed=false; MoveInput=Vector2.zero; yawInput=0;
   if(Input.GetKeyDown(KeyCode.Escape)){if(Screen==GameScreen.Playing)Pause();else if(Screen==GameScreen.Paused)Resume();else Screen=GameScreen.Menu;}
   if(Screen!=GameScreen.Playing){AttackHeld=false;return;} Elapsed+=Time.deltaTime;
   MoveInput=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0));
   JumpPressed=Input.GetKeyDown(KeyCode.Space);DashPressed=Input.GetKeyDown(KeyCode.LeftShift);AttackPressed=Input.GetKeyDown(KeyCode.J);AttackReleased=Input.GetKeyUp(KeyCode.J);AttackHeld=Input.GetKey(KeyCode.J);CastPressed=Input.GetKeyDown(KeyCode.K);
   if(Input.GetKeyDown(KeyCode.Q))Player.Power=(Player.Power+1)%3;
   if(Input.GetMouseButton(1))yawInput=Input.GetAxis("Mouse X")*3;
   float sx=1280f/UnityEngine.Screen.width,sy=720f/UnityEngine.Screen.height;
   foreach(var t in Input.touches){
    Vector2 p=new Vector2(t.position.x*sx,(UnityEngine.Screen.height-t.position.y)*sy);
    bool began=t.phase==TouchPhase.Began,ended=t.phase==TouchPhase.Ended||t.phase==TouchPhase.Canceled;
    if(began&&p.x<420&&p.y>380&&joyFinger<0){joyFinger=t.fingerId;joyOrigin=p;}
    if(t.fingerId==joyFinger){if(ended)joyFinger=-1;else MoveInput=Vector2.ClampMagnitude(new Vector2(p.x-joyOrigin.x,joyOrigin.y-p.y)/60f,1f);continue;}
    if(p.x>640&&p.y>400){
     if(t.fingerId==camFinger)camFinger=-1;
     if(p.x>1100){if(began)JumpPressed=true;}
     else if(p.x>950){AttackHeld|=!ended;if(began)AttackPressed=true;if(ended)AttackReleased=true;}
     else if(p.x>800){if(began)CastPressed=true;}
     else if(p.x>640){if(began)DashPressed=true;}
     continue;
    }
    if(began&&(p.y<=400||(p.x>=420&&p.x<=640))&&p.y>80&&camFinger<0){camFinger=t.fingerId;}
    if(t.fingerId==camFinger){if(ended)camFinger=-1;else if(t.phase==TouchPhase.Moved)yawInput+=t.deltaPosition.x*sx*0.16f;}
   }
   MoveInput=Vector2.ClampMagnitude(MoveInput,1); CameraRig.Yaw+=yawInput;
  }
  public void Pause(){Screen=GameScreen.Paused;Music.Pause();joyFinger=-1;}
  public void Resume(){Screen=GameScreen.Playing;if(Save.music)Music.UnPause();}
  void OnApplicationPause(bool paused){if(paused&&Screen==GameScreen.Playing)Pause();Persist();}
  void OnApplicationFocus(bool focused){if(!focused&&Screen==GameScreen.Playing)Pause();}
  public void Tell(string message,float seconds=3){Notice=message;noticeUntil=Time.unscaledTime+seconds;}
  public void Sound(string name){if(!Save.sound)return;var clip=Resources.Load<AudioClip>("Audio/sfx_"+name);if(clip)Sfx.PlayOneShot(clip);}
  public void Collect(bool gem){if(gem)Gems++;else Coins++;Sound(gem?"gem":"coin");}
  public void ActivateCheckpoint(Vector3 position){Checkpoint=position;CheckpointActive=true;Sound("checkpoint");Tell("Checkpoint restored. Your trail is safe.");}
  public void Respawn(){Player.Warp(Checkpoint);Player.Health=Player.MaxHealth;Player.Energy=100;Sound("respawn");Tell("Returned to the checkpoint.");}
  public void Defeat(){Screen=GameScreen.Defeated;Sound("defeat");}
  public void Finish(){if(Screen!=GameScreen.Playing)return;if(World.IsBoss&&Enemies.Exists(x=>x&&x.Boss&&x.Health>0)){Tell("Defeat the guardian to open this gate.");return;}
   EarnedStars=1+(Gems>0?1:0)+(DamageTaken==0?1:0); Save.stars[Level-1]=Mathf.Max(Save.stars[Level-1],EarnedStars); if(Save.best[Level-1]<=0||Elapsed<Save.best[Level-1])Save.best[Level-1]=Elapsed;
   Save.coins+=Coins+EarnedStars*10;Save.gems+=Gems;Save.unlocked=Mathf.Max(Save.unlocked,Mathf.Min(10,Level+1));Persist();Screen=GameScreen.Complete;Sound("complete");
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
   label=new GUIStyle(GUI.skin.label){fontSize=22,wordWrap=true};label.normal.textColor=new Color(.88f,.95f,.96f);
   small=new GUIStyle(label){fontSize=16};
   button=new GUIStyle(GUI.skin.button){fontSize=20,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
   button.normal.textColor=new Color(1f,.96f,.88f);button.padding=new RectOffset(12,12,10,10);
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
   BoxOutline(r,new Color(.028f,.06f,.09f,.95f),Accent,2f);
   Box(new Rect(x+6,y+6,w-12,2),new Color(Accent.r,Accent.g,Accent.b,.25f));
  }
  void OnGUI(){
   Styles();
   GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(UnityEngine.Screen.width/1280f,UnityEngine.Screen.height/720f,1));
   if(Screen==GameScreen.Playing){
    // Health & Realm Banner
    Panel(24,20,360,112);
    Text(42,28,320,24,$"REALM {Realm+1}  /  {Realms[Realm]}",small);
    Text(42,54,320,32,Titles[Level-1]);

    float targetH=Player?Mathf.Clamp01((float)Player.Health/Player.MaxHealth):1f;
    healthLag=Mathf.Lerp(healthLag,targetH,Time.unscaledDeltaTime*3.5f);
    BoxOutline(new Rect(42,98,230,14),new Color(.12f,.18f,.22f),new Color(.28f,.38f,.45f),1f);
    if(healthLag>targetH)Box(new Rect(43,99,228*healthLag,12),new Color(1f,.65f,.35f,.75f));
    Box(new Rect(43,99,228*targetH,12),new Color(.95f,.28f,.28f));
    Text(282,91,95,26,$"HP {Player.Health}/{Player.MaxHealth}",small);

    // Collectibles Panel
    Panel(404,20,240,68);
    Text(422,34,215,40,$"COINS {Coins:00}   GEMS {Gems}");

    // Power Selector & Energy Bar
    string[] powerNames={"EMBER [FIRE]","FROST [ICE]","GALE [WIND]"};
    Color[] powerCols={new Color(1f,.45f,.1f),new Color(.2f,.85f,1f),new Color(.2f,1f,.55f)};
    if(Button(660,20,205,50,powerNames[Player.Power]))Player.Power=(Player.Power+1)%3;
    BoxOutline(new Rect(660,74,205,8),new Color(.1f,.15f,.2f),powerCols[Player.Power]*.5f,1f);
    Box(new Rect(661,75,203*Mathf.Clamp01(Player.Energy/100f),6),powerCols[Player.Power]);

    // Timer and Pause Button
    Panel(882,20,135,50);Text(898,32,105,28,$"TIME {Clock(Elapsed)}",small);
    if(Button(1035,20,120,50,"PAUSE"))Pause();

    // Boss Bar
    var boss=Enemies.Find(x=>x&&x.Boss&&x.Health>0);
    if(boss&&Vector3.Distance(Player.transform.position,boss.transform.position)<32){
     Panel(390,105,500,68);
     Text(410,112,470,26,boss.DisplayName,small);
     BoxOutline(new Rect(410,144,460,14),new Color(.25f,.12f,.15f),new Color(.6f,.25f,.3f),1f);
     Box(new Rect(411,145,458*Mathf.Clamp01(boss.Health/boss.MaxHealth),12),Accent);
    }

    if(Time.unscaledTime<noticeUntil){
     Panel(260,192,760,62);
     Text(282,204,716,48,Notice,small);
    }

    // Touch and Keyboard Controls Guide
    Box(new Rect(28,580,140,98),new Color(.04f,.09f,.13f,.72f));
    Text(44,596,120,60,"MOVE\nW A S D\nSTICK",small);
    Control(685,"DODGE","SHIFT / BUTTON");
    Control(830,"POWER","K / BURST");
    Control(975,"BLADE","J / SWING");
    Control(1120,"JUMP","SPACE / AIR");
    Text(24,692,1200,24,"Double jump to cross gaps  *  Hold attack for charged slash  *  Orbit camera by dragging screen",small);
    return;
   }

   // Backdrop Dimmer
   Box(new Rect(0,0,1280,720),new Color(.015f,.03f,.045f,.75f));

   if(Screen==GameScreen.Menu){
    Panel(64,65,660,585);
    Text(96,90,600,32,"✦ LEGENDS OF THE LOST REALMS ✦",small);
    Text(96,135,600,145,"Three realms.\nOne lost heart.",title);
    Box(new Rect(96,290,590,2),new Color(Accent.r,Accent.g,Accent.b,.4f));
    Text(96,308,580,72,"Aster's epic journey in full 3D.\nWield elemental blades, cross treacherous floating isles, and restore the ancient portals.");
    if(Button(96,415,280,62,"► CONTINUE JOURNEY"))LoadLevel(Save.unlocked);
    if(Button(396,415,280,62,"🗺 REALM ATLAS"))Screen=GameScreen.Map;
    if(Button(96,495,280,58,"⚙ SANCTUARY"))Screen=GameScreen.Settings;
    Text(96,585,580,26,"UNITY 3D EDITION  *  TOUCH + GAMEPAD + KEYBOARD",small);
    return;
   }

   if(Screen==GameScreen.Map){
    Panel(50,45,1180,630);
    Text(80,65,1080,56,"Realm Atlas",title);
    Text(80,130,1050,34,$"Treasury: {Save.coins} Gold   *   {Save.gems} Gems   *   {TotalStars()}/30 Stars");
    Box(new Rect(80,170,1120,2),new Color(Accent.r,Accent.g,Accent.b,.35f));
    for(int i=0;i<10;i++){
     float x=80+(i%5)*228,y=195+(i/5)*175;
     bool unlocked=i+1<=Save.unlocked;
     GUI.enabled=unlocked;
     string starStr=unlocked?(Save.stars[i]>0?new string('★',Save.stars[i]):"---"):"LOCKED";
     if(Button(x,y,212,145,$"CHAPTER {i+1:00}\n\n{Titles[i]}\n\n{starStr}"))LoadLevel(i+1);
     GUI.enabled=true;
    }
    if(Button(80,588,200,56,"◄ BACK"))Screen=GameScreen.Menu;
    return;
   }

   if(Screen==GameScreen.Settings){
    Panel(200,55,880,610);
    Text(240,78,800,58,"Sanctuary & Blessings",title);
    Box(new Rect(240,145,800,2),new Color(Accent.r,Accent.g,Accent.b,.35f));
    if(Button(240,165,385,58,"MUSIC: "+(Save.music?"ENABLED":"MUTED"))){
     Save.music=!Save.music;if(Save.music)Music.Play();else Music.Pause();Persist();
    }
    if(Button(655,165,385,58,"SOUND EFFECTS: "+(Save.sound?"ENABLED":"MUTED"))){
     Save.sound=!Save.sound;Persist();
    }
    Text(240,250,800,34,$"Available Resources:  {Save.coins} Gold   *   {Save.gems} Gems");
    if(Button(240,300,800,65,$"VITALITY RANK {Save.healthRank}/3  (Max HP +{Save.healthRank})   -   Cost: {50+Save.healthRank*40} Gold")){
     int cost=50+Save.healthRank*40;
     if(Save.healthRank<3&&Save.coins>=cost){Save.coins-=cost;Save.healthRank++;Persist();Sound("upgrade");}
    }
    if(Button(240,385,800,65,$"ELEMENTAL POWER RANK {Save.powerRank}/3  (Damage +{Save.powerRank*20}%)   -   Cost: {3+Save.powerRank*2} Gems")){
     int cost=3+Save.powerRank*2;
     if(Save.powerRank<3&&Save.gems>=cost){Save.gems-=cost;Save.powerRank++;Persist();Sound("upgrade");}
    }
    Text(240,480,800,54,"Sanctuary blessings apply instantly and persist across all chapters.",small);
    if(Button(240,565,220,56,"◄ BACK"))Screen=GameScreen.Menu;
    return;
   }

   // Pause / Complete / Defeat Screens
   Panel(320,120,640,485);
   string heading=Screen==GameScreen.Paused?"A Moment of Rest":Screen==GameScreen.Complete?(Level==10?"The Lost Realms Restored!":"Chapter Cleared!"):"The Light Remains";
   Text(355,150,570,95,heading,title);
   Box(new Rect(355,255,570,2),new Color(Accent.r,Accent.g,Accent.b,.4f));
   if(Screen==GameScreen.Complete){
    string starDisplay=new string('★',EarnedStars);
    Text(355,275,570,80,$"TRIUMPH!  {starDisplay}   Elapsed: {Clock(Elapsed)}\nRewards: +{Coins+EarnedStars*10} Gold   +{Gems} Gems");
    if(Button(355,385,570,60,Level==10?"RETURN TO REALM ATLAS":"NEXT CHAPTER")){
     if(Level==10)Screen=GameScreen.Map;else LoadLevel(Level+1);
    }
   }else if(Screen==GameScreen.Paused){
    Text(355,280,570,60,"Your journey is paused. All progress is safe.");
    if(Button(355,375,570,60,"RESUME JOURNEY"))Resume();
   }else{
    Text(355,280,570,65,"Rise again at your last shrine checkpoint.");
    if(Button(355,375,570,60,"TRY AGAIN")){Respawn();Resume();}
   }
   if(Button(355,475,270,58,"🗺 ATLAS"))Screen=GameScreen.Map;
   if(Button(645,475,280,58,"↺ RESTART"))LoadLevel(Level);
  }
  void Control(float x,string name,string key){
   BoxOutline(new Rect(x,570,132,108),new Color(.05f,.11f,.16f,.85f),Accent*.7f,1.5f);
   Text(x+10,585,115,35,name,small);
   Text(x+10,630,115,30,key,small);
  }
  int TotalStars(){int n=0;foreach(int s in Save.stars)n+=s;return n;}
 }
}
