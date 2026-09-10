using System.Collections.Generic;
using UnityEngine;

namespace LostRealms {
 public static class RelicArt {
  static readonly Dictionary<Color,Material> GlowMaterials=new Dictionary<Color,Material>();
  static Mesh gemCore,gemShard,shrineCore,dais,smallDais,halo;

  static Material Glow(Color accent){
   if(GlowMaterials.TryGetValue(accent,out var material))return material;
   var shader=Shader.Find("Standard");
   material=new Material(shader?shader:Shader.Find("Sprites/Default"));
   material.name="Relic glow "+ColorUtility.ToHtmlStringRGB(accent);
   material.color=Color.Lerp(accent,Color.white,.12f);
   if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.72f);
   if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",.14f);
   if(material.HasProperty("_EmissionColor")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",accent*.55f);}
   GlowMaterials[accent]=material;return material;
  }

  static GameObject MeshObject(string name,Transform parent,Mesh mesh,Material material,Vector3 localPosition,Vector3 localScale){
   var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=localPosition;go.transform.localScale=localScale;
   go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go;
  }

  static Mesh Crystal(string name,int sides,float radius,float height,float waist){
   var vertices=new List<Vector3>();var triangles=new List<int>();
   int bottom=vertices.Count;vertices.Add(Vector3.down*height*.5f);
   int lower=vertices.Count;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*radius*waist,-height*.14f,Mathf.Sin(a)*radius*waist));}
   int upper=vertices.Count;for(int i=0;i<sides;i++){float a=(i+.5f)*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*radius,height*.12f,Mathf.Sin(a)*radius));}
   int top=vertices.Count;vertices.Add(Vector3.up*height*.5f);
   for(int i=0;i<sides;i++){
    int next=(i+1)%sides;triangles.AddRange(new[]{bottom,lower+next,lower+i,lower+i,lower+next,upper+i,lower+next,upper+next,upper+i,upper+i,upper+next,top});
   }
   var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }

  static Mesh Prism(string name,int sides,float radius,float height){
   var vertices=new List<Vector3>();var triangles=new List<int>();
   int lower=0;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*radius,-height*.5f,Mathf.Sin(a)*radius));}
   int upper=vertices.Count;for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices.Add(new Vector3(Mathf.Cos(a)*radius,height*.5f,Mathf.Sin(a)*radius));}
   int bottom=vertices.Count;vertices.Add(Vector3.down*height*.5f);int top=vertices.Count;vertices.Add(Vector3.up*height*.5f);
   for(int i=0;i<sides;i++){int next=(i+1)%sides;triangles.AddRange(new[]{bottom,lower+i,lower+next,upper+i,top,upper+next,lower+i,upper+i,lower+next,lower+next,upper+i,upper+next});}
   var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }

  static Mesh Torus(string name,float majorRadius,float minorRadius,int majorSegments,int minorSegments){
   var vertices=new List<Vector3>();var triangles=new List<int>();
   for(int major=0;major<majorSegments;major++)for(int minor=0;minor<minorSegments;minor++){
    float a=major*Mathf.PI*2/majorSegments,b=minor*Mathf.PI*2/minorSegments;
    float radial=majorRadius+Mathf.Cos(b)*minorRadius;vertices.Add(new Vector3(Mathf.Cos(a)*radial,Mathf.Sin(b)*minorRadius,Mathf.Sin(a)*radial));
   }
   for(int major=0;major<majorSegments;major++)for(int minor=0;minor<minorSegments;minor++){
    int nextMajor=(major+1)%majorSegments,nextMinor=(minor+1)%minorSegments;int a=major*minorSegments+minor,b=nextMajor*minorSegments+minor,c=major*minorSegments+nextMinor,d=nextMajor*minorSegments+nextMinor;triangles.AddRange(new[]{a,b,c,c,b,d});
   }
   var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
  }

  static Mesh GemCore=>gemCore??(gemCore=Crystal("Faceted gem core",6,.34f,.92f,.64f));
  static Mesh GemShard=>gemShard??(gemShard=Crystal("Orbiting gem shard",5,.16f,.48f,.55f));
  static Mesh ShrineCore=>shrineCore??(shrineCore=Crystal("Sanctuary heart",7,.46f,2.05f,.58f));
  static Mesh Dais=>dais??(dais=Prism("Octagonal shrine dais",8,1f,.22f));
  static Mesh SmallDais=>smallDais??(smallDais=Prism("Octagonal shrine step",8,1f,.18f));
  static Mesh Halo=>halo??(halo=Torus("Relic rune halo",1f,.045f,16,4));

  public static void Gem(Transform root,Color accent){
   var visual=root.gameObject.AddComponent<GemVisual>();visual.Accent=accent;
   var glow=Glow(accent);var highlight=Glow(Color.Lerp(accent,Color.white,.58f));
   visual.Core=MeshObject("Faceted gem heart",root,GemCore,glow,Vector3.zero,Vector3.one).transform;
   var inner=MeshObject("Gem inner light",visual.Core,GemCore,highlight,Vector3.zero,Vector3.one*.52f);visual.GlowRenderers.Add(inner.GetComponent<Renderer>());
   visual.GlowRenderers.Add(visual.Core.GetComponent<Renderer>());
   var ring=MeshObject("Gem orbit halo",root,Halo,glow,Vector3.zero,Vector3.one*.52f).transform;ring.localRotation=Quaternion.Euler(62,0,0);visual.Halos.Add(ring);
   for(int i=0;i<4;i++){
    var shard=MeshObject("Orbiting gem shard",root,GemShard,glow,Vector3.zero,Vector3.one*(i%2==0?.72f:.58f)).transform;visual.Shards.Add(shard);visual.GlowRenderers.Add(shard.GetComponent<Renderer>());
   }
  }

  public static void Checkpoint(Transform root,Color accent,Color stone){
   var visual=root.gameObject.AddComponent<CheckpointVisual>();visual.Accent=accent;
   var glow=Glow(accent);var highlight=Glow(Color.Lerp(accent,Color.white,.56f));var stoneMat=Art.Material(stone*.86f);
   MeshObject("Sanctuary octagonal dais",root,Dais,stoneMat,new Vector3(0,.11f,0),new Vector3(1.72f,1,1.72f));
   MeshObject("Sanctuary inner dais",root,SmallDais,stoneMat,new Vector3(0,.31f,0),new Vector3(1.25f,1,1.25f));
   visual.Core=MeshObject("Checkpoint heart crystal",root,ShrineCore,glow,new Vector3(0,1.45f,0),Vector3.one).transform;visual.GlowRenderers.Add(visual.Core.GetComponent<Renderer>());
   var inner=MeshObject("Checkpoint inner light",visual.Core,ShrineCore,highlight,Vector3.zero,Vector3.one*.42f);visual.GlowRenderers.Add(inner.GetComponent<Renderer>());
   for(int i=0;i<4;i++){
    float a=i*Mathf.PI*.5f+Mathf.PI*.25f;var shard=MeshObject("Sanctuary sentinel shard",root,GemShard,glow,new Vector3(Mathf.Cos(a)*1.05f,.85f,Mathf.Sin(a)*1.05f),new Vector3(.82f,1.52f,.82f)).transform;shard.localRotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,14);visual.Shards.Add(shard);visual.GlowRenderers.Add(shard.GetComponent<Renderer>());
   }
   for(int i=0;i<2;i++){
    var ring=MeshObject("Floating checkpoint rune",root,Halo,glow,new Vector3(0,.58f+i*.76f,0),Vector3.one*(1.08f-i*.2f)).transform;ring.localRotation=Quaternion.Euler(i==0?0:72,i*32,0);visual.Halos.Add(ring);visual.GlowRenderers.Add(ring.GetComponent<Renderer>());
   }
   var light=root.gameObject.AddComponent<Light>();light.type=LightType.Point;light.color=accent;light.range=6.5f;light.intensity=.55f;light.shadows=LightShadows.None;light.transform.localPosition=Vector3.up*1.25f;visual.Aura=light;
  }
 }

 public abstract class RealmRelicVisual:MonoBehaviour {
  protected static readonly int EmissionColor=Shader.PropertyToID("_EmissionColor");
  MaterialPropertyBlock block;
  public Color Accent;public readonly List<Renderer> GlowRenderers=new List<Renderer>();
  protected void SetGlow(float intensity){
   Color emission=Accent*Mathf.Max(.08f,intensity);
   if(block==null)block=new MaterialPropertyBlock();
   foreach(var renderer in GlowRenderers){
    if(!renderer||!renderer.gameObject)continue;
    renderer.GetPropertyBlock(block);block.SetColor(EmissionColor,emission);renderer.SetPropertyBlock(block);
   }
  }
  protected float Clock=>RealmGame.I?RealmGame.I.Elapsed:Time.time;
 }

 public sealed class GemVisual:RealmRelicVisual {
  public Transform Core;public readonly List<Transform> Shards=new List<Transform>();public readonly List<Transform> Halos=new List<Transform>();
  void Update(){
   float t=Clock;float pulse=.72f+Mathf.Sin(t*5.4f)*.28f;
   if(Core){Core.localRotation=Quaternion.Euler(0,t*128f,0);Core.localScale=Vector3.one*(.93f+pulse*.1f);}
   for(int i=0;i<Shards.Count;i++)if(Shards[i]){float a=t*(1.35f+i*.11f)+i*Mathf.PI*2/Shards.Count;Shards[i].localPosition=new Vector3(Mathf.Cos(a)*.42f,Mathf.Sin(a*1.7f)*.13f,Mathf.Sin(a)*.42f);Shards[i].localRotation=Quaternion.Euler(t*92f+i*38f,t*70f,26);}
   for(int i=0;i<Halos.Count;i++)if(Halos[i])Halos[i].localRotation=Quaternion.Euler(62,t*(52+i*18),0);
   SetGlow(.8f+pulse*.85f);
  }
 }

 public sealed class CheckpointVisual:RealmRelicVisual {
  public Transform Core;public Light Aura;public readonly List<Transform> Shards=new List<Transform>();public readonly List<Transform> Halos=new List<Transform>();
  bool activated;float activationTime;
  public bool Activated=>activated;
  public void Activate(){if(activated)return;activated=true;activationTime=Clock;}
  void Update(){
   float t=Clock;float beat=.5f+Mathf.Sin(t*3.2f)*.18f;float activation=activated?1f:.32f;
   if(Core){Core.localPosition=Vector3.up*(1.42f+Mathf.Sin(t*2.2f)*.08f);Core.localRotation=Quaternion.Euler(0,t*(activated?64f:28f),0);}
   for(int i=0;i<Shards.Count;i++)if(Shards[i]){float a=i*Mathf.PI*.5f+Mathf.PI*.25f+t*(activated?18f:7f);Shards[i].localPosition=new Vector3(Mathf.Cos(a)*1.05f,.84f+Mathf.Sin(t*2+i)*.07f,Mathf.Sin(a)*1.05f);}
   for(int i=0;i<Halos.Count;i++)if(Halos[i]){Halos[i].localRotation=Quaternion.Euler(i==0?0:72,t*(activated?68f:24f)*(i%2==0?1:-1),0);}
   float arrival=activated?Mathf.Clamp01((t-activationTime)*3.8f):0f;float flare=activated?1f+Mathf.Exp(-Mathf.Max(0,t-activationTime)*4f)*1.4f:1f;
   transform.localScale=Vector3.one*(1f+arrival*.04f+Mathf.Sin(t*10f)*.012f*arrival);
   SetGlow((.34f+beat)*activation*flare);if(Aura)Aura.intensity=(.3f+beat*.38f)*activation*flare;
  }
 }
}
