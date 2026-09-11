using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LostRealms {
 // Scatters imported low-poly nature props (SimpleNaturePack) around each island
 // so the realms use real models instead of primitives. Models live in
 // Resources/Props/Nature; one shared palette texture feeds a single material.
 public static class RealmProps {
  static readonly Material[] byRealm=new Material[3];
  static readonly Dictionary<string,GameObject> cache=new Dictionary<string,GameObject>();

  // Realm 2 uses a snow-converted palette so frozen islands read as snow-laden;
  // realms 0/1 use the lush palette.
  static Material ForRealm(int realm){
   if(realm<0||realm>2)realm=0;
   if(byRealm[realm])return byRealm[realm];
   var texture=Resources.Load<Texture2D>(realm==2?"Props/Textures/Nature_snow":"Props/Textures/Nature_basecolor");
   if(!texture)return null;
   var material=new Material(Shader.Find("Standard")){name="Realm nature "+realm,color=Color.white};
   material.mainTexture=texture;material.SetFloat("_Metallic",0f);material.SetFloat("_Glossiness",.18f);
   byRealm[realm]=material;return material;
  }
  static GameObject Model(string name){
   if(cache.TryGetValue(name,out var cached))return cached;
   var model=Resources.Load<GameObject>("Props/Nature/"+name);
   cache[name]=model;return model;
  }
  static string[][] Sets(int realm){
   if(realm==0)return new[]{new[]{"Tree_01","Tree_03","Tree_04"},new[]{"Rock_01","Rock_02","Rock_03"},new[]{"Bush_01","Bush_02","Bush_03","Grass_01","Grass_02","Flowers_01","Mushroom_01"}};
   if(realm==1)return new[]{new[]{"Tree_02","Tree_05"},new[]{"Rock_03","Rock_04","Rock_05"},new[]{"Grass_02","Mushroom_01","Stump_01","Bush_02"}};
   return new[]{new[]{"Tree_05","Tree_02"},new[]{"Rock_04","Rock_05","Rock_01"},new[]{"Grass_02","Bush_03","Stump_01"}};
  }
  public static void Scatter(Transform world,int realm,System.Random rng){
   var material=ForRealm(realm);if(!material)return;
   var sets=Sets(realm);
   foreach(Transform island in world){
    if(island.name!="Island")continue;
    var surface=island.Find("Realm surface");if(!surface)continue;
    float width=surface.localScale.x-.08f,length=surface.localScale.z-.08f;
    if(width<=7f)continue;
    int trees=2+rng.Next(0,2);
    for(int i=0;i<trees;i++)Place(island,sets[0][rng.Next(sets[0].Length)],width,length,rng,realm==0?3.2f:2.6f,3.6f,material);
    int rocks=2+rng.Next(0,3);
    for(int i=0;i<rocks;i++)Place(island,sets[1][rng.Next(sets[1].Length)],width,length,rng,.6f,1.1f,material);
    int small=5+rng.Next(0,4);
    for(int i=0;i<small;i++)Place(island,sets[2][rng.Next(sets[2].Length)],width,length,rng,.35f,.6f,material);
   }
  }
static void Place(Transform island,string name,float width,float length,System.Random rng,float hMin,float hMax,Material material){
    var source=Model(name);if(!source)return;
    float halfWidth=Mathf.Max(.6f,width*.5f-.7f);
    int side=rng.Next(0,2)==0?-1:1;
    float x=side*Mathf.Min(halfWidth,1.6f+(float)rng.NextDouble()*halfWidth);
    float z=((float)rng.NextDouble()-.5f)*Mathf.Max(.6f,length-.9f);
    var prop=Object.Instantiate(source,island);
    prop.name="Prop "+name;
    prop.transform.localPosition=new Vector3(x,.02f,z);
    prop.transform.localRotation=Quaternion.identity;
    Fit(prop,hMin+(float)rng.NextDouble()*(hMax-hMin));
    AddCollider(prop);
    prop.transform.localRotation=Quaternion.Euler(0,(float)rng.NextDouble()*360f,0);
    foreach(var renderer in prop.GetComponentsInChildren<Renderer>(true)){
     renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
    }
   }
   static void AddCollider(GameObject prop){
    if(prop.GetComponentInChildren<Collider>())return;
    var renderers=prop.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
    Bounds b=renderers[0].bounds;
    for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);
    Vector3 center=prop.transform.InverseTransformPoint(b.center);
    float s=prop.transform.localScale.x;if(s<=1e-4f)s=1f;
    if(prop.name.StartsWith("Prop Tree")){
     var col=prop.AddComponent<CapsuleCollider>();
     col.direction=1;col.center=center;col.height=Mathf.Max(.5f,b.size.y*.9f)/s;col.radius=.3f/s;
    }else{
     var col=prop.AddComponent<BoxCollider>();
     col.center=center;col.size=new Vector3(b.size.x*.92f/s,b.size.y*.9f/s,b.size.z*.92f/s);
    }
   }
  static void Fit(GameObject prop,float targetHeight){
   var renderers=prop.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
   Bounds bounds=renderers[0].bounds;
   for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
   float height=bounds.size.y;if(height>1e-4f)prop.transform.localScale=Vector3.one*(prop.transform.localScale.x*targetHeight/height);
  }
 }
}
