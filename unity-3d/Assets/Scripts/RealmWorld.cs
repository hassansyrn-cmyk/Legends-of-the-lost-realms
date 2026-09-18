using UnityEngine;
using System.Collections.Generic;
namespace LostRealms {
 public static class Art {
  static readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
  public static Material Material(Color color){
   if(materials.TryGetValue(color,out var m)&&m)return m;
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
   if(PickupModel("Coin",parent,new Color(1f,.8f,.25f),.34f,null))return;
   Shape("CoinRim",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.46f,.06f,.46f),new Color(1f,.78f,.2f),parent);
   Shape("CoinCore",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.38f,.075f,.38f),new Color(.92f,.68f,.15f),parent);
   var rune=Shape("StarRune",PrimitiveType.Cube,Vector3.zero,new Vector3(.18f,.09f,.18f),new Color(1f,.92f,.45f),parent);
   rune.transform.localRotation=Quaternion.Euler(0,45,0);
  }
  public static void GemMesh(Transform parent,Color accent){
   if(PickupModel("5SideDiamond",parent,accent,.3f,null))return;
   var top=Shape("GemTop",PrimitiveType.Cube,Vector3.up*.04f,new Vector3(.32f,.32f,.32f),accent,parent);
   top.transform.localRotation=Quaternion.Euler(45,45,0);
   var core=Shape("GemCore",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.22f,Color.Lerp(accent,Color.white,.65f),parent);
   for(int i=0;i<3;i++){
    float a=i*Mathf.PI*2f/3f;
    var shard=Shape("Shard",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.32f,Mathf.Cos(a)*.08f,Mathf.Cos(a)*.32f),Vector3.one*.08f,accent*1.2f,parent);
    shard.transform.localRotation=Quaternion.Euler(30,a*Mathf.Rad2Deg,45);
   }
  }
  // Instantiate a BenjaTheMaker pickup model, normalized to targetHeight and
  // tinted. Returns null when the model is missing so builders fall back to
  // the old procedural shapes.
 public static GameObject PickupModel(string name,Transform parent,Color color,float targetHeight,Material glow=null){
     var prefab=Resources.Load<GameObject>("Pickups/"+name);
     if(!prefab)return null;
     // worldPositionStays=false: the model must start exactly on the pickup
     // root. The 2-arg Instantiate keeps the prefab's world pose, which parks
     // the mesh near the world origin with a giant local offset — the offset
     // child then whips around the spinning pickup root ("flying pickups").
     var go=Object.Instantiate(prefab,parent,false);
     go.name=name;
     go.transform.localPosition=Vector3.zero;
     go.transform.localRotation=Quaternion.identity;
     go.transform.localScale=Vector3.one;
    var material=(glow!=null&&glow)?glow:new Material(Shader.Find("Standard")){name="Pickup "+name};
    if(!glow){
     material.color=color;
     if(material.HasProperty("_Glossiness"))material.SetFloat("_Glossiness",.7f);
     if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",name=="Coin"||name=="StarCoin"?.6f:.12f);
     if(material.HasProperty("_EmissionColor")){material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",color*.45f);}
    }
    foreach(var renderer in go.GetComponentsInChildren<Renderer>(true))renderer.sharedMaterial=material;
    var renderers=go.GetComponentsInChildren<Renderer>(true);
    if(renderers.Length>0){
     var bounds=renderers[0].bounds;
     for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
     if(bounds.size.y>.0001f){
      float s=targetHeight/bounds.size.y;
      go.transform.localScale=Vector3.one*s;
      // Re-measure AFTER scaling, then shift in WORLD space so the mesh sits
      // grounded and centered on the pickup root. Renderer.bounds is world
      // space — never mix it into localPosition: far down-route that parks
      // the visible mesh back near the world origin ("flying pickups").
      bounds=renderers[0].bounds;
      for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
      Vector3 anchor=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
      go.transform.position-=anchor-parent.position;
     }
    }
    return go;
  }
 }
 public enum IslandArchetype { Standard, Arena, NarrowBridge, SteppingStones, TieredPlatform, MovingFerry }
 public enum IslandMotionStyle { Sinusoidal, PingPongDwell, VerticalElevator, Orbit }
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
    new float[]{0,-3,-1,2,4,2,0,0},
    new float[]{0,1,3,1,-1,-3,-1,2,4,2,-1,0},
    new float[]{0,2,4,2,-1,-4,-2,1,3,1,-2,0},
    new float[]{0,-1,-3,-1,2,4,2,-1,-4,-2,1,0},
    new float[]{0,0,2,4,1,-2,-4,-1,2,4,1,0},
    new float[]{0,2,4,1,-2,-4,-1,0}};
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
    new float[]{0,.8f,1.6f,2.4f,3.2f,4,4.8f,4.8f},
    new float[]{0,.5f,1,.5f,0,.6f,1.2f,.8f,.4f,.9f,.4f,.4f},
    new float[]{0,.7f,1.4f,2.1f,1.4f,.7f,1.5f,2.2f,1.5f,.7f,0,0},
    new float[]{0,.6f,1.2f,1.8f,2.4f,1.6f,.8f,1.6f,2.2f,1.3f,.5f,0},
    new float[]{0,.8f,1.6f,2.4f,3.2f,3.9f,3.2f,2.4f,1.6f,.8f,0,0},
    new float[]{0,1,2,3,2,1,0,0}};
  int realm,level;Color top,stone,accent;System.Random random;
