using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LostRealms {
 [InitializeOnLoad] public static class Stage3MenuProbe {
  const string Key="LostRealms.Stage3MenuProbe";
  static double next;static int index=-1,waitFrames;static bool waiting;
  static string[] Views=>new[]{"sanctuary","mastery","mastery-locked","relics","relics-page2","quest","quest-claim"};
  static Stage3MenuProbe(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
  static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
  public static void Run(){
   Directory.CreateDirectory("Validation/ExpansionStage3/Menus");
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
   if(EditorApplication.timeSinceStartup<next)return;
   if(waiting){
    if(++waitFrames<12)return;
    if(UnityEngine.Screen.width!=1280||UnityEngine.Screen.height!=720){Debug.LogError("MENU_CAPTURE_WRONG_SIZE");Finish(3);return;}
    Debug.Log("MENU_CAPTURE_SIZE "+UnityEngine.Screen.width+"x"+UnityEngine.Screen.height+" "+Views[index]);
    ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/ExpansionStage3/Menus/"+Views[index]+".png"));
    waiting=false;next=EditorApplication.timeSinceStartup+1.5;return;
   }
   if(index>=0&&!File.Exists("Validation/ExpansionStage3/Menus/"+Views[index]+".png")){
    Debug.LogError("MENU_CAPTURE_MISSING "+Views[index]);Finish(2);return;
   }
   if(++index>=Views.Length){Debug.Log("STAGE3_MENU_REVIEW_CAPTURED "+Views.Length);Finish(0);return;}
   string capturePath="Validation/ExpansionStage3/Menus/"+Views[index]+".png";if(File.Exists(capturePath))File.Delete(capturePath);
   foreach(string font in new[]{"CinzelDecorative-Bold","Cinzel-Bold","CrimsonText-Regular"})if(!Resources.Load<Font>("UI/Fonts/"+font)){Debug.LogError("MENU_FONT_MISSING "+font);Finish(4);return;}
   g.Save=new Progress{unlocked=15,coins=2345,gems=120,equippedWeapon=12};
   for(int i=0;i<=WeaponCatalog.MaxId;i++)g.Save.weapons.Add(i);
   g.Save.skins=new System.Collections.Generic.List<int>{0,1,2,3,4,5};
   g.Save.bestiary=new int[22];for(int i=0;i<22;i++)g.Save.bestiary[i]=i%3==0?0:11;
   g.Save.achievements=new System.Collections.Generic.List<string>();for(int i=0;i<Codex.All.Length;i+=3)g.Save.achievements.Add(Codex.All[i].Id);
   for(int i=0;i<15;i++)g.Save.stars[i]=i%4;
   foreach(string f in new[]{"showBuild","showArsenal","showSkins","showCodex","showAchievements","showVault","showDropRates"})Field(g,f,false);
   g.Screen=GameScreen.Menu;Time.timeScale=0;g.Notice="";
   string v=Views[index];
   if(v.StartsWith("mastery")||v.StartsWith("relics")||v.StartsWith("quest")){
    g.Screen=GameScreen.Settings;g.Save.NormalizeBuild();g.Save.masteryChoices=new[]{1,2,1,2};
    g.Save.relics=new System.Collections.Generic.List<int>{0,1,2,3,4,5,6,7};g.Save.relicA=2;g.Save.relicB=7;
    if(v=="mastery-locked")g.Save.stars=new int[15];
    g.Save.wayfinderQuest=v=="quest-claim"?4:2;
    g.OpenBuild(v.StartsWith("mastery")?0:v.StartsWith("relics")?1:2);if(v=="relics-page2")Field(g,"relicPage",1);
   }
   else g.Screen=GameScreen.Settings;
   waiting=true;waitFrames=0;next=EditorApplication.timeSinceStartup+1;
  }
 }
}
