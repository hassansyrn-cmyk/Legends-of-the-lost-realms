using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LostRealms {
 [InitializeOnLoad] public static class MenuVisualProbe {
  const string Key="LostRealms.MenuVisualProbe";
  static double next;static int index=-1,waitFrames;static bool waiting;
  static readonly string[] AllViews={"main","atlas","atlas-locked","sanctuary","pause","arsenal","arsenal-page2","arsenal-empty","wardrobe","wardrobe-locked","bestiary","bestiary-page5","achievements","achievements-page6","vault","probabilities","vault-opening","vault-reward","vault-duplicate","complete","complete-final","defeated"};
  static string[] Views=>SessionState.GetBool(Key+".wardrobe",false)?new[]{"wardrobe-preview-0","wardrobe-preview-1","wardrobe-preview-2","wardrobe-preview-3","wardrobe-preview-4","wardrobe-preview-5","wardrobe-closed"}:SessionState.GetBool(Key+".hud",false)?new[]{"hud-idle","hud-frost","hud-gale","hud-low-health","hud-long-title","hud-boss"}:SessionState.GetBool(Key+".alignment",false)?new[]{"atlas","atlas-locked","vault"}:SessionState.GetBool(Key+".chests",false)?new[]{"atlas","vault","vault-rare","vault-reward","vault-duplicate","vault-common","vault-settled"}:AllViews;
  static MenuVisualProbe(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
  public static void RunAlignment(){SessionState.SetBool(Key+".alignment",true);Run();}
  public static void RunChests(){SessionState.SetBool(Key+".chests",true);Run();}
  public static void RunWardrobe(){SessionState.SetBool(Key+".wardrobe",true);Run();}
  public static void RunHUD(){
   // Exercise the unchanged input routing as well as the generated visuals.
   var t=new TouchRouter();
   for(int i=0;i<6;i++){
    t.Reset();t.Sample(i,TouchRouter.ActionRect(i).center,Vector2.zero,TouchPhase.Began);
    bool ok=i==0?t.Dodge:i==1?t.Power:i==2?t.BladePressed&&t.BladeHeld:i==3?t.Jump:i==4?t.Parry:t.Spell&&t.SpellHeld;
    if(!ok)throw new Exception("HUD action routing failed: "+i);
    t.BeginFrame();t.Sample(i,Vector2.zero,Vector2.zero,TouchPhase.Ended);
    if(i==2&&!t.BladeReleased||i==5&&!t.SpellReleased)throw new Exception("HUD hold release failed");
   }
   t.Reset();t.Sample(42,new Vector2(150,612),Vector2.zero,TouchPhase.Began);
   t.BeginFrame();t.Sample(42,new Vector2(215,612),new Vector2(65,0),TouchPhase.Moved);
   if(t.Move!=Vector2.right)throw new Exception("HUD inner knob movement failed");
   t.BeginFrame();t.Sample(42,new Vector2(215,612),Vector2.zero,TouchPhase.Ended);
   if(t.Move!=Vector2.zero)throw new Exception("HUD inner knob return failed");
   Debug.Log("HUD_CONTROL_ROUTING_PASSED");
   SessionState.SetBool(Key+".hud",true);Run();
  }
  static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
  public static void Run(){
   Directory.CreateDirectory("Validation/MenuReview");
   index=-1;waiting=false;waitFrames=0;
   SessionState.SetBool(Key,true);
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!="Assets/Scenes/Main.unity")EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
   var editorAssembly=typeof(EditorWindow).Assembly;
   var gameViewType=editorAssembly.GetType("UnityEditor.GameView");
   var view=EditorWindow.GetWindow(gameViewType);view.Show();view.Focus();
   var sizesType=editorAssembly.GetType("UnityEditor.GameViewSizes");
   var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
   var sizes=singleton.GetProperty("instance").GetValue(null);
   var groupType=sizesType.GetProperty("currentGroupType",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(sizes);
   var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
   var sizeType=editorAssembly.GetType("UnityEditor.GameViewSize");
   var enumType=editorAssembly.GetType("UnityEditor.GameViewSizeType");
   var size=Activator.CreateInstance(sizeType,new object[]{Enum.ToObject(enumType,1),1280,720,"Menu Review"});
   group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
   int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
   gameViewType.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,count-1);
   Debug.Log("MENU_CAPTURE_GROUP "+groupType);
   EditorApplication.update-=Tick;EditorApplication.update+=Tick;
   EditorApplication.EnterPlaymode();next=EditorApplication.timeSinceStartup+5;
  }
  static void Field(RealmGame g,string name,object value){typeof(RealmGame).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(g,value);}
  static void Tick(){
   if(!EditorApplication.isPlaying)return;
   var g=RealmGame.I;if(!g)return;
   if(index>=0&&index<Views.Length&&(Views[index]=="vault-rare"||Views[index]=="vault-reward"))Field(g,"rewardRevealStarted",Time.unscaledTime-.65f);
   if(EditorApplication.timeSinceStartup<next)return;
   if(waiting){
    if(++waitFrames<12)return;
    if(Views[index].StartsWith("wardrobe-preview-")){
     var preview=g.GetComponent<WardrobePreview>();
     if(!preview||!preview.Frame||preview.Error!=null||preview.IdleTime<=0||preview.Skin!=int.Parse(Views[index].Substring("wardrobe-preview-".Length))||g.Save.equippedSkin!=0){
      Debug.LogError("WARDROBE_PREVIEW_FAILED "+Views[index]);Finish(6);return;
     }
     Debug.Log("WARDROBE_PREVIEW_PASSED "+preview.Skin+" idle="+preview.IdleTime);
    }
    if(Views[index]=="wardrobe-closed"&&(g.GetComponent<WardrobePreview>()||GameObject.Find("Wardrobe display stage"))){Debug.LogError("WARDROBE_PREVIEW_CLEANUP_FAILED");Finish(7);return;}
    if(UnityEngine.Screen.width!=1280||UnityEngine.Screen.height!=720){Debug.LogError("MENU_CAPTURE_WRONG_SIZE");Finish(3);return;}
    Debug.Log("MENU_CAPTURE_SIZE "+UnityEngine.Screen.width+"x"+UnityEngine.Screen.height+" "+Views[index]);
    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/MenuReview/"+Views[index]+".png"));
    waiting=false;next=EditorApplication.timeSinceStartup+1.5;return;
   }
   if(index>=0&&!File.Exists("Validation/MenuReview/"+Views[index]+".png")){
    Debug.LogError("MENU_CAPTURE_MISSING "+Views[index]);Finish(2);return;
   }
   if(++index>=Views.Length){Debug.Log("MENU_VISUAL_REVIEW_CAPTURED "+Views.Length);Finish(0);return;}
   string capturePath="Validation/MenuReview/"+Views[index]+".png";if(File.Exists(capturePath))File.Delete(capturePath);
   foreach(string font in new[]{"CinzelDecorative-Bold","Cinzel-Bold","CrimsonText-Regular"})if(!Resources.Load<Font>("UI/Fonts/"+font)){Debug.LogError("MENU_FONT_MISSING "+font);Finish(4);return;}
   g.Save=new Progress{unlocked=15,coins=2345,gems=120,equippedWeapon=12};
   for(int i=0;i<=WeaponCatalog.MaxId;i++)g.Save.weapons.Add(i);
   g.Save.skins=new System.Collections.Generic.List<int>{0,1,2,3,4,5};
   g.Save.bestiary=new int[22];for(int i=0;i<22;i++)g.Save.bestiary[i]=i%3==0?0:11;
   g.Save.achievements=new System.Collections.Generic.List<string>();for(int i=0;i<Codex.All.Length;i+=3)g.Save.achievements.Add(Codex.All[i].Id);
   for(int i=0;i<15;i++)g.Save.stars[i]=i%4;
   foreach(string f in new[]{"showArsenal","showSkins","showCodex","showAchievements","showVault","showDropRates"})Field(g,f,false);
   Field(g,"vaultState",Enum.Parse(typeof(RealmGame).GetField("vaultState",BindingFlags.NonPublic|BindingFlags.Instance).FieldType,"Browse"));
   g.Screen=GameScreen.Menu;Time.timeScale=0;g.Notice="";
   string v=Views[index];
   if(v.StartsWith("hud-")){
    g.LoadLevel(v=="hud-boss"?4:v=="hud-long-title"?7:1);Time.timeScale=0;
    g.Notice="";Field(g,"noticeUntil",0f);Field(g,"bossIntroUntil",0f);
    g.Coins=123;g.Gems=24;g.Elapsed=125;
    g.Player.Power=v=="hud-frost"?1:v=="hud-gale"?2:0;
    g.Player.Energy=v=="hud-low-health"?16:75;
    if(v=="hud-low-health")g.Player.Health=1;
    if(v=="hud-boss"){
     var boss=g.Enemies.Find(e=>e&&e.Boss);
     if(boss)g.Player.transform.position=boss.transform.position+Vector3.back*8;
    }
   }
   else if(v.StartsWith("atlas")){g.Screen=GameScreen.Map;if(v.EndsWith("locked")){g.Save.unlocked=3;g.Save.stars=new int[15];}}
   else if(v=="sanctuary")g.Screen=GameScreen.Settings;
   else if(v=="pause")g.Screen=GameScreen.Paused;
   else if(v.StartsWith("arsenal")){g.Screen=GameScreen.Paused;Field(g,"showArsenal",true);Field(g,"arsenalPage",v.EndsWith("2")?1:0);if(v.EndsWith("empty")){g.Save.weapons.Clear();g.Save.equippedWeapon=-1;}}
   else if(v=="wardrobe-closed")g.Screen=GameScreen.Map;
   else if(v.StartsWith("wardrobe")){
    g.Screen=GameScreen.Paused;Field(g,"showSkins",true);
    if(v.EndsWith("locked")||v=="wardrobe-preview-5")g.Save.skins=new System.Collections.Generic.List<int>{0};
    if(v.StartsWith("wardrobe-preview-"))g.SelectWardrobePreview(int.Parse(v.Substring("wardrobe-preview-".Length)));
   }
   else if(v.StartsWith("bestiary")){g.Screen=GameScreen.Paused;Field(g,"showCodex",true);Field(g,"codexPage",v.EndsWith("5")?4:0);}
   else if(v.StartsWith("achievements")){g.Screen=GameScreen.Paused;Field(g,"showAchievements",true);Field(g,"achievementsPage",v.EndsWith("6")?5:0);}
   else if(v.StartsWith("vault")||v=="probabilities"){
    g.Screen=GameScreen.Paused;Field(g,"showVault",true);Field(g,"showDropRates",v=="probabilities");
    if(v=="vault-opening"){Field(g,"vaultState",Enum.Parse(typeof(RealmGame).GetField("vaultState",BindingFlags.NonPublic|BindingFlags.Instance).FieldType,"Opening"));Field(g,"vaultAnimTimer",-100f);}
    if(v=="vault-reward"||v=="vault-duplicate"||v=="vault-rare"||v=="vault-common"||v=="vault-settled"){
     Field(g,"vaultState",Enum.Parse(typeof(RealmGame).GetField("vaultState",BindingFlags.NonPublic|BindingFlags.Instance).FieldType,"Revealed"));
     Field(g,"currentChestReward",v=="vault-common"?ChestReward.MakeResources(125,0,RewardRarity.Common):v=="vault-rare"?ChestReward.MakeWeapon((WeaponId)12,false,0,0,RewardRarity.Rare):ChestReward.MakeSkin((SkinId)4,v=="vault-duplicate",450,10));
     Field(g,"rewardRevealStarted",Time.unscaledTime-(v=="vault-settled"?5f:.35f));
     var reveal=typeof(RealmGame).GetMethod("HasRareItemReveal",BindingFlags.NonPublic|BindingFlags.Instance);
     bool expected=v!="vault-duplicate"&&v!="vault-common";
     if((bool)reveal.Invoke(g,null)!=expected){Debug.LogError("MENU_REWARD_EFFECT_GATE_FAILED "+v);Finish(5);return;}
    }
   }
   else if(v.StartsWith("complete")){g.Screen=GameScreen.Complete;g.EarnedStars=3;g.Level=v.EndsWith("final")?15:1;}
   else if(v=="defeated")g.Screen=GameScreen.Defeated;
   waiting=true;waitFrames=0;next=EditorApplication.timeSinceStartup+.2;
  }
 }
}
