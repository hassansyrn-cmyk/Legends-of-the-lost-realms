using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
namespace LostRealms {
 public static class ExpansionTripoSetup {
  public static readonly string[] Roles={"StormStalker","StormScribe","TempestRegent","AstralSentinel","StarOracle","FractureKeeper"};
  public static void Prepare(){
   AssetDatabase.Refresh();
   foreach(string role in Roles){
    string path="Assets/Resources/Characters/Textures/"+role+"_normal.png";
    var imp=AssetImporter.GetAtPath(path) as TextureImporter;
    if(imp){imp.textureType=TextureImporterType.NormalMap;imp.SaveAndReimport();}
   }
   StormAssetSetup.Prepare();AstralAssetSetup.Prepare();
   foreach(string role in Roles){var source=Resources.Load<AnimationClip>("Animations/"+role+"_gethit");var hit=UnityEngine.Object.Instantiate(source);hit.name=role+"_hit";string dst="Assets/Resources/Animations/"+hit.name+".anim";var old=AssetDatabase.LoadAssetAtPath<AnimationClip>(dst);if(old){EditorUtility.CopySerialized(hit,old);UnityEngine.Object.DestroyImmediate(hit);}else AssetDatabase.CreateAsset(hit,dst);}
   Directory.CreateDirectory("Validation/TripoEnemies");
   var report=new System.Text.StringBuilder();
   foreach(string role in Roles){
    var obj=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/"+role));
    var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();
    if(!skin||skin.bones.Length<15||skin.sharedMesh.vertexCount>90000)throw new Exception("Invalid optimized rig "+role);
    var mat=new Material(Shader.Find("Standard")){name=role,mainTexture=Resources.Load<Texture2D>("Characters/Textures/"+role+"_basecolor"),color=Color.white};
    mat.SetFloat("_Glossiness",.24f);
    var normal=Resources.Load<Texture2D>("Characters/Textures/"+role+"_normal");if(normal){mat.SetTexture("_BumpMap",normal);mat.EnableKeyword("_NORMALMAP");}
    if(!mat.mainTexture)throw new Exception("Texture missing "+role);
    foreach(var r in obj.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
    report.AppendLine(role+" bones="+skin.bones.Length+" vertices="+skin.sharedMesh.vertexCount);
    foreach(string state in new[]{"idle","walk","run","attack","attack_2","cast","gethit","victory","death"}){
     var clip=Resources.Load<AnimationClip>("Animations/"+role+"_"+state);if(!clip)throw new Exception("Missing "+role+" "+state);
     var bindings=AnimationUtility.GetCurveBindings(clip);
     int missing=bindings.Count(b=>b.type==typeof(Transform)&&b.path!=""&&!obj.transform.Find(b.path));
     if(missing>0)throw new Exception("Unbound animation "+role+" "+state+": "+missing);
     bool motion=bindings.Any(b=>{var c=AnimationUtility.GetEditorCurve(clip,b);return c!=null&&c.keys.Any(k=>Mathf.Abs(k.value-c.keys[0].value)>.01f);});
     if(!motion)throw new Exception("Static animation "+role+" "+state);
     report.AppendLine("  "+state+" length="+clip.length+" bindings="+bindings.Length+" missing="+missing);
    }
    if(role=="FractureKeeper"&&skin.bones.Count(b=>b.name.StartsWith("UpperArm"))!=6)throw new Exception("Keeper must have six independent arms");
    UnityEngine.Object.DestroyImmediate(obj);UnityEngine.Object.DestroyImmediate(mat);
   }
   File.WriteAllText("Validation/TripoEnemies/rig-validation.txt",report.ToString());
   AssetDatabase.SaveAssets();Render();Debug.Log("TRIPO_ENEMY_INTEGRATION_PASSED");
  }
  public static void Render(){
   Directory.CreateDirectory("Validation/TripoEnemies");
   var root=new GameObject("Tripo review");var light=new GameObject("Review light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.transform.rotation=Quaternion.Euler(35,-35,0);
   var cam=new GameObject("Review camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=1.45f;cam.backgroundColor=new Color(.12f,.16f,.21f);cam.clearFlags=CameraClearFlags.SolidColor;cam.transform.position=new Vector3(3,1.3f,5);cam.transform.LookAt(new Vector3(0,1,0));
   var rt=new RenderTexture(640,640,24);cam.targetTexture=rt;var oldAmbient=RenderSettings.ambientLight;RenderSettings.ambientLight=new Color(.55f,.55f,.55f);
   foreach(string role in Roles){
    var obj=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/"+role),root.transform);var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();
    var mat=new Material(Shader.Find("Standard")){mainTexture=Resources.Load<Texture2D>("Characters/Textures/"+role+"_basecolor"),color=Color.white};mat.SetFloat("_Glossiness",.2f);skin.sharedMaterial=mat;
    var bounds=skin.bounds;obj.transform.localScale=Vector3.one*(2/bounds.size.y);bounds=skin.bounds;obj.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
    cam.orthographicSize=role=="TempestRegent"?2.6f:1.45f;
    foreach(string state in new[]{"idle","walk","attack","attack_2","cast","death"}){
     var clip=Resources.Load<AnimationClip>("Animations/"+role+"_"+state);
     var graph=PlayableGraph.Create("Tripo review pose");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var playable=AnimationClipPlayable.Create(graph,clip);var output=AnimationPlayableOutput.Create(graph,"Pose",obj.GetComponent<Animator>());output.SetSourcePlayable(playable);graph.Play();playable.SetTime(clip.length*(state=="death"?.9f:state=="walk"?.25f:.48f));graph.Evaluate(.00001f);skin.updateWhenOffscreen=true;
     var posed=new Mesh();skin.BakeMesh(posed,false);var display=new GameObject("Posed review mesh");display.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);display.transform.localScale=skin.transform.lossyScale;display.AddComponent<MeshFilter>().sharedMesh=posed;display.AddComponent<MeshRenderer>().sharedMaterial=mat;skin.enabled=false;
     var rb=display.GetComponent<Renderer>().bounds;cam.transform.position=rb.center+new Vector3(3,1,5);cam.transform.LookAt(rb.center);cam.orthographicSize=Mathf.Max(rb.size.y,rb.size.x)*.75f;
     cam.Render();RenderTexture.active=rt;var tex=new Texture2D(640,640,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,640,640),0,0);tex.Apply();File.WriteAllBytes("Validation/TripoEnemies/"+role+"_"+state+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);skin.enabled=true;UnityEngine.Object.DestroyImmediate(display);UnityEngine.Object.DestroyImmediate(posed);graph.Destroy();
    }
    UnityEngine.Object.DestroyImmediate(obj);UnityEngine.Object.DestroyImmediate(mat);
   }
   RenderSettings.ambientLight=oldAmbient;RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(cam.gameObject);UnityEngine.Object.DestroyImmediate(light.gameObject);UnityEngine.Object.DestroyImmediate(root);
  }
  public static void Diagnose(){
   var obj=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Characters/StormStalker"));var skin=obj.GetComponentInChildren<SkinnedMeshRenderer>();var sb=new System.Text.StringBuilder();
   void Dump(string phase){var m=new Mesh();skin.BakeMesh(m,false);sb.AppendLine(phase+" renderer="+skin.bounds+" baked="+m.bounds+" localToWorld="+skin.transform.lossyScale+" submeshes="+m.subMeshCount);foreach(var t in obj.GetComponentsInChildren<Transform>())sb.AppendLine(t.name+" p="+t.localPosition+" s="+t.localScale+" r="+t.localEulerAngles);UnityEngine.Object.DestroyImmediate(m);}
   Dump("rest");obj.transform.localScale=Vector3.one*(1.8f/skin.bounds.size.y);Dump("normalized");
   var graph=PlayableGraph.Create();graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);var p=AnimationClipPlayable.Create(graph,Resources.Load<AnimationClip>("Animations/StormStalker_idle"));var o=AnimationPlayableOutput.Create(graph,"pose",obj.GetComponent<Animator>());o.SetSourcePlayable(p);graph.Play();p.SetTime(1);graph.Evaluate(.00001f);Dump("animated");graph.Destroy();File.WriteAllText("Validation/TripoEnemies/diagnose.txt",sb.ToString());UnityEngine.Object.DestroyImmediate(obj);
  }
 }
}
