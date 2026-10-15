using UnityEngine;
namespace LostRealms {
 public static class ExpansionEnemyAccessories {
  static readonly System.Collections.Generic.Dictionary<string,Material> Materials=new System.Collections.Generic.Dictionary<string,Material>();
  public static void Attach(CharacterVisual visual,int kind){
   if(!visual)return;
   if(kind==27)Add(visual,"StormStaff","Hand_R",1.85f);
   if(kind==29){Add(visual,"SentinelLance","Hand_R",2f);Add(visual,"SentinelBuckler","Hand_L",.6f);}
  }
  static void Add(CharacterVisual visual,string name,string handName,float length){
   Transform hand=null;foreach(var t in visual.GetComponentsInChildren<Transform>())if(t.name==handName){hand=t;break;}
   var prefab=Resources.Load<GameObject>("Characters/Accessories/"+name);if(!hand||!prefab)return;
   var holder=new GameObject(name+" grip");holder.transform.rotation=visual.transform.parent.rotation;
   var model=Object.Instantiate(prefab,holder.transform);model.transform.localPosition=Vector3.zero;
   var renderers=model.GetComponentsInChildren<Renderer>();if(renderers.Length==0){Object.Destroy(holder);return;}
   Bounds b=renderers[0].bounds;foreach(var r in renderers)b.Encapsulate(r.bounds);
   float height=kindHeight(visual);float actual=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));if(actual>.001f)holder.transform.localScale=Vector3.one*(length*height/2f/actual);
   if(!Materials.TryGetValue(name,out var mat)){var texture=Resources.Load<Texture2D>("Characters/Accessories/"+name+"_basecolor");mat=new Material(Shader.Find("Standard")){name=name,mainTexture=texture,color=Color.white};mat.SetFloat("_Glossiness",.24f);Materials[name]=mat;}foreach(var r in renderers)r.sharedMaterial=mat;
   holder.transform.position=hand.position;holder.transform.SetParent(hand,true);
  }
  static float kindHeight(CharacterVisual v)=>v.name.Contains("StormScribe")?2.25f:1.9f;
 }
}
