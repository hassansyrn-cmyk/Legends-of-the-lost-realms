using UnityEngine;
using UnityEngine.Rendering;
namespace LostRealms {
 // Second trap generation (Sep 2026 difficulty pass): CrusherPillar, DartTurret,
 // RollingBoulder and WindVent. The first three use Blender-built low-poly
 // models under Resources/Props/Traps with named parts (Stone*/Trim*/Runes*/Mouth/
 // Rock*) tinted per realm at place time; every trap falls back to procedural
 // Art.Shape visuals when the model is missing, so nothing breaks in stripped
 // builds. All follow the TrapsAndHazards conventions: static Place(), phase
 // state machines, g.Player.Damage for the hero and foe.Hit for enemies.
 public static class TrapArt {
  // Trap spots reserve space so RealmProps never scatters a prop through one.
  public struct ReservedSpot{public Transform island;public Vector3 local;public float radius;}
  public static readonly System.Collections.Generic.List<ReservedSpot> Reserved=new System.Collections.Generic.List<ReservedSpot>();
  public static void Reserve(Transform island,Vector3 local,float radius){Reserved.Add(new ReservedSpot{island=island,local=local,radius=radius});}
  public static bool IsClear(Transform island,Vector3 local,float radius){
   for(int i=0;i<Reserved.Count;i++){
    var s=Reserved[i];
    if(!s.island||s.island!=island)continue;
    if(Vector3.Distance(s.local,local)<s.radius+radius)return false;
   }
   return true;
  }
  public static GameObject Load(string name,Transform parent,Color accent,Color stone){
   var prefab=Resources.Load<GameObject>("Props/Traps/"+name);
   if(!prefab)return null;
   var go=Object.Instantiate(prefab,parent);
   go.name=name;
   go.transform.localPosition=Vector3.zero;
   go.transform.localRotation=Quaternion.identity;
   foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
    string n=r.gameObject.name;
    Color c;
    if(n.StartsWith("Trim")||n.StartsWith("Runes"))c=Color.Lerp(accent,Color.white,.2f);
    else if(n.StartsWith("Mouth"))c=new Color(.05f,.04f,.05f);
    else if(n.StartsWith("Rock"))c=stone*.92f;
    else c=stone;
    var m=new Material(Shader.Find("Standard")){name="TrapMat "+n};
    m.color=c;m.SetFloat("_Glossiness",n.StartsWith("Trim")?.45f:.2f);
    if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",n.StartsWith("Trim")?.4f:.05f);
    if(n.StartsWith("Trim")||n.StartsWith("Runes")){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",accent*.35f);}
    r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
   }
   return go;
  }
  // Loader for the textured Tripo trap models: bind the exported texture
  // files (Props/Traps/Textures/<name>_0 = basecolor, _1 = normal) explicitly —
  // Unity does not reliably auto-bind FBX textures. The imported root rotation
  // (270,0,0 on GLB→FBX models) is preserved: it IS what stands them upright.
  public static GameObject LoadTextured(string name,Transform parent){
   var prefab=Resources.Load<GameObject>("Props/Traps/"+name);
   if(!prefab)return null;
   var go=Object.Instantiate(prefab,parent);
   go.name=name;
   go.transform.localPosition=Vector3.zero;
   var albedo=Resources.Load<Texture2D>("Props/Traps/Textures/"+name+"_0");
   var normal=Resources.Load<Texture2D>("Props/Traps/Textures/"+name+"_1");
   Material skin=null;
   if(albedo){
    skin=new Material(Shader.Find("Standard")){name="TrapTex "+name};
    skin.mainTexture=albedo;skin.color=Color.white;skin.SetFloat("_Glossiness",.3f);
    if(normal){skin.SetTexture("_BumpMap",normal);skin.SetFloat("_BumpScale",1f);skin.EnableKeyword("_NORMALMAP");}
   }
   foreach(var r in go.GetComponentsInChildren<Renderer>(true)){
    if(skin){r.sharedMaterial=skin;continue;}
    var m=r.sharedMaterial;
    if(m&&m.mainTexture)continue;
    var nm=new Material(Shader.Find("Standard")){name="TrapMat "+r.gameObject.name};
    nm.color=new Color(.44f,.41f,.39f);nm.SetFloat("_Glossiness",.2f);
    r.sharedMaterial=nm;
   }
   return go;
  }
 }