public void Build(int stage,int world){level=stage;realm=world;IsBoss=stage==4||stage==7||stage==10||stage==15;random=new System.Random(stage*793);accent=RealmGame.Accents[world];
    top=world==0?new Color(.18f,.38f,.29f):world==1?new Color(.63f,.38f,.20f):world==2?new Color(.59f,.75f,.82f):new Color(.32f,.22f,.27f);
    stone=world==0?new Color(.10f,.20f,.20f):world==1?new Color(.32f,.20f,.16f):world==2?new Color(.23f,.35f,.48f):new Color(.14f,.12f,.15f);
    RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.011f;
    RenderSettings.fogColor=world==0?new Color(.13f,.25f,.28f):world==1?new Color(.44f,.25f,.22f):world==2?new Color(.22f,.35f,.5f):new Color(.32f,.18f,.20f);
    RenderSettings.ambientLight=new Color(.6f,.66f,.72f);RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
    Camera.main.backgroundColor=RenderSettings.fogColor;Camera.main.clearFlags=CameraClearFlags.SolidColor;
    var sun=new GameObject("Realm sunlight").AddComponent<Light>();sun.transform.SetParent(transform);sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(48,-35,0);
    sun.color=world==1?new Color(1,.83f,.62f):world==3?new Color(1,.55f,.38f):new Color(.88f,.95f,1);sun.intensity=world==3?.95f:1.25f;sun.shadows=LightShadows.Soft;
   int count=IsBoss?8:12;int weaponIsland=random.Next(2,count-2);WeaponId weaponId=(WeaponId)(stage==1?4:random.Next(1,41));
   for(int i=0;i<count;i++){
    float z=i*10f,x=RouteX[stage-1][i],y=RouteY[stage-1][i];
    Vector3 p=new Vector3(x,y,z);Route.Add(p);bool last=i==count-1;
    IslandArchetype arch=IslandArchetype.Standard;
    if(i>0&&!last){
     if(IsBoss){
      if(i==2)arch=stage>=2?IslandArchetype.NarrowBridge:IslandArchetype.Standard;
      else if(i==3)arch=IslandArchetype.Arena;
      else if(i==4)arch=stage>=3?IslandArchetype.SteppingStones:IslandArchetype.Standard;
      else if(i==5)arch=stage>=2?IslandArchetype.MovingFerry:IslandArchetype.Standard;
      else if(i==6)arch=IslandArchetype.TieredPlatform;
     }else{
      int cycle=i%6;
      if(cycle==2)arch=stage>=2?IslandArchetype.NarrowBridge:IslandArchetype.Standard;
      else if(cycle==3)arch=IslandArchetype.Arena;
      else if(cycle==4)arch=IslandArchetype.TieredPlatform;
      else if(cycle==5)arch=stage>=2?IslandArchetype.SteppingStones:IslandArchetype.Standard;
      else if(cycle==0&&i>=6)arch=stage>=3?IslandArchetype.MovingFerry:IslandArchetype.Standard;
     }
    }
    float width=last&&IsBoss?19:last?11:arch==IslandArchetype.Arena?13.5f:arch==IslandArchetype.NarrowBridge?4.2f:arch==IslandArchetype.TieredPlatform?10.5f:arch==IslandArchetype.MovingFerry?6f:arch==IslandArchetype.SteppingStones?7.5f:9;
    float length=last?15:arch==IslandArchetype.Arena?13f:arch==IslandArchetype.NarrowBridge?14f:arch==IslandArchetype.TieredPlatform?10f:arch==IslandArchetype.MovingFerry?6f:arch==IslandArchetype.SteppingStones?8f:8.3f;
    var islandObj=Island(p,width,length,i,arch,last&&IsBoss);if(i==0)Spawn=p+Vector3.up*.05f;
    for(int j=-1;j<=1;j++){float py=(arch==IslandArchetype.TieredPlatform&&j<0)?2.1f:.8f;Pickup(p+new Vector3(0,py,j*2.2f),false);}
    if(i>1&&!last&&i%2==0){var side=p+new Vector3((i%4==0?-1:1)*9.5f,1.2f,0);Island(side,4.8f,5.3f,100+i);Pickup(side+Vector3.up*.9f,true);if(i==6||i==10){var move=FindIsland(side);if(move){var motion=move.AddComponent<MovingIsland>();motion.Origin=move.transform.position;motion.Offset=new Vector3(0,0,1.3f);motion.AddThrusters(accent);}}}
     int mid=count/2;
     if(i==mid)Checkpoint(p+new Vector3(-2.2f,0,-0.8f));
    if(i>=2&&i%4==0){float hx=(i%2==0?-1:1)*(1.2f+(float)random.NextDouble()*.9f);HealPickup(p+new Vector3(hx,.6f,-1.2f+(float)random.NextDouble()*2.4f));}
    if(i==weaponIsland)WeaponDrop(p+new Vector3((i%2==0?-2.3f:2.3f),.18f,-1.4f),weaponId);
    if(stage>=2&&!last&&(i==3||i==7)){
     BouncePad.Place(transform,p+new Vector3((i%2==0?-2.8f:2.8f),.05f,-1.8f),accent);
     Vector3 secretPos=p+new Vector3((i%2==0?-4.8f:4.8f),8.5f,1.5f);
     Island(secretPos,5.2f,5.2f,300+i,IslandArchetype.Standard);
     Pickup(secretPos+Vector3.up*.9f,true);
     Pickup(secretPos+new Vector3(-1f,.9f,0),false);
     Pickup(secretPos+new Vector3(1f,.9f,0),false);
     BreakableCrate.Place(transform,secretPos+new Vector3(0,.05f,1.2f),stone*1.3f);
    }
    if(stage>=2&&!last&&(i==5||i==9)){
     Vector3 nextP=new Vector3(RouteX[stage-1][i+1],RouteY[stage-1][i+1],(i+1)*10f);
     SpeedRing.Place(transform,(p+nextP)*.5f+Vector3.up*1.5f,nextP-p,accent);
    }
    if(i>=2&&!last){int kind=(stage+i)%8;
     if(stage>=9&&i%6==2)kind=17;else if(stage>=3&&i%6==2)kind=11;else if(stage>=5&&i%5==4)kind=12;else if(stage>=6&&i%7==3)kind=13;else if(stage>=4&&i%6==5)kind=14;else if(stage>=4&&i%7==6)kind=15;else if(stage>=5&&i==7)kind=16;else if(stage>=3&&((stage+i)%8)==2)kind=18;else if(stage>=4&&((stage+i)%8)==5)kind=19;else if(stage>=4&&((stage+i)%8)==6)kind=20;
     float ez=arch==IslandArchetype.TieredPlatform?-2.2f:1f;float ey=arch==IslandArchetype.TieredPlatform?1.28f:.03f;
     SpawnEnemy(p+new Vector3(1.8f,ey,ez),kind,false,p,width,length);
     if(stage>2&&i%3==0&&arch!=IslandArchetype.Arena){
      float hz=arch==IslandArchetype.TieredPlatform?-2.2f:1f;float hy=arch==IslandArchetype.TieredPlatform?1.35f:.08f;
      Hazard(islandObj.transform,new Vector3(-0.8f,hy,hz),realm);
     }
    }
    if(last){EndZ=z+4;Gate(p+new Vector3(0,0,5));if(IsBoss)SpawnEnemy(p+new Vector3(0,.05f,-1),world==3?21:world+8,true,p,width,length);}
   }
    RealmScenery.Upgrade(this,realm);
    RealmProps.Scatter(transform,realm,random);
  }
   GameObject FindIsland(Vector3 p){foreach(Transform t in transform)if(t.name=="Island"&&Vector3.Distance(t.position,p)<.1f)return t.gameObject;return null;}
     GameObject DressIslandVisual(Transform root,IslandArchetype archetype,float width,float length,int index,bool isBoss){
      if(realm>3)return null;
      string prefix=realm==3?"R3_":realm==2?"R2_":realm==1?"R1_":"";
      string modelName=null;Vector3 baseDim=Vector3.one;
      if(archetype==IslandArchetype.Arena){
       modelName=prefix+"Island_Arena";baseDim=isBoss?new Vector3(19f,1f,15f):new Vector3(13.5f,1f,13f);
      }else if(archetype==IslandArchetype.NarrowBridge){
       modelName=prefix+"Island_Bridge";baseDim=new Vector3(4.2f,1f,14f);
      }else if(archetype==IslandArchetype.TieredPlatform){
       modelName=prefix+"Island_Tiered";baseDim=new Vector3(10.5f,1f,10f);
      }else if(archetype==IslandArchetype.MovingFerry){
       modelName=prefix+"Island_Ferry";baseDim=new Vector3(6f,1f,6f);
      }else if(index>=100){
       modelName=prefix+"Island_Small";baseDim=new Vector3(5.2f,1f,5.2f);
      }else{
       if(index%2==0){modelName=prefix+"Island_Plateau";baseDim=new Vector3(9f,1f,8.3f);}
       else{modelName=prefix+"Island_Meadow";baseDim=new Vector3(9f,1f,9f);}
      }
      var prefab=Resources.Load<GameObject>("Islands/"+modelName);
      if(!prefab)return null;
      var go=Instantiate(prefab,root,false);
      go.name="IslandMeshVisual";
      go.transform.localPosition=new Vector3(0,.02f,0);
      go.transform.localRotation=Quaternion.identity;
      go.transform.localScale=new Vector3(width/baseDim.x,1f,length/baseDim.z);
      var tex=Resources.Load<Texture2D>("Islands/Textures/"+modelName+"_basecolor");
      if(tex){
       var mat=new Material(Shader.Find("Standard")){name=modelName+"_Mat"};
       mat.mainTexture=tex;mat.SetFloat("_Glossiness",.25f);
       foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mat;
      }
      foreach(var mf in go.GetComponentsInChildren<MeshFilter>()){
       if(mf&&mf.sharedMesh&&mf.sharedMesh.vertexCount>0&&!mf.GetComponent<Collider>()){
        var mc=mf.gameObject.AddComponent<MeshCollider>();
        mc.sharedMesh=mf.sharedMesh;
       }
      }
      return go;
     }
     GameObject Island(Vector3 pos,float width,float length,int index,IslandArchetype archetype=IslandArchetype.Standard,bool isBoss=false){
      var root=new GameObject("Island");root.transform.SetParent(transform);root.transform.position=pos;
       if(archetype==IslandArchetype.SteppingStones){
        Vector3[] stepOffsets=new[]{new Vector3(-1.6f,0,-2.5f),new Vector3(1.6f,.25f,0),new Vector3(-1f,.1f,2.5f)};
         string stepModel=realm==3?"Islands/R3_Island_SteppingStone":realm==2?"Islands/R2_Island_SteppingStone":realm==1?"Islands/R1_Island_SteppingStone":realm==0?"Islands/Island_SteppingStone":null;
         var stepPrefab=stepModel!=null?Resources.Load<GameObject>(stepModel):null;
         string stepTexPath=realm==3?"Islands/Textures/R3_Island_SteppingStone_basecolor":realm==2?"Islands/Textures/R2_Island_SteppingStone_basecolor":realm==1?"Islands/Textures/R1_Island_SteppingStone_basecolor":realm==0?"Islands/Textures/Island_SteppingStone_basecolor":null;
        var stepTex=stepTexPath!=null?Resources.Load<Texture2D>(stepTexPath):null;
       Material stepMat=null;
       if(stepTex){stepMat=new Material(Shader.Find("Standard")){name="StepStone_Mat"};stepMat.mainTexture=stepTex;stepMat.SetFloat("_Glossiness",.25f);}
      for(int k=0;k<stepOffsets.Length;k++){
       var sp=Art.Shape("StepPillar",PrimitiveType.Cylinder,stepOffsets[k]+new Vector3(0,-.6f,0),new Vector3(3.4f,1.2f,3.4f),stone,root.transform,true);
       var ss=Art.Shape("StepSurface",PrimitiveType.Cylinder,stepOffsets[k]+new Vector3(0,.02f,0),new Vector3(3.5f,.12f,3.5f),top,root.transform);
       if(stepPrefab){
        var spr=sp.GetComponent<Renderer>();if(spr)spr.enabled=false;
        var ssr=ss.GetComponent<Renderer>();if(ssr)ssr.enabled=false;
        var stepObj=Instantiate(stepPrefab,root.transform,false);
        stepObj.name="StepVisual_"+k;
        stepObj.transform.localPosition=stepOffsets[k]+new Vector3(0,.02f,0);
        stepObj.transform.localScale=Vector3.one;
        if(stepMat)foreach(var r in stepObj.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=stepMat;
        foreach(var mf in stepObj.GetComponentsInChildren<MeshFilter>()){
         if(mf&&mf.sharedMesh&&mf.sharedMesh.vertexCount>0&&!mf.GetComponent<Collider>()){
          var mc=mf.gameObject.AddComponent<MeshCollider>();
          mc.sharedMesh=mf.sharedMesh;
         }
        }
       }
       Art.Ring(stepOffsets[k]+new Vector3(0,.1f,0),1f,accent*.7f,root.transform);
       if(k==1){
        var magmaPrefab=Resources.Load<GameObject>("Props/MagmaPlatform");
        if(magmaPrefab){
         var mp=Instantiate(magmaPrefab,sp.transform,false);
         mp.name="MagmaPlatformVisual";mp.transform.localPosition=Vector3.up*.6f;
         mp.transform.localScale=new Vector3(.9f,.9f,.9f);
         var mTex=Resources.Load<Texture2D>("Props/Textures/MagmaPlatform_basecolor");
         if(mTex){
          var mmat=new Material(Shader.Find("Standard")){name="MagmaPlat_Mat"};
          mmat.mainTexture=mTex;mmat.SetFloat("_Glossiness",.25f);
          foreach(var r in mp.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=mmat;
         }
        }
        CrumblePlatform.Attach(sp.gameObject);
       }
      }
      return root;
     }
     // 1. Walkable solid stone base
     var solid=Art.Shape("Walkable stone",PrimitiveType.Cube,new Vector3(0,-.92f,0),new Vector3(width,1.7f,length),stone,root.transform,true);
     // 2. Realm terrain surface with slight border reveal
     var surf=Art.Shape("Realm surface",PrimitiveType.Cube,new Vector3(0,.02f,0),new Vector3(width+.08f,.16f,length+.08f),top,root.transform);
     var visual=DressIslandVisual(root.transform,archetype,width,length,index,isBoss);
     if(visual!=null){
      var sr=solid.GetComponent<Renderer>();if(sr)sr.enabled=false;
      var surfr=surf.GetComponent<Renderer>();if(surfr)surfr.enabled=false;
     }else{
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
     }
     if(archetype==IslandArchetype.Arena){
      if(visual==null){
       for(int sx=-1;sx<=1;sx+=2){
        for(int sz=-1;sz<=1;sz+=2){
         Art.Shape("ArenaPillar",PrimitiveType.Cube,new Vector3(sx*5.2f,1.8f,sz*5.2f),new Vector3(.85f,3.6f,.85f),stone*1.2f,root.transform);
         Art.Crystal(new Vector3(sx*5.2f,3.8f,sz*5.2f),.5f,accent,root.transform);
        }
       }
      }else if(realm==0){
       for(int sx=-1;sx<=1;sx+=2){
        for(int sz=-1;sz<=1;sz+=2){
         Art.Crystal(new Vector3(sx*(width*.38f),1.1f,sz*(length*.38f)),.5f,accent,root.transform);
        }
       }
      }
      BreakableCrate.Place(root.transform,new Vector3(-4.2f,.05f,4f),stone*1.3f);
      BreakableCrate.Place(root.transform,new Vector3(4.2f,.05f,4f),stone*1.3f,true);
      ExplosiveBarrel.Place(root.transform,new Vector3(-4.2f,.05f,-4f),accent);
      if(level>=3&&index%2==1)FloorBladeTrap.Place(root.transform,new Vector3(0,.05f,0),accent);
      else SpikeTrap.Place(root.transform,new Vector3(0,.05f,0),accent);
     }else if(archetype==IslandArchetype.NarrowBridge){
      PendulumTrap.Place(root.transform,new Vector3(0,.05f,0),width,accent);
     }else if(archetype==IslandArchetype.TieredPlatform){
      var ut=Art.Shape("UpperTerrace",PrimitiveType.Cube,new Vector3(-.84f,.20f,-2.55f),new Vector3(8.50f,2.20f,4.90f),stone,root.transform,true);
      var utRight=Art.Shape("UpperTerraceRight",PrimitiveType.Cube,new Vector3(3.48f,.08f,-2.90f),new Vector3(3.35f,2.20f,3.80f),stone,root.transform,true);
      var ramp=Art.Shape("StairRamp",PrimitiveType.Cube,new Vector3(0,.51f,.55f),new Vector3(3.90f,.40f,1.98f),stone*1.15f,root.transform,true);
      ramp.transform.localRotation=Quaternion.Euler(38.43f,0,0);
      var wallLeft=Art.Shape("StairWallLeft",PrimitiveType.Cube,new Vector3(-3.45f,.18f,-.20f),new Vector3(3.10f,2.20f,2.00f),stone,root.transform,true);
      var wallRight=Art.Shape("StairWallRight",PrimitiveType.Cube,new Vector3(3.53f,-.10f,-.10f),new Vector3(3.25f,1.60f,2.20f),stone,root.transform,true);
      var ts=Art.Shape("TerraceSurface",PrimitiveType.Cube,new Vector3(-.84f,1.35f,-2.55f),new Vector3(8.55f,.12f,4.95f),top,root.transform);
      if(visual!=null){
       var ur=ut.GetComponent<Renderer>();if(ur)ur.enabled=false;
       var urr=utRight.GetComponent<Renderer>();if(urr)urr.enabled=false;
       var rr=ramp.GetComponent<Renderer>();if(rr)rr.enabled=false;
       var wlr=wallLeft.GetComponent<Renderer>();if(wlr)wlr.enabled=false;
       var wrr=wallRight.GetComponent<Renderer>();if(wrr)wrr.enabled=false;
       var tr=ts.GetComponent<Renderer>();if(tr)tr.enabled=false;
      }
     }else if(archetype==IslandArchetype.MovingFerry){
      if(visual==null){
       var crystalPrefab=Resources.Load<GameObject>("Props/CrystalPlatform");
       var skullPrefab=Resources.Load<GameObject>("Props/Platform_Skull_01");
       if(index%2==0&&crystalPrefab){
        var cObj=Instantiate(crystalPrefab,root.transform,false);
        cObj.name="CrystalPlatformVisual";cObj.transform.localPosition=Vector3.up*.02f;
        cObj.transform.localScale=new Vector3(width/5.8f,1f,length/5.8f);
        var cTex=Resources.Load<Texture2D>("Props/Textures/CrystalPlatform_basecolor");
        if(cTex){
         var cmat=new Material(Shader.Find("Standard")){name="CrystalPlat_Mat"};
         cmat.mainTexture=cTex;cmat.SetFloat("_Glossiness",.35f);
         foreach(var r in cObj.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=cmat;
        }
       }else if(skullPrefab){
        var skullObj=Instantiate(skullPrefab,root.transform,false);
        skullObj.name="SkullPlatformVisual";
        skullObj.transform.localPosition=new Vector3(0,-.15f,0);
        skullObj.transform.localRotation=Quaternion.Euler(-90,0,0);
        skullObj.transform.localScale=new Vector3(width/3.1f,length/3.1f,1.8f);
        var skullTex=Resources.Load<Texture2D>("Props/Textures/Platform_Skull_basecolor");
        if(skullTex){
         var smat=new Material(Shader.Find("Standard")){name="SkullPlat_Mat",color=stone*1.2f};
         smat.mainTexture=skullTex;smat.SetFloat("_Glossiness",.3f);
         foreach(var r in skullObj.GetComponentsInChildren<Renderer>(true))r.sharedMaterial=smat;
        }
       }
      }
      var motion=root.AddComponent<MovingIsland>();
      motion.Origin=pos;
      motion.Offset=new Vector3((index%2==0?1:-1)*3.8f,0,0);
      motion.Style=IslandMotionStyle.PingPongDwell;
      motion.Speed=1.35f;motion.DwellTime=1.2f;
      motion.AddThrusters(accent);
     }else{
     // 6. Perimeter decor & ancient runes
     if(visual==null){
      for(int side=-1;side<=1;side+=2){
       if(width<7)break;
       for(int n=0;n<2;n++){
        Vector3 p=new Vector3(side*(width*.5f-.85f),0,(n==0?-1:1)*(length*.5f-.95f));
        Decor(p,root.transform,index+n);
       }
      }
     }
     Art.Ring(new Vector3(0,.11f,0),.55f,accent*.7f,root.transform);
    }
    Physics.SyncTransforms();
    return root;
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
   }else if(realm==2){
    var g=Art.Crystal(p+Vector3.up*1.2f,1.25f,accent,parent);g.transform.localScale=new Vector3(.65f,1.85f,.65f);
    Art.Shape("IceShard",PrimitiveType.Cube,p+new Vector3(-.35f,.35f,.25f),new Vector3(.3f,.7f,.3f),Color.Lerp(accent,Color.white,.4f),parent);
   }else{
    var g=Art.Crystal(p+Vector3.up*1.2f,1.25f,accent,parent);g.transform.localScale=new Vector3(.75f,2.1f,.75f);
    Art.Shape("EmberHotStone",PrimitiveType.Cube,p+new Vector3(.4f,.3f,-.2f),new Vector3(.5f,.5f,.5f),top*1.35f,parent);
   }
  }
  void Pickup(Vector3 position,bool gem){
    Physics.SyncTransforms();
   var hits=Physics.RaycastAll(position+Vector3.up*5f,Vector3.down,10f,~0,QueryTriggerInteraction.Ignore);
   float bestY=float.NegativeInfinity;
   for(int i=0;i<hits.Length;i++){if(hits[i].normal.y>=.5f&&!hits[i].transform.name.StartsWith("Prop ")&&hits[i].point.y>bestY)bestY=hits[i].point.y;}
   if(bestY>float.NegativeInfinity)position=new Vector3(position.x,bestY+(gem?.65f:.48f),position.z);
   var go=new GameObject(gem?"GemPickup":"CoinPickup");
   go.transform.SetParent(transform,false);go.transform.position=position;
   if(gem)RelicArt.Gem(go.transform,accent);
   else Art.CoinMesh(go.transform);
   var pickup=go.AddComponent<RealmPickup>();pickup.Gem=gem;pickup.Origin=position;
  }
   void Checkpoint(Vector3 p){
    Physics.SyncTransforms();
    var hits=Physics.RaycastAll(p+Vector3.up*6f,Vector3.down,12f,~0,QueryTriggerInteraction.Ignore);
    float bestY=float.NegativeInfinity;
    for(int j=0;j<hits.Length;j++){
     var h=hits[j];
     if(h.normal.y>=.5f&&!h.transform.name.StartsWith("Prop ")&&h.point.y>bestY)bestY=h.point.y;
    }
    if(bestY>float.NegativeInfinity)p=new Vector3(p.x,bestY,p.z);
    var go=new GameObject("Checkpoint shrine");go.transform.SetParent(transform);go.transform.position=p;
    RelicArt.Checkpoint(go.transform,accent,stone);
    go.AddComponent<RealmCheckpoint>();
   }
void WeaponDrop(Vector3 p,WeaponId id){
    var go=new GameObject("Weapon drop - "+WeaponCatalog.Get(id).Name);go.transform.SetParent(transform,false);go.transform.position=p;
    var drop=go.AddComponent<WeaponDrop>();drop.Configure(id,p);
    Vfx.Play("ga_vfx_LootDrop_01",p+Vector3.up*1f,Quaternion.identity,.85f);
   }
   void HealPickup(Vector3 p){
    Physics.SyncTransforms();
    var hits=Physics.RaycastAll(p+Vector3.up*6f,Vector3.down,12f,~0,QueryTriggerInteraction.Ignore);
    float bestY=float.NegativeInfinity;
    for(int i=0;i<hits.Length;i++){if(hits[i].normal.y>=.5f&&!hits[i].transform.name.StartsWith("Prop ")&&hits[i].point.y>bestY)bestY=hits[i].point.y;}
    if(bestY>float.NegativeInfinity)p=new Vector3(p.x,bestY+.75f,p.z);
    var go=new GameObject("Heal pickup");go.transform.SetParent(transform,false);go.transform.position=p;
    Color heart=new Color(.92f,.12f,.16f);
    if(!Art.PickupModel("Heart",go.transform,heart,.55f,null)){
     Art.Shape("HeartLobeL",PrimitiveType.Sphere,new Vector3(-.1f,.1f,0),new Vector3(.36f,.4f,.34f),heart,go.transform);
     Art.Shape("HeartLobeR",PrimitiveType.Sphere,new Vector3(.1f,.1f,0),new Vector3(.36f,.4f,.34f),heart,go.transform);
     var point=Art.Shape("HeartPoint",PrimitiveType.Cube,new Vector3(0,-.06f,0),new Vector3(.32f,.34f,.3f),new Color(.82f,.1f,.14f),go.transform);
     point.transform.localRotation=Quaternion.Euler(0,0,45);
     Art.Shape("HeartGlint",PrimitiveType.Cube,new Vector3(-.11f,.2f,0),Vector3.one*.06f,new Color(1f,.86f,.88f),go.transform);
    }
    go.AddComponent<RealmHeal>().Origin=p;
   }
   void Gate(Vector3 p){
    string[] gateNames={"Verdant","Desert","Snow","Lava"};
    string realmName=realm<gateNames.Length?gateNames[realm]:"Verdant";
    var prefab=Resources.Load<GameObject>("Gates/TeleportGate_"+realmName);
    GameObject root;
    if(prefab!=null){
     root=Instantiate(prefab,transform);
     root.name="Realm gate";
     root.transform.position=p;
     if(IsBoss)root.transform.localScale=Vector3.one*1.25f;
    }else{
     root=TeleportGateFactory.Create(transform,realm,p,IsBoss);
    }
    Vfx.Play("ga_vfx_Heal_02",p+Vector3.up*2f,Quaternion.identity,1.1f);
   }
   void Hazard(Transform island,Vector3 localPos,int kind){
    if(kind==3||realm==3){
     FireGeyser.Place(island,localPos,accent);
    }else if(kind==1||realm==1){
     SawTrap.Place(island,localPos-new Vector3(2.6f,0,0),localPos+new Vector3(2.6f,0,0),accent);
    }else if(kind==2||realm==2){
     SpikeTrap.Place(island,localPos,accent);
    }else{
     var go=new GameObject("Realm hazard");go.transform.SetParent(island,false);go.transform.localPosition=localPos;
     for(int i=0;i<5;i++){
      var spike=Art.Shape("Thorn",PrimitiveType.Cube,new Vector3((i%3)*.37f-.4f,.28f,(i/3)*.4f),new Vector3(.18f,.65f,.18f),kind==1?new Color(1,.35f,.12f):accent,go.transform);
      spike.transform.localRotation=Quaternion.Euler(0,30,15);
     }
     go.AddComponent<RealmHazard>();
    }
   }
   void Hazard(Vector3 p,int kind){
    var island=FindIsland(p);
    if(island!=null)Hazard(island.transform,island.transform.InverseTransformPoint(p),kind);
    else Hazard(transform,p,kind);
   }
  void SpawnEnemy(Vector3 p,int kind,bool boss,Vector3 center,float width,float length){
   var go=new GameObject(boss?"Realm guardian":"Realm enemy");go.transform.SetParent(transform);go.transform.position=p;
   var e=go.AddComponent<Enemy>();e.Configure(kind,boss,center,new Vector2(width,length));
   if(boss)Vfx.Play("ga_vfx_Portal_02",p+Vector3.up*1.7f,Quaternion.identity,1.2f);
  }
 }
public class RealmPickup:MonoBehaviour {
   public bool Gem;public Vector3 Origin;
   void Start(){
    var hits=Physics.RaycastAll(Origin+Vector3.up*5f,Vector3.down,10f,~0,QueryTriggerInteraction.Ignore);
    float bestY=float.NegativeInfinity;
    for(int i=0;i<hits.Length;i++){if(hits[i].normal.y>=.5f&&hits[i].point.y>bestY)bestY=hits[i].point.y;}
    if(bestY>float.NegativeInfinity){float minSafe=bestY+(Gem?.65f:.48f);if(Origin.y<minSafe)Origin.y=minSafe;}
    transform.position=Origin;
   }
   void Update(){
    var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
    // GemVisual supplies the main faceted motion. Keep the pickup root slow so
    // its orbiting shards read clearly instead of turning into a spinning cube.
transform.Rotate(0,(Gem?12f:45f)*Time.deltaTime,0,Space.World);
     Vector3 target=g.Player.transform.position+Vector3.up*.8f;
     if(Vector3.Distance(Origin,target)<(Gem?1.85f:2.1f))Origin=Vector3.MoveTowards(Origin,target,Time.deltaTime*(Gem?5.4f:4.5f));
     transform.position=Origin+Vector3.up*Mathf.Sin(RealmGame.I.Elapsed*1.4f)*.09f;
    if(Vector3.Distance(g.Player.transform.position+Vector3.up*.8f,transform.position)<1.15f){
     HitSpark.Burst(transform.position,Vector3.up,Gem?g.Accent:new Color(1f,.85f,.2f),Gem?18:12);
     Vfx.Play(Gem?"ga_vfx_LootDrop_02":"ga_vfx_Sparks_01",transform.position,Quaternion.identity,Gem?.8f:.55f);
     g.Collect(Gem);Destroy(gameObject);
    }
   }
   // Spawn one or more gem pickups around a point (enemy drops). Same magnet
   // behaviour as the scattered route gems, so nothing new to learn.
   public static void Drop(Vector3 position,int count){
    var g=RealmGame.I;if(!g||!g.World||count<=0)return;
    for(int i=0;i<count;i++){
     var go=new GameObject("Loot gem");go.transform.SetParent(g.World.transform,false);
     float a=(i+1)*(360f/count)*Mathf.Deg2Rad;
     Vector3 p=position+new Vector3(Mathf.Cos(a)*.8f,.3f,Mathf.Sin(a)*.8f);
     var hits=Physics.RaycastAll(p+Vector3.up*5f,Vector3.down,10f,~0,QueryTriggerInteraction.Ignore);
     float bestY=float.NegativeInfinity;
     for(int k=0;k<hits.Length;k++){if(hits[k].normal.y>=.5f&&hits[k].point.y>bestY)bestY=hits[k].point.y;}
     if(bestY>float.NegativeInfinity)p.y=bestY+.65f;
     go.transform.position=p;
     RelicArt.Gem(go.transform,g.Accent);
     var pickup=go.AddComponent<RealmPickup>();pickup.Gem=true;pickup.Origin=go.transform.position;
    }
   }
  }
  public class RealmHeal:MonoBehaviour {
   public Vector3 Origin;public int Amount=3;
   void Start(){
    var hits=Physics.RaycastAll(Origin+Vector3.up*6f,Vector3.down,12f,~0,QueryTriggerInteraction.Ignore);
    float bestY=float.NegativeInfinity;
    for(int i=0;i<hits.Length;i++){if(hits[i].normal.y>=.5f&&hits[i].point.y>bestY)bestY=hits[i].point.y;}
    if(bestY>float.NegativeInfinity){float minSafe=bestY+.75f;if(Origin.y<minSafe)Origin.y=minSafe;}
    transform.position=Origin;
   }
   void Update(){
    var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
    transform.Rotate(0,28f*Time.deltaTime,0,Space.World);
    transform.position=Origin+Vector3.up*Mathf.Sin(RealmGame.I.Elapsed*1.4f)*.09f;
    if(Vector3.Distance(g.Player.transform.position+Vector3.up*.8f,transform.position)<1.15f){
     int before=g.Player.Health;g.Player.Health=Mathf.Min(g.Player.MaxHealth,before+Amount);
     if(g.Player.Health>before){HitSpark.Burst(transform.position,Vector3.up,new Color(1f,.38f,.48f),22);Vfx.Play("ga_vfx_Heal_01",transform.position,Quaternion.identity,.9f);g.Sound("heal");}
     else{g.Sound("gem");}
     Destroy(gameObject);
    }
   }
  }
 public class RealmCheckpoint:MonoBehaviour {
  bool active;CheckpointVisual visual;
  void Awake(){visual=GetComponent<CheckpointVisual>();}
  void Update(){
   var g=RealmGame.I;
   if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
    if(!active&&Vector3.Distance(g.Player.transform.position,transform.position)<2.45f){
    active=true;if(visual)visual.Activate();HitSpark.Burst(transform.position+Vector3.up*1.35f,Vector3.up,g.Accent,28);Vfx.Play("ga_vfx_Portal_01",transform.position+Vector3.up*1.3f,Quaternion.identity,.9f);
    g.ActivateCheckpoint(transform.position+Vector3.forward*1.5f+Vector3.up*.05f);
   }
  }
 }
  public class RealmGate:MonoBehaviour {
   float next;public readonly System.Collections.Generic.List<Transform> spin=new System.Collections.Generic.List<Transform>();public readonly System.Collections.Generic.List<float> speeds=new System.Collections.Generic.List<float>();
   void Update(){
    var g=RealmGame.I;
    if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
    for(int i=0;i<spin.Count;i++)if(spin[i])spin[i].localRotation*=Quaternion.Euler(0,(speeds.Count>i?speeds[i]:30)*Time.deltaTime,0);
    if(GetComponent<TeleportGateController>()!=null)return;
    if(g.Elapsed>next&&Vector3.Distance(g.Player.transform.position,transform.position)<1.6f){
     next=RealmGame.I.Elapsed+2;HitSpark.Burst(transform.position+Vector3.up*2f,Vector3.up,g.Accent,24);Vfx.Play("ga_vfx_Portal_02",transform.position+Vector3.up*2f,Quaternion.identity,1.25f);
     g.Finish();
    }
   }
  }
 public class RealmHazard:MonoBehaviour {
  void Update(){
   var g=RealmGame.I;
   if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   if(Vector3.Distance(g.Player.transform.position,transform.position)<.85f)
    g.Player.Damage(1,transform.position);
  }
 }
 [DefaultExecutionOrder(-10)] public class MovingIsland:MonoBehaviour {
  public Vector3 Origin,Offset;public IslandMotionStyle Style=IslandMotionStyle.Sinusoidal;
  public float Speed=1.2f,DwellTime=1.1f;
  float motionTime,dwellTimer;int direction=1;float progress=0.5f;
  public void AddThrusters(Color accent){
   var thruster=new GameObject("LevitationThruster");thruster.transform.SetParent(transform,false);
   thruster.transform.localPosition=new Vector3(0,-2.4f,0);
   Art.Ring(Vector3.zero,1.35f,accent*.9f,thruster.transform);
   Art.Crystal(Vector3.down*.3f,.55f,accent,thruster.transform);
  }
  void FixedUpdate(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   bool riding=g.Player.Grounded&&Physics.Raycast(g.Player.transform.position+Vector3.up*.2f,Vector3.down,out var hit,.65f,~0,QueryTriggerInteraction.Ignore)&&hit.transform.IsChildOf(transform);
   Vector3 prior=transform.position;
   if(Style==IslandMotionStyle.PingPongDwell||Style==IslandMotionStyle.VerticalElevator){
    if(dwellTimer>0){
     dwellTimer-=Time.fixedDeltaTime;
    }else{
     progress+=direction*(Speed*.35f)*Time.fixedDeltaTime;
     if(progress>=1f){progress=1f;direction=-1;dwellTimer=DwellTime;}
     else if(progress<=0f){progress=0f;direction=1;dwellTimer=DwellTime;}
    }
    float eased=(1f-Mathf.Cos(progress*Mathf.PI))*.5f;
    transform.position=Origin+Offset*(eased*2f-1f);
   }else if(Style==IslandMotionStyle.Orbit){
    motionTime+=Time.fixedDeltaTime*Speed;
    transform.position=Origin+new Vector3(Mathf.Cos(motionTime)*Offset.x,Offset.y*Mathf.Sin(motionTime*.8f),Mathf.Sin(motionTime)*Offset.z);
   }else{
    motionTime+=Time.fixedDeltaTime*Speed;
    transform.position=Origin+Offset*Mathf.Sin(motionTime*.8f);
   }
   Vector3 delta=transform.position-prior;
   if(riding)g.Player.CarryByPlatform(delta);
   if(g.Enemies!=null){
    foreach(var foe in g.Enemies){
     if(!foe||foe.Health<=0)continue;
     if(Physics.Raycast(foe.transform.position+Vector3.up*.5f,Vector3.down,out var fhit,1.6f,~0,QueryTriggerInteraction.Ignore)&&fhit.transform.IsChildOf(transform)){
      foe.transform.position+=delta;
      foe.ShiftCenter(delta);
     }
    }
   }
  }
 }
}
