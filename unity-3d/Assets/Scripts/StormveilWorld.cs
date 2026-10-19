using UnityEngine;
namespace LostRealms {
 public static class StormArt {
  static readonly System.Collections.Generic.Dictionary<string,Material> materials=new System.Collections.Generic.Dictionary<string,Material>();
  static Material beam;
  public static Material Beam {get{if(!beam)beam=new Material(Shader.Find("Sprites/Default")){name="Stormveil lightning and wind markings"};return beam;}}
  public static Material GetMaterial(string asset){
   if(materials.TryGetValue(asset,out var m)&&m)return m;
   var mat=new Material(Shader.Find("Standard")){enableInstancing=true,name="Stormveil "+asset};
   var tex=Resources.Load<Texture2D>("Stormveil/Textures/"+asset+"_basecolor");
   var norm=Resources.Load<Texture2D>("Stormveil/Textures/"+asset+"_normal");
   mat.mainTexture=tex?tex:Resources.Load<Texture2D>("Stormveil/StormPalette");
   mat.SetFloat("_Glossiness",.25f);
   mat.SetFloat("_Metallic",.18f);
   if(norm){
    mat.SetTexture("_BumpMap",norm);
    mat.SetFloat("_BumpScale",1f);
    mat.EnableKeyword("_NORMALMAP");
   }
   materials[asset]=mat;
   return mat;
  }
  public static GameObject Place(string asset,Transform parent,Vector3 position,Vector3 scale,bool solid=false){
   var prefab=Resources.Load<GameObject>("Stormveil/"+asset);if(!prefab)throw new System.Exception("Missing Stormveil model "+asset);
   var go=new GameObject(asset=="Tower"?"Prop Storm tower":asset=="Conductor"?"Prop Storm conductor":asset);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
   // FBX root rotation, scale and pivot offsets are part of its import pose.
   // Resize through an identity wrapper; resetting the mesh root lays it flat.
   Object.Instantiate(prefab,go.transform,false);
   var mat=GetMaterial(asset);
   foreach(var r in go.GetComponentsInChildren<Renderer>())r.sharedMaterial=mat;
   if(solid)foreach(var f in go.GetComponentsInChildren<MeshFilter>()){var c=f.gameObject.AddComponent<MeshCollider>();c.sharedMesh=f.sharedMesh;}
   return go;
  }
 }
 public partial class RealmWorld {
  // Individually authored routes: switchback terraces, paired watchtowers,
  // alternating causeways, eye spiral, then the broad guardian approach.
  static readonly float[][] StormX={
   new float[]{0,0,3,6,6,3,0,-3,-6,-6,-3,0},
   new float[]{0,-3,-6,-6,0,6,6,0,-6,-6,0,6,6,0},
   new float[]{0,0,-4,-4,0,4,4,0,-4,-4,0,4,4,0},
   new float[]{0,3,6,6,3,0,-3,-6,-6,-3,0,3,3,0},
   new float[]{0,0,-3,-3,0,3,3,0,0}};
  static readonly int[][] CircuitIndices={new[]{3,8},new[]{3,6,10},new[]{4,8,11},new[]{3,6,10,12},new[]{2,5}};
  public void BuildStorm(int stage){BuildExploration(stage,false);}
 }
 public sealed class StormWindWindow:MonoBehaviour {
  public float PhaseOffset,Length=12;public Vector3 Direction=Vector3.right;
  public float Cycle=>Mathf.Repeat(RealmGame.I.Elapsed+PhaseOffset,8f);
  public bool Gusting=>Cycle>=4f&&Cycle<6.5f;
  public bool Warning=>Cycle>=2.5f&&Cycle<4f;
  float nextHint,nextPulse;LineRenderer tell;readonly System.Collections.Generic.List<LineRenderer> streams=new System.Collections.Generic.List<LineRenderer>();
  void Start(){
   for(int i=0;i<9;i++){var obj=new GameObject("Directional gust");obj.transform.SetParent(transform,false);var line=obj.AddComponent<LineRenderer>();line.useWorldSpace=false;line.positionCount=2;line.startWidth=.055f;line.endWidth=.015f;line.sharedMaterial=StormArt.Beam;streams.Add(line);}
   tell=gameObject.AddComponent<LineRenderer>();tell.useWorldSpace=false;tell.positionCount=2;tell.SetPosition(0,new Vector3(-1.3f,.12f,0));tell.SetPosition(1,new Vector3(1.3f,.12f,0));tell.startWidth=tell.endWidth=.12f;tell.sharedMaterial=StormArt.Beam;StormArt.Place("WindGrate",transform,new Vector3(0,.02f,0),new Vector3(2.6f,.1f,Mathf.Min(Length*.5f,6f)),false);}
  void Update(){var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;var tint=Gusting?new Color(.95f,.3f,.18f):Warning?new Color(1f,.72f,.22f):new Color(.35f,.85f,.7f);tell.startColor=tell.endColor=tint;
   for(int i=0;i<streams.Count;i++){
    var line=streams[i];line.enabled=Gusting||Warning;if(!line.enabled)continue;
    var dir=transform.InverseTransformDirection(Direction.normalized);var side=Vector3.Cross(dir,Vector3.up);
    float slide=Mathf.Repeat(g.Elapsed*(Gusting?3.7f:1.1f)+i*.47f,5)-2.5f;
    var start=side*((i%3-1)*.65f)+dir*slide+Vector3.up*(.35f+(i/3)*.45f);
    line.SetPosition(0,start);line.SetPosition(1,start+dir*(Gusting?1.1f:.4f));line.startColor=line.endColor=new Color(tint.r,tint.g,tint.b,Gusting?.65f:.28f);
   }
   var p=transform.InverseTransformPoint(g.Player.transform.position);if(Mathf.Abs(p.z)>Length*.5f||Mathf.Abs(p.x)>4||Mathf.Abs(p.y)>3)return;
   if(g.Elapsed>nextHint){nextHint=g.Elapsed+1.5f;g.Tell(Gusting?"WIND GUST — stay on the broad terrace":Warning?"WIND RISING — wait for the green crossing":"WIND CALM — cross now",1.7f);if(Warning)g.Sound("storm_wind");}
   float exposedHalf=Mathf.Max(1.2f,Length*.5f-5.2f);
   if(Gusting&&g.Elapsed>=nextPulse){nextPulse=g.Elapsed+.45f;
    foreach(var e in g.Enemies){if(!e||e.Boss||e.Health<=0)continue;var q=transform.InverseTransformPoint(e.transform.position);if(Mathf.Abs(q.z)<exposedHalf&&Mathf.Abs(q.x)<1.8f&&Mathf.Abs(q.y)<3)e.PushBack(Direction,.45f,.2f);}
   }
  }
  void FixedUpdate(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player||!Gusting)return;
   var p=transform.InverseTransformPoint(g.Player.transform.position);
   var dir=transform.InverseTransformDirection(Direction.normalized);float across=Vector3.Dot(p,dir),along=Vector3.Dot(p,Vector3.Cross(dir,Vector3.up));
   if(Mathf.Abs(across)<3.5f&&Mathf.Abs(along)<Mathf.Min(Length*.25f,3f)+.35f&&p.y>-.3f&&p.y<2.8f)
    g.Player.ApplyForce(Direction.normalized*(g.Player.Grounded?85f:28f)*Time.fixedDeltaTime);
  }

 }
}
