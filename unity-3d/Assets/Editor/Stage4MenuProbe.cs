using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LostRealms {
 [InitializeOnLoad] public static class Stage4MenuProbe {
  const string Key="LostRealms.Stage4MenuProbe";
  static double next;static int index=-1,waitFrames;static bool waiting;
  static string[] Views=>new[]{"atlas-original","atlas-stormveil","atlas-stormveil-locked","bestiary-stormveil","stormveil-hud","regent-encounter"};
  static Stage4MenuProbe(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
  static void Finish(int code){SessionState.SetBool(Key,false);EditorApplication.Exit(code);}
  public static void Run(){
   Directory.CreateDirectory("Validation/ExpansionStage4/Menus");
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
    Time.timeScale=0;g.Notice="";Field(g,"noticeUntil",0f);ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/ExpansionStage4/Menus/"+Views[index]+".png"));
    waiting=false;next=EditorApplication.timeSinceStartup+1.5;return;
   }
   if(index>=0&&!File.Exists("Validation/ExpansionStage4/Menus/"+Views[index]+".png")){
    Debug.LogError("MENU_CAPTURE_MISSING "+Views[index]);Finish(2);return;
   }
   if(++index>=Views.Length){Debug.Log("STAGE4_MENU_REVIEW_CAPTURED "+Views.Length);Finish(0);return;}
   string capturePath="Validation/ExpansionStage4/Menus/"+Views[index]+".png";if(File.Exists(capturePath))File.Delete(capturePath);
   foreach(string font in new[]{"CinzelDecorative-Bold","Cinzel-Bold","CrimsonText-Regular"})if(!Resources.Load<Font>("UI/Fonts/"+font)){Debug.LogError("MENU_FONT_MISSING "+font);Finish(4);return;}
   g.Save=new Progress{unlocked=20,coins=2345,gems=120,equippedWeapon=12,healthRank=3};Campaign.Normalize(g.Save);Codex.Normalize(g.Save);
   for(int i=0;i<20;i++)g.Save.stars[i]=i%4;
   for(int i=0;i<Codex.Kinds;i++)g.Save.bestiary[i]=11; foreach(var achievement in Codex.All)g.Save.achievements.Add(achievement.Id);
   foreach(string f in new[]{"showBuild","showArsenal","showSkins","showCodex","showAchievements","showVault","showDropRates"})Field(g,f,false);
   Time.timeScale=0;g.Notice="";string v=Views[index];
   if(v.StartsWith("atlas")){g.Screen=GameScreen.Map;Field(g,"atlasPage",v=="atlas-original"?0:1);if(v.EndsWith("locked")){g.Save.unlocked=16;for(int i=15;i<20;i++)g.Save.stars[i]=0;}}
   else if(v.StartsWith("bestiary")){g.Screen=GameScreen.Paused;Field(g,"showCodex",true);Field(g,"codexPage",5);}
   else {g.LoadLevel(v=="stormveil-hud"?16:20);g.Player.Warp(g.World.Route[v=="stormveil-hud"?3:g.World.Route.Count-1]+new Vector3(0,.05f,-4));g.CameraRig.Snap();Field(g,"bossIntroUntil",0f);g.Notice="";Field(g,"noticeUntil",0f);Time.timeScale=1;}
   waiting=true;waitFrames=0;next=EditorApplication.timeSinceStartup+1;
  }
 }
}



