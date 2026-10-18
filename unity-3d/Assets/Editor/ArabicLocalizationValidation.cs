using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LostRealms {
 [InitializeOnLoad] public static class ArabicLocalizationValidation {
  const string Key="LostRealms.ArabicLocalizationValidation";
  static ArabicLocalizationValidation(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
  public static void Run(){
   if(!RealmGame.Testing)throw new Exception("Use -menuVisualProbe to preserve player saves");
   
   Directory.CreateDirectory("Validation/ArabicLocalization");SessionState.SetBool(Key,true);
   EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
   var assembly=typeof(EditorWindow).Assembly;var viewType=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);view.Show();view.Focus();
   var sizeType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizeType);var sizes=singleton.GetProperty("instance").GetValue(null);
   var groupType=sizeType.GetProperty("currentGroupType",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(sizes);var group=sizeType.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
   var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),1280,720,"Arabic localization"});
   group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1);
   EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
  }
  static void Tick(){if(!EditorApplication.isPlaying||!RealmGame.I)return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;new GameObject("Arabic localization checks").AddComponent<ArabicLocalizationDriver>();}
 }

 public sealed class ArabicLocalizationDriver:MonoBehaviour {
  readonly StringBuilder report=new StringBuilder();
  string oldPreference;bool fixture,countdownFixture;Transform countdownTarget;
  void Update(){if(countdownFixture&&countdownTarget)InteractionCountdown.Report(countdownTarget,.7f,2.5f,"RESTORING");}
  void OnGUI(){if(!fixture)return;GUI.matrix=Matrix4x4.identity;GUI.color=new Color(.02f,.03f,.05f);GUI.DrawTexture(new Rect(0,0,1280,720),Texture2D.whiteTexture);GUI.color=Color.white;
   var style=new GUIStyle(GUI.skin.label){font=GameLanguage.ArabicFont,fontSize=34,alignment=TextAnchor.MiddleCenter};
   string sample="أساطير العوالم المفقودة — متابعة الرحلة — العربية (+12% / 0.5%)";
   GUI.Label(new Rect(30,20,1220,60),"A: native logical text",style);GUI.Label(new Rect(30,80,1220,70),sample,style);
   GUI.Label(new Rect(30,185,1220,60),"B: pre-shaped visual text",style);GUI.Label(new Rect(30,245,1220,70),GameLanguage.Shape(sample),style);
   GUI.Label(new Rect(30,350,1220,60),"C: pre-shaped visual with LTR override",style);GUI.Label(new Rect(30,410,1220,70),"\u202D"+GameLanguage.Shape(sample)+"\u202C",style);
   GUI.Label(new Rect(30,515,1220,60),"D: presentation forms in logical order",style);GUI.Label(new Rect(30,575,1220,70),new string(GameLanguage.Shape(sample).Reverse().ToArray()),style);
  }
  void Check(bool success,string message){if(!success)throw new Exception(message);report.AppendLine("PASS "+message);Debug.Log("ARABIC_PASS "+message);}
  static void Field(RealmGame game,string name,object value)=>typeof(RealmGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,value);
  static void Capture(string name)=>ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/ArabicLocalization/"+name+".png"));
  IEnumerator Start(){
   oldPreference=PlayerPrefs.GetString(GameLanguage.PreferenceKey,"<unset>");
   var run=Checks();
   while(true){bool next;try{next=run.MoveNext();}catch(Exception error){Debug.LogError("ARABIC_LOCALIZATION_FAILED "+error);File.WriteAllText("Validation/ArabicLocalization/results.txt",report+"FAIL "+error);EditorApplication.Exit(1);yield break;}if(!next)break;yield return run.Current;}
   var observed=new StringBuilder();foreach(var pair in GameLanguage.ObservedText)observed.AppendLine(pair.Key.Replace("\n","\\n")+"\t"+pair.Value.Replace("\n","\\n"));
   File.WriteAllText("Validation/ArabicLocalization/observed-text.tsv",observed.ToString());
   report.AppendLine("ARABIC_LOCALIZATION_PASSED");File.WriteAllText("Validation/ArabicLocalization/results.txt",report.ToString());Debug.Log("ARABIC_LOCALIZATION_PASSED");EditorApplication.Exit(0);
  }
  IEnumerator Shot(string name){yield return null;yield return null;Capture(name);yield return new WaitForSecondsRealtime(.8f);}
  IEnumerator Checks(){
   ShaderUtil.allowAsyncCompilation=false;yield return null;var game=RealmGame.I;Time.timeScale=0;
   GameLanguage.SetArabic(false,false);game.Screen=GameScreen.Menu;yield return Shot("english-main");
   Check(GameLanguage.Translate("CONTINUE JOURNEY")=="CONTINUE JOURNEY","English source remains unchanged");
   GameLanguage.SetArabic(true,false);yield return null;
   Check(GameLanguage.Translate("CONTINUE JOURNEY")=="متابعة الرحلة","Arabic menu text loads");
   Check(GameLanguage.Translate("HP 8/12")=="الصحة 8/12","Dynamic values and fractions survive template translation");
   Check(GameLanguage.Translate("Time remaining: 1.8s")=="الوقت المتبقي: 1.8 ث","Decimal countdown translates without changing its value");
   Check(GameLanguage.Translate("OPEN: 35 GEMS")=="فتح: 35 جواهر","Chest currency values translate accurately");
   Check(GameLanguage.Translate("WEAPON  WAR AXE").Contains("فأس"),"Dynamic inserted equipment name is translated");
   var shaped=GameLanguage.Shape("العربية لا 3258 12.5% WASD");
   Check(shaped.Any(c=>c>='\uFB50'&&c<='\uFEFF')&&shaped.Contains("3258")&&shaped.Contains("12.5")&&shaped.Contains("WASD"),"Arabic connects letters while retaining number and keyboard runs");
   Check(GameLanguage.ArabicFont!=null,"Bundled Arabic font loads through Resources");
   Check(GameLanguage.Translate("WEAPON  FISTS").StartsWith("السلاح"),"Timer template cannot intercept weapon names ending in S");
   Check(GameLanguage.Translate("MUSIC: ENABLED")=="الموسيقى: مفعّلة","Concatenated settings labels translate completely");
   Check(!System.Text.RegularExpressions.Regex.IsMatch(GameLanguage.Translate("Stand still beside SHRINES 1 to restore it."),"[A-Za-z]"),"Composed objective hints translate completely");
   Check(GameLanguage.Shape("احتمال (0.5%). +25% -22% 2/3 01:08").Contains("(0.5%)")&&GameLanguage.Shape("احتمال (0.5%). +25% -22% 2/3 01:08").Contains("+25%"),"Numeric protection retains percentages, signs and balanced parentheses");
   int templateCount=0;
   foreach(var entry in GameLanguage.Entries){
    if(!System.Text.RegularExpressions.Regex.IsMatch(entry.english,@"\{\d+\}"))continue;
    string sample=System.Text.RegularExpressions.Regex.Replace(entry.english,@"\{(\d+)\}",m=>(12+int.Parse(m.Groups[1].Value)).ToString());
    string expected=System.Text.RegularExpressions.Regex.Replace(entry.arabic,@"\{(\d+)\}",m=>(12+int.Parse(m.Groups[1].Value)).ToString());
    Check(GameLanguage.Translate(sample)==expected,"Dynamic template round-trip "+entry.key);templateCount++;
   }
   Check(templateCount>40,"All dynamic UI templates are exercised");
   int glyphs=0;var missing=new System.Collections.Generic.HashSet<char>();
   foreach(var entry in GameLanguage.Entries){
    if(string.IsNullOrWhiteSpace(entry.arabic))throw new Exception("Missing translation "+entry.key);
    var enTokens=System.Text.RegularExpressions.Regex.Matches(entry.english,@"\{\d+\}").Cast<System.Text.RegularExpressions.Match>().Select(m=>m.Value).OrderBy(x=>x).ToArray();
    var arTokens=System.Text.RegularExpressions.Regex.Matches(entry.arabic,@"\{\d+\}").Cast<System.Text.RegularExpressions.Match>().Select(m=>m.Value).OrderBy(x=>x).ToArray();
    if(!enTokens.SequenceEqual(arTokens))throw new Exception("Placeholder mismatch "+entry.english);
    foreach(char c in GameLanguage.Shape(entry.arabic))if((c>='\u0600'&&c<='\u06ff'||c>='\ufb50'&&c<='\ufeff')&&!char.IsWhiteSpace(c)){glyphs++;if(!GameLanguage.ArabicFont.HasCharacter(c))missing.Add(c);}
   }
   Check(missing.Count==0,"Bundled font covers every shaped Arabic glyph ("+glyphs+" occurrences); missing="+string.Concat(missing));
   game.enabled=false;fixture=true;yield return Shot("rendering-fixture");fixture=false;game.enabled=true;
   yield return Shot("arabic-main");
   game.Screen=GameScreen.Settings;yield return Shot("arabic-settings");
   game.Screen=GameScreen.Map;yield return Shot("arabic-atlas");Field(game,"atlasPage",1);yield return Shot("arabic-atlas-late");
   game.Screen=GameScreen.Settings;Field(game,"showSkins",true);game.SelectWardrobePreview(6);yield return Shot("arabic-wardrobe");Field(game,"showSkins",false);
   Field(game,"showArsenal",true);game.Save.weapons=Enumerable.Range(0,WeaponCatalog.MaxId+1).ToList();yield return Shot("arabic-arsenal");Field(game,"showArsenal",false);
   Field(game,"showCodex",true);for(int i=0;i<game.Save.bestiary.Length;i++)game.Save.bestiary[i]=4;yield return Shot("arabic-bestiary");Field(game,"showCodex",false);
   Field(game,"showAchievements",true);yield return Shot("arabic-achievements");Field(game,"showAchievements",false);
   game.OpenBuild();yield return Shot("arabic-mastery");Field(game,"buildTab",1);yield return Shot("arabic-relics");Field(game,"buildTab",2);yield return Shot("arabic-quest");Field(game,"showBuild",false);
   Field(game,"showVault",true);yield return Shot("arabic-vault");Field(game,"showDropRates",true);yield return Shot("arabic-probabilities");Field(game,"showDropRates",false);Field(game,"showVault",false);
   game.OpenDeveloperPrompt();yield return Shot("arabic-developer");Field(game,"showDeveloperPrompt",false);
   Check(game.TryEnableDeveloperMode("3258"),"Developer Mode still activates");game.DisableDeveloperMode();Check(GameLanguage.IsArabic,"Developer Mode restoration keeps language preference");
   var worldText=game.World.GetComponentInChildren<LocalizedWorldText>();Check(worldText,"World labels are bound for live language updates");
   yield return null;var mesh=worldText.GetComponent<TextMesh>();string arabicLabel=mesh.text;
   GameLanguage.SetArabic(false,false);yield return null;yield return null;Check(mesh.text!=arabicLabel&&!GameLanguage.ContainsArabic(mesh.text),"Existing world labels switch back to English without reloading");
   GameLanguage.SetArabic(true,false);yield return null;yield return null;Check(mesh.text==arabicLabel,"Existing world labels switch back to Arabic without reloading");
   game.Screen=GameScreen.Playing;game.Tell("Stand still beside SHRINES 1 to restore it.",10f);
   countdownTarget=game.Adventure.Nodes[0].transform;game.Player.Warp(countdownTarget.position+Vector3.left*.6f+Vector3.up*.1f);
   game.CameraRig.enabled=false;var viewCamera=game.CameraRig.GetComponent<Camera>();viewCamera.transform.position=countdownTarget.position+new Vector3(4,3.5f,-6);viewCamera.transform.LookAt(countdownTarget.position+Vector3.up);
   countdownFixture=true;yield return null;
   var timer=countdownTarget.GetComponent<InteractionCountdown>();Check(timer&&timer.Visible,"Arabic gameplay countdown is live");
   var anchor=(Vector3)typeof(InteractionCountdown).GetField("localAnchor",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(timer);
   Debug.Log("ARABIC_TIMER_SCREEN "+viewCamera.WorldToScreenPoint(countdownTarget.TransformPoint(anchor))+" root="+viewCamera.WorldToScreenPoint(countdownTarget.position));
   yield return Shot("arabic-hud");countdownFixture=false;
   game.Screen=GameScreen.Paused;yield return Shot("arabic-pause");game.Screen=GameScreen.Complete;yield return Shot("arabic-complete");
   game.Screen=GameScreen.Defeated;yield return Shot("arabic-defeated");
   var root=new GameObject("Legacy Arabic fixture");var cameraObject=new GameObject("Arabic fixture camera");var camera=cameraObject.AddComponent<Camera>();
   camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=4;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.02f,.03f,.05f);camera.cullingMask=1<<31;
   var target=new RenderTexture(1280,720,24);camera.targetTexture=target;
   for(int i=0;i<2;i++){
    var item=new GameObject("Arabic fixture row");item.layer=31;item.transform.SetParent(root.transform);item.transform.position=new Vector3(0,i==0?1.4f:-1.4f,0);
    var text=item.AddComponent<TextMesh>();text.anchor=TextAnchor.MiddleCenter;text.font=GameLanguage.ArabicFont;text.fontSize=80;text.characterSize=.035f;text.color=Color.white;
    text.text=(i==0?"A native logical: ":"B shaped visual: ")+(i==0?"أساطير العوالم المفقودة — العربية 0.5%":GameLanguage.Shape("أساطير العوالم المفقودة — العربية 0.5%"));item.GetComponent<Renderer>().sharedMaterial=text.font.material;
   }
   yield return null;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Validation/ArabicLocalization/world-rendering-fixture.png",image.EncodeToPNG());RenderTexture.active=previous;
   Destroy(root);Destroy(cameraObject);Destroy(image);target.Release();Destroy(target);
   Check(PlayerPrefs.GetString(GameLanguage.PreferenceKey,"<unset>")==oldPreference,"QA leaves player's saved language preference untouched");
  }
 }
}
