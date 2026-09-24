using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace LostRealms {
 public sealed class FantasyCapture : MonoBehaviour {
  [Serializable] sealed class CaptureRecord {
   public string state;
   public int requestedWidth,requestedHeight,actualWidth,actualHeight;
   public string file;
  }
  [Serializable] sealed class CaptureManifest {
   public bool success;
   public int level=10;
   public string error;
   public string generatedUtc;
   public List<CaptureRecord> captures=new List<CaptureRecord>();
  }

  static readonly Vector2Int[] Sizes={
   new Vector2Int(1280,720),new Vector2Int(1440,720),
   new Vector2Int(1560,720),new Vector2Int(1280,960)
  };
  static bool requested;
  static string outputDirectory;
  readonly CaptureManifest manifest=new CaptureManifest();
  string failure;

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
  static void DetectRequest(){
   var args=Environment.GetCommandLineArgs();
   int index=Array.IndexOf(args,"-fantasyUiCapture");
   if(index<0)return;
   requested=true;
   if(index+1<args.Length&&!string.IsNullOrWhiteSpace(args[index+1])&&!args[index+1].StartsWith("-"))
    outputDirectory=Path.GetFullPath(args[index+1]);
   ForceMobileUI();
  }

  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void StartCapture(){
   if(!requested)return;
   var host=new GameObject("Fantasy UI capture");
   DontDestroyOnLoad(host);
   host.AddComponent<FantasyCapture>();
  }

  static void ForceMobileUI(){
   // FantasyUI owns this optional capture-only switch. Reflection keeps the
   // harness harmless when an older player without the new Canvas UI is built.
   var type=typeof(FantasyCapture).Assembly.GetType("LostRealms.FantasyUI");
   if(type==null)return;
   const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
   var field=type.GetField("ForceMobileControls",flags);
   if(field!=null&&field.FieldType==typeof(bool)){field.SetValue(null,true);return;}
   var property=type.GetProperty("ForceMobileControls",flags);
   if(property!=null&&property.PropertyType==typeof(bool)&&property.CanWrite)property.SetValue(null,true,null);
  }

  IEnumerator Start(){
   manifest.generatedUtc=DateTime.UtcNow.ToString("o");
   if(string.IsNullOrEmpty(outputDirectory)){
    Finish("The -fantasyUiCapture option requires an output directory.");
    yield break;
   }
   try{Directory.CreateDirectory(outputDirectory);}
   catch(Exception e){Finish("Cannot create capture directory: "+e.Message);yield break;}

   float deadline=Time.realtimeSinceStartup+45f;
   Canvas canvas=null;
   while(Time.realtimeSinceStartup<deadline){
    canvas=FindAnyObjectByType<Canvas>();
    if(RealmGame.I&&RealmGame.I.Player&&canvas)break;
    yield return null;
   }
   var game=RealmGame.I;
   if(!game||!game.Player||!canvas){
    Finish("Timed out waiting for RealmGame, Player, and the runtime Canvas.");
    yield break;
   }

   try{game.LoadLevel(10);}
   catch(Exception e){Finish("Level 10 failed to load: "+e.Message);yield break;}
   for(int i=0;i<5;i++)yield return new WaitForEndOfFrame();
   if(!game.Player||game.Level!=10){
    Finish("The live Level 10 game was not available after loading.");
    yield break;
   }

   foreach(var size in Sizes){
    yield return SetResolution(size.x,size.y);
    if(failure!=null){Finish(failure);yield break;}
    yield return CaptureState(game,GameScreen.Menu,"menu",size);
    if(failure!=null){Finish(failure);yield break;}
    game.Screen=GameScreen.Playing;
    game.Pause();
    yield return CaptureState(game,GameScreen.Paused,"paused",size);
    if(failure!=null){Finish(failure);yield break;}
    game.Resume();
    yield return CaptureState(game,GameScreen.Playing,"playing",size);
    if(failure!=null){Finish(failure);yield break;}
   }
   Finish(null);
  }

  IEnumerator SetResolution(int width,int height){
   UnityEngine.Screen.SetResolution(width,height,FullScreenMode.Windowed);
   float deadline=Time.realtimeSinceStartup+10f;
   int settled=0;
   while(Time.realtimeSinceStartup<deadline){
    yield return new WaitForEndOfFrame();
    if(UnityEngine.Screen.width==width&&UnityEngine.Screen.height==height){
     if(++settled>=4)yield break;
    }else settled=0;
   }
   failure=$"Resolution mismatch: requested {width}x{height}, got {UnityEngine.Screen.width}x{UnityEngine.Screen.height}.";
  }

  IEnumerator CaptureState(RealmGame game,GameScreen screen,string label,Vector2Int requestedSize){
   if(screen==GameScreen.Menu)game.Screen=GameScreen.Menu;
   for(int i=0;i<4;i++)yield return new WaitForEndOfFrame();
   int actualWidth=UnityEngine.Screen.width,actualHeight=UnityEngine.Screen.height;
   if(actualWidth!=requestedSize.x||actualHeight!=requestedSize.y){
    failure=$"Resolution changed before {label}: requested {requestedSize.x}x{requestedSize.y}, got {actualWidth}x{actualHeight}.";
    yield break;
   }
   string fileName=$"level10-{label}-{requestedSize.x}x{requestedSize.y}.png";
   string path=Path.Combine(outputDirectory,fileName);
   try{
    if(File.Exists(path))File.Delete(path);
    ScreenCapture.CaptureScreenshot(path);
   }catch(Exception e){failure="Could not request "+fileName+": "+e.Message;yield break;}
   yield return new WaitForEndOfFrame();
   float deadline=Time.realtimeSinceStartup+10f;
   while(Time.realtimeSinceStartup<deadline){
    bool ready=false;
    try{ready=File.Exists(path)&&new FileInfo(path).Length>0;}catch(Exception){}
    if(ready)break;
    yield return null;
   }
   bool written=false;
   try{written=File.Exists(path)&&new FileInfo(path).Length>0;}
   catch(Exception e){failure="Could not verify "+path+": "+e.Message;yield break;}
   if(!written){
    failure="Screenshot was not written: "+path;
    yield break;
   }
   manifest.captures.Add(new CaptureRecord{
    state=label,requestedWidth=requestedSize.x,requestedHeight=requestedSize.y,
    actualWidth=actualWidth,actualHeight=actualHeight,file=fileName
   });
  }

  void Finish(string error){
   manifest.success=string.IsNullOrEmpty(error);
   manifest.error=error??"";
   if(!string.IsNullOrEmpty(outputDirectory)){
    try{File.WriteAllText(Path.Combine(outputDirectory,"manifest.json"),JsonUtility.ToJson(manifest,true));}
    catch(Exception e){
     manifest.success=false;
     manifest.error="Could not write manifest: "+e.Message;
     Debug.LogError("FANTASY_UI_CAPTURE_MANIFEST_FAILED "+e);
    }
   }
   if(manifest.success)Debug.Log("FANTASY_UI_CAPTURE_PASSED "+manifest.captures.Count);
   else Debug.LogError("FANTASY_UI_CAPTURE_FAILED "+error);
   Application.Quit(manifest.success?0:1);
  }
 }
}