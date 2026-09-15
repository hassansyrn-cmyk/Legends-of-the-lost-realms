using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

namespace LostRealms {
 public static class QualityValidation {
  [MenuItem("Lost Realms/Quality/Repair Warden Avatar")]
  public static void RepairWardenAvatar(){
   const string path="Assets/Resources/Characters/DogKnight.fbx";
   string original=File.ReadAllText(path+".meta");
   try{
    var importer=(ModelImporter)AssetImporter.GetAtPath(path);
    importer.animationType=ModelImporterAnimationType.Human;
    importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
    importer.SaveAndReimport();
    var avatar=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Avatar>().FirstOrDefault(a=>a.isHuman&&a.isValid);
    if(!avatar)throw new System.Exception("Warden humanoid mapping failed; original importer will be restored.");
    Debug.Log("WARDEN_AVATAR_VALID "+avatar.name);
    InspectAnimation();PropCensus.Run();
   }catch{
    File.WriteAllText(path+".meta",original);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);throw;
   }
  }
  public static void InspectAnimation(){
   var report=new StringBuilder();
   foreach(string role in new[]{"DogKnight","Footman"}){
    var model=Object.Instantiate(Resources.Load<GameObject>("Characters/"+role));
    var animator=model.GetComponentInChildren<Animator>();
    report.AppendLine(role+" animator="+(bool)animator+" avatar="+(animator&&animator.avatar?animator.avatar.name:"none"));
    foreach(var avatar in Resources.LoadAll<Avatar>("Characters/"+role))report.AppendLine("avatar="+avatar.name+" human="+avatar.isHuman+" valid="+avatar.isValid);
    foreach(string state in new[]{"idle","walk","attack","death"}){
     var clip=Resources.Load<AnimationClip>("Animations/"+role+"_"+state);if(!clip)continue;
     int resolved=0,missing=0;foreach(var b in AnimationUtility.GetCurveBindings(clip))if(b.type==typeof(Transform)){if(model.transform.Find(b.path)||b.path=="")resolved++;else missing++;}
     report.AppendLine(state+" humanoid="+clip.isHumanMotion+" transformBindings="+resolved+" missing="+missing);
    }
    foreach(var bone in model.GetComponentsInChildren<Transform>())report.AppendLine("bone "+bone.name);
    Object.DestroyImmediate(model);
   }
   File.WriteAllText("Validation/quality-animation.txt",report.ToString());
  }
  // Run with -batchmode -force-d3d11 -realmTest; no character rebake or save writes.
  public static void Run(){
   EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
   EditorApplication.EnterPlaymode();
  }
 }
}
