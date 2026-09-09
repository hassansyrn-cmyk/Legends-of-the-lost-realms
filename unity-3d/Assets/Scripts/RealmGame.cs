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
  void Styles(){if(title!=null)return;pixel=Texture2D.whiteTexture;
   title=new GUIStyle(GUI.skin.label){fontSize=48,fontStyle=FontStyle.Bold,wordWrap=true}; title.normal.textColor=Color.white;
   label=new GUIStyle(GUI.skin.label){fontSize=22,wordWrap=true};label.normal.textColor=new Color(.88f,.94f,.95f);
   small=new GUIStyle(label){fontSize=16}; button=new GUIStyle(GUI.skin.button){fontSize=21,fontStyle=FontStyle.Bold}; button.normal.textColor=Color.white;button.padding=new RectOffset(12,12,10,10);
  }
  void Box(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,pixel);GUI.color=Color.white;}
  void Text(float x,float y,float w,float h,string s,GUIStyle st=null){GUI.Label(new Rect(x,y,w,h),s,st??label);}
  bool Button(float x,float y,float w,float h,string s){return GUI.Button(new Rect(x,y,w,h),s,button);}
  void Panel(float x,float y,float w,float h){Box(new Rect(x,y,w,h),new Color(.025f,.055f,.08f,.94f));Box(new Rect(x,y,4,h),Accent);}
  void OnGUI(){Styles();GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(UnityEngine.Screen.width/1280f,UnityEngine.Screen.height/720f,1));
   if(Screen==GameScreen.Playing){
    Panel(24,22,352,108);Text(42,32,310,26,Realms[Realm],small);Text(42,60,320,30,Titles[Level-1]);
    Box(new Rect(42,103,220,8),new Color(.15f,.22f,.25f));Box(new Rect(42,103,220*Mathf.Clamp01((float)Player.Health/Player.MaxHealth),8),new Color(.95f,.34f,.34f));Text(277,91,90,28,$"{Player.Health}/{Player.MaxHealth}",small);
    Panel(400,22,245,70);Text(420,35,220,40,$"GOLD {Coins:00}    GEMS {Gems}");
    if(Button(665,24,195,52,new[]{"EMBER","FROST","GALE"}[Player.Power]))Player.Power=(Player.Power+1)%3;Box(new Rect(665,81,195*Mathf.Clamp01(Player.Energy/100),5),Accent);
    Text(887,38,145,30,Clock(Elapsed));if(Button(1100,24,150,52,"PAUSE"))Pause();
    var boss=Enemies.Find(x=>x&&x.Boss&&x.Health>0);if(boss&&Vector3.Distance(Player.transform.position,boss.transform.position)<30){Panel(420,112,440,63);Text(438,118,425,26,boss.DisplayName,small);Box(new Rect(440,153,400,8),new Color(.3f,.15f,.18f));Box(new Rect(440,153,400*Mathf.Clamp01(boss.Health/boss.MaxHealth),8),Accent);}
    if(Time.unscaledTime<noticeUntil){Panel(260,192,760,62);Text(282,204,716,48,Notice,small);}
    Box(new Rect(28,582,135,95),new Color(.06f,.13f,.18f,.65f));Text(45,599,120,60,"MOVE\nW A S D",small);
    Control(692,"DODGE","SHIFT");Control(838,"POWER","K");Control(982,"BLADE","J / HOLD");Control(1126,"JUMP","SPACE");
    Text(24,690,1200,25,"Double jump to cross gaps   /   Q switches power   /   Right mouse or drag to orbit the camera",small);return;
   }
   Box(new Rect(0,0,1280,720),new Color(.015f,.035f,.055f,.66f));
   if(Screen==GameScreen.Menu){Panel(64,75,635,568);Text(94,96,570,35,"LEGENDS OF THE LOST REALMS",small);Text(94,150,575,150,"Three realms.\nOne lost heart.",title);Text(94,316,545,72,"Aster's journey, rebuilt in 3D. Restore the ancient gates and confront the guardians.");
    if(Button(96,432,264,60,"CONTINUE JOURNEY"))LoadLevel(Save.unlocked);if(Button(378,432,260,60,"REALM MAP"))Screen=GameScreen.Map;if(Button(96,514,264,55,"SETTINGS & UPGRADES"))Screen=GameScreen.Settings;Text(94,597,560,26,"UNITY 3D  /  ANDROID + DESKTOP CONTROLS",small);return;}
   if(Screen==GameScreen.Map){Panel(60,55,1160,610);Text(90,76,1080,64,"The realm paths",title);Text(90,145,1050,36,$"Gold {Save.coins}  /  Gems {Save.gems}  /  Stars {TotalStars()}");
    for(int i=0;i<10;i++){float x=90+(i%5)*218,y=221+(i/5)*157;bool unlocked=i+1<=Save.unlocked;GUI.enabled=unlocked;if(Button(x,y,200,125,$"{i+1:00}\n{Titles[i]}\n{(unlocked?new string('*',Save.stars[i]):"LOCKED")}"))LoadLevel(i+1);GUI.enabled=true;}
    if(Button(90,565,210,56,"BACK"))Screen=GameScreen.Menu;return;}
   if(Screen==GameScreen.Settings){Panel(220,65,840,595);Text(256,86,760,68,"Make the journey yours",title);
    if(Button(258,180,345,58,"MUSIC: "+(Save.music?"ON":"OFF"))){Save.music=!Save.music;if(Save.music)Music.Play();else Music.Pause();Persist();}if(Button(623,180,345,58,"SOUND: "+(Save.sound?"ON":"OFF"))){Save.sound=!Save.sound;Persist();}
    Text(258,275,720,35,$"Gold {Save.coins}   /   Gems {Save.gems}");
    if(Button(258,329,710,60,$"VITALITY {Save.healthRank}/3  -  {50+Save.healthRank*40} GOLD")){int cost=50+Save.healthRank*40;if(Save.healthRank<3&&Save.coins>=cost){Save.coins-=cost;Save.healthRank++;Persist();Sound("upgrade");}}
    if(Button(258,410,710,60,$"ELEMENTAL STRENGTH {Save.powerRank}/3  -  {3+Save.powerRank*2} GEMS")){int cost=3+Save.powerRank*2;if(Save.powerRank<3&&Save.gems>=cost){Save.gems-=cost;Save.powerRank++;Persist();Sound("upgrade");}}
    Text(258,498,720,57,"Upgrades apply on the next level. Progress is stored locally for this 3D edition.",small);if(Button(258,575,210,55,"BACK"))Screen=GameScreen.Menu;return;}
   Panel(340,135,600,462);string heading=Screen==GameScreen.Paused?"A moment of calm":Screen==GameScreen.Complete?(Level==10?"The realms are restored":"A path restored"):"The light remains";Text(375,165,530,125,heading,title);
   if(Screen==GameScreen.Complete){Text(375,294,525,80,$"{new string('*',EarnedStars)}   {Clock(Elapsed)}\n+{Coins+EarnedStars*10} gold    +{Gems} gems");if(Button(375,408,530,57,Level==10?"RETURN TO REALM MAP":"NEXT CHAPTER")){if(Level==10)Screen=GameScreen.Map;else LoadLevel(Level+1);}}
   else if(Screen==GameScreen.Paused){Text(375,293,530,56,"Your place on the trail is safe.");if(Button(375,380,530,56,"RESUME"))Resume();}
   else{Text(375,290,530,70,"Rise again at your last checkpoint.");if(Button(375,380,530,56,"TRY AGAIN")){Respawn();Resume();}}
   if(Button(375,491,252,56,"REALM MAP"))Screen=GameScreen.Map;if(Button(647,491,258,56,"RESTART LEVEL"))LoadLevel(Level);
  }
  void Control(float x,string name,string key){Box(new Rect(x,573,126,104),new Color(.06f,.14f,.19f,.78f));Text(x+12,590,116,35,name,small);Text(x+12,632,116,30,key,small);}
  int TotalStars(){int n=0;foreach(int s in Save.stars)n+=s;return n;}
 }
}


