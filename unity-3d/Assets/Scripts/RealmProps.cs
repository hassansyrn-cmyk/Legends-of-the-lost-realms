using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LostRealms {
 // Scatters imported low-poly nature props (SimpleNaturePack) around each island
 // so the realms use real models instead of primitives. Models live in
 // Resources/Props/Nature; one shared palette texture feeds a single material.
 public static class RealmProps {
   static readonly Material[] byRealm=new Material[4];
   static readonly Dictionary<string,GameObject> cache=new Dictionary<string,GameObject>();
   static Material stoneMat;
   static readonly Dictionary<string,Material> glowMats=new Dictionary<string,Material>();
   static Mesh shardMesh;

  // Realm 2 uses a snow-converted palette and the imported snow models;
  // realm 1 uses the imported desert-city models on a sandy palette so the
  // architecture reads as stone instead of lush grass; realm 0 keeps the lush
  // nature palette.
  static Material ForRealm(int realm){
if(realm<0||realm>3)realm=0;
    if(byRealm[realm])return byRealm[realm];
    var texture=Resources.Load<Texture2D>(realm==2?"Props/Textures/Nature_snow":realm==1?"Props/Textures/Desert_palette":realm==3?"Props/Textures/Ember_palette":"Props/Textures/Nature_basecolor");
   if(!texture)return null;
   var material=new Material(Shader.Find("Standard")){name="Realm nature "+realm,color=Color.white};
   material.mainTexture=texture;material.SetFloat("_Metallic",0f);material.SetFloat("_Glossiness",.18f);
   byRealm[realm]=material;return material;
  }
  static string Folder(int realm,string name){
   if(name.StartsWith("rpgpp_lt_"))return "Village";
   if(realm==2)return "Snow";
   if(realm==1&&(name.StartsWith("House_")||name.StartsWith("Ruin_")||name=="Tower_01"||name=="Gate_01"||name=="Tent_01"||name=="Church_01"||name=="Wall_01"))return "Desert";
   return "Nature";
  }
  static GameObject Model(string folder,string name){
   string key=folder+"/"+name;
   if(cache.TryGetValue(key,out var cached))return cached;
   var model=Resources.Load<GameObject>("Props/"+folder+"/"+name);
   cache[key]=model;return model;
  }
  // RPGPP_LT village props are UV-mapped to their own atlas (not the realm
  // palettes), so they get their own material, tinted per realm to sit in the
  // scene: lush in realm 0, sandy in realm 1, icy in realm 2.
static readonly Material[] villageByRealm=new Material[4];
   static Material VillageMat(int realm){
    if(realm<0||realm>3)realm=0;
    if(villageByRealm[realm])return villageByRealm[realm];
    var texture=Resources.Load<Texture2D>("Props/Textures/rpgpp_lt_tex_a");
    if(!texture)return null;
    var material=new Material(Shader.Find("Standard")){name="Village atlas "+realm};
    material.mainTexture=texture;material.SetFloat("_Metallic",0f);material.SetFloat("_Glossiness",.18f);
    material.color=realm==0?new Color(.92f,.95f,.88f):realm==1?new Color(1f,.85f,.62f):realm==2?new Color(.72f,.82f,1f):new Color(.6f,.42f,.36f);
   villageByRealm[realm]=material;return material;
  }
  static Material MaterialFor(string name,int realm)=>name.StartsWith("rpgpp_lt_")?VillageMat(realm):ForRealm(realm);
  static string[][] Sets(int realm){
   if(realm==0)return new[]{new[]{"Tree_01","Tree_03","Tree_04","rpgpp_lt_tree_01","rpgpp_lt_tree_02"},new[]{"Rock_01","Rock_02","Rock_03"},new[]{"Bush_01","Bush_02","Bush_03","Grass_01","Grass_02","Flowers_01","Mushroom_01","rpgpp_lt_bush_01","rpgpp_lt_flower_01","rpgpp_lt_flower_02","rpgpp_lt_grass_small_01a"}};
if(realm==1)return new[]{new[]{"House_01","House_02","Tower_01","Ruin_01","Gate_01","Tent_01","Church_01"},new[]{"Rock_03","Rock_04","Rock_05"},new[]{"Grass_02","Mushroom_01","Bush_02","rpgpp_lt_rock_small_01","rpgpp_lt_rock_small_02"}};
    if(realm==3)return new[]{new[]{"Tree_01","Tree_02","Tree_03","DeadTree_01"},new[]{"Rock_03","Rock_04","Rock_05"},new[]{"Grass_02","Flowers_01","Bush_02","rpgpp_lt_rock_small_01","rpgpp_lt_rock_small_02"}};
    return new[]{new[]{"Pine_01","Pine_02","Pine_03","Tree_01","Tree_02","DeadTree_01","rpgpp_lt_tree_pine_01"},new[]{"Cottage_01","Cottage_02","Snowman_01","Well_01","Stump_01","Bridge_01"},new[]{"Fence_01","Fence_02","Sign_01","Stump_01","rpgpp_lt_rock_small_01","rpgpp_lt_bucket_01"}};
  }
  public static void Scatter(Transform world,int realm,System.Random rng){
   var material=ForRealm(realm);if(!material)return;
   var sets=Sets(realm);
   foreach(Transform island in world){
    if(island.name!="Island")continue;
    var surface=island.Find("Realm surface");if(!surface)continue;
    float width=surface.localScale.x-.08f,length=surface.localScale.z-.08f;
    if(width<=7f)continue;
if(realm==1){PlaceDesertBuildings(island,width,length,rng,material);}
     else{
      int trees=2+rng.Next(0,2);
      for(int i=0;i<trees;i++)Place(island,realm,sets[0][rng.Next(sets[0].Length)],width,length,rng,realm==0?3.2f:2.6f,3.6f,material);
     }
    int rocks=2+rng.Next(0,3);
    for(int i=0;i<rocks;i++)Place(island,realm,sets[1][rng.Next(sets[1].Length)],width,length,rng,.6f,1.1f,material);
     int small=5+rng.Next(0,4);
     for(int i=0;i<small;i++)Place(island,realm,sets[2][rng.Next(sets[2].Length)],width,length,rng,.35f,.6f,material);
     PlaceVillage(island,realm,width,length,rng);
     PlaceOrnaments(island,realm,width,length,rng);
    }
   }
  // RPGPP village dressing: at most one structure per island, parked beside the
  // travel lane (same lane-safe philosophy as the desert buildings), plus a few
  // crates/barrels/benches and small clutter near the edges.
  static readonly string[] VillageStructures={"rpgpp_lt_shed_wood_01","rpgpp_lt_shed_wood_02","rpgpp_lt_well_01","rpgpp_lt_wagon_01"};
  static readonly string[] VillageMedium={"rpgpp_lt_barrel_01","rpgpp_lt_barrel_02","rpgpp_lt_crate_01","rpgpp_lt_crate_02","rpgpp_lt_bench_wood_01","rpgpp_lt_box_wood_01","rpgpp_lt_log_wood_01","rpgpp_lt_rock_01","rpgpp_lt_rock_02","rpgpp_lt_rock_03","rpgpp_lt_ladder_01"};
  static readonly string[] VillageSmall={"rpgpp_lt_sack_01","rpgpp_lt_bucket_01","rpgpp_lt_vase_01","rpgpp_lt_rock_small_01","rpgpp_lt_rock_small_02","rpgpp_lt_bush_02"};
static void PlaceVillage(Transform island,int realm,float width,float length,System.Random rng){
    if(realm==3)return;
    if(width>9f&&rng.Next(0,100)<60)Place(island,realm,VillageStructures[rng.Next(VillageStructures.Length)],width,length,rng,1.9f,2.4f,null);
   int medium=1+rng.Next(0,3);
   for(int i=0;i<medium;i++)Place(island,realm,VillageMedium[rng.Next(VillageMedium.Length)],width,length,rng,.7f,1.1f,null);
   int tiny=1+rng.Next(0,3);
   for(int i=0;i<tiny;i++)Place(island,realm,VillageSmall[rng.Next(VillageSmall.Length)],width,length,rng,.3f,.5f,null);
  }
static void Place(Transform island,int realm,string name,float width,float length,System.Random rng,float hMin,float hMax,Material material){
     var source=Model(Folder(realm,name),name);if(!source)return;
     material=MaterialFor(name,realm);if(!material)return;
     float halfWidth=Mathf.Max(.6f,width*.5f-.7f);
     int side=rng.Next(0,2)==0?-1:1;
     float x=side*Mathf.Min(halfWidth,1.6f+(float)rng.NextDouble()*halfWidth);
     float z=((float)rng.NextDouble()-.5f)*Mathf.Max(.6f,length-.9f);
     var prop=Object.Instantiate(source,island);
     prop.name="Prop "+name;
     prop.transform.localPosition=new Vector3(x,.02f,z);
     // Keep the model's import rotation (some packs stand up via a baked root
     // rotation, e.g. (270,180,0)); identity here flattens those props.
     var baseRot=source.transform.localRotation;
     prop.transform.localRotation=baseRot;
     Fit(prop,hMin+(float)rng.NextDouble()*(hMax-hMin));
     // Some packs (RPGPP village props) ship meshes extending below their pivot
     // (negative census pivotMinY) — ground every prop on its real bounds so
     // rocks/logs/wagons never sink into the island.
     var groundRenderers=prop.GetComponentsInChildren<Renderer>(true);
     if(groundRenderers.Length>0){
      Bounds gb=groundRenderers[0].bounds;
      for(int i=1;i<groundRenderers.Length;i++)gb.Encapsulate(groundRenderers[i].bounds);
      float surfaceY=island.TransformPoint(new Vector3(0,.02f,0)).y;
      prop.transform.position+=Vector3.up*(surfaceY-gb.min.y);
     }
     AddCollider(prop);
     float yaw=(float)rng.NextDouble()*360f;
     prop.transform.localRotation=Quaternion.Euler(0,yaw,0)*baseRot;
    foreach(var renderer in prop.GetComponentsInChildren<Renderer>(true)){
     renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
    }
   }
   static void AddCollider(GameObject prop){
    if(prop.GetComponentInChildren<Collider>())return;
    var renderers=prop.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
    Bounds b=renderers[0].bounds;
    for(int i=1;i<renderers.Length;i++)b.Encapsulate(renderers[i].bounds);
    // Express the AABB in the prop's local frame so a baked root rotation
    // (desert/snow packs) doesn't put the height on the wrong local axis.
    Vector3 min=Vector3.one*float.PositiveInfinity,max=Vector3.one*float.NegativeInfinity;
    Vector3[] world={new Vector3(b.min.x,b.min.y,b.min.z),new Vector3(b.max.x,b.min.y,b.min.z),new Vector3(b.min.x,b.max.y,b.min.z),new Vector3(b.min.x,b.min.y,b.max.z),new Vector3(b.max.x,b.max.y,b.min.z),new Vector3(b.max.x,b.min.y,b.max.z),new Vector3(b.min.x,b.max.y,b.max.z),new Vector3(b.max.x,b.max.y,b.max.z)};
    for(int i=0;i<world.Length;i++){var c=prop.transform.InverseTransformPoint(world[i]);min=Vector3.Min(min,c);max=Vector3.Max(max,c);}
    Vector3 center=(min+max)*.5f,size=max-min;
if(prop.name.StartsWith("Prop Tree")){
      var col=prop.AddComponent<CapsuleCollider>();
      if(size.y>=size.x&&size.y>=size.z)col.direction=1;else if(size.z>=size.x&&size.z>=size.y)col.direction=2;else col.direction=0;
      float longest=size.x;if(size.y>longest)longest=size.y;if(size.z>longest)longest=size.z;
      col.center=center;col.height=Mathf.Max(.5f,longest*.9f);col.radius=.3f;
     }else if(IsBuilding(prop.name)){
      // Buildings keep their real mesh for collision: the concave MeshCollider
      // follows the model, so doorway/gate openings stay passable while the
      // walls still block. One collider per renderer (same GameObject, so the
      // shared mesh matches that transform exactly).
      foreach(var renderer in prop.GetComponentsInChildren<MeshRenderer>(true)){
       var filter=renderer.GetComponent<MeshFilter>();
       if(!filter||!filter.sharedMesh||filter.sharedMesh.vertexCount==0)continue;
       if(renderer.GetComponent<Collider>())continue;
       var mc=renderer.gameObject.AddComponent<MeshCollider>();
       mc.sharedMesh=filter.sharedMesh;mc.convex=false;
      }
     }else{
      var col=prop.AddComponent<BoxCollider>();
      col.center=center;col.size=new Vector3(size.x*.92f,size.y*.9f,size.z*.92f);
     }
    }
    static bool IsBuilding(string propName){
     // Desert city buildings + snow cottages/bridge have hollow doorways;
     // "Prop "+name where name is the model file. RPGPP sheds/wagons keep their
     // openings passable the same way.
     foreach(var n in new[]{"Gate_01","Church_01","House_01","House_02","Ruin_01","Tent_01","Tower_01","Wall_01","Cottage_01","Cottage_02","Bridge_01","rpgpp_lt_shed_wood_01","rpgpp_lt_shed_wood_02","rpgpp_lt_wagon_01"})if(propName.EndsWith(" "+n))return true;
     return false;
    }
    // Desert realm: buildings go to organized, path-safe slots — flanking the
    // travel lane at fixed offsets with their front face toward the path, so the
    // way stays clear instead of the old random edge/mid-lane scatter. The Gate_01
    // (whose only reliable portal is measured in Validation/door-facing.txt) is
    // optionally built across the lane as a pass-through arch (yaw 90 puts its
    // opening along the travel axis); its exact mesh collision keeps walls solid.
    static void PlaceDesertBuildings(Transform island,float width,float length,System.Random rng,Material material){
     string[] flank={"House_01","House_02","Tower_01","Ruin_01","Tent_01","Church_01"};
     int n=width>=11f?3:2;
     float halfX=Mathf.Min(Mathf.Max(width*.5f-1.05f,1f),4.2f);
     float zf=Mathf.Max(.9f,length*.22f);
     bool gateway=rng.NextDouble()<0.35f;
     if(gateway)PlaceBuilding(island,"Gate_01",halfX,0,0f,rng,material,gateway:true);
     if(gateway){
      PlaceStatue(island,rng.Next(0,2)==0?"Elite":"Bomber",-3.6f,0f,0f,2.9f);
      PlaceStatue(island,rng.Next(0,2)==0?"Summoner":"Flyer",3.6f,0f,180f,2.9f);
     }
     for(int i=0;i<n;i++){
      int side=(i%2==0)?1:-1;
      float z=(i==0)?-zf*1.15f:(i==1)?(n==3?0f:zf*1.15f):zf*1.15f;
      if(n==3&&i==1&&gateway)continue; // gateway occupies the center slot
      string name=flank[rng.Next(flank.Length)];
      PlaceBuilding(island,name,halfX,side,z,rng,material,false);
     }
    }
    static void PlaceBuilding(Transform island,string name,float xMag,int side,float z,System.Random rng,Material material,bool gateway){
     var source=Model("Desert",name);if(!source)return;
     var prop=Object.Instantiate(source,island);
     prop.name="Prop "+name;
     prop.transform.localPosition=new Vector3(gateway?0f:side*xMag,.02f,z);
     var baseRot=source.transform.localRotation;
     // Gateway: gate across the lane (yaw 90 aims its portal down the route); flank
     // buildings face the path with the same side so the layout reads as rows.
     float yaw=gateway?90f:(side==-1?0f:180f);
     prop.transform.localRotation=Quaternion.AngleAxis(yaw,Vector3.up)*baseRot;
     Fit(prop,2.6f+(float)rng.NextDouble()*.9f);
     AddCollider(prop);
     foreach(var renderer in prop.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
    }
   static GameObject CharModel(string name){
    string key="Char/"+name;
    if(cache.TryGetValue(key,out var cached))return cached;
    var model=Resources.Load<GameObject>("Characters/"+name);
    cache[key]=model;return model;
   }
   static Material Stone(){
    if(stoneMat)return stoneMat;
    stoneMat=new Material(Shader.Find("Standard")){name="Petrified stone"};
    stoneMat.color=new Color(.52f,.5f,.47f);stoneMat.SetFloat("_Metallic",.05f);stoneMat.SetFloat("_Glossiness",.4f);
    return stoneMat;
   }
   static Material GlowMat(Color c){
    string key=ColorUtility.ToHtmlStringRGB(c);
    if(glowMats.TryGetValue(key,out var m))return m;
    m=new Material(Shader.Find("Standard")){name="Prop glow "+key};
    m.SetColor("_Color",c);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*1.6f);
    m.SetFloat("_Metallic",0f);m.SetFloat("_Glossiness",.3f);glowMats[key]=m;return m;
   }
   static Mesh ShardMesh(){
    if(shardMesh)return shardMesh;
    var m=new Mesh{name="Prop shard"};
    m.vertices=new[]{new Vector3(0,.5f,0),new Vector3(0,-.5f,0),new Vector3(.3f,0,0),new Vector3(-.3f,0,0),new Vector3(0,0,.3f),new Vector3(0,0,-.3f)};
    m.triangles=new[]{0,2,4, 0,4,3, 0,3,5, 0,5,2, 1,4,2, 1,3,4, 1,5,3, 1,2,5};
    m.RecalculateNormals();m.RecalculateBounds();shardMesh=m;return m;
   }
   // Petrified guardians: fallen Flyer/Bomber/Summoner/Elite meshes in stone,
   // flanking gate arches and the boss arena. Named "Prop Statue *" so the
   // shared AddCollider gives them a plain box.
   static void PlaceStatue(Transform island,string name,float x,float z,float yaw,float height){
    var source=CharModel(name);if(!source)return;
    var prop=Object.Instantiate(source,island);
    prop.name="Prop Statue "+name;
    prop.transform.localPosition=new Vector3(x,.02f,z);
    prop.transform.localRotation=Quaternion.AngleAxis(yaw,Vector3.up)*source.transform.localRotation;
    Fit(prop,height);
    AddCollider(prop);
    foreach(var renderer in prop.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterial=Stone();renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
   }
   static void PlaceCrystal(Transform island,Color color,float x,float z,float size,System.Random rng){
    var go=new GameObject("Prop Crystal");go.transform.SetParent(island,false);
    go.transform.localPosition=new Vector3(x,.02f,z);
    go.transform.localRotation=Quaternion.Euler((float)rng.NextDouble()*14-7,(float)rng.NextDouble()*360f,(float)rng.NextDouble()*14-7);
    go.transform.localScale=new Vector3(size,size*(1.6f+(float)rng.NextDouble()*.9f),size);
    var mf=go.AddComponent<MeshFilter>();mf.sharedMesh=ShardMesh();
    var mr=go.AddComponent<MeshRenderer>();mr.sharedMaterial=GlowMat(color);mr.shadowCastingMode=ShadowCastingMode.Off;mr.receiveShadows=false;
    AddCollider(go);
   }
   static void PlaceBanner(Transform island,float x,float z){
    var go=new GameObject("Prop Banner");go.transform.SetParent(island,false);
    go.transform.localPosition=new Vector3(x,.02f,z);
    Art.Shape("Banner pole",PrimitiveType.Cylinder,Vector3.up*1.1f,new Vector3(.09f,2.2f,.09f),new Color(.3f,.2f,.14f),go.transform);
    var cloth=Art.Shape("Banner cloth",PrimitiveType.Cube,new Vector3(.3f,1.75f,0),new Vector3(.55f,.7f,.05f),new Color(.75f,.16f,.14f),go.transform);
    cloth.transform.localRotation=Quaternion.Euler(0,0,8);
    foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
    AddCollider(go);
   }
   static void PlaceLantern(Transform island,float x,float z){
    var go=new GameObject("Prop Lantern");go.transform.SetParent(island,false);
    go.transform.localPosition=new Vector3(x,.02f,z);
    Art.Shape("Lantern post",PrimitiveType.Cube,new Vector3(0,.15f,0),new Vector3(.3f,.3f,.3f),new Color(.25f,.28f,.33f),go.transform);
    var pivot=new GameObject("Lantern pivot");pivot.transform.SetParent(go.transform,false);pivot.transform.localPosition=new Vector3(0,1.7f,0);
    Art.Shape("Lantern arm",PrimitiveType.Cube,new Vector3(0,-.85f,0),new Vector3(.07f,1.7f,.07f),new Color(.25f,.28f,.33f),pivot.transform);
    var orb=Art.Shape("Lantern orb",PrimitiveType.Sphere,new Vector3(0,-1.55f,0),Vector3.one*.34f,Color.white,pivot.transform);
    orb.GetComponent<Renderer>().sharedMaterial=GlowMat(new Color(.55f,.85f,1f));
    Art.Ring(new Vector3(0,-1.55f,0),.34f,new Color(.55f,.85f,1f),pivot.transform);
    var swing=go.AddComponent<LanternSwing>();swing.Bind(pivot.transform);
    foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;}
    AddCollider(go);
   }
   // Interactive props: crates/barrels to smash, an occasional pushable block.
   static void PlaceBreakables(Transform island,int realm,float width,float length,System.Random rng){
    float halfX=Mathf.Max(.7f,width*.5f-1.1f);
    Color wood=realm==0?new Color(.5f,.36f,.2f):realm==1?new Color(.66f,.46f,.24f):new Color(.48f,.42f,.36f);
    int n=1+rng.Next(0,3);
    for(int i=0;i<n;i++){
     int side=rng.Next(0,2)==0?-1:1;
     float x=side*Mathf.Min(halfX,1.2f+(float)rng.NextDouble()*halfX);
     float z=((float)rng.NextDouble()-.5f)*Mathf.Max(.6f,length-1.2f);
     BreakableCrate.Place(island,new Vector3(x,.02f,z),wood,rng.NextDouble()<.4f);
    }
    if(width>8f&&rng.NextDouble()<.3f)PushableBlock.Place(island,new Vector3((rng.Next(0,2)==0?-1:1)*1.4f,.02f,((float)rng.NextDouble()-.5f)*2f),new Color(.45f,.42f,.4f));
   }
   static void PlaceOrnaments(Transform island,int realm,float width,float length,System.Random rng){
    float halfX=Mathf.Max(.6f,width*.5f-.7f);
    if(width>=14f){
     PlaceStatue(island,rng.Next(0,2)==0?"Elite":"Summoner",-3.2f,length*.5f-2.6f,0f,3.1f);
     PlaceStatue(island,rng.Next(0,2)==0?"Bomber":"Flyer",3.2f,length*.5f-2.6f,180f,3.1f);
    }
    if(rng.NextDouble()<.7){
     Color crystal=realm==0?new Color(.3f,1,.5f):realm==1?new Color(1,.6f,.2f):realm==2?new Color(.5f,.85f,1f):new Color(1f,.38f,.22f);
     int n=2+rng.Next(0,2);
     for(int i=0;i<n;i++){
      int side=rng.Next(0,2)==0?-1:1;
      PlaceCrystal(island,crystal,side*Mathf.Min(halfX,1.8f+(float)rng.NextDouble()*halfX),((float)rng.NextDouble()-.5f)*Mathf.Max(.6f,length-.9f),.5f+(float)rng.NextDouble()*.6f,rng);
     }
    }
    if(realm==1&&rng.NextDouble()<.35){
     PlaceBanner(island,-(halfX-.4f),-1.4f);PlaceBanner(island,halfX-.4f,1.4f);
    }
    if(realm==3&&rng.NextDouble()<.4){
     PlaceBanner(island,-(halfX-.4f),-1.4f);PlaceBanner(island,halfX-.4f,1.4f);
    }
    if(realm==2&&rng.NextDouble()<.4){
     int side=rng.Next(0,2)==0?-1:1;
     PlaceLantern(island,side*2f,((float)rng.NextDouble()-.5f)*2f);
     PlaceLantern(island,-side*2.4f,((float)rng.NextDouble()-.5f)*2f);
    }
    PlaceBreakables(island,realm,width,length,rng);
   }
   static void Fit(GameObject prop,float targetHeight){
   var renderers=prop.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)return;
   Bounds bounds=renderers[0].bounds;
   for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
   float height=bounds.size.y;if(height>1e-4f)prop.transform.localScale=Vector3.one*(prop.transform.localScale.x*targetHeight/height);
  }
 }
}
