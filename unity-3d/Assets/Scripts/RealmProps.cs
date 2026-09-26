using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace LostRealms {
 // Scatters imported low-poly nature props (SimpleNaturePack) around each island
 // so the realms use real models instead of primitives. Models live in
 // Resources/Props/Nature; one shared palette texture feeds a single material.
 public static class RealmProps {
  // World-space spawn point of the current chapter: props keep a 2.6 m clear
  // radius around it so Aster can never be wedged in at level start.
  public static Vector3 SpawnGuard;
   static readonly Material[] byRealm=new Material[4];
   static readonly Dictionary<string,GameObject> cache=new Dictionary<string,GameObject>();
   static Material stoneMat;
   static readonly Dictionary<string,Material> glowMats=new Dictionary<string,Material>();

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
   if(realm==1&&(name.StartsWith("House_")||name.StartsWith("Ruin_")||name=="Tower_01"||name=="Gate_01"||name=="Tent_01"||name=="Wall_01"))return "Desert";
   return "Nature";
  }
   static GameObject Model(string folder,string name){
   string key=folder+"/"+name;
   if(cache.TryGetValue(key,out var cached))return cached;
   var model=Resources.Load<GameObject>("Props/"+folder+"/"+name);
   if(!model&&folder=="Snow")model=Resources.Load<GameObject>("Props/Nature/"+name);
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
if(realm==1)return new[]{new[]{"House_01","House_02","Tower_01","Ruin_01","Gate_01","Tent_01"},new[]{"Rock_03","Rock_04","Rock_05"},new[]{"Grass_02","Mushroom_01","Bush_02","rpgpp_lt_rock_small_01","rpgpp_lt_rock_small_02"}};
    if(realm==3)return new[]{new[]{"Tree_01","Tree_02","Tree_03","DeadTree_01"},new[]{"Rock_03","Rock_04","Rock_05"},new[]{"Grass_02","Flowers_01","Bush_02","rpgpp_lt_rock_small_01","rpgpp_lt_rock_small_02"}};
    return new[]{new[]{"Pine_01","Pine_02","Pine_03","Tree_01","Tree_02","DeadTree_01","rpgpp_lt_tree_pine_01"},new[]{"Rock_01","Rock_02","Rock_03","Rock_04","Rock_05","Snowman_01","Well_01","Stump_01","rpgpp_lt_rock_01","rpgpp_lt_rock_02"},new[]{"Sign_01","Stump_01","Bush_01","Bush_02","Mushroom_01","rpgpp_lt_rock_small_01","rpgpp_lt_rock_small_02","rpgpp_lt_bucket_01"}};
  }
  public static void Scatter(Transform world,int realm,System.Random rng){
   var material=ForRealm(realm);if(!material)return;
   var sets=Sets(realm);   foreach(Transform island in world){
    if(island.name!="Island")continue;
    var surface=island.Find("Realm surface");if(!surface)continue;
    float width=surface.localScale.x-.08f,length=surface.localScale.z-.08f;
    if(width<=7.5f)continue;
if(realm==1){if(!island.Find("BossArenaMarker"))PlaceDesertBuildings(island,width,length,rng,material);}
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
    if(width>9.5f&&rng.Next(0,100)<50)Place(island,realm,VillageStructures[rng.Next(VillageStructures.Length)],width,length,rng,1.8f,2.2f,null);
   int medium=1+rng.Next(0,2);
   for(int i=0;i<medium;i++)Place(island,realm,VillageMedium[rng.Next(VillageMedium.Length)],width,length,rng,.65f,1.0f,null);
   int tiny=1+rng.Next(0,3);
   for(int i=0;i<tiny;i++)Place(island,realm,VillageSmall[rng.Next(VillageSmall.Length)],width,length,rng,.3f,.5f,null);
  }
   static bool TrySurfaceY(Transform island,float x,float z,out float localY){
    localY=.02f;
    var hits=Physics.RaycastAll(island.TransformPoint(new Vector3(x,8f,z)),Vector3.down,16f,~0,QueryTriggerInteraction.Ignore);
    float bestY=float.NegativeInfinity;
    bool found=false;
    for(int i=0;i<hits.Length;i++){
     var h=hits[i];
     if(!h.transform.IsChildOf(island))continue;
     string n=h.transform.name;
     if(n.StartsWith("Prop ") || n.StartsWith("Breakable") || n.StartsWith("Pushable"))continue;
     if(h.normal.y<.55f)continue;
     float ly=island.InverseTransformPoint(h.point).y;
     if(ly<-2.5f)continue;
     if(ly>bestY){bestY=ly;found=true;}
    }
    if(found){localY=bestY;return true;}
    return false;
   }
    static float SurfaceY(Transform island,float x,float z){
     if(TrySurfaceY(island,x,z,out float ly))return ly;
     return .02f;
    }
    // Strict deck query shared by the settle pass and the organization sweep
    // (so both converge): island-owned scenery/deck only — never props,
    // traps, enemies or pickups, and never the tested prop itself.
    public static bool TryDeckSurface(Transform island,float x,float z,Transform ignoreRoot,out float localY){
     localY=.02f;
     var hits=Physics.RaycastAll(island.TransformPoint(new Vector3(x,8f,z)),Vector3.down,20f,~0,QueryTriggerInteraction.Ignore);
     float bestY=float.NegativeInfinity;bool found=false;
     foreach(var h in hits){
      if(!IsDeckHit(h,island,ignoreRoot))continue;
      if(h.normal.y<.55f)continue;
      float ly=island.InverseTransformPoint(h.point).y;
      if(ly<-2.5f)continue;
      if(ly>bestY){bestY=ly;found=true;}
     }
     if(found){localY=bestY;return true;}
     return false;
    }
    static bool IsDeckHit(RaycastHit h,Transform island,Transform ignoreRoot){
     var renderer=h.collider.GetComponent<Renderer>();
     if(!(h.collider is MeshCollider)&&renderer&&!renderer.enabled)return false;
     var t=h.transform;
     if(!t||t==ignoreRoot||t.IsChildOf(ignoreRoot))return false;
     while(t!=null&&t!=island){
      var n=t.name;
      if(n.StartsWith("Prop ")||n.StartsWith("Breakable")||n.StartsWith("Pushable"))return false;
      if(t.GetComponent<Enemy>()||t.GetComponent<RealmHeal>()||t.GetComponent<WeaponDrop>())return false;
      if(t.GetComponent<SawTrap>()||t.GetComponent<FloorBladeTrap>()||t.GetComponent<SpikeTrap>()||t.GetComponent<PendulumTrap>()||t.GetComponent<FireGeyser>()||t.GetComponent<CrusherPillar>()||t.GetComponent<DartTurret>()||t.GetComponent<RollingBoulder>()||t.GetComponent<WindVent>()||t.GetComponent<FlameBrazier>()||t.GetComponent<FrostTotem>()||t.GetComponent<SerpentStatue>())return false;
      t=t.parent;
     }
     return t==island;
    }
    // Post-scatter settle: snap clear outliers straight onto the deck, either
    // direction (floaters down, half-buried props up). Capped and deck-only —
    // scenery under terraces/overhangs is never teleported, planted bases and
    // deep pits are untouched.
    public static void SettleProps(Transform world){
     foreach(Transform island in world){
      if(island.name!="Island")continue;
      foreach(Transform c in island){
       if(!c.name.StartsWith("Prop ")&&!c.name.StartsWith("Breakable")&&!c.name.StartsWith("Pushable"))continue;
       var r=c.GetComponentInChildren<Renderer>();if(!r)continue;
       Bounds b=r.bounds;foreach(var r2 in c.GetComponentsInChildren<Renderer>())b.Encapsulate(r2.bounds);
       Vector3 bc=island.InverseTransformPoint(b.center);
       if(!TryDeckSurface(island,bc.x,bc.z,c,out float ly))continue;
       float surfY=island.TransformPoint(new Vector3(bc.x,ly,bc.z)).y;
       float gap=b.min.y-surfY;
       if(Mathf.Abs(gap)>.35f&&Mathf.Abs(gap)<1.6f)c.position+=Vector3.down*gap;
      }
     }
    }
    static void Place(Transform island,int realm,string name,float width,float length,System.Random rng,float hMin,float hMax,Material material){
     var source=Model(Folder(realm,name),name);if(!source)return;
     material=MaterialFor(name,realm);if(!material)return;
     // Probe with the real footprint (at max fitted height, so no rng is
     // consumed out of order): big models like wagons extend well past a
     // fixed radius, and a post-place Destroy would linger in batch runs.
     float probeR=FootprintRadius(source,hMax);
     float halfWidth=Mathf.Max(.6f,width*.5f-1.2f);
     int side=rng.Next(0,2)==0?-1:1;
     float x=side*Mathf.Min(halfWidth,1.4f+(float)rng.NextDouble()*halfWidth);
     float z=((float)rng.NextDouble()-.5f)*Mathf.Max(.6f,length-1.8f);
     // Keep props off trap spots (reserved circles) and away from the chapter
     // spawn point — a rock beside Aster's landing can wedge the capsule.
     if(!TrapArt.IsClear(island,new Vector3(x,.05f,z),probeR))return;
     if(SpawnGuard!=Vector3.zero){
      var surfaceY=island.parent?island.position:Vector3.zero;
      if(Vector3.Distance(island.TransformPoint(new Vector3(x,.05f,z)),SpawnGuard)<2.6f)return;
     }
     float localY=.02f;
     bool valid=false;
     for(int attempt=0;attempt<6;attempt++){
      if(TrySurfaceY(island,x,z,out localY)){valid=true;break;}
      x*=.75f;z*=.75f;
     }
     if(!valid)return;
     var prop=Object.Instantiate(source,island);
     prop.name="Prop "+name;
     prop.transform.localPosition=new Vector3(x,localY,z);
     // Apply rotation FIRST so the prop's bounding box is measured in its final orientation
     var baseRot=source.transform.localRotation;
     float yaw=(float)rng.NextDouble()*360f;
     prop.transform.localRotation=Quaternion.Euler(0,yaw,0)*baseRot;
     Fit(prop,hMin+(float)rng.NextDouble()*(hMax-hMin));
     // Ground prop accurately on its oriented, scaled bounds
     var groundRenderers=prop.GetComponentsInChildren<Renderer>(true);
     if(groundRenderers.Length>0){
      Bounds gb=groundRenderers[0].bounds;
      for(int i=1;i<groundRenderers.Length;i++)gb.Encapsulate(groundRenderers[i].bounds);
      float surfaceY=island.TransformPoint(new Vector3(x,localY,z)).y;
      prop.transform.position+=Vector3.up*(surfaceY-gb.min.y);
     }
     AddCollider(prop);
    foreach(var renderer in prop.GetComponentsInChildren<Renderer>(true)){
     renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
    }
   }
   // Real footprint radius of a source model at a fitted height (square,
   // yaw-agnostic so it stays valid after the random spin).
   static float FootprintRadius(GameObject source,float fitH){
    var rs=source.GetComponentsInChildren<Renderer>(true);
    if(rs.Length==0)return .9f;
    Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);
    if(b.size.y<1e-4f)return .9f;
    float s=fitH/b.size.y;
    return Mathf.Max(.35f,Mathf.Max(b.size.x,b.size.z)*.5f*s);
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
      foreach(var n in new[]{"Gate_01","House_01","House_02","Ruin_01","Tent_01","Tower_01","Wall_01","Cottage_01","Cottage_02","Bridge_01","rpgpp_lt_shed_wood_01","rpgpp_lt_shed_wood_02","rpgpp_lt_wagon_01"})if(propName.EndsWith(" "+n))return true;
     return false;
    }
    // Desert realm: buildings go to organized, path-safe slots — flanking the
    // travel lane at fixed offsets with their front face toward the path, so the
    // way stays clear instead of the old random edge/mid-lane scatter. The Gate_01
    // (whose only reliable portal is measured in Validation/door-facing.txt) is
    // optionally built across the lane as a pass-through arch (yaw 90 puts its
    // opening along the travel axis); its exact mesh collision keeps walls solid.
    static void PlaceDesertBuildings(Transform island,float width,float length,System.Random rng,Material material){
      string[] flank={"House_01","House_02","Tower_01","Ruin_01","Tent_01"};
     int n=width>=11f?3:2;
     float halfX=Mathf.Min(Mathf.Max(width*.5f-1.05f,1f),4.2f);
     float zf=Mathf.Max(.9f,length*.22f);
     bool gateway=rng.NextDouble()<0.35f;
     if(gateway)PlaceBuilding(island,"Gate_01",halfX,0,0f,rng,material,gateway:true);
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
      float x=gateway?0f:side*xMag;
      // Buildings honour trap reservations too — a tent on the serpent statue
      // was possible because this path bypassed Place(). Probe with the real
      // footprint (houses are metres wide).
      if(!TrapArt.IsClear(island,new Vector3(x,.08f,z),FootprintRadius(source,3.5f)))return;
     float localY=.02f;
     bool valid=false;
     for(int attempt=0;attempt<6;attempt++){
      if(TrySurfaceY(island,x,z,out localY)){valid=true;break;}
      x*=.8f;z*=.8f;
     }
     if(!valid)return;
     var prop=Object.Instantiate(source,island);
     prop.name="Prop "+name;
     prop.transform.localPosition=new Vector3(x,localY,z);
     var baseRot=source.transform.localRotation;
     // Gateway: gate across the lane (yaw 90 aims its portal down the route); flank
     // buildings face the path with the same side so the layout reads as rows.
     float yaw=gateway?90f:(side==-1?0f:180f);
     prop.transform.localRotation=Quaternion.AngleAxis(yaw,Vector3.up)*baseRot;
     Fit(prop,2.6f+(float)rng.NextDouble()*.9f);
     var groundRenderers=prop.GetComponentsInChildren<Renderer>(true);
     if(groundRenderers.Length>0){
      Bounds gb=groundRenderers[0].bounds;
      for(int i=1;i<groundRenderers.Length;i++)gb.Encapsulate(groundRenderers[i].bounds);
      float surfaceY=island.TransformPoint(new Vector3(x,localY,z)).y;
      prop.transform.position+=Vector3.up*(surfaceY-gb.min.y);
     }
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
    // Petrified guardians: fallen Flyer/Bomber/Summoner/Elite meshes in stone,
   // flanking gate arches and the boss arena. Named "Prop Statue *" so the
   // shared AddCollider gives them a plain box.
   static void PlaceStatue(Transform island,string name,float x,float z,float yaw,float height){
    var source=CharModel(name);if(!source)return;
    float localY=.02f;
    bool valid=false;
    for(int attempt=0;attempt<6;attempt++){
     if(TrySurfaceY(island,x,z,out localY)){valid=true;break;}
     x*=.8f;z*=.8f;
    }
    if(!valid)return;
    if(!TrapArt.IsClear(island,new Vector3(x,localY,z),1.0f))return;
    var prop=Object.Instantiate(source,island);
    prop.name="Prop Statue "+name;
    prop.transform.localPosition=new Vector3(x,localY,z);
    prop.transform.localRotation=Quaternion.AngleAxis(yaw,Vector3.up)*source.transform.localRotation;
    Fit(prop,height);
    var groundRenderers=prop.GetComponentsInChildren<Renderer>(true);
    if(groundRenderers.Length>0){
      Bounds gb=groundRenderers[0].bounds;
      for(int i=1;i<groundRenderers.Length;i++)gb.Encapsulate(groundRenderers[i].bounds);
       float surfaceY=island.TransformPoint(new Vector3(x,localY,z)).y;
        prop.transform.position+=Vector3.up*(surfaceY-gb.min.y);
       }
       AddCollider(prop);
     foreach(var renderer in prop.GetComponentsInChildren<Renderer>(true)){renderer.sharedMaterial=Stone();renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;}
    }
    static void PlaceBanner(Transform island,float x,float z){
    float localY=.02f;
    bool valid=false;
    for(int attempt=0;attempt<6;attempt++){
     if(TrySurfaceY(island,x,z,out localY)){valid=true;break;}
     x*=.8f;z*=.8f;
    }
    if(!valid)return;
    if(!TrapArt.IsClear(island,new Vector3(x,localY,z),.8f))return;
    var go=new GameObject("Prop Banner");go.transform.SetParent(island,false);
    go.transform.localPosition=new Vector3(x,localY,z);
    Art.Shape("Banner pole",PrimitiveType.Cylinder,Vector3.up*1.1f,new Vector3(.09f,2.2f,.09f),new Color(.3f,.2f,.14f),go.transform);
    var cloth=Art.Shape("Banner cloth",PrimitiveType.Cube,new Vector3(.3f,1.75f,0),new Vector3(.55f,.7f,.05f),new Color(.75f,.16f,.14f),go.transform);
    cloth.transform.localRotation=Quaternion.Euler(0,0,8);
    foreach(var r in go.GetComponentsInChildren<Renderer>()){r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;}
    AddCollider(go);
   }
   static void PlaceLantern(Transform island,float x,float z){
    float localY=.02f;
    bool valid=false;
    for(int attempt=0;attempt<6;attempt++){
     if(TrySurfaceY(island,x,z,out localY)){valid=true;break;}
     x*=.8f;z*=.8f;
    }
    if(!valid)return;
    if(!TrapArt.IsClear(island,new Vector3(x,localY,z),.8f))return;
    var go=new GameObject("Prop Lantern");go.transform.SetParent(island,false);
    go.transform.localPosition=new Vector3(x,localY,z);
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
     float localY=.02f;
     bool valid=false;
     for(int attempt=0;attempt<6;attempt++){
      if(TrySurfaceY(island,x,z,out localY)){valid=true;break;}
      x*=.8f;z*=.8f;
     }
     if(!valid)continue;
      if(!TrapArt.IsClear(island,new Vector3(x,localY,z),.9f))continue;
      BreakableCrate.Place(island,new Vector3(x,localY,z),wood,rng.NextDouble()<.4f);
    }
    if(width>8f&&rng.NextDouble()<.3f){
     float bx=(rng.Next(0,2)==0?-1:1)*1.4f;
     float bz=((float)rng.NextDouble()-.5f)*2f;
     float localY=.02f;
     if(TrySurfaceY(island,bx,bz,out localY)){
      if(!TrapArt.IsClear(island,new Vector3(bx,localY,bz),1f))return;
      PushableBlock.Place(island,new Vector3(bx,localY,bz),new Color(.45f,.42f,.4f));
     }
    }
   }
    static void PlaceOrnaments(Transform island,int realm,float width,float length,System.Random rng){
     float halfX=Mathf.Max(.6f,width*.5f-1.2f);
    if(realm==1&&rng.NextDouble()<.35){
     PlaceBanner(island,-(halfX-.4f),-1.4f);PlaceBanner(island,halfX-.4f,1.4f);
    }
    if(realm==3&&rng.NextDouble()<.4){
     PlaceBanner(island,-(halfX-.4f),-1.4f);PlaceBanner(island,halfX-.4f,1.4f);
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