 // 1. CRUSHER PILLAR — ancient ruin crusher. Hovers, telegraphs with a ring and
 // dust, slams down (damage + knockdown), then grinds back up. Chapters 4+.
 public sealed class CrusherPillar:MonoBehaviour {
  Transform head,colObj;float timer;Vector3 rest;bool down;Color accent;
  enum Phase{Hover,Warning,Slammed,Rising}Phase phase=Phase.Hover;
  public static CrusherPillar Place(Transform parent,Vector3 localPos,Color accent,Color stone){
   var go=new GameObject("Crusher pillar");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.LoadTextured("Trap_Crusher",go.transform);
   Transform head;
   if(model){
    head=model.transform;head.localPosition=new Vector3(0,3.1f,0);
   }else{
    head=Art.Shape("Stone Head",PrimitiveType.Cube,new Vector3(0,3.1f,0),new Vector3(2.3f,1.4f,2.3f),stone,go.transform).transform;
    Art.Shape("Trim Band",PrimitiveType.Cylinder,new Vector3(0,3.1f-.8f,0),new Vector3(2.5f,.16f,2.5f),Color.Lerp(accent,Color.white,.2f),go.transform);
   }
   var c=go.AddComponent<CrusherPillar>();c.head=head;c.rest=head.localPosition;c.accent=accent;
   // Physical head collider: must be parented to root 'go' (scale 1,1,1) rather than
   // 'head' (whose FBX import scale is 100, which would blow the collider up to 240m!).
   var col=new GameObject("CrusherCollider");
   col.transform.SetParent(go.transform,false);
   col.transform.localPosition=head.localPosition;
   col.transform.localRotation=Quaternion.identity;
   col.transform.localScale=Vector3.one;
   var box=col.AddComponent<BoxCollider>();
   box.size=new Vector3(2.3f,2.2f,2.3f);box.center=new Vector3(0,1.1f,0);
   c.colObj=col.transform;
   TrapArt.Reserve(parent,localPos,2.2f);
   return c;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   switch(phase){
    case Phase.Hover:
     head.localPosition=rest+Vector3.up*Mathf.Sin(timer*2.1f)*.14f;
     if(colObj)colObj.localPosition=head.localPosition;
     if(timer>=2.3f){
      if(Vector3.Distance(transform.position,g.Player.transform.position)<=5.5f){
       phase=Phase.Warning;timer=0;
       g.TrapSound("trap_warning",transform.position,4f,16f,.8f);
       CombatTelegraph.Create(transform.position,1.7f,.65f,transform);
      }else{
       timer=1.2f;
      }
     }
     break;
    case Phase.Warning:
     head.localPosition=rest+new Vector3(Mathf.Sin(timer*60f)*.05f,0,Mathf.Cos(timer*53f)*.05f);
     if(colObj)colObj.localPosition=head.localPosition;
     if(timer>=.65f){phase=Phase.Slammed;timer=0;down=true;
      head.localPosition=new Vector3(rest.x,0f,rest.z);
      if(colObj)colObj.localPosition=head.localPosition;
      g.TrapSound("trap_crush",transform.position,5f,20f,1f);
      g.CameraRig.Shake=.32f;g.HitStop(.05f,.35f);
      KenneyPuff.Burst(transform.position+Vector3.up*.15f,new Color(.62f,.56f,.46f),14,1.3f);
      Vfx.Play("ga_vfx_Impact_01",transform.position+Vector3.up*.4f,Quaternion.identity,1.1f);
      Vector3 pd=g.Player.transform.position-transform.position;pd.y=0;
      if(pd.magnitude<1.55f&&g.Player.transform.position.y<transform.position.y+1.6f){
       Vector3 knockDir=pd.sqrMagnitude>.01f?pd.normalized:-transform.forward;
       if(g.Player.Damage(1,transform.position))g.Player.Bounce(6.0f,knockDir*4.5f);
      }
      if(g.Enemies!=null)foreach(var foe in g.Enemies){
       if(!foe||foe.Health<=0)continue;
       Vector3 fd=foe.transform.position-transform.position;fd.y=0;
       if(fd.magnitude<1.7f&&!foe.Boss)foe.Hit(4f,0,false,Vector3.down);
      }
     }
     break;
    case Phase.Slammed:
     if(colObj)colObj.localPosition=head.localPosition;
     if(timer>=.9f){phase=Phase.Rising;timer=0;}
     break;
    case Phase.Rising:
     head.localPosition=Vector3.MoveTowards(head.localPosition,rest,Time.deltaTime*2.6f);
     if(colObj)colObj.localPosition=head.localPosition;
     if(head.localPosition==rest){phase=Phase.Hover;timer=0;down=false;}
     break;
   }
  }
 }

