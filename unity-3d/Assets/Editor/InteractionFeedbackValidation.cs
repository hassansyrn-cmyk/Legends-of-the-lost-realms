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
 [InitializeOnLoad] public static class InteractionFeedbackValidation {
  const string Key="LostRealms.InteractionFeedbackValidation";
  static InteractionFeedbackValidation(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
  public static void Run(){
   if(!RealmGame.Testing)throw new Exception("Use -menuVisualProbe to preserve player saves");
   SovereignSkinBuilder.Validate();
   Directory.CreateDirectory("Validation/InteractionFeedback");SessionState.SetBool(Key,true);
   EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
   var assembly=typeof(EditorWindow).Assembly;var viewType=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);view.Show();view.Focus();
   var sizeType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizeType);var sizes=singleton.GetProperty("instance").GetValue(null);
   var groupType=sizeType.GetProperty("currentGroupType",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(sizes);var group=sizeType.GetMethod("GetGroup").Invoke(sizes,new[]{groupType});
   var size=Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"),new object[]{Enum.ToObject(assembly.GetType("UnityEditor.GameViewSizeType"),1),1280,720,"Interaction feedback"});
   group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});viewType.GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null)-1);
   EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
  }
  static void Tick(){if(!EditorApplication.isPlaying||!RealmGame.I)return;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;new GameObject("Interaction feedback checks").AddComponent<InteractionFeedbackDriver>();}
 }
 public sealed class InteractionFeedbackDriver:MonoBehaviour {
  readonly StringBuilder report=new StringBuilder();
  void Check(bool success,string message){if(!success)throw new Exception(message);report.AppendLine("PASS "+message);Debug.Log("FEEDBACK_PASS "+message);}
  static void Field(RealmGame game,string name,object value)=>typeof(RealmGame).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(game,value);
  void Capture(string name){ScreenCapture.CaptureScreenshot(Path.GetFullPath("Validation/InteractionFeedback/"+name+".png"));}
  void View(Transform target){
   var game=RealmGame.I;game.CameraRig.enabled=false;var camera=game.CameraRig.GetComponent<Camera>();
   camera.fieldOfView=48;camera.transform.position=target.position+new Vector3(4,3.5f,-6);camera.transform.LookAt(target.position+Vector3.up*1.1f);
   game.Notice="";Field(game,"noticeUntil",0f);
  }
  IEnumerator Start(){
   var run=Checks();
   while(true){bool next;try{next=run.MoveNext();}catch(Exception error){Debug.LogError("INTERACTION_FEEDBACK_FAILED "+error);File.WriteAllText("Validation/InteractionFeedback/results.txt",report+"FAIL "+error);EditorApplication.Exit(1);yield break;}if(!next)break;yield return run.Current;}
   report.AppendLine("INTERACTION_FEEDBACK_PASSED");File.WriteAllText("Validation/InteractionFeedback/results.txt",report.ToString());Debug.Log("INTERACTION_FEEDBACK_PASSED");EditorApplication.Exit(0);
  }
  IEnumerator Checks(){
   ShaderUtil.allowAsyncCompilation=false;
   yield return null;var game=RealmGame.I;
   game.Save=new Progress();var normal=game.Save;Check(game.TryEnableDeveloperMode("3258"),"Developer password accepted");
   Check(game.Save.unlocked==Campaign.Chapters&&normal.unlocked==1,"All 25 test chapters unlocked without changing normal progression");
   game.Screen=GameScreen.Map;Time.timeScale=0;Field(game,"atlasPage",1);Capture("developer-all-chapters");yield return new WaitForSecondsRealtime(.5f);
   game.DisableDeveloperMode();Check(game.Save==normal&&game.Save.unlocked==1,"Normal chapter locks restored");
   Check(InteractionCountdown.Fraction(0,2)==1&&InteractionCountdown.Fraction(1,2)==.5f&&InteractionCountdown.Fraction(2,2)==0,"Countdown decreases from full to empty");
   game.LoadLevel(11);game.Player.Health=10000;foreach(var enemy in game.Enemies)if(enemy){enemy.enabled=false;enemy.Health=0;}
   var node=game.Adventure.Nodes[0];node.Guards.Clear();game.Player.Warp(node.transform.position+Vector3.left*.65f+Vector3.up*.05f);View(node.transform);
   yield return new WaitForSeconds(.4f);var countdown=node.GetComponent<InteractionCountdown>();
   Check(countdown&&countdown.Visible&&countdown.RemainingSeconds>0,"Real ritual interaction displays its timer");float before=countdown.RemainingFraction;
   yield return new WaitForSeconds(.2f);Check(countdown.RemainingFraction<before,"Displayed bar drains as gameplay channel advances");
   Time.timeScale=0;Capture("ritual-countdown");yield return new WaitForSecondsRealtime(.5f);
   game.Pause();float paused=countdown.RemainingSeconds;yield return new WaitForSecondsRealtime(.3f);Check(Mathf.Approximately(paused,countdown.RemainingSeconds)&&!countdown.Visible,"Pause freezes the timer and hides its bar");
   game.Resume();Time.timeScale=1;game.Player.Warp(game.World.Spawn);yield return new WaitForSeconds(.2f);Check(!countdown.Visible&&!node.Completed,"Leaving cancels and hides an incomplete interaction");
   game.Save.relicA=7;game.Player.Warp(node.transform.position+Vector3.left*.65f+Vector3.up*.05f);yield return new WaitForSeconds(.4f);
   Check(countdown.RemainingSeconds<1.875f&&countdown.RemainingSeconds>0,"Faster channel relic uses its real reduced duration");
   yield return new WaitForSeconds(2f);Check(node.Completed&&node.GetComponent<ObjectiveRestoration>(),"Proximity completion applies restored visuals");
   yield return new WaitForSeconds(.1f);Check(!countdown.Visible,"Completed interaction hides countdown");
   game.LoadLevel(3);foreach(var enemy in game.Enemies)if(enemy){enemy.enabled=false;enemy.Health=0;}
   var key=game.Adventure.Nodes[0];key.Guards.Clear();game.Player.Warp(key.transform.position+Vector3.left*.65f+Vector3.up*.05f);View(key.transform);
   yield return new WaitForSeconds(.3f);var keyCountdown=key.GetComponent<InteractionCountdown>();
   Check(keyCountdown&&keyCountdown.Visible&&!key.Completed,"Guarded key collection shows its real countdown after guards clear");
   Time.timeScale=0;Capture("key-countdown");yield return new WaitForSecondsRealtime(.5f);Time.timeScale=1;
   yield return new WaitForSeconds(.9f);Check(key.Completed&&!key.gameObject.activeSelf&&!keyCountdown.Visible,"Collected key and its countdown disappear together");
   game.LoadLevel(2);foreach(var enemy in game.Enemies)if(enemy)enemy.enabled=false;
   var beacon=game.Adventure.Nodes[0];beacon.enabled=false;
   var renderer=beacon.GetComponentsInChildren<MeshRenderer>().First(r=>!r.GetComponent<TextMesh>());var original=renderer.sharedMaterial;var texture=original.mainTexture;var tint=original.color;var position=beacon.transform.position;
   View(beacon.transform);Time.timeScale=0;Capture("beacon-before");yield return new WaitForSecondsRealtime(.5f);
   beacon.MarkRestored();
   Check(renderer.sharedMaterial.mainTexture==texture&&renderer.sharedMaterial.color==tint&&original.color==tint,"Restoration preserves original texture, colour and shared material");
   Check(renderer.sharedMaterial.shader.name=="LostRealms/ObjectiveJewel","Only jewel pixels receive the new emission shader");
   Check(beacon.transform.position==position,"Restored light is a child and cannot move the objective");
   beacon.MarkRestored();Check(beacon.GetComponents<ObjectiveRestoration>().Length==1,"Repeated restoration cannot duplicate effects");
   Capture("beacon-restored");yield return new WaitForSecondsRealtime(.5f);
   var bannerRoot=new GameObject("Banner visual check");bannerRoot.transform.SetParent(game.World.transform,false);bannerRoot.transform.position=game.World.Spawn+Vector3.right*4;
   ExpansionChapter.PlacePuzzleModel("Props/Expansion/RallyBanner",bannerRoot.transform,Color.white,out _);
   ObjectiveRestoration.Activate(bannerRoot);var green=Resources.Load<Texture2D>("Props/Expansion/Textures/RallyBanner_green");
   Check(bannerRoot.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial.mainTexture==green),"Banner retains its red-to-green texture change");
   Check(bannerRoot.GetComponentInChildren<Light>()==null,"Banner keeps its original effect without a new cyan light");
   View(bannerRoot.transform);Capture("banner-restored");yield return new WaitForSecondsRealtime(.5f);
   var shader=Resources.Load<Shader>("Shaders/ObjectiveJewel");Check(shader&&shader.isSupported&&!ShaderUtil.GetShaderMessages(shader).Any(message=>message.severity.ToString()=="Error"),"Jewel shader renders on Direct3D 11 without compiler errors");
   game.LoadLevel(21);foreach(var enemy in game.Enemies)if(enemy){enemy.enabled=false;enemy.Health=0;}
   var route=game.Astral.Mechanisms[0];Check(route.Attune(route.Side),"Astral focus activates through its existing controller");
   var focus=route.Focuses[route.Side];View(focus);yield return new WaitForSeconds(.2f);
   Check(focus.GetComponent<ObjectiveRestoration>()&&focus.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterial.shader==shader),"Astral controller retains selective jewel glow while updating its legacy emission property");
   Time.timeScale=0;Capture("astral-focus-restored");yield return new WaitForSecondsRealtime(.5f);
   game.Screen=GameScreen.Paused;Field(game,"showVault",true);
   Field(game,"vaultState",Enum.Parse(typeof(RealmGame).GetField("vaultState",BindingFlags.Instance|BindingFlags.NonPublic).FieldType,"Opening"));Field(game,"vaultAnimTimer",.25f);
   Capture("vault-countdown");yield return new WaitForSecondsRealtime(.5f);Field(game,"showVault",false);
   game.Screen=GameScreen.Paused;game.Save.skins.Add(6);game.EquipSkin(SkinId.AetherSovereign);Field(game,"showSkins",true);game.SelectWardrobePreview(6);
   Capture("sovereign-quality");yield return new WaitForSecondsRealtime(.5f);
   Check(game.Player.Visual.GetComponentInChildren<LODGroup>().GetLODs().Length==2,"Improved skin provides near and distance LODs");
   var skin=game.Player.Visual.GetComponentsInChildren<SkinnedMeshRenderer>().First();Check(skin.sharedMaterial.mainTexture.width==2048,"Improved skin uses the full 2048px texture");
   foreach(var path in Directory.GetFiles("Validation/InteractionFeedback","*.png")){
    var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));int magenta=0;var pixels=image.GetPixels32();
    foreach(var pixel in pixels)if(pixel.r>220&&pixel.b>220&&pixel.g<35)magenta++;
    UnityEngine.Object.Destroy(image);Check(magenta<pixels.Length*.005f,"No magenta shader failure in "+Path.GetFileName(path));
   }
  }
 }
}
