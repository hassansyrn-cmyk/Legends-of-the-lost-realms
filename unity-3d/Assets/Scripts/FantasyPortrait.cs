using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LostRealms {
 // Copy only the visible mesh state, never Instantiate the live hero or its
 // MonoBehaviours. No Awake, animation graph, collider or weapon logic can run.
 public static class FantasyPortrait {
  public static RenderTexture Capture(Hero hero){
   if(!hero||!hero.Visual||hero.Visual.UsesFallback)throw new InvalidOperationException("Aster's genuine visual is not available for portrait capture.");
   const int layer=31;
   var origin=new Vector3(18000,18000,18000);
   GameObject studio=null;RenderTexture target=null;
   var baked=new List<Mesh>();
   try{
    studio=new GameObject("Aster portrait • isolated visual-only studio");
    studio.SetActive(false);studio.transform.position=origin;
    var inverse=Quaternion.Inverse(hero.transform.rotation);
    Bounds bounds=new Bounds();bool found=false;
    foreach(var source in hero.Visual.GetComponentsInChildren<Renderer>()){
     if(!source.enabled||!source.gameObject.activeInHierarchy||source.GetComponentInParent<EquippedWeapon>())continue;
     Mesh mesh=null;
     if(source is SkinnedMeshRenderer skin){mesh=new Mesh{name="Aster portrait baked pose"};baked.Add(mesh);skin.BakeMesh(mesh);}
     else if(source is MeshRenderer){var filter=source.GetComponent<MeshFilter>();if(filter)mesh=filter.sharedMesh;}
     if(!mesh||mesh.vertexCount==0)continue;
     var copy=new GameObject(source.name+" • render only",typeof(MeshFilter),typeof(MeshRenderer));copy.layer=layer;copy.transform.SetParent(studio.transform,false);
     copy.transform.position=origin+inverse*(source.transform.position-hero.transform.position);
     copy.transform.rotation=inverse*source.transform.rotation;copy.transform.localScale=source.transform.lossyScale;
     copy.GetComponent<MeshFilter>().sharedMesh=mesh;
     var renderer=copy.GetComponent<MeshRenderer>();renderer.sharedMaterials=source.sharedMaterials;
     renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;renderer.lightProbeUsage=LightProbeUsage.Off;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;
     // Compute from copied geometry, even while the staging hierarchy is inactive.
     var local=mesh.bounds;var worldBounds=new Bounds(copy.transform.TransformPoint(local.center),Vector3.zero);
     for(int corner=0;corner<8;corner++)worldBounds.Encapsulate(copy.transform.TransformPoint(local.center+Vector3.Scale(local.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
     if(!found){bounds=worldBounds;found=true;}else bounds.Encapsulate(worldBounds);
    }
    if(!found)throw new InvalidOperationException("Aster portrait capture found no visible character meshes.");
    var cameraObject=new GameObject("Portrait camera",typeof(Camera));cameraObject.transform.SetParent(studio.transform,false);
    var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<layer;
    camera.orthographic=true;camera.orthographicSize=Mathf.Max(.2f,bounds.size.y*.31f);
    camera.nearClipPlane=.05f;camera.farClipPlane=12;camera.clearFlags=CameraClearFlags.SolidColor;
    camera.backgroundColor=new Color(.025f,.06f,.09f,0);camera.allowHDR=false;camera.allowMSAA=true;
    var focus=new Vector3(bounds.center.x,bounds.max.y-bounds.size.y*.28f,bounds.center.z);
    camera.transform.position=focus+new Vector3(.12f,.02f,4);camera.transform.LookAt(focus);
    AddLight(studio.transform,layer,new Vector3(25,155,0),1.35f,new Color(1,.90f,.76f));
    AddLight(studio.transform,layer,new Vector3(340,215,0),.8f,new Color(.56f,.78f,1));
    target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32){name="Aster • genuine runtime portrait",antiAliasing=2};
    if(!target.Create())throw new InvalidOperationException("Could not allocate Aster portrait render texture.");
    camera.targetTexture=target;studio.SetActive(true);camera.Render();camera.targetTexture=null;
    return target;
   }catch{if(target){target.Release();UnityEngine.Object.Destroy(target);}throw;}
   finally{
    if(studio){studio.SetActive(false);UnityEngine.Object.Destroy(studio);}
    foreach(var mesh in baked)UnityEngine.Object.Destroy(mesh);
   }
  }
  static void AddLight(Transform parent,int layer,Vector3 rotation,float intensity,Color color){
   var lightObject=new GameObject("Portrait light",typeof(Light));lightObject.transform.SetParent(parent,false);lightObject.transform.rotation=Quaternion.Euler(rotation);
   var light=lightObject.GetComponent<Light>();light.type=LightType.Directional;light.cullingMask=1<<layer;light.intensity=intensity;light.color=color;light.shadows=LightShadows.None;
  }
 }
}