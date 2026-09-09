using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Animations;
using UnityEngine.Playables;
using System.IO;
using System.Linq;
public static class VisualReview {
 public static void Render(){EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);Directory.CreateDirectory("Validation/Models");RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.6f,.6f,.6f);RenderSettings.fog=false;
  var light=new GameObject("Review key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(35,190,0);
  var camera=new GameObject("Review camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.06f,.085f);camera.transform.position=new Vector3(0,1.6f,6.5f);camera.transform.LookAt(new Vector3(0,1.5f,0));camera.fieldOfView=35;
  foreach(string role in new[]{"Aster","Goblin","Elemental","Demon","Heartwood","Sunscar","Whiteout","Caster"})foreach(string pose in new[]{"idle","attack"}){
   var source=Resources.Load<GameObject>("Characters/"+role);if(!source)throw new System.Exception("No prefab "+role);var model=Object.Instantiate(source);var rs=model.GetComponentsInChildren<Renderer>();var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);model.transform.localScale*=3/Mathf.Max(.1f,bounds.size.y);
   var animator=model.GetComponentInChildren<Animator>();PlayableGraph graph=default;bool animated=false;if(animator){animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;var clip=Resources.Load<AnimationClip>("Animations/"+role+"_"+pose)??Resources.Load<AnimationClip>("Animations/Shared/"+pose);if(clip){graph=PlayableGraph.Create();var output=AnimationPlayableOutput.Create(graph,"Pose",animator);var playable=AnimationClipPlayable.Create(graph,clip);output.SetSourcePlayable(playable);playable.SetTime(pose=="attack"?.5:0);graph.Play();graph.Evaluate(.016f);animated=true;}}
   var target=new RenderTexture(512,512,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var image=new Texture2D(512,512,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,512,512),0,0);image.Apply();File.WriteAllBytes("Validation/Models/"+role+"_"+pose+".png",image.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;Object.DestroyImmediate(image);Object.DestroyImmediate(target);if(animated)graph.Destroy();Object.DestroyImmediate(model);
  }
  Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(light.gameObject);EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");Debug.Log("MODEL_POSE_RENDERS_COMPLETE");
 }
}

