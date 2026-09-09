using UnityEngine;
using System.Collections.Generic;
namespace LostRealms {
 public static class Art {
  static readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
  public static Material Material(Color color){
   if(materials.TryGetValue(color,out var m))return m;
   m=new Material(Shader.Find("Standard"));m.color=color;m.SetFloat("_Glossiness",.22f);
   materials[color]=m;return m;
  }
  public static GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Color color,Transform parent,bool solid=false){
   var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);
   go.transform.localPosition=pos;go.transform.localScale=scale;
   go.GetComponent<Renderer>().sharedMaterial=Material(color);
   if(!solid){var c=go.GetComponent<Collider>();if(c){c.enabled=false;Object.Destroy(c);}}
   return go;
  }
  public static GameObject Crystal(Vector3 pos,float size,Color color,Transform parent){
   var g=Shape("Realm crystal",PrimitiveType.Cube,pos,Vector3.one*size,color,parent);
   g.transform.localRotation=Quaternion.Euler(45,45,0);return g;
  }
  public static GameObject Ring(Vector3 pos,float radius,Color color,Transform parent){
   var root=new GameObject("Rune ring");root.transform.SetParent(parent,false);root.transform.localPosition=pos;
   for(int i=0;i<24;i++){
    float a=i*Mathf.PI*2/24;
    var g=Shape("Rune",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius),new Vector3(.12f,.035f,.38f),color,root.transform);
    g.transform.localRotation=Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
   }
   return root;
  }
  public static void CoinMesh(Transform parent){
   Shape("CoinRim",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.46f,.06f,.46f),new Color(1f,.78f,.2f),parent);
   Shape("CoinCore",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.38f,.075f,.38f),new Color(.92f,.68f,.15f),parent);
   var rune=Shape("StarRune",PrimitiveType.Cube,Vector3.zero,new Vector3(.18f,.09f,.18f),new Color(1f,.92f,.45f),parent);
   rune.transform.localRotation=Quaternion.Euler(0,45,0);
  }
  public static void GemMesh(Transform parent,Color accent){
   var top=Shape("GemTop",PrimitiveType.Cube,Vector3.up*.04f,new Vector3(.32f,.32f,.32f),accent,parent);
   top.transform.localRotation=Quaternion.Euler(45,45,0);
   var core=Shape("GemCore",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.22f,Color.Lerp(accent,Color.white,.65f),parent);
   for(int i=0;i<3;i++){
    float a=i*Mathf.PI*2f/3f;
    var shard=Shape("Shard",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.32f,Mathf.Cos(a)*.08f,Mathf.Cos(a)*.32f),Vector3.one*.08f,accent*1.2f,parent);
    shard.transform.localRotation=Quaternion.Euler(30,a*Mathf.Rad2Deg,45);
   }
  }
 }
 public class RealmWorld:MonoBehaviour {
  public Vector3 Spawn;public bool IsBoss;public float EndZ; public readonly List<Vector3> Route=new List<Vector3>();
  static readonly float[][] RouteX={
   new float[]{0,1,2,2,0,-1,-2,-1,0,2,1,0},
   new float[]{0,2,4,1,-2,-4,-1,2,4,2,-1,0},
   new float[]{0,-2,-4,-2,1,4,2,-1,-4,-2,1,0},
   new float[]{0,0,2,3,1,-2,-1,0},
   new float[]{0,0,3,5,3,0,-3,-5,-3,0,2,0},
   new float[]{0,3,5,3,0,-3,-5,-3,0,3,1,0},
   new float[]{0,2,4,2,0,-3,-2,0},
   new float[]{0,-2,0,3,1,-2,0,3,1,-2,0,0},
   new float[]{0,3,0,-3,0,3,5,2,-1,-4,-2,0},
   new float[]{0,-3,-1,2,4,2,0,0}};
  static readonly float[][] RouteY={
   new float[]{0,0,.7f,0,0,.7f,0,0,.7f,0,0,0},
   new float[]{0,.6f,1.2f,.6f,0,.6f,1.2f,.6f,0,.6f,1.2f,1.2f},
   new float[]{0,.8f,1.6f,.8f,0,.8f,1.6f,.8f,0,.8f,1.6f,1.6f},
   new float[]{0,.5f,1,.5f,0,.5f,1,1},
   new float[]{0,.7f,1.4f,1.4f,.7f,0,.7f,1.4f,1.4f,.7f,0,0},
   new float[]{0,1,2,3,2,1,0,1,2,3,2,2},
   new float[]{0,.8f,1.6f,.8f,0,.8f,1.6f,1.6f},
   new float[]{0,.7f,1.4f,2.1f,2.8f,3.5f,4.2f,4.9f,5.6f,6.3f,7,7},
   new float[]{0,1,2,3,2,1,2,3,4,3,2,2},
   new float[]{0,.8f,1.6f,2.4f,3.2f,4,4.8f,4.8f}};
  int realm,level;Color top,stone,accent;System.Random random;
  public void Build(int stage,int world){level=stage;realm=world;IsBoss=stage==4||stage==7||stage==10;random=new System.Random(stage*793);accent=RealmGame.Accents[world];
   top=world==0?new Color(.18f,.38f,.29f):world==1?new Color(.63f,.38f,.20f):new Color(.59f,.75f,.82f);
   stone=world==0?new Color(.10f,.20f,.20f):world==1?new Color(.32f,.20f,.16f):new Color(.23f,.35f,.48f);
   RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.011f;
   RenderSettings.fogColor=world==0?new Color(.13f,.25f,.28f):world==1?new Color(.44f,.25f,.22f):new Color(.22f,.35f,.5f);
   RenderSettings.ambientLight=new Color(.6f,.66f,.72f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
   Camera.main.backgroundColor=RenderSettings.fogColor;Camera.main.clearFlags=CameraClearFlags.SolidColor;
   var sun=new GameObject("Realm sunlight").AddComponent<Light>();sun.transform.SetParent(transform);sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-35,0);
   sun.color=world==1?new Color(1,.83f,.62f):new Color(.88f,.95f,1);sun.intensity=1.25f;sun.shadows=LightShadows.Soft;
   int count=IsBoss?8:12;
   for(int i=0;i<count;i++){
    float z=i*10f,x=RouteX[stage-1][i],y=RouteY[stage-1][i];
    Vector3 p=new Vector3(x,y,z);Route.Add(p);bool last=i==count-1;float width=last&&IsBoss?19:9;float length=last?15:8.3f;
    Island(p,width,length,i);if(i==0)Spawn=p+Vector3.up*.05f;
    for(int j=-1;j<=1;j++)Pickup(p+new Vector3(0,.8f,j*2.2f),false);
    if(i>1&&!last&&i%2==0){var side=p+new Vector3((i%4==0?-1:1)*9.5f,1.2f,0);Island(side,4.8f,5.3f,100+i);Pickup(side+Vector3.up*.9f,true);if(i==6||i==10){var move=FindIsland(side);if(move){var motion=move.AddComponent<MovingIsland>();motion.Origin=move.transform.position;motion.Offset=new Vector3(0,0,1.3f);}}}
    if(i==count/2)Checkpoint(p+new Vector3(-2,0,-1));
    if(i>=2&&!last){int kind=(stage+i)%8;SpawnEnemy(p+new Vector3(1.8f,.03f,1),kind,false,p,width,length);if(stage>2&&i%3==0)Hazard(p+new Vector3(-2.4f,.08f,1),realm);}
    if(last){EndZ=z+4;Gate(p+new Vector3(0,0,5));if(IsBoss)SpawnEnemy(p+new Vector3(0,.05f,-1),world+8,true,p,width,length);}
   }
   for(int i=0;i<40;i++){float z=-16+i*4.8f;float side=i%2==0?-1:1;Vector3 p=new Vector3(side*(16+(float)random.NextDouble()*24),-4,z);float h=8+(float)random.NextDouble()*22;Art.Shape("Distant realm spire",PrimitiveType.Cylinder,p,new Vector3(6,h,6),stone*.75f,transform);if(realm==0)Art.Shape("Distant canopy",PrimitiveType.Sphere,p+Vector3.up*h*.5f,new Vector3(12,6,12),top*.68f,transform);}
   RealmScenery.Upgrade(this,realm);
  }
  GameObject FindIsland(Vector3 p){foreach(Transform t in transform)if(t.name=="Island"&&Vector3.Distance(t.position,p)<.1f)return t.gameObject;return null;}
  void Island(Vector3 pos,float width,float length,int index){
   var root=new GameObject("Island");root.transform.SetParent(transform);root.transform.position=pos;
   // 1. Walkable solid stone base
   Art.Shape("Walkable stone",PrimitiveType.Cube,new Vector3(0,-.92f,0),new Vector3(width,1.7f,length),stone,root.transform,true);
   // 2. Realm terrain surface with slight border reveal
   Art.Shape("Realm surface",PrimitiveType.Cube,new Vector3(0,.02f,0),new Vector3(width+.08f,.16f,length+.08f),top,root.transform);
   // 3. Beveled outer stone curb trim
   Art.Shape("CurbNorth",PrimitiveType.Cube,new Vector3(0,.08f,length*.5f),new Vector3(width+.25f,.14f,.25f),stone*1.25f,root.transform);
   Art.Shape("CurbSouth",PrimitiveType.Cube,new Vector3(0,.08f,-length*.5f),new Vector3(width+.25f,.14f,.25f),stone*1.25f,root.transform);
   Art.Shape("CurbEast",PrimitiveType.Cube,new Vector3(width*.5f,.08f,0),new Vector3(.25f,.14f,length+.25f),stone*1.25f,root.transform);
   Art.Shape("CurbWest",PrimitiveType.Cube,new Vector3(-width*.5f,.08f,0),new Vector3(.25f,.14f,length+.25f),stone*1.25f,root.transform);
   // 4. Tiered rocky underside (shelf + tapered keel)
   var shelf=Art.Shape("CliffShelf",PrimitiveType.Cube,new Vector3(0,-2.1f,0),new Vector3(width*.82f,1.6f,length*.82f),stone*.85f,root.transform);
   shelf.transform.localRotation=Quaternion.Euler(0,4,3);
   var keel=Art.Shape("RockKeel",PrimitiveType.Cube,new Vector3(0,-3.6f,0),new Vector3(width*.52f,2.6f,length*.52f),stone*.7f,root.transform);
   keel.transform.localRotation=Quaternion.Euler(0,14,7);
   // 5. Floating keystone fragment
   var keystone=Art.Shape("FloatingKeystone",PrimitiveType.Cube,new Vector3(Mathf.Sin(index)*1.2f,-5.2f,Mathf.Cos(index)*1.2f),new Vector3(1.1f,1.3f,1.1f),stone*.65f,root.transform);
   keystone.transform.localRotation=Quaternion.Euler(20,index*35,15);
   // 6. Perimeter decor & ancient runes
   for(int side=-1;side<=1;side+=2){
    if(width<7)break;
    for(int n=0;n<2;n++){
     Vector3 p=new Vector3(side*(width*.5f-.85f),0,(n==0?-1:1)*(length*.5f-.95f));
     Decor(p,root.transform,index+n);
    }
   }
   Art.Ring(new Vector3(0,.11f,0),.55f,accent*.7f,root.transform);
  }
  void Decor(Vector3 p,Transform parent,int seed){
   if(realm==0){
    Art.Shape("Ancient trunk",PrimitiveType.Cylinder,p+Vector3.up*1.6f,new Vector3(.42f,1.8f,.42f),new Color(.24f,.18f,.14f),parent);
    for(int i=0;i<3;i++)Art.Shape("Foliage",PrimitiveType.Sphere,p+new Vector3(Mathf.Sin(i*2)*.45f,2.7f+i*.6f,Mathf.Cos(i*2)*.35f),new Vector3(2.2f,1.9f,2.2f),top*(1.2f+i*.12f),parent);
    Art.Shape("MossyCairn",PrimitiveType.Cube,p+new Vector3(.4f,.2f,-.3f),new Vector3(.45f,.4f,.45f),stone*1.2f,parent);
   }else if(realm==1){
    Art.Shape("Ruined pillar",PrimitiveType.Cube,p+Vector3.up*1.4f,new Vector3(.72f,2.8f,.72f),top*1.2f,parent);
    Art.Shape("Pillar crown",PrimitiveType.Cube,p+Vector3.up*2.85f,new Vector3(1.2f,.28f,1.2f),accent*.85f,parent);
    var shard=Art.Shape("PillarFragment",PrimitiveType.Cube,p+new Vector3(.45f,.25f,.35f),new Vector3(.45f,.5f,.4f),top*.9f,parent);
    shard.transform.localRotation=Quaternion.Euler(15,35,10);
   }else{
    var g=Art.Crystal(p+Vector3.up*1.2f,1.25f,accent,parent);g.transform.localScale=new Vector3(.65f,1.85f,.65f);
    Art.Shape("IceShard",PrimitiveType.Cube,p+new Vector3(-.35f,.35f,.25f),new Vector3(.3f,.7f,.3f),Color.Lerp(accent,Color.white,.4f),parent);
   }
  }
  void Pickup(Vector3 position,bool gem){
   var go=new GameObject(gem?"GemPickup":"CoinPickup");
   go.transform.SetParent(transform,false);go.transform.position=position;
   if(gem)Art.GemMesh(go.transform,accent);
   else Art.CoinMesh(go.transform);
   var pickup=go.AddComponent<RealmPickup>();pickup.Gem=gem;pickup.Origin=position;
  }
  void Checkpoint(Vector3 p){
   var go=new GameObject("Checkpoint shrine");go.transform.SetParent(transform);go.transform.position=p;
   Art.Shape("Shrine dais",PrimitiveType.Cylinder,Vector3.up*.06f,new Vector3(1.8f,.12f,1.8f),stone*1.3f,go.transform);
   Art.Shape("Shrine base",PrimitiveType.Cylinder,Vector3.up*.16f,new Vector3(1.4f,.12f,1.4f),stone,go.transform);
   Art.Crystal(Vector3.up*1.2f,.7f,accent,go.transform);
   Art.Ring(Vector3.up*.04f,1.35f,accent,go.transform);
   go.AddComponent<RealmCheckpoint>();
  }
  void Gate(Vector3 p){
   var root=new GameObject("Realm gate");root.transform.SetParent(transform);root.transform.position=p;
   for(int side=-1;side<=1;side+=2){
    Art.Shape("Gate column",PrimitiveType.Cube,new Vector3(side*1.85f,2.1f,0),new Vector3(.65f,4.2f,.85f),top*1.3f,root.transform);
    Art.Shape("ColumnCapital",PrimitiveType.Cube,new Vector3(side*1.85f,4.3f,0),new Vector3(.85f,.3f,1f),stone*1.2f,root.transform);
   }
   Art.Shape("Gate lintel",PrimitiveType.Cube,new Vector3(0,4.45f,0),new Vector3(4.4f,.55f,.9f),stone,root.transform);
   Art.Crystal(new Vector3(0,3.5f,0),.8f,accent,root.transform);
   Art.Ring(new Vector3(0,.04f,0),1.6f,accent,root.transform);
   root.AddComponent<RealmGate>();
  }
  void Hazard(Vector3 p,int kind){
   var go=new GameObject("Realm hazard");go.transform.SetParent(transform);go.transform.position=p;
   for(int i=0;i<5;i++){
    var spike=Art.Shape("Thorn",PrimitiveType.Cube,new Vector3((i%3)*.37f-.4f,.28f,(i/3)*.4f),new Vector3(.18f,.65f,.18f),kind==1?new Color(1,.35f,.12f):accent,go.transform);
    spike.transform.localRotation=Quaternion.Euler(0,30,15);
   }
   go.AddComponent<RealmHazard>();
  }
  void SpawnEnemy(Vector3 p,int kind,bool boss,Vector3 center,float width,float length){
   var go=new GameObject(boss?"Realm guardian":"Realm enemy");go.transform.SetParent(transform);go.transform.position=p;
   var e=go.AddComponent<Enemy>();e.Configure(kind,boss,center,new Vector2(width,length));
  }
 }
 public class RealmPickup:MonoBehaviour {
  public bool Gem;public Vector3 Origin;
  void Update(){
   var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;
   transform.Rotate(0,(Gem?75f:110f)*Time.deltaTime,0,Space.World);
   Vector3 target=g.Player.transform.position+Vector3.up*.8f;
   if(Vector3.Distance(Origin,target)<(Gem?1.6f:2.1f))Origin=Vector3.MoveTowards(Origin,target,Time.deltaTime*4.5f);
   transform.position=Origin+Vector3.up*Mathf.Sin(RealmGame.I.Elapsed*2.8f)*.15f;
   if(Vector3.Distance(g.Player.transform.position+Vector3.up*.8f,transform.position)<1.15f){
    HitSpark.Burst(transform.position,Vector3.up,Gem?g.Accent:new Color(1f,.85f,.2f),12);
    g.Collect(Gem);Destroy(gameObject);
   }
  }
 }
 public class RealmCheckpoint:MonoBehaviour {
  bool active;
  void Update(){
   var g=RealmGame.I;
   if(!active&&g.Screen==GameScreen.Playing&&Vector3.Distance(g.Player.transform.position,transform.position)<1.8f){
    active=true;HitSpark.Burst(transform.position+Vector3.up*1.2f,Vector3.up,g.Accent,18);
    g.ActivateCheckpoint(transform.position+Vector3.forward*1.5f+Vector3.up*.05f);
   }
  }
 }
 public class RealmGate:MonoBehaviour {
  float next;
  void Update(){
   var g=RealmGame.I;
   if(g.Screen==GameScreen.Playing&&RealmGame.I.Elapsed>next&&Vector3.Distance(g.Player.transform.position,transform.position)<1.6f){
    next=RealmGame.I.Elapsed+2;HitSpark.Burst(transform.position+Vector3.up*2f,Vector3.up,g.Accent,24);
    g.Finish();
   }
  }
 }
 public class RealmHazard:MonoBehaviour {
  void Update(){
   var g=RealmGame.I;
   if(g.Screen==GameScreen.Playing&&Vector3.Distance(g.Player.transform.position,transform.position)<.85f)
    g.Player.Damage(1,transform.position);
  }
 }
 public class MovingIsland:MonoBehaviour {
  public Vector3 Origin,Offset;Vector3 prior;
  void Start(){prior=transform.position;}
  void Update(){
   var g=RealmGame.I;if(g.Screen!=GameScreen.Playing)return;
   transform.position=Origin+Offset*Mathf.Sin(RealmGame.I.Elapsed*.8f);
   Vector3 delta=transform.position-prior;prior=transform.position;
   if(g.Player&&g.Player.Grounded&&Physics.Raycast(g.Player.transform.position+Vector3.up*.1f,Vector3.down,out var hit,.5f)&&hit.transform.IsChildOf(transform))
    g.Player.Controller.Move(delta);
  }
 }
}


