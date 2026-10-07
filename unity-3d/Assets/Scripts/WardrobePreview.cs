using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace LostRealms {
 // A separate display model: never attaches Hero, changes its rig, or rebakes clips.
 public sealed class WardrobePreview:MonoBehaviour {
  const int PreviewLayer=31;
  readonly Vector3 stageOrigin=new Vector3(10000,10000,10000);
  GameObject stage,model;
  Camera previewCamera;
  RenderTexture frame;
  PlayableGraph idleGraph;
  AnimationClipPlayable idle;
  float idleTime;
  public int Skin {get;private set;}=-1;
  public Texture Frame=>frame;
  public string Error {get;private set;}
  public double IdleTime=>idle.IsValid()?idle.GetTime():0;

  public void Show(SkinDefinition outfit){
   if(Skin==(int)outfit.Id&&model)return;
   ClearModel();Skin=(int)outfit.Id;Error=null;
   var prefab=Resources.Load<GameObject>("Characters/"+outfit.PrefabName);
   var clip=Resources.Load<AnimationClip>("Animations/Aster/idle");
   if(!prefab||!clip){Error="Preview unavailable";return;}
   if(!stage){
    stage=new GameObject("Wardrobe display stage");stage.transform.position=stageOrigin;
    frame=new RenderTexture(384,576,24,RenderTextureFormat.ARGB32){name="Wardrobe live preview",antiAliasing=2};frame.Create();
    var cameraObject=new GameObject("Wardrobe preview camera");cameraObject.transform.SetParent(stage.transform,false);
    previewCamera=cameraObject.AddComponent<Camera>();previewCamera.enabled=false;
    previewCamera.targetTexture=frame;previewCamera.cullingMask=1<<PreviewLayer;
    previewCamera.clearFlags=CameraClearFlags.SolidColor;previewCamera.backgroundColor=new Color(.015f,.035f,.065f,0);
    previewCamera.orthographic=true;previewCamera.aspect=frame.width/(float)frame.height;
    previewCamera.nearClipPlane=.05f;previewCamera.farClipPlane=10;previewCamera.allowHDR=false;
    AddLight("Display key",new Vector3(25,-35,0),new Color(1f,.91f,.78f),1.15f);
    AddLight("Display rim",new Vector3(10,150,0),new Color(.55f,.72f,1f),.65f);
   }
   model=Instantiate(prefab,stage.transform);model.name="Outfit preview "+outfit.Name;
   model.transform.localPosition=Vector3.zero;model.transform.localRotation=Quaternion.Euler(0,-18,0);
   foreach(var part in model.GetComponentsInChildren<Transform>(true))part.gameObject.layer=PreviewLayer;
   foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
   var material=Resources.Load<Material>("Materials/"+outfit.MaterialName);
   foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)){
    if(material)renderer.sharedMaterial=material;
    renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   }
   var animator=model.GetComponentInChildren<Animator>();
   if(!animator){Error="Preview animation unavailable";return;}
   animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
   idleGraph=PlayableGraph.Create("Wardrobe idle preview");idleGraph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
   idle=AnimationClipPlayable.Create(idleGraph,clip);idle.SetApplyFootIK(false);
   var output=AnimationPlayableOutput.Create(idleGraph,"Display Aster",animator);output.SetSourcePlayable(idle);
   idleGraph.Play();idleGraph.Evaluate(0);
   var renderers=model.GetComponentsInChildren<Renderer>();
   if(renderers.Length==0){Error="Preview unavailable";return;}
   Bounds bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
   // Idle arms stay beside the body. Frame by height rather than the imported
   // bounds' outstretched bind-pose width, which makes armored outfits tiny.
   float halfHeight=bounds.extents.y*1.12f;
   previewCamera.orthographicSize=Mathf.Max(.5f,halfHeight);
   previewCamera.transform.position=bounds.center+Vector3.forward*4;
   previewCamera.transform.LookAt(bounds.center);
   previewCamera.Render();
  }
  void AddLight(string label,Vector3 rotation,Color color,float intensity){
   var lamp=new GameObject(label);lamp.transform.SetParent(stage.transform,false);lamp.transform.localRotation=Quaternion.Euler(rotation);
   var light=lamp.AddComponent<Light>();light.type=LightType.Directional;light.color=color;light.intensity=intensity;light.cullingMask=1<<PreviewLayer;
  }
  void LateUpdate(){
   if(!idleGraph.IsValid()||!previewCamera||!model)return;
   // Paused gameplay has timeScale=0; this display owns an independent clock.
   idleTime=(idleTime+Time.unscaledDeltaTime)%Mathf.Max(.01f,idle.GetAnimationClip().length);
   idle.SetTime(idleTime);idleGraph.Evaluate(0);previewCamera.Render();
  }
  void ClearModel(){
   if(idleGraph.IsValid())idleGraph.Destroy();idleTime=0;
   if(model){model.SetActive(false);Destroy(model);}model=null;
  }
  void OnDestroy(){
   ClearModel();
   if(previewCamera)previewCamera.targetTexture=null;
   if(frame){frame.Release();Destroy(frame);}
   if(stage){stage.SetActive(false);Destroy(stage);}
  }
 }
}
