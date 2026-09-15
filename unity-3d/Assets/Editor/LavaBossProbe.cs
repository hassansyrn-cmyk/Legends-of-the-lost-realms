using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LostRealms {
 public static class LavaBossProbe {
  [MenuItem("Lost Realms/Lava Boss/Validate rig and render")]
  public static void Run(){
   var report=new StringBuilder();var scene=EditorSceneManager.NewPreviewScene();
   var root=new GameObject("Boss validation");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
   var visual=CharacterVisual.Create("LavaBoss",root.transform,3.6f,Color.white);
   var animator=visual.animator;if(!animator||!animator.avatar||!animator.avatar.isValid||!animator.isHuman)throw new Exception("Invalid lava boss avatar");
   animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   LavaBossWeapon.Attach(visual);
   var weapon=visual.GetComponentInChildren<LavaBossWeapon>();if(!weapon)throw new Exception("Missing boss weapon");
   var head=animator.GetBoneTransform(HumanBodyBones.Head);var hips=animator.GetBoneTransform(HumanBodyBones.Hips);
   report.AppendLine("Avatar valid; head above hips: "+(head.position.y>hips.position.y));
   if(head.position.y<=hips.position.y)throw new Exception("Boss is upside down");
   var graph=PlayableGraph.Create("Lava boss probe");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   var output=AnimationPlayableOutput.Create(graph,"Boss",animator);
   var cameraObject=new GameObject("Probe camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.09f,.12f);camera.transform.position=new Vector3(6,3.5f,7);camera.transform.LookAt(new Vector3(0,1.8f,0));camera.fieldOfView=38;
   var lightObject=new GameObject("Probe key");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.25f;light.transform.rotation=Quaternion.Euler(35,-135,0);
   var fillObject=new GameObject("Probe fill");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,scene);var fill=fillObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.65f;fill.color=new Color(.55f,.68f,1);fill.transform.rotation=Quaternion.Euler(20,40,0);
   var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(floor,scene);floor.transform.localScale=Vector3.one*2;var floorMat=new Material(Shader.Find("Standard"));floorMat.color=new Color(.16f,.17f,.2f);floor.GetComponent<Renderer>().sharedMaterial=floorMat;
   string folder="Validation/LavaBoss-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(folder);
   try{
    foreach(string state in new[]{"idle","walk","run","slash","overhead","attack_1","attack_2","attack_3","charged","roar","flex","death"}){
     var clip=Resources.Load<AnimationClip>("Animations/LavaBoss/"+state);if(!clip||!clip.isHumanMotion)throw new Exception("Missing clip "+state);
     foreach(var b in AnimationUtility.GetCurveBindings(clip))if(b.propertyName.StartsWith("RootT")){var c=AnimationUtility.GetEditorCurve(clip,b);if(c.keys.Any(k=>Mathf.Abs(k.value-c.keys[0].value)>.0001f))throw new Exception("Drifting root "+state);}
     var playable=AnimationClipPlayable.Create(graph,clip);output.SetSourcePlayable(playable);graph.Play();playable.SetTime(clip.length*.15f);graph.Evaluate(0);
     var arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);var q=arm.localRotation;
     playable.SetTime(clip.length*.5f);graph.Evaluate(0);
     weapon.SendMessage("LateUpdate");
     float motion=Quaternion.Angle(q,arm.localRotation);report.AppendLine(state+" duration="+clip.length.ToString("F2")+" armMotion="+motion.ToString("F2")+" headY="+head.position.y.ToString("F2"));
     if(state=="slash"&&motion<1)throw new Exception("Slash does not animate");
     if(state=="idle"||state=="slash"||state=="overhead"||state=="death")Capture(camera,folder+"/"+state+".png");
     graph.DestroyPlayable(playable);
    }
    report.AppendLine("LAVA_BOSS_PROBE_PASSED");report.AppendLine("Renders: "+folder);File.WriteAllText("Validation/lava-boss-probe.txt",report.ToString());Debug.Log(report.ToString());
   }finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(floorMat);EditorSceneManager.ClosePreviewScene(scene);}
  }
  static void Capture(Camera camera,string path){var rt=RenderTexture.GetTemporary(1000,1000,24);var previous=RenderTexture.active;var image=new Texture2D(1000,1000,TextureFormat.RGB24,false);try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1000,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}}
 }
}