 // 2. DART TURRET — ruined-temple sentry. Glows, tracks the player, then fires
 // a straight dart; strafing or dashing dodges it. Chapters 5+.
 public sealed class DartTurret:MonoBehaviour {
  public enum TurretKind { Stone, Dragon, Lion }
  TurretKind kind;
  public float PlacementRadius=>kind==TurretKind.Lion?1.6f:kind==TurretKind.Dragon?1.4f:1.3f;
  Transform aim;
  Renderer[] glowRenderers;
  float timer;
  Color accent;

  public static DartTurret Place(Transform parent,Vector3 localPos,Color accent,Color stone,TurretKind kind=TurretKind.Stone){
   var go=new GameObject("Dart turret");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   go.transform.localRotation=Quaternion.identity;

   // Visual pivot so aiming smoothly pivots the entire statue without detaching from island grid
   var headPivot=new GameObject("HeadPivot");
   headPivot.transform.SetParent(go.transform,false);
   headPivot.transform.localPosition=Vector3.zero;
   headPivot.transform.localRotation=Quaternion.identity;
   var aim=headPivot.transform;

   string modelName=kind==TurretKind.Dragon?"Trap_Turret_Dragon":(kind==TurretKind.Lion?"Trap_Turret_Lion":"Trap_Turret_Stone");
   var model=TrapArt.LoadTextured(modelName,headPivot.transform);
   if(!model&&kind==TurretKind.Stone)model=TrapArt.LoadTextured("Trap_Turret",headPivot.transform);

   Renderer[] glow=null;
   if(model){
    glow=model.GetComponentsInChildren<Renderer>(true);
   }else{
    Art.Shape("Stone Base",PrimitiveType.Cube,new Vector3(0,.3f,0),new Vector3(1.5f,.6f,1.5f),stone,go.transform);
    var face=Art.Shape("Stone Face",PrimitiveType.Cube,new Vector3(0,1.6f,0),new Vector3(.9f,.9f,.9f),stone,headPivot.transform);
    var m=Art.Shape("Mouth",PrimitiveType.Cube,new Vector3(0,1.55f,.42f),new Vector3(.26f,.26f,.2f),new Color(.05f,.04f,.05f),headPivot.transform);
    glow=new[]{face.GetComponent<Renderer>(),m.GetComponent<Renderer>()};
   }

   var t=go.AddComponent<DartTurret>();t.kind=kind;t.aim=aim;t.glowRenderers=glow;t.accent=accent;
   t.timer=UnityEngine.Random.value*0.8f;

   // Pedestal collider sized to the visible statue bounds
   var col=go.AddComponent<CapsuleCollider>();
   if(kind==TurretKind.Lion){
    col.radius=.85f;col.height=2.0f;col.center=new Vector3(0,1.0f,0);
    TrapArt.Reserve(parent,localPos,1.6f);
   }else if(kind==TurretKind.Dragon){
    col.radius=.70f;col.height=2.2f;col.center=new Vector3(0,1.1f,0);
    TrapArt.Reserve(parent,localPos,1.4f);
   }else{
    col.radius=.65f;col.height=2.2f;col.center=new Vector3(0,1.1f,0);
    TrapArt.Reserve(parent,localPos,1.3f);
   }
   Debug.Log($"[DartTurret] Spawned {kind} Turret on {parent.name} at world pos {go.transform.position.ToString("F1")}");
   return t;
  }

  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   float dist=Vector3.Distance(transform.position,g.Player.transform.position);
   if(dist>18f||(g.World&&Vector3.Distance(g.Player.transform.position,g.World.Spawn)<=3.2f))return;

   timer+=Time.deltaTime;

