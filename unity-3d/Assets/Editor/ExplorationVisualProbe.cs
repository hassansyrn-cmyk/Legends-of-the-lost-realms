using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace LostRealms {
 [InitializeOnLoad] public static class ExplorationVisualProbe {
  const string Key="LostRealms.ExplorationVisualProbe";
  static double next;static int step;
  static ExplorationVisualProbe(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
  public static void Run(){
   SessionState.SetBool(Key,true);step=0;Directory.CreateDirectory("Validation/ExplorationHUD");
   EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
   var asm=typeof(EditorWindow).Assembly;var type=asm.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(type);view.Show();view.Focus();
   var sizesType=asm.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);var sizes=singleton.GetProperty("instance").GetValue(null);
   var groupType=sizesType.GetProperty("currentGroupType",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).GetValue(sizes);var group=sizesType.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
   var size=Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"),1),1280,720,"Exploration review"});group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});
   type.GetProperty("selectedSizeIndex",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(view,(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1);
   EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();next=EditorApplication.timeSinceStartup+6;
  }
  static void Tick(){
   if(!EditorApplication.isPlaying||!RealmGame.I||EditorApplication.timeSinceStartup<next)return;
   var g=RealmGame.I;
   if(step==0||step==2){
    g.Save=new Progress{unlocked=25};Campaign.Normalize(g.Save);g.LoadLevel(step==0?16:21);
    foreach(var enemy in g.Enemies)enemy.enabled=false;
    g.Player.Warp(g.World.Exploration.Sites[1].transform.position+Vector3.up*.08f);
    typeof(RealmGame).GetField("fieldMap",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(g,true);
    foreach(int i in new[]{0,1,2,9})g.World.Exploration.Sites[i].Visited=true;
    next=EditorApplication.timeSinceStartup+5;step++;return;
   }
   if(step==1||step==3){
    if(g.Elapsed<2)return; // Wait for actual game frames, not just editor/import wall time.
    g.Notice="";ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/ExplorationHUD/"+(step==1?"stormveil":"astral")+".png"));step++;next=EditorApplication.timeSinceStartup+2;return;
   }
   SessionState.SetBool(Key,false);Debug.Log("EXPLORATION_HUD_CAPTURED");EditorApplication.Exit(0);
  }
 }
}
