using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 public sealed class ObjectiveRestoration:MonoBehaviour {
  readonly List<Material> ownedMaterials=new List<Material>();
  public static void Activate(GameObject root){
   if(!root||root.GetComponent<ObjectiveRestoration>())return;
   root.AddComponent<ObjectiveRestoration>().Restore();
  }
  void Restore(){
   var glow=new Color(.35f,.88f,1f);var shader=Resources.Load<Shader>("Shaders/ObjectiveJewel");
   var green=Resources.Load<Texture2D>("Props/Expansion/Textures/RallyBanner_green");
   Bounds bounds=new Bounds(transform.position,Vector3.one);bool found=false,containsBanner=false;
   foreach(var renderer in GetComponentsInChildren<Renderer>()){
    if(renderer.GetComponent<TextMesh>()||(!(renderer is MeshRenderer)&&!(renderer is SkinnedMeshRenderer)))continue;
    if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
    var materials=renderer.sharedMaterials;
    bool bannerRenderer=false;
    for(int i=0;i<materials.Length;i++){
     var source=materials[i];if(!source)continue;
     var material=new Material(source);ownedMaterials.Add(material);
     bool banner=source.name.Contains("RallyBanner")||renderer.name.Contains("Banner");
     if(banner){
      containsBanner=true;
      bannerRenderer=true;
      if(green)material.mainTexture=green;
      material.color=Color.white;material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.2f,.95f,.35f)*.75f);
     }else if(source.mainTexture&&shader){
      material.shader=shader;material.SetColor("_JewelGlow",glow);material.SetFloat("_GlowStrength",1.8f);
     }
     materials[i]=material;
    }
    renderer.sharedMaterials=materials;
    // Remove the old whole-object emission override without touching base colour.
    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);block.SetColor("_EmissionColor",bannerRenderer?new Color(.2f,.95f,.35f)*.75f:Color.black);renderer.SetPropertyBlock(block);
   }
   if(containsBanner){foreach(var text in GetComponentsInChildren<TextMesh>())text.color=new Color(.4f,1f,.55f);return;}
   foreach(var gem in GetComponentsInChildren<GemVisual>())gem.Accent=glow;
   foreach(var text in GetComponentsInChildren<TextMesh>())text.color=new Color(1f,.86f,.55f);
   var lamp=new GameObject("Restored jewel light");lamp.transform.SetParent(transform,false);
   lamp.transform.position=new Vector3(bounds.center.x,bounds.max.y-.25f,bounds.center.z);
   var light=lamp.AddComponent<Light>();light.type=LightType.Point;light.color=glow;light.intensity=.65f;light.range=3.2f;light.shadows=LightShadows.None;
   Vfx.Play("eric_FX_LightPillar",new Vector3(bounds.center.x,bounds.min.y,bounds.center.z),Quaternion.identity,.55f);
  }
  void OnDestroy(){foreach(var material in ownedMaterials)if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
 }
}