   // Telegraph window (1.6s to 2.2s): glow warning + tracking
   if(timer>1.6f&&timer<2.2f){
    float pulse=Mathf.PingPong((timer-1.6f)*6f,1f);
    Color emColor=accent*(0.8f+pulse*1.4f);
    if(glowRenderers!=null){
     foreach(var r in glowRenderers){
      if(r&&r.material&&r.material.HasProperty("_EmissionColor")){
       r.material.EnableKeyword("_EMISSION");
       r.material.SetColor("_EmissionColor",emColor);
      }
     }
    }
    if(aim){
     Vector3 to=g.Player.transform.position+Vector3.up*.9f-transform.position;
     Vector3 localDir=transform.InverseTransformDirection(to);localDir.y=0;
     if(localDir.sqrMagnitude>.01f)aim.localRotation=Quaternion.Slerp(aim.localRotation,Quaternion.LookRotation(localDir.normalized,Vector3.up),Time.deltaTime*5f);
    }
   }else if(timer<1.6f){
    if(glowRenderers!=null&&timer<.25f){
     foreach(var r in glowRenderers){
      if(r&&r.material&&r.material.HasProperty("_EmissionColor")){
       r.material.SetColor("_EmissionColor",accent*.1f);
      }
     }
    }
    if(aim){
     Vector3 to=g.Player.transform.position+Vector3.up*.9f-transform.position;
     Vector3 localDir=transform.InverseTransformDirection(to);localDir.y=0;
     if(localDir.sqrMagnitude>.01f)aim.localRotation=Quaternion.Slerp(aim.localRotation,Quaternion.LookRotation(localDir.normalized,Vector3.up),Time.deltaTime*2.5f);
    }
   }

   if(timer>=2.2f){
    timer=0;
    Vector3 forward=aim?aim.forward:transform.forward;
    Vector3 origin;
    if(kind==TurretKind.Dragon)origin=transform.position+forward*.65f+Vector3.up*1.45f;
    else if(kind==TurretKind.Lion)origin=transform.position+forward*.80f+Vector3.up*1.30f;
    else origin=transform.position+forward*.45f+Vector3.up*1.55f;

    Vector3 target=g.Player.transform.position+Vector3.up*.85f;
    Vector3 aimDir=(target-origin).normalized;

    if(kind==TurretKind.Lion){
     // Steampunk Lion Turret: shoots 3 arrows together in a fan volley
     Fire(origin,Quaternion.AngleAxis(-9f,Vector3.up)*aimDir,12.5f);
     Fire(origin,aimDir,13f);
     Fire(origin,Quaternion.AngleAxis(9f,Vector3.up)*aimDir,12.5f);
     g.TrapSound("trap_dart",transform.position,4f,18f,.9f);
     KenneyPuff.Burst(origin,accent,6,.7f);
    }else if(kind==TurretKind.Dragon){
     Fire(origin,aimDir,14.5f);
     g.TrapSound("trap_dart",transform.position,4f,18f,.85f);
     KenneyPuff.Burst(origin,new Color(1f,.45f,.2f),5,.65f);
     Vfx.Play("ga_vfx_Impact_01",origin,Quaternion.identity,.8f);
    }else{
     Fire(origin,aimDir,13f);
     g.TrapSound("trap_dart",transform.position,3f,16f,.75f);
     KenneyPuff.Burst(origin,accent,4,.5f);
    }
   }
  }

  void Fire(Vector3 origin,Vector3 dir,float speed=13f){
   var g=RealmGame.I;
   var arrowRoot=new GameObject("Trap Arrow");
   arrowRoot.transform.SetParent(g!=null&&g.World!=null?g.World.transform:null,true);
   arrowRoot.transform.position=origin;
   arrowRoot.transform.rotation=Quaternion.LookRotation(dir,Vector3.up);

   var model=TrapArt.LoadTextured("Trap_Arrow",arrowRoot.transform);
   if(!model){
    var dart=Art.Shape("Dart",PrimitiveType.Cube,Vector3.zero,new Vector3(.09f,.09f,.65f),accent,arrowRoot.transform);
    var r=dart.GetComponent<Renderer>();var m=new Material(Shader.Find("Standard")){name="Dart"};
    m.color=accent;m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",accent*1.4f);
    r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;
   }else{
    foreach(var r in arrowRoot.GetComponentsInChildren<Renderer>(true)){
     r.shadowCastingMode=ShadowCastingMode.Off;
     r.receiveShadows=false;
    }
   }
   float flightTime=g&&g.Player?Vector3.Distance(origin,g.Player.transform.position+Vector3.up*.85f)/speed:0f;
   darts.Add(new Dart{go=arrowRoot.transform,dir=dir*speed+Vector3.up*(.5f*ArrowGravity*flightTime),age=0});
  }

  const float ArrowGravity=2.5f;
  class Dart{public Transform go;public Vector3 dir;public float age;}
  readonly System.Collections.Generic.List<Dart> darts=new System.Collections.Generic.List<Dart>();
  void OnDestroy(){foreach(var dart in darts)if(dart!=null&&dart.go)Destroy(dart.go.gameObject);darts.Clear();}
  void LateUpdate(){
   var g=RealmGame.I;
   if(!g||g.Screen!=GameScreen.Playing)return;
   for(int i=darts.Count-1;i>=0;i--){
    var d=darts[i];bool dead=d==null||!d.go||d.age>2.2f;
    if(!dead){
     d.age+=Time.deltaTime;
     float dt=Time.deltaTime;
     Vector3 previous=d.go.position,next=previous+d.dir*dt+Vector3.down*(.5f*ArrowGravity*dt*dt);
     d.dir+=Vector3.down*(ArrowGravity*dt);
     d.go.rotation=Quaternion.LookRotation(d.dir,Vector3.up);
     Vector3 travel=next-previous;
     bool wall=Physics.Raycast(previous,travel.normalized,out RaycastHit obstacle,travel.magnitude,~0,QueryTriggerInteraction.Ignore);
     if(wall)next=obstacle.point;
     d.go.position=next;

     if(d.age>0.05f&&UnityEngine.Random.value<0.25f){
      KenneyPuff.Burst(next-d.dir.normalized*0.4f,new Color(.85f,.8f,.7f,.4f),1,.25f);
     }

     if(g&&g.Screen==GameScreen.Playing&&g.Player){
      bool bodyHit=wall&&obstacle.collider.GetComponentInParent<Hero>()==g.Player;
      if(bodyHit||ProjectileSweep.Hits(previous,next,g.Player.transform.position+Vector3.up*.9f,.55f,out _)){
       g.Player.Damage(1,transform.position);
       dead=true;
       Vfx.Play("ga_vfx_Impact_01",next,Quaternion.identity,.7f);
       KenneyPuff.Burst(next,accent,5,.5f);
      }else if(g.Enemies!=null)foreach(var foe in g.Enemies){
       if(!foe||foe.Health<=0||foe.transform.IsChildOf(transform))continue;
       if(!foe.Boss&&Vector3.Distance(d.go.position,foe.transform.position+Vector3.up*.9f)<.75f){
        foe.Hit(2f,0,false,d.dir);
        dead=true;
        Vfx.Play("ga_vfx_Impact_01",next,Quaternion.identity,.7f);
        KenneyPuff.Burst(next,accent,4,.5f);
        break;
       }
      }
     }
     if(wall){
      dead=true;
      KenneyPuff.Burst(next,new Color(.6f,.55f,.5f),4,.4f);
     }else if(d.go.position.y<transform.position.y-4f){
      dead=true;
     }
    }
    if(dead){if(d!=null&&d.go)Destroy(d.go.gameObject);darts.RemoveAt(i);}
   }
  }
 }

 // 3. ROLLING BOULDER — a rumbling boulder careens down the lane when the player
 // closes in. Dodge sideways or dash past it. Chapters 6+.
  public sealed class RollingBoulder:MonoBehaviour {
  Transform rock;Vector3 start,dir;float timer,traveled,maxTravel;bool rolling;Color accent;
  const float Speed=7.2f,Range=10f,Radius=.95f;
  public static RollingBoulder Place(Transform parent,Vector3 localPos,Vector3 rollDir,Color accent,Color stone,float extent=8.3f){
   if(!TrapArt.IsClear(parent,localPos,1.8f))return null;
   var go=new GameObject("Rolling boulder");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   var model=TrapArt.Load("Trap_Boulder",go.transform,accent,stone);
   Transform rock;
   if(model){rock=model.transform;rock.localPosition=Vector3.up*Radius;}
   else rock=Art.Shape("Rock",PrimitiveType.Sphere,Vector3.up*Radius,Vector3.one*Radius*2f,stone*.92f,go.transform).transform;
   var b=go.AddComponent<RollingBoulder>();b.rock=rock;b.start=rock.localPosition;b.dir=rollDir.sqrMagnitude>.01f?rollDir.normalized:new Vector3(0,0,-1);b.accent=accent;
   // Roll stops just past the island's far edge: start distance along the roll
   // axis plus the half-extent and a little overhang.
   b.maxTravel=-Vector3.Dot(localPos,b.dir)+extent*.5f+1f;
   // The boulder is a solid rolling body: it shoves and blocks for real.
   var ball=rock.gameObject.AddComponent<SphereCollider>();
   ball.radius=1f;ball.center=Vector3.zero;
   TrapArt.Reserve(parent,localPos,1.4f);
   TrapArt.Reserve(parent,localPos+b.dir*(b.maxTravel*.5f),1.6f);
   return b;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   if(!rolling){
    if(rock.localPosition!=start)rock.localPosition=Vector3.MoveTowards(rock.localPosition,start,Time.deltaTime*4f);
    if(timer>=5f){
     Vector3 to=g.Player.transform.position-transform.position;float height=Mathf.Abs(to.y);to.y=0;
     float along=Vector3.Dot(to,dir);
     if(along>.5f&&along<Range&&Mathf.Abs(Vector3.Dot(to,Vector3.Cross(dir,Vector3.up)))<3.6f){
      rolling=true;traveled=0;timer=0;
      g.TrapSound("trap_boulder",transform.position,4f,18f,.7f);
      KenneyPuff.Burst(transform.position+Vector3.up*.3f,new Color(.62f,.56f,.46f),10,1.1f);
     }
    }
   }else{
    float step=Speed*Time.deltaTime;
    rock.localPosition+=dir*step;traveled+=step;
    rock.Rotate(Vector3.Cross(Vector3.up,dir),Speed*Time.deltaTime*Mathf.Rad2Deg/Radius*.5f,Space.Self);
    if(Random.value<.35f)KenneyPuff.Burst(rock.position+Vector3.down*(Radius-.2f),new Color(.62f,.56f,.46f),1,.7f);
    if(Vector3.Distance(rock.position,g.Player.transform.position+Vector3.up*.8f)<Radius+.55f){
     if(g.Player.Damage(1,rock.position))g.Player.Bounce(6.5f);
    }
    if(g.Enemies!=null)foreach(var foe in g.Enemies){
     if(!foe||foe.Health<=0)continue;
     if(!foe.Boss&&Vector3.Distance(rock.position,foe.transform.position+Vector3.up*.9f)<Radius+.5f)foe.Hit(5f,0,false,dir);
    }
    if(traveled>maxTravel){rolling=false;timer=0;rock.localPosition=start;KenneyPuff.Burst(rock.position+Vector3.down*(Radius-.2f),new Color(.62f,.56f,.46f),8,1f);}
   }
  }
 }

 // 4. WIND VENT — gale-realm vent. Harmless itself, but its gust shoves anyone
 // standing in the lane sideways: deadly next to a bridge edge. Chapters 3+.
 public sealed class WindVent:MonoBehaviour {
  Vector3 gust;float timer;Transform rune;
  public static WindVent Place(Transform parent,Vector3 localPos,Vector3 pushDir,Color accent){
   var go=new GameObject("Wind vent");go.transform.SetParent(parent,false);go.transform.localPosition=localPos;
   Art.Shape("VentGrate",PrimitiveType.Cylinder,new Vector3(0,.05f,0),new Vector3(1.5f,.1f,1.5f),new Color(.2f,.22f,.24f),go.transform,true);
   var runeT=Art.Shape("VentRune",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(.7f,.06f,.7f),accent,go.transform).transform;
   var v=go.AddComponent<WindVent>();v.rune=runeT;v.gust=pushDir.sqrMagnitude>.01f?pushDir.normalized:Vector3.right;v.timer=Random.value*2f;
   TrapArt.Reserve(parent,localPos,1.6f);
   return v;
  }
  void Update(){
   var g=RealmGame.I;if(!g||g.Screen!=GameScreen.Playing||!g.Player)return;
   timer+=Time.deltaTime;
   bool blowing=timer%3.4f>1.2f&&timer%3.4f<2.4f;
   if(rune)rune.GetComponent<Renderer>().material.color=blowing?Color.Lerp(gustColor,Color.white,.3f):gustColor;
   if(blowing){
    if(timer%3.4f<1.28f)g.TrapSound("trap_wind",transform.position,3f,14f,.6f);
    Vector3 to=g.Player.transform.position-transform.position;float height=Mathf.Abs(to.y);to.y=0;
    if(to.magnitude<2.6f&&height<2.2f)g.Player.ApplyForce(gust*10f*Time.deltaTime);
    if(Random.value<.5f)KenneyPuff.Burst(transform.position+Vector3.up*.2f+gust*Random.Range(0f,1.6f),new Color(.85f,.95f,1f,.5f),1,.5f);
   }
  }
  Color gustColor=>RealmGame.I?RealmGame.I.Accent:new Color(.5f,.8f,1f);
 }
}
